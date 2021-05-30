using Discord;
using Discord.WebSocket;
using Estranged.Automation.Runner.Discord.Events;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Handlers
{
    public sealed class DeletedMessageQuoter : IMessageDeleted
    {
        private readonly DiscordSocketClient _discordClient;

        public DeletedMessageQuoter(DiscordSocketClient discordClient) => _discordClient = discordClient;

        public async Task MessageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel, CancellationToken token)
        {
            const string deletionsChannel = "deletions";
            if (channel.Name == deletionsChannel ||
                !channel.IsPublicChannel() ||
                !message.HasValue)
            {
                return;
            }

            await _discordClient.GetChannelByName(deletionsChannel).SendMessageAsync(embed: message.Value.QuoteMessage(), options: token.ToRequestOptions());
        }
    }
}
