using Microsoft.Extensions.Configuration;
using Stanley.Bot.Configuration;

namespace Stanley.Bot.Tests;

/// <summary>
/// The test-server list is the one setting that is written in two shapes: an array in
/// appsettings, and a comma-separated value in an environment variable. Both have to arrive as
/// the same list, and an empty variable must not erase what appsettings says.
/// </summary>
public sealed class DiscordOptionsBindingTests
{
    private const string Key = $"{DiscordOptions.SectionName}:{nameof(DiscordOptions.TestGuildIds)}";

    [Fact]
    public void An_array_in_appsettings_binds_to_the_list()
    {
        DiscordOptions options = Bind(new Dictionary<string, string?> { [$"{Key}:0"] = "111", [$"{Key}:1"] = "222" });

        Assert.Equal<ulong>([111, 222], options.TestGuildIds);
    }

    [Fact]
    public void A_comma_separated_variable_binds_to_the_list()
    {
        DiscordOptions options = Bind(new Dictionary<string, string?> { [Key] = " 111 , 222 " });

        Assert.Equal<ulong>([111, 222], options.TestGuildIds);
    }

    [Fact]
    public void An_empty_variable_leaves_the_appsettings_list_alone()
    {
        DiscordOptions options = Bind(
            new Dictionary<string, string?> { [$"{Key}:0"] = "111" },
            new Dictionary<string, string?> { [Key] = string.Empty });

        Assert.Equal<ulong>([111], options.TestGuildIds);
    }

    [Fact]
    public void A_shorter_variable_replaces_the_appsettings_list_rather_than_merging_into_it()
    {
        DiscordOptions options = Bind(
            new Dictionary<string, string?> { [$"{Key}:0"] = "111", [$"{Key}:1"] = "222" },
            new Dictionary<string, string?> { [Key] = "333" });

        Assert.Equal<ulong>([333], options.TestGuildIds);
    }

    [Fact]
    public void No_test_servers_configured_leaves_the_list_empty()
    {
        DiscordOptions options = Bind([]);

        Assert.Empty(options.TestGuildIds);
    }

    /// <summary>
    /// Mirrors what Program.cs does: the later sources win, the comma-separated form is
    /// expanded, and only then is the section bound.
    /// </summary>
    private static DiscordOptions Bind(params Dictionary<string, string?>[] sources)
    {
        ConfigurationBuilder builder = new();
        foreach (Dictionary<string, string?> source in sources)
        {
            builder.AddInMemoryCollection(source);
        }

        IConfigurationRoot configuration = builder.Build();
        builder.AddInMemoryCollection(
            GuildIdListConfiguration.ExpandCommaSeparated(configuration, Key));

        DiscordOptions options = new();
        builder.Build().GetSection(DiscordOptions.SectionName).Bind(options);
        return options;
    }
}
