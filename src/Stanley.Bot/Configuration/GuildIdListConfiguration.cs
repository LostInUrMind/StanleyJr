using Microsoft.Extensions.Configuration;

namespace Stanley.Bot.Configuration;

/// <summary>
/// Lets a list of guild IDs be written as one comma-separated value, the shape an environment
/// variable or a <c>.env</c> file can carry. Only used for setting up test guild.
/// </summary>
public static class GuildIdListConfiguration
{
    public static List<KeyValuePair<string, string?>> ExpandCommaSeparated(
        IConfiguration configuration,
        string key)
    {
        List<KeyValuePair<string, string?>> expanded = [];

        string? value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            return expanded;
        }

        string[] ids = value.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        for (int index = 0; index < ids.Length; index++)
        {
            expanded.Add(new KeyValuePair<string, string?>($"{key}:{index}", ids[index]));
        }

        int existing = configuration.GetSection(key).GetChildren().Count();
        for (int index = ids.Length; index < existing; index++)
        {
            expanded.Add(new KeyValuePair<string, string?>($"{key}:{index}", string.Empty));
        }

        return expanded;
    }
}
