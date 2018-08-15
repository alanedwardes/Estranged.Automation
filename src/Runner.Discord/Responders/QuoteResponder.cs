using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using Microsoft.Extensions.Logging;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public class QuoteResponder : IResponder
    {
        public QuoteResponder(ILogger<QuoteResponder> logger, HttpClient httpClient, IDiscordClient discordClient)
        {
            this.logger = logger;
            this.httpClient = httpClient;
            this.discordClient = discordClient;
        }

        private readonly ILogger<QuoteResponder> logger;
        private readonly HttpClient httpClient;
        private readonly IDiscordClient discordClient;

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            const string MessageLinkRegex = "https://discordapp.com/channels/(?<guildId>[0-9]*)/(?<channelId>[0-9]*)/(?<messageId>[0-9]*)";

            var regex = new Regex(MessageLinkRegex);

            var match = regex.Match(message.Content);
            if (!match.Success)
            {
                return;
            }

            var guildId = ulong.Parse(match.Groups["guildId"].Value);
            var channelId = ulong.Parse(match.Groups["channelId"].Value);
            var messageId = ulong.Parse(match.Groups["messageId"].Value);

            var guild = await discordClient.GetGuildAsync(guildId, options: token.ToRequestOptions());
            var channel = await guild.GetTextChannelAsync(channelId, options: token.ToRequestOptions());
            var quotedMessage = await channel.GetMessageAsync(messageId, options: token.ToRequestOptions());

            var guildChannel = (IGuildChannel)quotedMessage.Channel;

            var builder = new EmbedBuilder()
                .WithTimestamp(quotedMessage.CreatedAt)
                .WithAuthor(quotedMessage.Author)
                .WithUrl(message.Content)
                .WithDescription(quotedMessage.Content)
                .WithTitle($"Quote from <#{channel.Id}>");

            await message.Channel.SendMessageAsync(string.Empty, false, builder.Build(), token.ToRequestOptions());
        }
    }
}
