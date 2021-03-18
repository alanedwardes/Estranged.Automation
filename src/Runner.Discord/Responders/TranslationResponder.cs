using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Google.Cloud.Translation.V2;
using Microsoft.Extensions.Logging;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public class TranslationResponder : IResponder
    {
        private readonly ILogger<EnglishTranslationResponder> _logger;
        private readonly TranslationClient _translation;
        private readonly IEnumerable<string> InvocationCommands = new[] { "!to", "!на", "!au", "!in" };

        public TranslationResponder(ILogger<EnglishTranslationResponder> logger, TranslationClient translation)
        {
            _logger = logger;
            _translation = translation;
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            string[] words = message.Content.Split(' ');
            if (!InvocationCommands.Contains(words[0], StringComparer.InvariantCultureIgnoreCase))
            {
                return;
            }

            string target = words[1].ToLower();
            string phrase = message.Content.Substring(words[0].Length).Trim().Substring(target.Length).Trim();

            using (message.Channel.EnterTypingState(token.ToRequestOptions()))
            {
                var translated = await _translation.TranslateTextAsync(phrase, target, null, cancellationToken: token);
                if (translated.TranslatedText == translated.OriginalText)
                {
                    return;
                }

                if (translated.DetectedSourceLanguage != "en")
                {
                    var english = await _translation.TranslateTextAsync(phrase, "en", translated.DetectedSourceLanguage, cancellationToken: token);
                    string response = $"Translated \"{translated.OriginalText}\" from {translated.DetectedSourceLanguage.ToUpper()}```{translated.TargetLanguage.ToUpper()}: {translated.TranslatedText}\nEN: {english.TranslatedText}```";
                    await message.Channel.SendMessageAsync(response, options: token.ToRequestOptions());
                    return;
                }

                string responseMessage = $"Translated \"{translated.OriginalText}\" from {translated.DetectedSourceLanguage.ToUpper()} to {translated.TargetLanguage.ToUpper()}```{translated.TranslatedText}```";
                await message.Channel.SendMessageAsync(responseMessage, options: token.ToRequestOptions());
            }
        }
    }
}
