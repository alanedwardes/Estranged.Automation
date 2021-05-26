using Discord;
using Discord.WebSocket;
using Estranged.Automation.Runner.Discord.Events;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Handlers
{
    public sealed class LeftMessageHandler : IUserLeftHandler
    {
        private readonly ILogger<LeftMessageHandler> _logger;
        private readonly DiscordSocketClient _discordClient;

        public LeftMessageHandler(ILogger<LeftMessageHandler> logger, DiscordSocketClient discordClient)
        {
            _logger = logger;
            _discordClient = discordClient;
        }

        public async Task UserLeft(SocketGuildUser user, CancellationToken token)
        {
            _logger.LogInformation("User left: {0}", user);

            var goodbye = $"User {user} left the server!";

            await _discordClient.GetChannelByName("goodbyes").SendMessageAsync(goodbye, options: token.ToRequestOptions());
        }
    }
}
