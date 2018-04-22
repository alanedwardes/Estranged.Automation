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
        private uint numberOfTranslations;
        private const uint MaximumTranslations = 512;

        public TranslationResponder(TranslationClient translation)
        {
            this.translation = translation;
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (numberOfTranslations >= MaximumTranslations)
            {
                return;
            }

            numberOfTranslations++;

            var response = await translation.TranslateTextAsync(message.Content, "en", cancellationToken: token);
            if (response.DetectedSourceLanguage == "en")
            {
                return;
            }

            await message.Channel.SendMessageAsync(response.TranslatedText, options: token.ToRequestOptions());
        }
    }
}
