using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Google.Cloud.Translation.V2;
using Microsoft.Extensions.Logging;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public class EnglishTranslationResponder : IResponder
    {
        private readonly ILogger<EnglishTranslationResponder> logger;
        private readonly TranslationClient translation;
        private const string InvocationCommand = "!translate";

        public EnglishTranslationResponder(ILogger<EnglishTranslationResponder> logger, TranslationClient translation)
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

            string messageContent = message.Content.Replace(InvocationCommand, string.Empty).Trim();

            if (Uri.TryCreate(messageContent, UriKind.Absolute, out var uri))
            {
                logger.LogInformation("Ignoring message {0} due to it being a URL", message.Content);
                return;
            }

            var detection = await translation.DetectLanguageAsync(messageContent, token);
            if (detection.Language == "en" || detection.Language == "und")
            {
                logger.LogInformation("Ignoring message {0} due to it being in {1}", message.Content, detection.Language);
                return;
            }

            logger.LogInformation("Message is written in {0} with {1} confidence", detection.Language, detection.Confidence);

            using (message.Channel.EnterTypingState(token.ToRequestOptions()))
            {
                var translated = await translation.TranslateTextAsync(messageContent, "en", detection.Language, cancellationToken: token);
                if (translated.TranslatedText == translated.OriginalText)
                {
                    return;
                }

                string responseMessage = $"Translated \"{translated.OriginalText}\" from {translated.SpecifiedSourceLanguage.ToUpper()}```{translated.TranslatedText}```";
                await message.Channel.SendMessageAsync(responseMessage, options: token.ToRequestOptions());
            }
        }
    }
}
