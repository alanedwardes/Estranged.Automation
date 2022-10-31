using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Estranged.Automation.Runner.Discord.Events;

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

        public TwitchResponder(IHttpClientFactory httpClientFactory)
        {
            emojiTask = new Lazy<Task<TwitchEmoji[]>>(async () =>
            {
                using var httpClient = httpClientFactory.CreateClient(DiscordHttpClientConstants.RESPONDER_CLIENT);

                var emojiListResponse = await httpClient.GetAsync("https://www.twitch.tv/creatorcamp/en/learn-the-basics/emotes/");

                emojiListResponse.EnsureSuccessStatusCode();

                var emojiList = await emojiListResponse.Content.ReadAsStringAsync();

                var matches = Regex.Matches(emojiList, "src=\"(?<url>/creatorcamp/assets/uploads/(?<name>.*?).png)\"");

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
