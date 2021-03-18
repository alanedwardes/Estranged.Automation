using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public class QuoteResponder : IResponder
    {
        public QuoteResponder(IDiscordClient discordClient)
        {
            _discordClient = discordClient;
        }

        private readonly IDiscordClient _discordClient;

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            const string MessageLinkRegex = "/channels/(?<guildId>[0-9]*)/(?<channelId>[0-9]*)/(?<messageId>[0-9]*)";

            var regex = new Regex(MessageLinkRegex);

            var match = regex.Match(message.Content);
            if (!match.Success)
            {
                return;
            }

            var guildId = ulong.Parse(match.Groups["guildId"].Value);
            var channelId = ulong.Parse(match.Groups["channelId"].Value);
            var messageId = ulong.Parse(match.Groups["messageId"].Value);

            var guild = await _discordClient.GetGuildAsync(guildId, options: token.ToRequestOptions());
            var channel = (IMessageChannel)await guild.GetChannelAsync(channelId, options: token.ToRequestOptions());
            var quotedMessage = await channel.GetMessageAsync(messageId, options: token.ToRequestOptions());

            if (message.Channel.Id != quotedMessage.Channel.Id && !quotedMessage.Channel.IsPublicChannel())
            {
                return;
            }

            await message.Channel.SendMessageAsync(string.Empty, false, quotedMessage.QuoteMessage(message.Author), token.ToRequestOptions());
        }
    }
}
