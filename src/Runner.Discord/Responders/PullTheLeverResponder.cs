using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public class PullTheLeverResponder : IResponder
    {
        private readonly string[] emoji = new[]
        {
            "grapes", "cherries", "tangerine",
            "lemon", "star", "gem", "moneybag"
        };

        private string GetIcon() => ':' + emoji.OrderBy(x => Guid.NewGuid()).First() + ':';

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (!message.Content.ToLower().Contains("pull the lever"))
            {
                return;
            }

            await message.Channel.SendMessageAsync($"{GetIcon()}{GetIcon()}{GetIcon()}", options: token.ToRequestOptions());
        }
    }
}
