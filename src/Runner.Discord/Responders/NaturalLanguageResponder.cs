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

        public async Task ProcessSentiment(IMessage message, CancellationToken token)
        {
            if (!message.Content.StartsWith("!sentiment"))
            {
                return;
            }

            var sentiment = await languageServiceClient.AnalyzeSentimentAsync(Document.FromPlainText(message.Content), token);

            await message.Channel.SendMessageAsync($"Sentiment score of {sentiment.DocumentSentiment.Score} with magnitude of {sentiment.DocumentSentiment.Magnitude}");
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            await ProcessSentiment(message, token);
        }
    }
}
