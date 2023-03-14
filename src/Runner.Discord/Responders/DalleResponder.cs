using Discord;
using Estranged.Automation.Runner.Discord.Events;
using OpenAI_API;
using OpenAI_API.Images;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Responders
{
    internal sealed class DalleResponder : IResponder
    {
        private readonly OpenAIAPI _openAi;
        private readonly IHttpClientFactory _httpClientFactory;

        public DalleResponder(OpenAIAPI openAi, IHttpClientFactory httpClientFactory)
        {
            _openAi = openAi;
            _httpClientFactory = httpClientFactory;
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (message.Channel.IsPublicChannel() || !FeatureFlagResponder.IsAiEnabled)
            {
                return;
            }

            const string trigger = "dalle";
            if (!message.Content.StartsWith(trigger, StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            var prompt = message.Content[trigger.Length..].Trim();

            if (FeatureFlagResponder.ShouldResetDalleAttempts())
            {
                // Refresh the bucket since time moved on
                FeatureFlagResponder.ResetDalleAttempts();
            }

            if (FeatureFlagResponder.DalleAttempts.Count >= 10)
            {
                await message.Channel.SendMessageAsync("wait until the next day", options: token.ToRequestOptions());
                return;
            }

            FeatureFlagResponder.DalleAttempts.Count++;

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

                using var httpClient = _httpClientFactory.CreateClient(DiscordHttpClientConstants.RESPONDER_CLIENT);
                using var image = await httpClient.GetStreamAsync(result.Url);

                await message.Channel.SendFileAsync(image, $"{prompt}.png", options: token.ToRequestOptions());
            }
        }
    }
}
