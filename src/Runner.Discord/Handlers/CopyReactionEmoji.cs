using Discord;
using Discord.WebSocket;
using Estranged.Automation.Runner.Discord.Events;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Handlers
{
    public sealed class CopyReactionEmoji : IReactionAddedHandler
    {
        private readonly IDiscordClient _discordClient;

        public CopyReactionEmoji(IDiscordClient discordClient) => _discordClient = discordClient;

        public async Task ReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction, CancellationToken token)
        {
            if (reaction.UserId == _discordClient.CurrentUser.Id)
            {
                return;
            }

            if (!RandomExtensions.PercentChance(25))
            {
                return;
            }

            await (await message.GetOrDownloadAsync()).AddReactionAsync(reaction.Emote, token.ToRequestOptions());
        }
    }
}
