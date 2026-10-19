using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Stanley.Core.Naming;
using Stanley.Data.Aliases;
using Stanley.Data.Changes;
using Stanley.Data.Entities;
using Stanley.Data.Lookup;

namespace Stanley.Data.Fighters;

/// <summary>
/// The fighter data an editor manages from Discord: adding a fighter (FR-12), changing one
/// (FR-13), writing and removing its signature abilities (FR-13), and removing it with
/// everything that belongs to it (FR-14).
/// </summary>
/// <remarks>
/// <para>
/// Every rule FR-13 says a refusal must state lives here rather than in the command, so the
/// rules are tested without Discord and cannot differ between the four subcommands. Discord
/// checks what it can — a required option, an integer above zero, a fixed list of choices, a
/// form field of at most 1,024 characters — and none of that is load-bearing: an option can
/// still arrive as whitespace, and the fixed lists are code while the rows are data.
/// </para>
/// <para>
/// The <c>fighter</c> option is resolved by <see cref="FighterLookup.ResolveVariantIdExactAsync"/>:
/// names and aliases, matched exactly on the slug, with no fuzzy step. Normalizing already
/// absorbs case, spacing and punctuation; what is left out is the typo fallback, which must not
/// decide which fighter an edit or a removal lands on.
/// </para>
/// <para>
/// Every write appends change records in the same transaction as the change, so a change that
/// could not be recorded is not kept (FR-15). A removal records one row per ability and per
/// alias as well as the fighter, because those records are the only copy of the rows that is
/// left (FR-16).
/// </para>
/// </remarks>
public sealed class FighterAdminRepository(IDbContextFactory<StanleyDbContext> contextFactory)
{
    /// <summary>
    /// Adds a fighter from the values FR-12 asks for. Its abilities are added afterwards, one
    /// at a time (FR-13).
    /// </summary>
    public async Task<FighterWriteResult> AddAsync(
        FighterDraft draft,
        EditorContext editor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);

        string name = draft.Name?.Trim() ?? string.Empty;
        string slug = NameNormalizer.Normalize(name);
        string saName = draft.AbilityName?.Trim() ?? string.Empty;
        string portraitUrl = draft.PortraitUrl?.Trim() ?? string.Empty;

        FighterWriteResult? refusal =
            CheckRequired(name, FighterField.Name)
            ?? CheckSlug(name, slug)
            ?? CheckRequired(saName, FighterField.AbilityName)
            ?? CheckStat(draft.BaseAtk, FighterField.BaseAtk)
            ?? CheckStat(draft.BaseHp, FighterField.BaseHp)
            ?? CheckStat(draft.BaseFs, FighterField.BaseFs)
            ?? CheckUrl(portraitUrl, FighterField.PortraitUrl);

        if (refusal is not null)
        {
            return refusal;
        }

        await using StanleyDbContext db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        if (await FindCharacterAsync(db, draft.Character, cancellationToken) is not { } character)
        {
            return FighterWriteResult.NotAllowed(FighterField.Character, draft.Character);
        }

        if (await FindTierAsync(db, draft.Rarity, cancellationToken) is not { } tier)
        {
            return FighterWriteResult.NotAllowed(FighterField.Rarity, draft.Rarity);
        }

        if (await FindElementAsync(db, draft.Element, cancellationToken) is not { } element)
        {
            return FighterWriteResult.NotAllowed(FighterField.Element, draft.Element);
        }

        await using IDbContextTransaction transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        if (await FindNameOwnerAsync(db, slug, null, cancellationToken) is string owner)
        {
            return FighterWriteResult.NameTaken(name, owner);
        }

        var variant = new Variant
        {
            Name = name,
            Slug = slug,
            CharacterId = character.Id,
            TierId = tier.Id,
            ElementId = element.Id,
            SaName = saName,
            BaseAtk = draft.BaseAtk,
            BaseHp = draft.BaseHp,
            BaseFs = draft.BaseFs,
            ImageUrl = portraitUrl,
        };

        db.Variants.Add(variant);
        await db.SaveChangesAsync(cancellationToken);

