using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
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
        public QuoteResponder(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        private const string ActivationPhrase = "!quote";
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
                var regularFontTask = httpClient.GetStreamAsync("https://github.com/google/fonts/raw/master/apache/opensans/OpenSans-Regular.ttf");
                var boldFontTask = httpClient.GetStreamAsync("https://github.com/google/fonts/raw/master/apache/opensans/OpenSans-Bold.ttf");
                regularFontFamily = fontCollection.Install(await regularFontTask);
                boldFontFamily = fontCollection.Install(await boldFontTask);
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
                x.DrawText(quotedMessage.Content, boldFont, new Rgba32(userColor.R, userColor.G, userColor.G), new PointF(5f, 5f));
                x.DrawText(quotedMessage.Content, boldFont, new Rgba32(userColor.R, userColor.G, userColor.G), new PointF(usernameSize.Width + 5f, 5f));
            });

            var ms = new MemoryStream();
            image.SaveAsPng(ms);

            await message.Channel.SendFileAsync(ms, messageId + ".png");
        }
    }
}
