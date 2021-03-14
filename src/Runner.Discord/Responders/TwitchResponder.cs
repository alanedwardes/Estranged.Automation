using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public sealed class TwitchResponder : IResponder
    {
        private class TwitchEmoji
        {
            public Uri Uri { get; set; }
            public string Name { get; set; }
        }

        private Lazy<Task<TwitchEmoji[]>> emojiTask;

        public TwitchResponder(HttpClient httpClient)
        {
            emojiTask = new Lazy<Task<TwitchEmoji[]>>(async () =>
            {
                var emojiList = await httpClient.GetStringAsync("https://www.twitch.tv/creatorcamp/en/learn-the-basics/emotes/");

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
            var randomFloat = RandomNumberGenerator.GetInt32(0, int.MaxValue) / (double)int.MaxValue;
            if (randomFloat < 0.75)
            {
                return;
            }

            var emoji = (await emojiTask.Value).OrderBy(x => Guid.NewGuid()).First();

            await message.Channel.SendMessageAsync(emoji.Uri.ToString());
        }
    }
}
