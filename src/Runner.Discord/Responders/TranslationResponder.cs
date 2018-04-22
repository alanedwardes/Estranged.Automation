using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Google.Cloud.Translation.V2;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public class TranslationResponder : IResponder
    {
        private readonly TranslationClient translation;
        private int numberOfCharacters;
        private const int MaximumCharacters = 15000;

        public TranslationResponder(TranslationClient translation)
        {
            this.translation = translation;
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (numberOfCharacters >= MaximumCharacters)
            {
                return;
            }

            numberOfCharacters += message.Content.Length;

            var detection = await translation.DetectLanguageAsync(message.Content, token);
            if (!detection.IsReliable || detection.Language == "en")
            {
                return;
            }

            using (message.Channel.EnterTypingState(token.ToRequestOptions()))
            {
                var translated = await translation.TranslateTextAsync(message.Content, "en", detection.Language, cancellationToken: token);
                string responseMessage = $"Translated \"{translated.OriginalText}\" from {translated.SpecifiedSourceLanguage}:\n{translated.TranslatedText}";
                await message.Channel.SendMessageAsync(responseMessage, options: token.ToRequestOptions());
            }
        }
    }
}
