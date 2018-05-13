using System.Threading;
using System.Threading.Tasks;
using Discord;
using Google.Cloud.Translation.V2;
using Microsoft.Extensions.Logging;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public class TranslationResponder : IResponder
    {
        private readonly ILogger<EnglishTranslationResponder> logger;
        private readonly TranslationClient translation;
        private const string InvocationCommand = "!to";

        public TranslationResponder(ILogger<EnglishTranslationResponder> logger, TranslationClient translation)
        {
            this.logger = logger;
            this.translation = translation;
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (!message.Content.ToLower().StartsWith(InvocationCommand))
            {
                return;
            }

            string messageContent = message.Content.Substring(InvocationCommand.Length).Trim();

            string[] words = messageContent.Split(' ');

            string targetLanguage = words[0];
            string phrase = messageContent.Substring(targetLanguage.Length).Trim();

            using (message.Channel.EnterTypingState(token.ToRequestOptions()))
            {
                var translated = await translation.TranslateTextAsync(phrase, targetLanguage, null, cancellationToken: token);
                if (translated.TranslatedText == translated.OriginalText)
                {
                    return;
                }

                string responseMessage = $"Translated \"{translated.OriginalText}\" from {translated.DetectedSourceLanguage.ToUpper()}```{translated.TranslatedText}```";
                await message.Channel.SendMessageAsync(responseMessage, options: token.ToRequestOptions());
            }
        }
    }
}
