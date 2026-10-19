using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Stanley.Core.Naming;
using Stanley.Data;
using Stanley.Data.Entities;

namespace Stanley.Importer;

/// <summary>
/// Imports v1's <c>fighters.json</c> into the database (FR-21).
/// </summary>
/// <remarks>
/// <para>
/// The import is idempotent: fighters are matched on <see cref="Variant.Slug"/> and updated in
/// place, so running it twice changes nothing. That is why it matches on the slug and not on a
/// position in the file.
/// </para>
/// <para>
/// It is a developer action with no editor behind it, so nothing is written to
/// <c>change_records</c> (database design section 5.3).
/// </para>
/// </remarks>
internal sealed partial class FighterImporter(StanleyDbContext db)
{
    /// <summary>Matches the custom-emoji prefix of a legacy character field.</summary>
    [GeneratedRegex(@"<a?:\w+:\d+>", RegexOptions.CultureInvariant)]
    private static partial Regex EmojiPattern();

    public async Task<ImportSummary> ImportAsync(string path, CancellationToken cancellationToken)
    {
        Dictionary<string, LegacyFighter> fighters = await ReadFileAsync(path, cancellationToken);

        Dictionary<string, Tier> tiers = await db.Tiers
            .ToDictionaryAsync(t => t.Name.ToLowerInvariant(), cancellationToken);
        Dictionary<string, Element> elements = await db.Elements
            .ToDictionaryAsync(e => e.Name.ToLowerInvariant(), cancellationToken);
        Dictionary<string, Character> characters = await db.Characters
            .ToDictionaryAsync(c => c.Name, cancellationToken);
        Dictionary<string, Variant> variants = await db.Variants
            .Include(v => v.SignatureAbilities)
            .ToDictionaryAsync(v => v.Slug, cancellationToken);

        var summary = new ImportSummary();

        foreach (LegacyFighter fighter in fighters.Values)
        {
            string slug = NameNormalizer.Normalize(fighter.Name);
            if (slug.Length == 0)
            {
                throw new InvalidOperationException(
                    $"'{fighter.Name}' normalizes to nothing and could never be looked up.");
            }

            Character character = ResolveCharacter(characters, fighter, summary);
            Tier tier = Resolve(tiers, fighter.Rarity, "tier");
            Element element = Resolve(elements, fighter.Element, "element");
            IReadOnlyList<string> abilities = fighter.SignatureAbilities();

            if (!variants.TryGetValue(slug, out Variant? variant))
            {
                variant = new Variant
                {
                    Name = fighter.Name,
                    Slug = slug,
                    SaName = fighter.Ability,
                    Character = character,
                    Tier = tier,
                    Element = element,
                    BaseAtk = ParseStat(fighter, fighter.Stats.Atk, "atk"),
                    BaseHp = ParseStat(fighter, fighter.Stats.Hp, "hp"),
                    BaseFs = ParseStat(fighter, fighter.Stats.Fs, "fs"),
                    ImageUrl = fighter.Image,

                    // More than two abilities means a Raid Boss: that is the only signal the
                    // legacy data carries (database design section 5.3).
                    IsRaidBoss = abilities.Count > 2,
                };

                db.Variants.Add(variant);
                variants[slug] = variant;
                summary.VariantsCreated++;
            }
            else
            {
                variant.Name = fighter.Name;
                variant.SaName = fighter.Ability;
                variant.Character = character;
                variant.Tier = tier;
                variant.Element = element;
                variant.BaseAtk = ParseStat(fighter, fighter.Stats.Atk, "atk");
                variant.BaseHp = ParseStat(fighter, fighter.Stats.Hp, "hp");
                variant.BaseFs = ParseStat(fighter, fighter.Stats.Fs, "fs");
                variant.ImageUrl = fighter.Image;
                variant.IsRaidBoss = abilities.Count > 2;
            }

            SyncAbilities(variant, abilities, summary);
        }

        // Nothing is marked modified unless a value actually differs, so a second run saves
        // nothing and leaves every updated_at alone.
        summary.RowsWritten = await db.SaveChangesAsync(cancellationToken);
        summary.FightersInFile = fighters.Count;

        return summary;
    }

