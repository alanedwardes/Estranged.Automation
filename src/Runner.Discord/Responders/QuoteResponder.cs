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

            string quoteContent = quotedMessage.Content;

            foreach (var embed in quotedMessage.Embeds)
            {
                if (string.IsNullOrWhiteSpace(quoteContent))
                {
                    quoteContent = embed.ToString();
                }
                else
                {
                    quoteContent = quoteContent + "\n" + embed.ToString();
                }

                foreach (var field in embed.Fields)
                {
                    if (string.IsNullOrWhiteSpace(quoteContent))
                    {
                        quoteContent = field.ToString();
                    }
                    else
                    {
                        quoteContent = quoteContent + "\n" + field.ToString();
                    }
                }
            }

            var guildChannel = (IGuildChannel)quotedMessage.Channel;

            var builder = new EmbedBuilder()
                .WithTimestamp(quotedMessage.CreatedAt)
                .WithAuthor(quotedMessage.Author)
                .WithUrl(message.Content)
                .WithDescription(quoteContent)
                .WithFooter($"Quoted by {message.Author.Username}, originally posted in #{channel.Name}");

            var deleteTask = message.DeleteAsync(token.ToRequestOptions());
            var sendMessageTask = message.Channel.SendMessageAsync(string.Empty, false, builder.Build(), token.ToRequestOptions());

            await deleteTask;
            await sendMessageTask;
        }
    }
}
