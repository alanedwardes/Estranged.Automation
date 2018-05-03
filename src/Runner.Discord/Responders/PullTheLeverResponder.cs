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
            "<:shibalert:436284164073979915>", "<:shibbandit:436284164896063489>",
            "<:shibbark:436284167307788299>", "<:shibbone:436284167471366156>",
            "<:shibbowl:436284167861436417>", "<:shibchatter:436284167760773137>",
            "<:shibchill:436284167051804673>", "<:shibcry:436284167664304129>",
            "<:shibdazzled:436284167953842176>", "<:shibdepressed:436284167773224972>",
            "<:shibdrop:436284167832207411>", "<:shibfacetouch:436284167987265542>",
            "<:shibfleas:436284167609909268>", "<:shibhide:436284167802716171>",
            "<:shibhuh:436284167462977537>", "<:shiblick:436284167974813706>",
            "<:shibmoist:436284167966294018>", "<:shibnosedrip:436284167811235851>",
            "<:shibpant:436284167488274439>", "<:shibpaw:436284168242987038>",
            "<:shibpeer:436284167878213634>", "<:shibpluck:436284168402632716>",
            "<:shibpress:436284168377204737>", "<:shibroar:436284167802716160>",
            "<:shibshake:436284167983202324>", "<:shibshocked:436284168213626891>",
            "<:shibsigh:436284168184397824>", "<:shibsmallsmile:436284168117157889>",
            "<:shibsmile:436284168004042766>", "<:shibsnarl:436284168482193446>",
            "<:shibspit:436284168020688909>", "<:shibstretch:436284168205238273>",
            "<:shibtail:436284168498839572>", "<:shibtug:436284168461090816>",
            "<:shibuh:436284168108900362>", "<:shibunimpressed:436284167735476244>",
            "<:shibwail:436284167433617409>", "<:shibwha:436284168385724425>",
            "<:shibwhine:436284167999848458>"
        };

        private string RandomEmoji(string[] icons) => icons.OrderBy(x => Guid.NewGuid()).First();

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (message.Content.ToLower().Contains("pull the lever"))
            {
                await message.Channel.SendMessageAsync($":{RandomEmoji(normalEmoji)}::{RandomEmoji(normalEmoji)}::{RandomEmoji(normalEmoji)}:", options: token.ToRequestOptions());
            }

            if (message.Content.ToLower().Contains("pull the shib"))
            {
                await message.Channel.SendMessageAsync($"{RandomEmoji(shibEmoji)}{RandomEmoji(shibEmoji)}{RandomEmoji(shibEmoji)}", options: token.ToRequestOptions());
            }
        }
    }
}
