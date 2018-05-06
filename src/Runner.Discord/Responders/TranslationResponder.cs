using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Translate;
using Amazon.Translate.Model;
using Discord;
using Microsoft.Extensions.Logging;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public class TranslationResponder : IResponder
    {
        private readonly ILogger<TranslationResponder> logger;
        private readonly IAmazonTranslate translation;
        private const string InvocationCommand = "!translate";

        public TranslationResponder(ILogger<TranslationResponder> logger, IAmazonTranslate translation)
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

            using (message.Channel.EnterTypingState(token.ToRequestOptions()))
            {
                var translated = await translation.TranslateTextAsync(new TranslateTextRequest
                {
                    SourceLanguageCode = "auto",
                    TargetLanguageCode = "en",
                    Text = messageContent
                });
                if (translated.TranslatedText == messageContent)
                {
                    return;
                }

                string responseMessage = $"Translated \"{messageContent}\" from {translated.SourceLanguageCode.ToUpper()}```{translated.TranslatedText}```";
                await message.Channel.SendMessageAsync(responseMessage, options: token.ToRequestOptions());
            }
        }
    }
}
