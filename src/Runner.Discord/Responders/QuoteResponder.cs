using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
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
        public QuoteResponder(ILogger<QuoteResponder> logger, HttpClient httpClient)
        {
            this.logger = logger;
            this.httpClient = httpClient;
        }

        private const string ActivationPhrase = "!quote";
        private readonly ILogger<QuoteResponder> logger;
        private readonly HttpClient httpClient;
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
                var boldFontTask = httpClient.GetStreamAsync("https://github.com/google/fonts/raw/master/apache/opensans/OpenSans-Bold.ttf");
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

            var userColor = Color.LighterGrey;//guildUser.Roles.LastOrDefault()?.Color ?? Color.LighterGrey;

            var regularFont = new Font(regularFontFamily, 18f);
            var boldFont = new Font(boldFontFamily, 18f);

            var usernameSize = TextMeasurer.Measure(quotedMessage.Author.Username, new RendererOptions(boldFont));
            var messageSize = TextMeasurer.Measure(quotedMessage.Content, new RendererOptions(regularFont));

            var image = new Image<Rgba32>((int)(usernameSize.Width + messageSize.Width) + 15, (int)Math.Max(usernameSize.Height, messageSize.Height) + 10);
            image.Mutate(x => {
                x.Fill(new Rgba32(51, 51, 51));
                x.DrawText(quotedMessage.Author.Username, boldFont, new Rgba32(userColor.R, userColor.G, userColor.G), new PointF(5f, 5f));
                x.DrawText(quotedMessage.Content, regularFont, new Rgba32(userColor.R, userColor.G, userColor.G), new PointF(usernameSize.Width + 5f, 5f));
            });

            var ms = new MemoryStream();
            image.SaveAsPng(ms);
            ms.Seek(0, SeekOrigin.Begin);

            await message.Channel.SendFileAsync(ms, messageId + ".png");
        }
    }
}
