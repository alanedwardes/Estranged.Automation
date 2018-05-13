﻿using System;
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
        private readonly ILogger<EnglishTranslationResponder> logger;
        private readonly TranslationClient translation;
        private readonly IEnumerable<string> InvocationCommands = new[] { "!to", "!до", "!au" };

        public TranslationResponder(ILogger<EnglishTranslationResponder> logger, TranslationClient translation)
        {
            this.logger = logger;
            this.translation = translation;
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
                var translated = await translation.TranslateTextAsync(phrase, target, null, cancellationToken: token);
                if (translated.TranslatedText == translated.OriginalText)
                {
                    return;
                }

                string responseMessage = $"Translated \"{translated.OriginalText}\" from {translated.DetectedSourceLanguage.ToUpper()} to {translated.TargetLanguage.ToUpper()}```{translated.TranslatedText}```";
                await message.Channel.SendMessageAsync(responseMessage, options: token.ToRequestOptions());
            }
        }
    }
}
