using Discord;
using Microsoft.Extensions.Logging;

namespace Stanley.Bot.Hosting;

/// <summary>
/// Sends Discord.Net's own log messages to the host's logger.
/// </summary>
internal static class DiscordLogForwarder
{
    public static Task ForwardAsync(ILogger logger, LogMessage message)
    {
        LogLevel level = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information,
        };

        logger.Log(level, message.Exception, "[{Source}] {Message}", message.Source, message.Message);
        return Task.CompletedTask;
    }
}
