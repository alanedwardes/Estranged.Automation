using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Estranged.Automation.Runner.Discord.Events;
using Microsoft.Extensions.Logging;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public sealed class TwitchResponder : IResponder
    {
        private class TwitchEmoji
        {
            public Uri Uri { get; set; }
            public string Name { get; set; }
        }

        private readonly Lazy<Task<TwitchEmoji[]>> emojiTask;
        private readonly ILogger<TwitchResponder> _logger;

        public TwitchResponder(ILogger<TwitchResponder> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;

            emojiTask = new Lazy<Task<TwitchEmoji[]>>(async () =>
            {
                using var httpClient = httpClientFactory.CreateClient(DiscordHttpClientConstants.RESPONDER_CLIENT);

                var helpPageResponse = await httpClient.GetAsync("https://www.twitch.tv/creatorcamp/en/paths/getting-started-on-twitch/emotes/");

                helpPageResponse.EnsureSuccessStatusCode();

                var helpPageResponseContent = await helpPageResponse.Content.ReadAsStringAsync();

                var matches = Regex.Matches(helpPageResponseContent, "(?<url>/creatorcamp/assets/uploads/(?<name>.*?).png)");

                if (!matches.Any())
                {
                    _logger.LogWarning("Unable to find emoji matches, got the following response: {Content}", helpPageResponseContent.Length > 512 ? helpPageResponseContent[..512] : helpPageResponseContent);
                }

                return matches.Select(x => new TwitchEmoji
                {
                    Name = x.Groups["name"].Value,
                    Uri = new Uri(new Uri("https://www.twitch.tv/"), x.Groups["url"].Value)
                }).ToArray();
            });
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (message.Channel.IsProtectedChannel())
            {
                return;
            }

            if (RandomExtensions.PercentChance(95))
            {
                return;
            }

            var emoji = (await emojiTask.Value).OrderBy(x => Guid.NewGuid()).First();

            using (message.Channel.EnterTypingState())
            {
                await Task.Delay(TimeSpan.FromSeconds(1), token);
                await message.Channel.SendMessageAsync(emoji.Uri.ToString(), options: token.ToRequestOptions());
            }
        }
    }
}
