using Discord;
using Estranged.Automation.Runner.Discord.Events;
using OpenAI_API;
using OpenAI_API.Images;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Responders
{
    internal sealed class OpenAiResponder : IResponder
    {
        private class AttemptsBucket
        {
            public AttemptsBucket() => Hour = CurrentHour;

            public int Count;
            public DateTime Hour;
        }

        private readonly OpenAIAPI _openAi;

        public OpenAiResponder(OpenAIAPI openAi)
        {
            _openAi = openAi;
        }

        private static DateTime CurrentHour => DateTime.UtcNow.AddMinutes(-DateTime.UtcNow.Minute);

        private AttemptsBucket Attempts = new AttemptsBucket();

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            const string trigger = "dalle";
            if (!message.Content.StartsWith(trigger, StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            if (Attempts.Hour != CurrentHour)
            {
                // Refresh the bucket since time moved on
                Attempts = new AttemptsBucket();
            }

            if (Attempts.Count >= 10)
            {
                await message.Channel.SendMessageAsync("wait until the top of the hour", options: token.ToRequestOptions());
                return;
            }

            Attempts.Count++;

            var prompt = message.Content[trigger.Length..].Trim();

            using (message.Channel.EnterTypingState())
            {
                var response = await _openAi.ImageGenerations.CreateImageAsync(new ImageGenerationRequest
                {
                    Size = ImageSize._256,
                    NumOfImages = 1,
                    ResponseFormat = ImageResponseFormat.Url,
                    Prompt = prompt
                });

                var result = response.Data.Single();

                await message.Channel.SendMessageAsync(result.Url, options: token.ToRequestOptions());
            }
        }
    }
}
