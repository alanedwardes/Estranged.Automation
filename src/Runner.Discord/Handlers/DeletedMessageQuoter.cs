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

        public async Task MessageDeleted(Cacheable<IMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, CancellationToken token)
        {
            var channelDownloaded = await channel.GetOrDownloadAsync();

            const string deletionsChannel = "deletions";
            if (channelDownloaded.Name == deletionsChannel ||
                !channelDownloaded.IsPublicChannel() ||
                !message.HasValue)
            {
                return;
            }

            await _discordClient.GetChannelByName(deletionsChannel).SendMessageAsync(embed: message.Value.QuoteMessage(), options: token.ToRequestOptions());
        }
    }
}
