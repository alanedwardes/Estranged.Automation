using Discord;
using Discord.WebSocket;
using Estranged.Automation.Runner.Discord.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Responders
{
    internal sealed class SpammerResponder : IResponder
    {
        private readonly IList<IMessage> _messages = new List<IMessage>();
        private readonly DiscordSocketClient _discordSocketClient;

        public SpammerResponder(DiscordSocketClient discordSocketClient) => _discordSocketClient = discordSocketClient;

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (!message.Channel.IsPublicChannel() || message.Author.IsBot)
            {
                return;
            }

            if (_messages.Count > 20)
            {
                _messages.RemoveAt(0);
            }

            _messages.Add(message);

            bool BySameAuthor(IMessage thisMessage) => thisMessage.Author.Id == message.Author.Id;

            var potentialSpamMessages = _messages.Where(BySameAuthor).Where(ContainsLink).Where(WithinLastMinute);

            // If this author posted a link in 3 different channels in the last 20 minutes
            if (potentialSpamMessages.Select(x => x.Channel.Id).Distinct().Count() >= 3)
            {
                await _discordSocketClient.GetChannelByName("moderators").SendMessageAsync($"HELP! I think <@{message.Author.Id}> is Spamming !! Lots of love xx https://alan.gdn/3eb70cea-8059-4797-b1aa-734a29e6779b.jpg");
            }
        }

        private static bool WithinLastMinute(IMessage message)
        {
            return DateTimeOffset.UtcNow - message.CreatedAt.UtcDateTime < TimeSpan.FromSeconds(60);
        }

        private static bool ContainsLink(IMessage message)
        {
            return message.Content.Contains("http://") || message.Content.Contains("https://");
        }
    }
}
