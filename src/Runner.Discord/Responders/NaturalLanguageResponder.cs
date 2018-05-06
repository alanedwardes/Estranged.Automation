using System.Linq;
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

        public async Task ProcessClassification(IMessage message, CancellationToken token)
        {
            const string command = "!classify";

            if (!message.Content.StartsWith(command))
            {
                return;
            }

            using (message.Channel.EnterTypingState())
            {
                var classification = await languageServiceClient.ClassifyTextAsync(Document.FromPlainText(message.Content.Substring(command.Length)), token);
                await message.Channel.SendMessageAsync($"Categories: {string.Join(", ", classification.Categories.Select(x => x.Name))}");
            }
        }

        public async Task ProcessSentiment(IMessage message, CancellationToken token)
        {
            const string command = "!sentiment";

            if (!message.Content.StartsWith(command))
            {
                return;
            }

            using (message.Channel.EnterTypingState())
            {
                var sentiment = await languageServiceClient.AnalyzeSentimentAsync(Document.FromPlainText(message.Content.Substring(command.Length)), token);
                await message.Channel.SendMessageAsync($"Sentiment score of {sentiment.DocumentSentiment.Score} with magnitude of {sentiment.DocumentSentiment.Magnitude}");
            }
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            await Task.WhenAll(
                ProcessClassification(message, token),
                ProcessSentiment(message, token)
            );
        }
    }
}
