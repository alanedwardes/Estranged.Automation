using Discord;
using Estranged.Automation.Runner.Discord.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Responders
{
    internal sealed class StableDiffusionResponder : IResponder
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IFeatureFlags _featureFlags;
        private int _steps = 3;

        public StableDiffusionResponder(IHttpClientFactory httpClientFactory, IFeatureFlags featureFlags)
        {
            _httpClientFactory = httpClientFactory;
            _featureFlags = featureFlags;
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (message.Channel.IsPublicChannel() || !_featureFlags.IsAiEnabled)
            {
                return;
            }

            const string stepsTrigger = "sdsteps";
            if (message.Content.StartsWith(stepsTrigger))
            {
                var stepsText = message.Content[stepsTrigger.Length..].Trim();
                if (int.TryParse(stepsText, out var steps) && steps > 0)
                {
                    _steps = steps;
                    await message.Channel.SendMessageAsync($"Set steps to {_steps}", options: token.ToRequestOptions());
                }
                else
                {
                    await message.Channel.SendMessageAsync("Invalid steps value. Please provide a positive number.", options: token.ToRequestOptions());
                }
                return;
            }

            const string sdTrigger = "sd";
            if (message.Content.StartsWith(sdTrigger, StringComparison.InvariantCultureIgnoreCase))
            {
                using (message.Channel.EnterTypingState())
                {
                    using var imageStream = await GenerateImage(message.Content[sdTrigger.Length..].Trim(), token);
                    await message.Channel.SendFileAsync(imageStream, $"{Guid.NewGuid()}.png", messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
                    return;
                }
            }
        }

        public async Task<MemoryStream> GenerateImage(string prompt, CancellationToken token)
        {
            var requestPayload = new
            {
                prompt = prompt,
                steps = _steps,
                width = 512,
                height = 512,
                seed = new Random().Next(0, int.MaxValue)
            };

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(Environment.GetEnvironmentVariable("SD_API_URL"));
            httpClient.Timeout = Timeout.InfiniteTimeSpan;

            var response = await httpClient.PostAsJsonAsync("/generate", requestPayload, token);
            
            if (response.IsSuccessStatusCode)
            {
                var imageBytes = await response.Content.ReadAsByteArrayAsync(token);
                return new MemoryStream(imageBytes);
            }
            else
            {
                throw new HttpRequestException($"Image generation failed with status code: {response.StatusCode}");
            }
        }
    }
}
