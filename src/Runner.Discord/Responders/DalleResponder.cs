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
        private readonly IFeatureFlags _featureFlags;

        public DalleResponder(OpenAIAPI openAi, IHttpClientFactory httpClientFactory, IFeatureFlags featureFlags)
        {
            _openAi = openAi;
            _httpClientFactory = httpClientFactory;
            _featureFlags = featureFlags;
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (message.Channel.IsPublicChannel() || !_featureFlags.IsAiEnabled)
            {
                return;
            }

            const string trigger = "dalle";
            if (!message.Content.StartsWith(trigger, StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            if (_featureFlags.ShouldResetDalleAttempts())
            {
                // Refresh the bucket since time moved on
                _featureFlags.ResetDalleAttempts();
            }

            int dalleLimit = 10;

            if (_featureFlags.DalleAttempts.Count >= dalleLimit)
            {
                await message.Channel.SendMessageAsync("wait until the next day", options: token.ToRequestOptions());
                return;
            }

            var prompt = message.Content[trigger.Length..].Trim();

            _featureFlags.DalleAttempts.Count++;

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

                await message.Channel.SendFileAsync(image, $"{prompt}.png", $"{_featureFlags.DalleAttempts.Count}/{dalleLimit}", messageReference: message.Reference, options: token.ToRequestOptions());
            }
        }
    }
}
