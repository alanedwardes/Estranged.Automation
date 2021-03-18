using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Estranged.Automation.Shared;
using Microsoft.Extensions.Logging;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public class PullTheLeverResponder : IResponder
    {
        private readonly string[] normalEmoji = new[]
        {
            "grapes", "cherries", "tangerine",
            "lemon", "star", "gem", "moneybag"
        };

        private readonly string[] allShibEmoji = new[]
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

        private readonly string[] easyShibEmoji = new[]
        {
            "<:shibdazzled:436284167953842176>", "<:shibfacetouch:436284167987265542>",
            "<:shibsmile:436284168004042766>", "<:shibbandit:436284164896063489>",
            "<:shibhide:436284167802716171>", "<:shibshocked:436284168213626891>",
            "<:shibchill:436284167051804673>"
        };
        private readonly ILogger<PullTheLeverResponder> _logger;
        private readonly IRateLimitingRepository _rateLimiting;

        public PullTheLeverResponder(ILogger<PullTheLeverResponder> logger, IRateLimitingRepository rateLimiting)
        {
            _logger = logger;
            _rateLimiting = rateLimiting;
        }

        private string RandomEmoji(string[] icons) => icons.OrderBy(x => Guid.NewGuid()).First();

        private const int LimitPerUserPerHour = 2;

        private async Task<bool> CheckWithinRateLimit(IUser user)
        {
            if (!await _rateLimiting.IsWithinLimit(nameof(PullTheLeverResponder) + user.Id + DateTime.UtcNow.ToString("MM-dd-yyyy-H"), LimitPerUserPerHour) && user.Id != 266644379576434688)
            {
                await user.SendMessageAsync($"Sorry, you've exceeded the maximum allowed pull the lever requests this hour ({LimitPerUserPerHour})");
                _logger.LogWarning($"Rate limiting {user.Username} for pull the lever");
                return false;
            }

            return true;
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            string messageContent = message.Content.ToLower();

            if (message.Channel.Name != "bots")
            {
                return;
            }

            if (messageContent.Contains("pull") && messageContent.Contains("the") && messageContent.Contains("lever") && await CheckWithinRateLimit(message.Author))
            {
                await message.Channel.SendMessageAsync($":{RandomEmoji(normalEmoji)}::{RandomEmoji(normalEmoji)}::{RandomEmoji(normalEmoji)}:", options: token.ToRequestOptions());
                return;
            }

            if (messageContent.Contains("pull the shib hard") && await CheckWithinRateLimit(message.Author))
            {
                await message.Channel.SendMessageAsync($"{RandomEmoji(allShibEmoji)}{RandomEmoji(allShibEmoji)}{RandomEmoji(allShibEmoji)}", options: token.ToRequestOptions());
                return;
            }

            if (messageContent.Contains("pull the shib") && await CheckWithinRateLimit(message.Author))
            {
                await message.Channel.SendMessageAsync($"{RandomEmoji(easyShibEmoji)}{RandomEmoji(easyShibEmoji)}{RandomEmoji(easyShibEmoji)}", options: token.ToRequestOptions());
                return;
            }
        }
    }
}