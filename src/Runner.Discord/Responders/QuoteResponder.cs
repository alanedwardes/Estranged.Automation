using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.Logging;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Drawing;
using SixLabors.ImageSharp.Processing.Text;
using SixLabors.Primitives;

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

        private const string ActivationPhrase = "!quote";
        private readonly ILogger<QuoteResponder> logger;
        private readonly HttpClient httpClient;
        private readonly IDiscordClient discordClient;
        private FontFamily regularFontFamily;
        private FontFamily boldFontFamily;
        private readonly FontCollection fontCollection = new FontCollection();

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (!message.Content.StartsWith(ActivationPhrase))
            {
                return;
            }

            if (!fontCollection.Families.Any())
            {
                logger.LogInformation("Downloading fonts");

                var regularFontTask = httpClient.GetStreamAsync("https://github.com/google/fonts/raw/master/apache/opensans/OpenSans-Regular.ttf");
                var boldFontTask = httpClient.GetStreamAsync("https://github.com/google/fonts/raw/master/apache/opensans/OpenSans-SemiBold.ttf");
                await regularFontTask;
                await boldFontTask;

                var regularFontStream = new MemoryStream();
                var regularCopyTask = regularFontTask.Result.CopyToAsync(regularFontStream);

                var boldFontStream = new MemoryStream();
                var boldCopyTask = boldFontTask.Result.CopyToAsync(boldFontStream);

                await regularCopyTask;
                regularFontStream.Seek(0, SeekOrigin.Begin);

                await boldCopyTask;
                boldFontStream.Seek(0, SeekOrigin.Begin);

                regularFontFamily = fontCollection.Install(regularFontStream);
                boldFontFamily = fontCollection.Install(boldFontStream);
            }

            ulong messageId = ulong.Parse(message.Content.Substring(ActivationPhrase.Length).Trim());

            var quotedMessage = await message.Channel.GetMessageAsync(messageId, options: token.ToRequestOptions());

            var regularFont = new Font(regularFontFamily, 18f, FontStyle.Regular);
            var boldFont = new Font(boldFontFamily, 18f, FontStyle.Bold);

            var userName = quotedMessage.Author.Username;
            var sb = new StringBuilder();
            foreach (var line in Batch(quotedMessage.Content.Split(' '), 10))
            {
                sb.AppendLine(string.Join(" ", line));
            }
            var quotedMessageText = sb.ToString();

            var usernameSize = TextMeasurer.Measure(userName, new RendererOptions(boldFont));
            var messageSize = TextMeasurer.Measure(quotedMessageText, new RendererOptions(regularFont));

            var image = new Image<Rgba32>((int)(usernameSize.Width + messageSize.Width) + 15, (int)Math.Max(usernameSize.Height, messageSize.Height) + 10);
            image.Mutate(x => {
                x.Fill(new Rgba32(54, 57, 63));
                x.DrawText(userName, boldFont, Rgba32.White, new PointF(5f, 5f));
                x.DrawText(quotedMessageText, regularFont, Rgba32.White, new PointF(usernameSize.Width + 10f, 5f));
            });

            var ms = new MemoryStream();
            image.SaveAsPng(ms);
            ms.Seek(0, SeekOrigin.Begin);

            var guildChannel = (IGuildChannel)quotedMessage.Channel;

            await message.Channel.SendFileAsync(ms, messageId + ".png", $"https://discordapp.com/channels/{guildChannel.Guild.Id}/{guildChannel.Id}/{quotedMessage.Id}");
        }

        public static IEnumerable<IEnumerable<T>> Batch<T>(IEnumerable<T> items, int maxItems)
        {
            return items.Select((item, inx) => new { item, inx })
                        .GroupBy(x => x.inx / maxItems)
                        .Select(g => g.Select(x => x.item));
        }
    }
}
