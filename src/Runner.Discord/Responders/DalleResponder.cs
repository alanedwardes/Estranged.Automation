using Discord;
using Estranged.Automation.Runner.Discord.Events;
using OpenAI;
using OpenAI.Images;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Responders
{
    internal sealed class DalleResponder : IResponder
    {
        private readonly OpenAIClient _openAiClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IFeatureFlags _featureFlags;

        public DalleResponder(OpenAIClient openAiClient, IHttpClientFactory httpClientFactory, IFeatureFlags featureFlags)
        {
            _openAiClient = openAiClient;
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
                await RequestImage(message, _featureFlags.DalleAttempts.Count, dalle2Limit, dalle2Trigger.Length, "dall-e-2", GeneratedImageSize.W256xH256, token);
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
                await RequestImage(message, _featureFlags.DalleHqAttempts.Count, dalle3Limit, dalle3Trigger.Length, "dall-e-3", GeneratedImageSize.W1024xH1024, token);
                return;
            }
        }

        private async Task RequestImage(IMessage message, int dalleAttempts, int dalleLimit, int initialMessagePrefixLength, string model, GeneratedImageSize size, CancellationToken token)
        {
            var prompt = message.Content[initialMessagePrefixLength..].Trim();

            using (message.Channel.EnterTypingState())
            {
                var imageClient = _openAiClient.GetImageClient(model);

                var response = await imageClient.GenerateImageAsync(prompt, new ImageGenerationOptions
                {
                    Size = size,
                    ResponseFormat = GeneratedImageFormat.Uri
                });

                var result = response.Value;

                using var httpClient = _httpClientFactory.CreateClient(DiscordHttpClientConstants.RESPONDER_CLIENT);
                using var image = await httpClient.GetStreamAsync(result.ImageUri);

                await message.Channel.SendFileAsync(image, $"{Guid.NewGuid()}.png", $"{dalleAttempts}/{dalleLimit}", messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
            }
        }
    }
}
