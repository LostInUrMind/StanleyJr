using System.ComponentModel.DataAnnotations;

namespace Stanley.Bot.Configuration;

/// <summary>
/// Everything Stanley needs to talk to Discord. 
/// </summary>
public sealed class DiscordOptions
{
    public const string SectionName = "Discord";

    /// <summary>
    /// The bot token.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage =
        "Set Discord:Token (dotnet user-secrets, or the Discord__Token environment variable).")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// The test servers. Leave it empty in production to register commands globally.
    /// </summary>
    public IList<ulong> TestGuildIds { get; } = [];

    /// <summary>
    /// The server that holds the Wiki Editor role. Write commands will be registered to this
    /// server only, so they do not appear anywhere else.
    /// </summary>
    public ulong? HomeGuildId { get; set; }

    /// <summary>
    /// The Wiki Editor role, matched by ID and never by name: anyone can create a role called
    /// "Wiki Editor" in their own server.
    /// </summary>
    public ulong? WikiEditorRoleId { get; set; }
}