    private static async Task<Dictionary<string, LegacyFighter>> ReadFileAsync(
        string path,
        CancellationToken cancellationToken)
    {
        await using FileStream stream = File.OpenRead(path);

        Dictionary<string, LegacyFighter>? fighters =
            await JsonSerializer.DeserializeAsync<Dictionary<string, LegacyFighter>>(
                stream,
                cancellationToken: cancellationToken);

        return fighters is { Count: > 0 }
            ? fighters
            : throw new InvalidOperationException($"'{path}' holds no fighters.");
    }

    /// <summary>
    /// Finds or creates the character, by name. The 18 rows arrive with names only and the rest
    /// of the character text is filled in after launch (database design section 5.4), so an
    /// existing row is never overwritten here.
    /// </summary>
    private Character ResolveCharacter(
        Dictionary<string, Character> characters,
        LegacyFighter fighter,
        ImportSummary summary)
    {
        string name = EmojiPattern().Replace(fighter.Character, string.Empty).Trim();

        if (name.Length == 0)
        {
            throw new InvalidOperationException(
                $"'{fighter.Name}' has no character name once its emoji is stripped.");
        }

        if (characters.TryGetValue(name, out Character? character))
        {
            return character;
        }

        character = new Character { Name = name };
        db.Characters.Add(character);
        characters[name] = character;
        summary.CharactersCreated++;

        return character;
    }

    /// <summary>
    /// Replaces the fighter's ability block with what the file holds, keeping ordinals
    /// contiguous from 1 (database design section 3.3).
    /// </summary>
    private void SyncAbilities(
        Variant variant,
        IReadOnlyList<string> abilities,
        ImportSummary summary)
    {
        if (abilities.Count == 0)
        {
            throw new InvalidOperationException($"'{variant.Name}' has no signature abilities.");
        }

        Dictionary<short, SignatureAbility> existing =
            variant.SignatureAbilities.ToDictionary(a => a.Ordinal);

        for (int i = 0; i < abilities.Count; i++)
        {
            short ordinal = (short)(i + 1);
            string text = abilities[i];

            if (text.Length > SignatureAbility.MaxTextLength)
            {
                throw new InvalidOperationException(
                    $"SA{ordinal} of '{variant.Name}' is {text.Length} characters; the limit is "
                    + $"{SignatureAbility.MaxTextLength}.");
            }

            if (existing.Remove(ordinal, out SignatureAbility? ability))
            {
                ability.AbilityText = text;
            }
            else
            {
                variant.SignatureAbilities.Add(new SignatureAbility
                {
                    Ordinal = ordinal,
                    AbilityText = text,
                });
                summary.AbilitiesCreated++;
            }
        }

        // Anything left is an ability the file no longer has. Removing it keeps the block
        // contiguous instead of leaving a hole above the last ordinal.
        foreach (SignatureAbility removed in existing.Values)
        {
            db.SignatureAbilities.Remove(removed);
            summary.AbilitiesRemoved++;
        }
    }

    private static TValue Resolve<TValue>(
        Dictionary<string, TValue> seeded,
        string name,
        string kind) =>
        seeded.TryGetValue(name.ToLowerInvariant(), out TValue? value)
            ? value
            : throw new InvalidOperationException(
                $"Unknown {kind} '{name}'. The migration seeds the valid ones.");

    private static int ParseStat(LegacyFighter fighter, string value, string stat) =>
        int.TryParse(value, CultureInfo.InvariantCulture, out int parsed) && parsed > 0
            ? parsed
            : throw new InvalidOperationException(
                $"'{fighter.Name}' has {stat} '{value}'; stats must be whole numbers greater "
                + "than zero (FR-13).");
}
