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

    internal sealed class DalleResponder : IResponder
    {
        private class AttemptsBucket
        {
            public AttemptsBucket() => Bucket = CurrentBucket;

            public int Count;
            public DateTime Bucket;
        }

        private readonly OpenAIAPI _openAi;

        public DalleResponder(OpenAIAPI openAi) => _openAi = openAi;

        private static DateTime CurrentBucket
        {
            get
            {
                var now = DateTime.UtcNow;
                return new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
            }
        }

        private AttemptsBucket Attempts = new AttemptsBucket();

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (message.Channel.IsPublicChannel())
            {
                return;
            }

            const string trigger = "dalle";
            if (!message.Content.StartsWith(trigger, StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            var prompt = message.Content[trigger.Length..].Trim();

            if (Attempts.Bucket != CurrentBucket)
            {
                // Refresh the bucket since time moved on
                Attempts = new AttemptsBucket();
            }

            if (Attempts.Count >= 10)
            {
                await message.Channel.SendMessageAsync("wait until the next day", options: token.ToRequestOptions());
                return;
            }

            Attempts.Count++;

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
