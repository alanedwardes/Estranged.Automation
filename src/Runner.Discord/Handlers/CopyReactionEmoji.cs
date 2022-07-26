using Discord;
using Discord.WebSocket;
using Estranged.Automation.Runner.Discord.Events;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Handlers
{
    public sealed class CopyReactionEmoji : IReactionAddedHandler, IResponder
    {
        private readonly IDiscordClient _discordClient;

        public CopyReactionEmoji(IDiscordClient discordClient) => _discordClient = discordClient;

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (!RandomExtensions.PercentChance(0.1f))
            {
                return;
            }

            await PostTrademark(message, token);
        }

        public async Task ReactionAdded(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction, CancellationToken token)
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

            if (RandomExtensions.PercentChance(0.1f))
            {
                await PostTrademark(downloadedMessage, token);
                return;
            }

            if (RandomExtensions.PercentChance(5))
            {
                await downloadedMessage.AddReactionAsync(new Emoji("🤥"), token.ToRequestOptions());
                return;
            }

            await downloadedMessage.AddReactionAsync(reaction.Emote, token.ToRequestOptions());
        }

        private async Task PostTrademark(IMessage message, CancellationToken token)
        {
            var trademark = new[]
            {
                new Emoji("🇪"),
                new Emoji("🇸"),
                new Emoji("🇹"),
                new Emoji("🇧"),
                new Emoji("🇴"),
                new Emoji("🤓")
            };

            foreach (var emoji in trademark)
            {
                await message.AddReactionAsync(emoji, token.ToRequestOptions());
            }            
        }
    }
}
