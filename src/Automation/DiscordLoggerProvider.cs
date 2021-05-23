using Discord;
using Microsoft.Extensions.Logging;

namespace Estranged.Automation
{
    public sealed class DiscordLoggerProvider : ILoggerProvider
    {
        private readonly IDiscordClient _discordClient;

        public DiscordLoggerProvider(IDiscordClient discordClient) => _discordClient = discordClient;

        public ILogger CreateLogger(string categoryName) => new DiscordLogger(_discordClient, categoryName);

        public void Dispose()
        {
        }
    }
}
