using Discord;
using Estranged.Automation.Events;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Processing.Processors.Quantization;

namespace Estranged.Automation.Responders
{
    internal sealed class StableDiffusionResponder : IResponder
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IFeatureFlags _featureFlags;
        private readonly IChatClientFactory _chatClientFactory;
        private int _steps = 20;

        public StableDiffusionResponder(IHttpClientFactory httpClientFactory, IConfiguration configuration, IFeatureFlags featureFlags, IChatClientFactory chatClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _featureFlags = featureFlags;
            _chatClientFactory = chatClientFactory;
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
                var prompt = message.Content[sdTrigger.Length..].Trim();

                using (message.Channel.EnterTypingState())
                {
                    var chatClient = _chatClientFactory.CreateClient($"urn:ollama:{_configuration["OLLAMA_MODEL"]}");
                    var response = await chatClient.GetResponseAsync(new[]
                    {
                        new ChatMessage(ChatRole.System, "You are a prompt generator for Stable Diffusion. You will be given a prompt, and you must add details to it to make it better. You must only output the improved prompt, and nothing else."),
                        new ChatMessage(ChatRole.User, prompt)
                    }, new ChatOptions { AdditionalProperties = new AdditionalPropertiesDictionary { { "Think", false } } }, cancellationToken: token);

                    var enhancedPrompt = response.Text.Trim();

                    var result = await GenerateImageWithGif(enhancedPrompt, token);
                    using var gifStream = result.gifStream;
                    await message.Channel.SendFileAsync(gifStream, $"{Guid.NewGuid()}.gif", text: enhancedPrompt, messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
                    using var imageStream = result.pngStream;
                    await message.Channel.SendFileAsync(imageStream, $"{Guid.NewGuid()}.png", messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
                    return;
                }
            }
        }

        public async Task<MemoryStream> GenerateImage(string prompt, CancellationToken token)
        {
            var requestPayload = new
            {
                prompt,
                steps = _steps,
                width = 512,
                height = 512,
                seed = new Random().Next(0, int.MaxValue)
            };

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(_configuration["SD_API_URL"]);
            httpClient.Timeout = TimeSpan.FromHours(24);

            var response = await httpClient.PostAsJsonAsync("/generate", requestPayload, token);
            
            if (response.IsSuccessStatusCode)
            {
                // generated_fcf9d38b-4ef8-4805-b44e-9944bf69bfca.png
                var filename = response.RequestMessage.RequestUri.PathAndQuery.Split("/generate/")[1];
                Console.WriteLine($"Generated image filename: {filename}");

                for (int step = 0; step < requestPayload.steps; step++)
                {
                    // generated_fcf9d38b-4ef8-4805-b44e-9944bf69bfca.png_preview_0.png
                    var stepFilename = $"{filename}_preview_{step}.png";
                    await httpClient.GetByteArrayAsync($"/generate/{stepFilename}", token);
                }

                var imageBytes = await response.Content.ReadAsByteArrayAsync(token);
                return new MemoryStream(imageBytes);
            }
            else
            {
                throw new HttpRequestException($"Image generation failed with status code: {response.StatusCode}");
            }
        }      

        private async Task<(MemoryStream gifStream, MemoryStream pngStream)> GenerateImageWithGif(string prompt, CancellationToken token)
        {
            var requestPayload = new
            {
                prompt,
                steps = _steps,
                width = 512,
                height = 512,
                seed = new Random().Next(0, int.MaxValue)
            };

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(_configuration["SD_API_URL"]);
            httpClient.Timeout = TimeSpan.FromHours(24);

            var response = await httpClient.PostAsJsonAsync("/generate", requestPayload, token);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Image generation failed with status code: {response.StatusCode}");
            }

            var filename = response.RequestMessage.RequestUri.PathAndQuery.Split("/images/")[1];

            var pngBytes = await response.Content.ReadAsByteArrayAsync(token);
            var pngStream = new MemoryStream(pngBytes);

            var frameBytes = new List<byte[]>(capacity: requestPayload.steps);
            for (int step = 0; step < requestPayload.steps; step++)
            {
                var stepFilename = $"{filename}_preview_{step}.png";
                var bytes = await httpClient.GetByteArrayAsync($"/images/{stepFilename}", token);
                frameBytes.Add(bytes);
            }

            using var first = SixLabors.ImageSharp.Image.Load<Rgba32>(frameBytes[0]);

            first.Metadata.GetGifMetadata().RepeatCount = 0;

            for (int i = 1; i < frameBytes.Count; i++)
            {
                using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(frameBytes[i]);
                first.Frames.AddFrame(img.Frames.RootFrame);
            }

            foreach (var frame in first.Frames)
            {
                var meta = frame.Metadata.GetGifMetadata();
                meta.FrameDelay = 25;
                meta.DisposalMethod = GifDisposalMethod.RestoreToBackground;
            }

            var gifStream = new MemoryStream();
            await first.SaveAsGifAsync(gifStream, new GifEncoder
            {
                ColorTableMode = GifColorTableMode.Global,
                Quantizer = new OctreeQuantizer()
            }, token);
            gifStream.Position = 0;

            return (gifStream, pngStream);
        }
    }
}
