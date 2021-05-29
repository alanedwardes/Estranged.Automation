using Discord;
using Estranged.Automation.Runner.Discord.Events;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public class EmojiReactionResponder : IResponder
    {
        private static readonly string[] EMOJI = new[] 
        {
            "😂", "😭", "🥺", "❤️", "🤣", "✨", "😍", "🙏", "🥰", "😊"
        };
        private readonly IDiscordClient _discordClient;

        public EmojiReactionResponder(IDiscordClient discordClient) => _discordClient = discordClient;

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (RandomExtensions.PercentChance(5))
            {
                await message.AddReactionAsync(new Emoji(EMOJI.OrderBy(x => Guid.NewGuid()).First()));
                return;
            }

            if (RandomExtensions.PercentChance(1))
            {
                var guild = await _discordClient.GetGuildAsync(368117880547573760, options: token.ToRequestOptions());
                await message.AddReactionAsync(guild.Emotes.OrderBy(x => Guid.NewGuid()).First(), options: token.ToRequestOptions());
            }
        }
    }
}
