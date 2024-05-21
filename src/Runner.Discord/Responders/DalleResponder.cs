using Discord;
using Estranged.Automation.Runner.Discord.Events;
using OpenAI_API;
using OpenAI_API.Images;
using OpenAI_API.Models;
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

            if (_featureFlags.ShouldResetDalleAttempts())
            {
                // Refresh the bucket since time moved on
                _featureFlags.ResetDalleAttempts();
            }

            if (_featureFlags.ShouldResetDalleHqAttempts())
            {
                // Refresh the bucket since time moved on
                _featureFlags.ResetDalleHqAttempts();
            }

            const string dalle2Trigger = "dalle ";
            if (message.Content.StartsWith(dalle2Trigger, StringComparison.InvariantCultureIgnoreCase))
            {
                const int dalle2Limit = 10;
                if (_featureFlags.DalleAttempts.Count >= dalle2Limit)
                {
                    await message.Channel.SendMessageAsync($"wait until the next period (current {_featureFlags.DalleAttempts.Bucket})", options: token.ToRequestOptions());
                    return;
                }

                _featureFlags.DalleAttempts.Count++;
                await RequestImage(message, _featureFlags.DalleAttempts.Count, dalle2Limit, dalle2Trigger.Length, Model.DALLE2, ImageSize._256, token);
                return;
            }

            const string dalle3Trigger = "dalle3 ";
            if (message.Content.StartsWith(dalle3Trigger, StringComparison.InvariantCultureIgnoreCase))
            {
                const int dalle3Limit = 2;
                if (_featureFlags.DalleHqAttempts.Count >= dalle3Limit)
                {
                    await message.Channel.SendMessageAsync($"wait until the next period (current {_featureFlags.DalleHqAttempts.Bucket})", options: token.ToRequestOptions());
                    return;
                }

                _featureFlags.DalleHqAttempts.Count++;
                await RequestImage(message, _featureFlags.DalleHqAttempts.Count, dalle3Limit, dalle3Trigger.Length, Model.DALLE3, ImageSize._1024, token);
                return;
            }
        }

        private async Task RequestImage(IMessage message, int dalleAttempts, int dalleLimit, int initialMessagePrefixLength, Model model, ImageSize size, CancellationToken token)
        {
            var prompt = message.Content[initialMessagePrefixLength..].Trim();

            using (message.Channel.EnterTypingState())
            {
                var response = await _openAi.ImageGenerations.CreateImageAsync(new ImageGenerationRequest
                {
                    Size = size,
                    Model = model,
                    NumOfImages = 1,
                    ResponseFormat = ImageResponseFormat.Url,
                    Prompt = prompt
                });

                var result = response.Data.Single();

                using var httpClient = _httpClientFactory.CreateClient(DiscordHttpClientConstants.RESPONDER_CLIENT);
                using var image = await httpClient.GetStreamAsync(result.Url);

                await message.Channel.SendFileAsync(image, $"{Guid.NewGuid()}.png", $"{dalleAttempts}/{dalleLimit}", messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
            }
        }
    }
}
