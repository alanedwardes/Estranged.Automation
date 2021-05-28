using Discord;
using Discord.WebSocket;
using Estranged.Automation.Runner.Discord.Events;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Handlers
{
    public sealed class DeletedMessageQuoter : IMessageDeleted, IResponder
    {
        private readonly IDictionary<ulong, IMessage> _publicMessageHistory = new ConcurrentDictionary<ulong, IMessage>();
        private readonly ILogger<DeletedMessageQuoter> _logger;
        private readonly DiscordSocketClient _discordClient;

        public DeletedMessageQuoter(ILogger<DeletedMessageQuoter> logger, DiscordSocketClient discordClient)
        {
            _logger = logger;
            _discordClient = discordClient;
        }

        public async Task MessageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel, CancellationToken token)
        {
            const string deletionsChannel = "deletions";
            if (!channel.IsPublicChannel() && channel.Name != deletionsChannel)
            {
                return;
            }

            if (_publicMessageHistory.TryGetValue(message.Id, out IMessage value))
            {
                await _discordClient.GetChannelByName(deletionsChannel).SendMessageAsync("Deleted:", false, value.QuoteMessage(), token.ToRequestOptions());
            }
        }

        public Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (message.Channel.IsPublicChannel())
            {
                _publicMessageHistory.Add(message.Id, message);

                if (_publicMessageHistory.Count > 100)
                {
                    _publicMessageHistory.Remove(_publicMessageHistory.Keys.First());
                }
            }

            return Task.CompletedTask;
        }
    }
}
