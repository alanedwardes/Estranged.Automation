using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public class PullTheLeverResponder : IResponder
    {
        private readonly string[] normalEmoji = new[]
        {
            "grapes", "cherries", "tangerine",
            "lemon", "star", "gem", "moneybag"
        };

        private readonly string[] shibEmoji = new[]
        {
            "shibalert", "shibbandit", "shibbark", "shibbone",
            "shibbowl", "shibchatter", "shibchill", "shibcry",
            "shibdazzled", "shibdepressed", "shibdrop",
            "shibfacetouch", "shibfleas", "shibhide", "shibhuh",
            "shiblick", "shibmoist", "shibnosedrip", "shibpant",
            "shibpaw", "shibpeer", "shibpluck", "shibpress",
            "shibroar", "shibshake", "shibshocked", "shibsigh",
            "shibsmallsmile", "shibsmile", "shibsnarl", "shibspit",
            "shibstretch", "shibtail", "shibtug", "shibuh",
            "shibunimpressed", "shibwail", "shibwha", "shibwhine"
        };

        private string RandomEmoji(string[] icons) => ':' + icons.OrderBy(x => Guid.NewGuid()).First() + ':';

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (message.Content.ToLower().Contains("pull the lever"))
            {
                await message.Channel.SendMessageAsync($"{RandomEmoji(normalEmoji)}{RandomEmoji(normalEmoji)}{RandomEmoji(normalEmoji)}", options: token.ToRequestOptions());
            }

            if (message.Content.ToLower().Contains("pull the shib"))
            {
                await message.Channel.SendMessageAsync($"{RandomEmoji(shibEmoji)}{RandomEmoji(shibEmoji)}{RandomEmoji(shibEmoji)}", options: token.ToRequestOptions());
            }
        }
    }
}
