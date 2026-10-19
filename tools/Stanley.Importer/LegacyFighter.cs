using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stanley.Importer;

/// <summary>
/// One entry of v1's <c>fighters.json</c>, as it is on disk.
/// </summary>
internal sealed class LegacyFighter
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Lower-case tier name: <c>bronze</c>, <c>silver</c>, <c>gold</c>, <c>diamond</c>.</summary>
    [JsonPropertyName("rarity")]
    public string Rarity { get; set; } = string.Empty;

    [JsonPropertyName("element")]
    public string Element { get; set; } = string.Empty;

    /// <summary>
    /// The character, prefixed with a custom emoji, for example
    /// <c>&lt;:sp_an:767922687204524032&gt; Annie</c>. The emoji is stripped on import: it is
    /// presentation, and v1's emoji only rendered in the server that owned them (FR-23).
    /// </summary>
    [JsonPropertyName("character")]
    public string Character { get; set; } = string.Empty;

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("stats")]
    public LegacyStats Stats { get; set; } = new();

    /// <summary>The signature ability name shared by the fighter's abilities.</summary>
    [JsonPropertyName("ability")]
    public string Ability { get; set; } = string.Empty;

    /// <summary>
    /// Everything else in the entry, which is where the <c>SA1</c>, <c>SA2</c>, ... keys land.
    /// They are read through <see cref="SignatureAbilities"/> because the count varies: Raid
    /// Bosses have five, and v1 never showed past SA2.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalFields { get; set; } = [];

    /// <summary>The fighter's abilities in SA order.</summary>
    public IReadOnlyList<string> SignatureAbilities()
    {
        var abilities = new List<(int Ordinal, string Text)>();

        foreach ((string key, JsonElement value) in AdditionalFields)
        {
            if (!key.StartsWith("SA", StringComparison.Ordinal)
                || !int.TryParse(key.AsSpan(2), out int ordinal)
                || value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            string? text = value.GetString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                abilities.Add((ordinal, text));
            }
        }

        return [.. abilities.OrderBy(a => a.Ordinal).Select(a => a.Text)];
    }
}

internal sealed class LegacyStats
{
    // v1 stored every stat as a string, so they are parsed, not deserialized as numbers.
    [JsonPropertyName("atk")]
    public string Atk { get; set; } = "0";

    [JsonPropertyName("hp")]
    public string Hp { get; set; } = "0";

    [JsonPropertyName("fs")]
    public string Fs { get; set; } = "0";
}
