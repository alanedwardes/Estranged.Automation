using System.Threading;
using System.Threading.Tasks;
using Discord;
using Google.Cloud.Language.V1;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public class NaturalLanguageResponder : IResponder
    {
        private readonly LanguageServiceClient languageServiceClient;

        public NaturalLanguageResponder(LanguageServiceClient languageServiceClient)
        {
            this.languageServiceClient = languageServiceClient;
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            var sentiment = await languageServiceClient.AnalyzeSentimentAsync(new AnalyzeSentimentRequest
            {
                Document = Document.FromPlainText(message.Content),
                EncodingType = EncodingType.Utf8
            });

            if (sentiment.DocumentSentiment.Score < 0f)
            {
                await message.Channel.SendMessageAsync("Don't be negative!", options: token.ToRequestOptions());
            }
        }
    }
}