        db.ChangeRecords.Add(ChangeRecords.Insert(
            editor,
            ChangeEntityType.Variant,
            Key(variant.Id),
            VariantSnapshot.Of(variant)));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return FighterWriteResult.Added(variant.Name);
    }

    /// <summary>
    /// Changes the fields the editor gave on the fighter <paramref name="fighter"/> names, and
    /// reports which of them actually changed (FR-13).
    /// </summary>
    public async Task<FighterWriteResult> EditAsync(
        string? fighter,
        FighterEdit edit,
        EditorContext editor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(edit);

        string fighterSlug = NameNormalizer.Normalize(fighter);

        if (fighterSlug.Length == 0)
        {
            return FighterWriteResult.NotFound();
        }

        string? newName = edit.Name?.Trim();
        string? newSlug = newName is null ? null : NameNormalizer.Normalize(newName);
        string? saName = edit.AbilityName?.Trim();
        string? quote = edit.Quote?.Trim();
        string? portraitUrl = edit.PortraitUrl?.Trim();
        string? wikiUrl = edit.WikiUrl?.Trim();

        // Checked before anything is read, so a refusal costs one round trip at most and the
        // message is the same whichever field was wrong.
        FighterWriteResult? refusal =
            CheckGiven(newName, FighterField.Name)
            ?? (newName is null ? null : CheckSlug(newName, newSlug!))
            ?? CheckGiven(saName, FighterField.AbilityName)
            ?? CheckGiven(quote, FighterField.Quote)
            ?? CheckStat(edit.BaseAtk, FighterField.BaseAtk)
            ?? CheckStat(edit.BaseHp, FighterField.BaseHp)
            ?? CheckStat(edit.BaseFs, FighterField.BaseFs)
            ?? CheckGivenUrl(portraitUrl, FighterField.PortraitUrl)
            ?? CheckGivenUrl(wikiUrl, FighterField.WikiUrl);

        if (refusal is not null)
        {
            return refusal;
        }

        await using StanleyDbContext db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        await using IDbContextTransaction transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        if (await FighterLookup.ResolveVariantIdExactAsync(db, fighterSlug, cancellationToken)
            is not int variantId)
        {
            return FighterWriteResult.NotFound();
        }

        // Tracked, and with the three reference rows, because the confirmation shows the old
        // value of whatever changed and a tier or element reads as its name, not its id.
        Variant? variant = await db.Variants
            .Include(v => v.Character)
            .Include(v => v.Tier)
            .Include(v => v.Element)
            .FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);

        if (variant is null)
        {
            return FighterWriteResult.NotFound();
        }

        VariantSnapshot before = VariantSnapshot.Of(variant);
        var changes = new List<FighterFieldChange>();

        if (newName is not null && !string.Equals(newName, variant.Name, StringComparison.Ordinal))
        {
            // The new slug has to pass the same check a new fighter's does, less the fighter's
            // own row: renaming Heat Synced to its own name is not a clash.
            if (await FindNameOwnerAsync(db, newSlug!, variant.Id, cancellationToken)
                is string owner)
            {
                return FighterWriteResult.NameTaken(newName, owner);
            }

            changes.Add(new FighterFieldChange(FighterField.Name, variant.Name, newName));
            variant.Name = newName;

            variant.Slug = newSlug!;
        }

        if (edit.Character is not null)
        {
            if (await FindCharacterAsync(db, edit.Character, cancellationToken)
                is not { } character)
            {
                return FighterWriteResult.NotAllowed(FighterField.Character, edit.Character);
            }

            if (character.Id != variant.CharacterId)
            {
                changes.Add(new FighterFieldChange(
                    FighterField.Character,
                    variant.Character.Name,
                    character.Name));

                variant.CharacterId = character.Id;
            }
        }

        if (edit.Rarity is not null)
        {
            if (await FindTierAsync(db, edit.Rarity, cancellationToken) is not { } tier)
            {
                return FighterWriteResult.NotAllowed(FighterField.Rarity, edit.Rarity);
            }

            if (tier.Id != variant.TierId)
            {
                changes.Add(new FighterFieldChange(
                    FighterField.Rarity,
                    variant.Tier.Name,
                    tier.Name));

                variant.TierId = tier.Id;
            }
        }

        if (edit.Element is not null)
        {
            if (await FindElementAsync(db, edit.Element, cancellationToken) is not { } element)
            {
                return FighterWriteResult.NotAllowed(FighterField.Element, edit.Element);
            }

            if (element.Id != variant.ElementId)
            {
                changes.Add(new FighterFieldChange(
                    FighterField.Element,
                    variant.Element.Name,
                    element.Name));

                variant.ElementId = element.Id;
            }
        }

        ApplyText(
            changes,
            FighterField.AbilityName,
            saName,
            variant.SaName,
            value => variant.SaName = value);

        ApplyText(
            changes,
            FighterField.Quote,
            quote,
            variant.Quote,
            value => variant.Quote = value);

        ApplyText(
            changes,
            FighterField.PortraitUrl,
            portraitUrl,
            variant.ImageUrl,
            value => variant.ImageUrl = value);

        ApplyText(
            changes,
            FighterField.WikiUrl,
            wikiUrl,
            variant.WikiUrl,
            value => variant.WikiUrl = value);

        ApplyStat(
            changes,
            FighterField.BaseAtk,
            edit.BaseAtk,
            variant.BaseAtk,
            value => variant.BaseAtk = value);

        ApplyStat(
            changes,
            FighterField.BaseHp,
            edit.BaseHp,
            variant.BaseHp,
            value => variant.BaseHp = value);

        ApplyStat(
            changes,
            FighterField.BaseFs,
            edit.BaseFs,
            variant.BaseFs,
            value => variant.BaseFs = value);

        if (edit.IsRaidBoss is bool isRaidBoss && isRaidBoss != variant.IsRaidBoss)
        {
            changes.Add(new FighterFieldChange(
                FighterField.RaidBoss,
                Flag(variant.IsRaidBoss),
                Flag(isRaidBoss)));

            variant.IsRaidBoss = isRaidBoss;
        }

        if (changes.Count == 0)
        {
            return FighterWriteResult.NothingToChange(variant.Name);
        }

        // Saved before the record is built, so the record's "after" carries the
        // <c>updated_at</c> the row ended up with rather than the one it had.
        await db.SaveChangesAsync(cancellationToken);

        db.ChangeRecords.Add(ChangeRecords.Update(
            editor,
            ChangeEntityType.Variant,
            Key(variant.Id),
            before,
            VariantSnapshot.Of(variant)));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return FighterWriteResult.Edited(variant.Name, changes);
    }

    /// <summary>
    /// The fighter a confirmation step is about to act on, and what would go with it (FR-13,
    /// FR-14). Null when <paramref name="fighter"/> names no fighter.
    /// </summary>
    public async Task<FighterTarget?> FindTargetAsync(
        string? fighter,
        CancellationToken cancellationToken = default)
    {
        string slug = NameNormalizer.Normalize(fighter);

        if (slug.Length == 0)
        {
            return null;
        }

        await using StanleyDbContext db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        if (await FighterLookup.ResolveVariantIdExactAsync(db, slug, cancellationToken)
            is not int variantId)
        {
            return null;
        }

        return await db.Variants
            .AsNoTracking()
            .Where(variant => variant.Id == variantId)
            .Select(variant => new FighterTarget(
                variant.Id,
                variant.Name,
                variant.SignatureAbilities.Count,
                variant.Aliases.Count))
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Works out which signature ability a form would write, and the text to pre-fill it with
    /// (FR-13). Nothing is written here.
    /// </summary>
    /// <param name="fighter">The fighter option, as the editor gave it.</param>
    /// <param name="saNumber">
    /// The SA number they asked for, or null to add one at the end of the block.
    /// </param>
    /// <param name="cancellationToken">Cancels the read.</param>
    public async Task<AbilityPrompt> PrepareAbilityAsync(
        string? fighter,
        int? saNumber,
        CancellationToken cancellationToken = default)
    {
        string slug = NameNormalizer.Normalize(fighter);

        if (slug.Length == 0)
        {
            return AbilityPrompt.NotFound();
        }

        await using StanleyDbContext db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        if (await FighterLookup.ResolveVariantIdExactAsync(db, slug, cancellationToken)
            is not int variantId)
        {
            return AbilityPrompt.NotFound();
        }

        var found = await db.Variants
            .AsNoTracking()
            .Where(variant => variant.Id == variantId)
            .Select(variant => new
            {
                variant.Name,
                Abilities = variant.SignatureAbilities
                    .OrderBy(ability => ability.Ordinal)
                    .Select(ability => new { ability.Ordinal, ability.AbilityText })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (found is null)
        {
            return AbilityPrompt.NotFound();
        }

        int count = found.Abilities.Count;

        // No number given means the next one: an ability is added at the end of the block
        // (database design section 3.3).
        int ordinal = saNumber ?? count + 1;

        if (ordinal < 1 || ordinal > count + 1)
        {
            return AbilityPrompt.OutOfRange(found.Name, count);
        }

        string? currentText = found.Abilities
            .FirstOrDefault(ability => ability.Ordinal == ordinal)
            ?.AbilityText;

        return AbilityPrompt.Ready(variantId, found.Name, (short)ordinal, currentText, count);
    }

    /// <summary>
    /// Writes one signature ability: the text of an ability that exists, or a new one at the
    /// end of the block (FR-13).
    /// </summary>
    /// <remarks>
    /// The ability is addressed by the row's own key, which the form carried, so nothing is
    /// matched by name a second time. The checks are made again all the same: a form can be
    /// submitted long after it was opened, and the block may have changed in between.
    /// </remarks>
    public async Task<AbilitySetResult> SetAbilityAsync(
        int variantId,
        short ordinal,
        string? text,
        EditorContext editor,
        CancellationToken cancellationToken = default)
    {
        string abilityText = text?.Trim() ?? string.Empty;

        if (abilityText.Length == 0)
        {
            return AbilitySetResult.Missing();
        }

        if (abilityText.Length > SignatureAbility.MaxTextLength)
        {
            return AbilitySetResult.TooLong();
        }

        await using StanleyDbContext db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        await using IDbContextTransaction transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        string? fighterName = await db.Variants
            .AsNoTracking()
            .Where(variant => variant.Id == variantId)
            .Select(variant => variant.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (fighterName is null)
        {
            return AbilitySetResult.NotFound();
        }

        List<SignatureAbility> block = await db.SignatureAbilities
            .Where(ability => ability.VariantId == variantId)
            .OrderBy(ability => ability.Ordinal)
            .ToListAsync(cancellationToken);

        SignatureAbility? existing = block.Find(ability => ability.Ordinal == ordinal);

        if (existing is null)
        {
            if (ordinal < 1 || ordinal > block.Count + 1)
            {
                return AbilitySetResult.OutOfRange(fighterName, block.Count);
            }

            var row = new SignatureAbility
            {
                VariantId = variantId,
                Ordinal = ordinal,
                AbilityText = abilityText,
            };

            db.SignatureAbilities.Add(row);
            await db.SaveChangesAsync(cancellationToken);

            db.ChangeRecords.Add(ChangeRecords.Insert(
                editor,
                ChangeEntityType.SignatureAbility,
                Key(variantId, ordinal),
                AbilitySnapshot.Of(row)));
        }
        else
        {
            AbilitySnapshot before = AbilitySnapshot.Of(existing);

            existing.AbilityText = abilityText;
            await db.SaveChangesAsync(cancellationToken);

            db.ChangeRecords.Add(ChangeRecords.Update(
                editor,
                ChangeEntityType.SignatureAbility,
                Key(variantId, ordinal),
                before,
                AbilitySnapshot.Of(existing)));
        }

        await db.SaveChangesAsync(cancellationToken);

        await RenumberAsync(db, variantId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return AbilitySetResult.Saved(
            fighterName,
            ordinal,
            existing is null,
            existing is null ? block.Count + 1 : block.Count);
    }

    /// <summary>
    /// Removes every signature ability the fighter has (FR-13). The fighter stays, and the
    /// block is re-entered from SA1.
    /// </summary>
    /// <remarks>
    /// There is no removing one ability on its own, so there is no method for it: a block that
    /// could lose its middle row is a block whose numbers can have a gap (database design
    /// section 3.3).
    /// </remarks>
    public async Task<AbilityRemovalResult> RemoveAbilitiesAsync(
        int variantId,
        EditorContext editor,
        CancellationToken cancellationToken = default)
    {
        await using StanleyDbContext db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        string? fighterName = await db.Variants
            .AsNoTracking()
            .Where(variant => variant.Id == variantId)
            .Select(variant => variant.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (fighterName is null)
        {
            return AbilityRemovalResult.NotFound();
        }

        List<SignatureAbility> block = await db.SignatureAbilities
            .Where(ability => ability.VariantId == variantId)
            .OrderBy(ability => ability.Ordinal)
            .ToListAsync(cancellationToken);

        if (block.Count == 0)
        {
            return AbilityRemovalResult.None(fighterName);
        }

        foreach (SignatureAbility ability in block)
        {
            // Taken before the delete: afterwards the record holds the only copy of the row
            // (FR-15), and it is what an undo would restore (FR-16).
            db.ChangeRecords.Add(ChangeRecords.Delete(
                editor,
                ChangeEntityType.SignatureAbility,
                Key(variantId, ability.Ordinal),
                AbilitySnapshot.Of(ability)));
        }

        db.SignatureAbilities.RemoveRange(block);

        // No explicit transaction: the deletes and their records are one SaveChanges, which EF
        // Core already sends as a single transaction.
        await db.SaveChangesAsync(cancellationToken);

        return AbilityRemovalResult.Removed(fighterName, block.Count);
    }

    /// <summary>
    /// Removes the fighter, its signature abilities and its aliases (FR-14).
    /// </summary>
    /// <remarks>
    /// The abilities and aliases are cascaded by the schema, and each one is recorded here all
    /// the same: a cascade leaves no trace of what it took, and FR-15 is what a developer reads
    /// to put a bad removal back.
    /// </remarks>
    public async Task<FighterRemovalResult> RemoveAsync(
        int variantId,
        EditorContext editor,
        CancellationToken cancellationToken = default)
    {
        await using StanleyDbContext db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        Variant? variant = await db.Variants
            .Include(v => v.SignatureAbilities)
            .Include(v => v.Aliases)
            .FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);

        if (variant is null)
        {
            return FighterRemovalResult.NotFound();
        }

        foreach (SignatureAbility ability in variant.SignatureAbilities)
        {
            db.ChangeRecords.Add(ChangeRecords.Delete(
                editor,
                ChangeEntityType.SignatureAbility,
                Key(variant.Id, ability.Ordinal),
                AbilitySnapshot.Of(ability)));
        }

        foreach (VariantAlias alias in variant.Aliases)
        {
            db.ChangeRecords.Add(ChangeRecords.Delete(
                editor,
                ChangeEntityType.VariantAlias,
                Key(alias.Id),
                AliasSnapshot.Of(alias)));
        }

        db.ChangeRecords.Add(ChangeRecords.Delete(
            editor,
            ChangeEntityType.Variant,
            Key(variant.Id),
            VariantSnapshot.Of(variant)));

        var removed = FighterRemovalResult.Removed(
            variant.Name,
            variant.SignatureAbilities.Count,
            variant.Aliases.Count);

        db.Variants.Remove(variant);
        await db.SaveChangesAsync(cancellationToken);

        return removed;
    }

    /// <summary>
    /// Suggests characters for what the editor has typed so far, for the <c>character</c>
    /// option of <c>/fighter-admin add</c> and <c>edit</c> (FR-12).
    /// </summary>
    /// <remarks>
    /// Matched in memory: there are 18 characters, the table has no slug column of its own, and
    /// <see cref="NameNormalizer"/> is a C# method with no SQL translation. Eighteen rows is
    /// also why a substring match is enough where the fighter suggestions need trigrams.
    /// </remarks>
    public async Task<IReadOnlyList<string>> SuggestCharactersAsync(
        string? typed,
        CancellationToken cancellationToken = default)
    {
        await using StanleyDbContext db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        List<string> names = await db.Characters
            .AsNoTracking()
            .OrderBy(character => character.Name)
            .Select(character => character.Name)
            .ToListAsync(cancellationToken);

        string wanted = NameNormalizer.Normalize(typed);

        IEnumerable<string> matches = wanted.Length == 0
            ? names
            : names
                .Where(name =>
                    NameNormalizer.Normalize(name).Contains(wanted, StringComparison.Ordinal))
                // What the editor is typing the start of first, as in the fighter suggestions.
                .OrderByDescending(name =>
                    NameNormalizer.Normalize(name).StartsWith(wanted, StringComparison.Ordinal))
                .ThenBy(name => name, StringComparer.Ordinal);

        return [.. matches.Take(FighterLookup.MaxSuggestions)];
    }

    /// <summary>
    /// Closes any gap in a fighter's SA numbers, so they always run from 1 with nothing
    /// missing (database design section 3.3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nothing in the database enforces contiguity; this is what guarantees it. With no partial
    /// removal and new abilities appended at the end, a completed command cannot leave a gap,
    /// so in practice this changes nothing and exists to repair a block that was written around
    /// the commands.
    /// </para>
    /// <para>
    /// One statement per row, in ascending order, inside the caller's transaction. Each
    /// statement moves a row down into a number nothing holds any more, so the
    /// <c>(variant_id, ordinal)</c> key is never violated on the way; a single multi-row update
    /// could collide mid-statement, because PostgreSQL checks a key that is not deferrable per
    /// row. The ordinal is part of the key, so this is SQL rather than a tracked change: EF
    /// Core refuses to modify a key property.
    /// </para>
    /// </remarks>
    private static async Task RenumberAsync(
        StanleyDbContext db,
        int variantId,
        CancellationToken cancellationToken)
    {
        List<short> ordinals = await db.SignatureAbilities
            .AsNoTracking()
            .Where(ability => ability.VariantId == variantId)
            .OrderBy(ability => ability.Ordinal)
            .Select(ability => ability.Ordinal)
            .ToListAsync(cancellationToken);

        for (int index = 0; index < ordinals.Count; index++)
        {
            short current = ordinals[index];
            short target = (short)(index + 1);

            if (current == target)
            {
                continue;
            }

            await db.SignatureAbilities
                .Where(ability =>
                    ability.VariantId == variantId && ability.Ordinal == current)
                .ExecuteUpdateAsync(
                    set => set.SetProperty(ability => ability.Ordinal, target),
                    cancellationToken);
        }
    }

    /// <summary>
    /// The fighter or alias that already answers to <paramref name="slug"/>, or null when
    /// nothing does (database design section 3.2).
    /// </summary>
    /// <remarks>
    /// <c>searchable_names</c> is already the union of names and aliases, so one query covers
    /// both halves of the rule, and joining the fighter gives the refusal the name it has to
    /// carry. The unique index on <c>slug</c> is the backstop for two editors adding the same
    /// name at once; that loses the race as a failed command, and the retry reports the
    /// duplicate properly.
    /// </remarks>
    /// <param name="db">The caller's context, so the check is inside its transaction.</param>
    /// <param name="slug">The normalized name being claimed.</param>
    /// <param name="exceptVariantId">
    /// The fighter doing the claiming, when it already exists: its own name and aliases are not
    /// a clash.
    /// </param>
    /// <param name="cancellationToken">Cancels the read.</param>
    private static async Task<string?> FindNameOwnerAsync(
        StanleyDbContext db,
        string slug,
        int? exceptVariantId,
        CancellationToken cancellationToken) =>
        await db.SearchableNames
            .AsNoTracking()
            // Written as a null check and a comparison rather than as `!=` alone: in SQL,
            // anything compared with NULL is NULL, so `variant_id <> NULL` would match no row
            // at all and a new fighter's name would never look taken.
            .Where(name =>
                name.Slug == slug
                && (exceptVariantId == null || name.VariantId != exceptVariantId))
            .Join(
                db.Variants,
                name => name.VariantId,
                variant => variant.Id,
                (name, variant) => variant.Name)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// The character <paramref name="typed"/> names, or null when it names none (FR-12).
    /// </summary>
    private static async Task<(int Id, string Name)?> FindCharacterAsync(
        StanleyDbContext db,
        string? typed,
        CancellationToken cancellationToken)
    {
        string wanted = NameNormalizer.Normalize(typed);

        if (wanted.Length == 0)
        {
            return null;
        }

        var characters = await db.Characters
            .AsNoTracking()
            .Select(character => new { character.Id, character.Name })
            .ToListAsync(cancellationToken);

        var match = characters.Find(character =>
            NameNormalizer.Normalize(character.Name) == wanted);

        return match is null ? null : (match.Id, match.Name);
    }

    /// <summary>
    /// The tier <paramref name="typed"/> names, or null when it names none. The FRD calls a
    /// tier a rarity.
    /// </summary>
    /// <remarks>
    /// Matched against the four seeded rows rather than trusted from the command's fixed list,
    /// so a list that has drifted from the table is a refusal naming the value and never a
    /// write against the wrong row.
    /// </remarks>
    private static async Task<(short Id, string Name)?> FindTierAsync(
        StanleyDbContext db,
        string? typed,
        CancellationToken cancellationToken)
    {
        string wanted = NameNormalizer.Normalize(typed);

        if (wanted.Length == 0)
        {
            return null;
        }

        var tiers = await db.Tiers
            .AsNoTracking()
            .Select(tier => new { tier.Id, tier.Name })
            .ToListAsync(cancellationToken);

        var match = tiers.Find(tier => NameNormalizer.Normalize(tier.Name) == wanted);

        return match is null ? null : (match.Id, match.Name);
    }

    /// <summary>The element <paramref name="typed"/> names, or null when it names none.</summary>
    private static async Task<(short Id, string Name)?> FindElementAsync(
        StanleyDbContext db,
        string? typed,
        CancellationToken cancellationToken)
    {
        string wanted = NameNormalizer.Normalize(typed);

        if (wanted.Length == 0)
        {
            return null;
        }

        var elements = await db.Elements
            .AsNoTracking()
            .Select(element => new { element.Id, element.Name })
            .ToListAsync(cancellationToken);

        var match = elements.Find(element => NameNormalizer.Normalize(element.Name) == wanted);

        return match is null ? null : (match.Id, match.Name);
    }

    /// <summary>A required value that arrived blank or as whitespace.</summary>
    private static FighterWriteResult? CheckRequired(string value, FighterField field) =>
        value.Length == 0 ? FighterWriteResult.Missing(field) : null;

    /// <summary>
    /// The same, for an optional value: left out is fine, given as whitespace is not. A field
    /// cannot be cleared by sending spaces, because a column that is empty on purpose and one
    /// that was emptied by accident would then be the same write.
    /// </summary>
    private static FighterWriteResult? CheckGiven(string? value, FighterField field) =>
        value is null ? null : CheckRequired(value, field);

    /// <summary>
    /// A name of punctuation or symbols alone normalizes to nothing, so no lookup could ever
    /// find the fighter again (FR-02).
    /// </summary>
    private static FighterWriteResult? CheckSlug(string name, string slug) =>
        slug.Length == 0 ? FighterWriteResult.NameNotUsable(name) : null;

    /// <summary>
    /// Stats are whole numbers greater than zero (FR-13). The column has the same check, which
    /// would fail the command rather than answer it.
    /// </summary>
    private static FighterWriteResult? CheckStat(int? value, FighterField field) =>
        value is int stat && stat <= 0 ? FighterWriteResult.NotPositive(field) : null;

    /// <summary>
    /// A URL has to be absolute and <c>http</c> or <c>https</c>: the portrait goes straight
    /// into an embed, and Discord rejects the whole embed over a URL it cannot read, which
    /// would break the card for everyone rather than for the editor who typed it.
    /// </summary>
    private static FighterWriteResult? CheckUrl(string value, FighterField field)
    {
        if (CheckRequired(value, field) is FighterWriteResult missing)
        {
            return missing;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                ? null
                : FighterWriteResult.NotAUrl(field, value);
    }

    private static FighterWriteResult? CheckGivenUrl(string? value, FighterField field) =>
        value is null ? null : CheckUrl(value, field);

    /// <summary>
    /// Applies one text field the editor gave, unless it already holds that text, and records
    /// the old and new values for the confirmation (FR-13).
    /// </summary>
    private static void ApplyText(
        List<FighterFieldChange> changes,
        FighterField field,
        string? given,
        string? current,
        Action<string> apply)
    {
        if (given is null || string.Equals(given, current, StringComparison.Ordinal))
        {
            return;
        }

        changes.Add(new FighterFieldChange(field, current, given));
        apply(given);
    }

    /// <summary>
    /// The same for a stat, formatted invariantly so the confirmation reads the same
    /// everywhere.
    /// </summary>
    private static void ApplyStat(
        List<FighterFieldChange> changes,
        FighterField field,
        int? given,
        int current,
        Action<int> apply)
    {
        if (given is not int value || value == current)
        {
            return;
        }

        changes.Add(new FighterFieldChange(
            field,
            current.ToString(CultureInfo.InvariantCulture),
            value.ToString(CultureInfo.InvariantCulture)));

        apply(value);
    }

    /// <summary>
    /// A flag as the stored value, not as wording: the reply layer decides what it reads as.
    /// </summary>
    private static string Flag(bool value) =>
        value.ToString(CultureInfo.InvariantCulture).ToLowerInvariant();

    /// <summary>
    /// A change record's <c>entity_key</c> for a fighter: text, because for one entity the key
    /// is composite and the row may already be gone (database design section 3.7).
    /// </summary>
    private static string Key(int id) => id.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// The same for a signature ability, whose key is the pair <c>(variant_id, ordinal)</c>.
    /// </summary>
    private static string Key(int variantId, short ordinal) =>
        Key(variantId) + ":" + ordinal.ToString(CultureInfo.InvariantCulture);
}
