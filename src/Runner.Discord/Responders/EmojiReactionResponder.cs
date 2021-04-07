using Discord;
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

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (RandomExtensions.PercentChance(5))
            {
                await message.AddReactionAsync(new Emoji(EMOJI.OrderBy(x => Guid.NewGuid()).First()));
            }
        }
    }
}
