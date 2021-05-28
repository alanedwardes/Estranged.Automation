using Discord;
using Discord.WebSocket;
using Estranged.Automation.Runner.Discord.Events;
using System;
using System.Linq;
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

            var downloadedMessage = await message.GetOrDownloadAsync();

            if (!RandomExtensions.PercentChance(1))
            {
                await PostReactions(downloadedMessage, token, new Emoji("🇪"), new Emoji("🇸"), new Emoji("🇹"), new Emoji("🇧"), new Emoji("🇴"), new Emoji("🤓"));
                return;
            }

            if (!RandomExtensions.PercentChance(5))
            {
                await PostReactions(downloadedMessage, token, new Emoji("🤥"));
                return;
            }

            await PostReactions(downloadedMessage, token, reaction.Emote);
        }

        private async Task PostReactions(IUserMessage message, CancellationToken token, params IEmote[] emotes)
        {
            foreach (var emote in emotes)
            {
                await message.AddReactionAsync(emote, token.ToRequestOptions());
            }
        }
    }
}
