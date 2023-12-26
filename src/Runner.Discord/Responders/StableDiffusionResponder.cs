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
        private string _negativePrompt = string.Empty;

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

            const string negativePromptTrigger = "sdn";
            if (message.Content.StartsWith(negativePromptTrigger))
            {
                _negativePrompt = message.Content[negativePromptTrigger.Length..].Trim(); ;
                await message.Channel.SendMessageAsync($"Set negative prompt to '{_negativePrompt}'", options: token.ToRequestOptions());
                return;
            }

            var random = new Random();
            var seed = random.Next(int.MaxValue);

            const string sdlTrigger = "sdxl";
            if (message.Content.StartsWith(sdlTrigger, StringComparison.InvariantCultureIgnoreCase))
            {
                using (message.Channel.EnterTypingState())
                {
                    using var imageStream = await GenerateImage(message.Content[sdlTrigger.Length..].Trim(), "sd-v1-5", seed, token);
                    await message.Channel.SendFileAsync(imageStream, $"{Guid.NewGuid()}-{seed}.jpg", messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
                    return;
                }
            }

            const string sdTrigger = "sd";
            if (message.Content.StartsWith(sdTrigger, StringComparison.InvariantCultureIgnoreCase))
            {
                using (message.Channel.EnterTypingState())
                {
                    using var imageStream = await GenerateImage(message.Content[sdTrigger.Length..].Trim(), "sd_xl_turbo_1.0_fp16", seed, token);
                    await message.Channel.SendFileAsync(imageStream, $"{Guid.NewGuid()}-{seed}.jpg", messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
                    return;
                }
            }
        }

        private sealed class StreamResponse
        {
            public StreamOutput[] Output { get; set; }
            public class StreamOutput
            {
                public string Data { get; set; }
            }
        }

        public async Task<MemoryStream> GenerateImage(string prompt, string model, int seed, CancellationToken token)
        {
            var size = 512;

            var requestPayload = new
            {
                prompt = prompt,
                seed = seed,
                used_random_seed = true,
                negative_prompt = _negativePrompt.Trim(),
                num_outputs = 1,
                num_inference_steps = 25,
                guidance_scale = 7.5,
                width = size,
                height = size,
                vram_usage_level = "low",
                sampler_name = "euler_a",
                use_stable_diffusion_model = model,
                clip_skip = false,
                use_vae_model = "",
                stream_progress_updates = true,
                stream_image_progress = false,
                show_only_filtered_image = true,
                block_nsfw = false,
                output_format = "jpeg",
                output_quality = 90,
                output_lossless = false,
                metadata_output_format = "none",
                original_prompt = prompt,
                active_tags = new string[0],
                inactive_tags = new string[0],
                enable_vae_tiling = true,
                session_id = "1703414464336"
            };

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(Environment.GetEnvironmentVariable("EASY_DIFFUSION"));
            httpClient.Timeout = TimeSpan.FromMinutes(5);

            var response = await httpClient.PostAsJsonAsync("/render", requestPayload, token);

            var job = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(token);

            var streamUrl = ((JsonElement)job["stream"]).GetString();

            using var timeoutCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCancellation.Token);

            while (true)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), linkedCancellation.Token);

                StreamResponse stream;
                try
                {
                    stream = await httpClient.GetFromJsonAsync<StreamResponse>(streamUrl, linkedCancellation.Token);
                }
                catch (JsonException)
                {
                    continue;
                }
                catch (HttpRequestException)
                {
                    continue;
                }

                if (stream.Output == null || stream.Output.Length == 0)
                {
                    continue;
                }

                var matchGroups = Regex.Match(stream.Output[0].Data, @"^data:((?<type>[\w\/]+))?;base64,(?<data>.+)$").Groups;
                var base64Data = matchGroups["data"].Value;
                return new MemoryStream(Convert.FromBase64String(base64Data));
            }
        }
    }
}
