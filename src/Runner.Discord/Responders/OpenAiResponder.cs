using Discord;
using Estranged.Automation.Runner.Discord.Events;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI_API;
using OpenAI_API.Images;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Responders
{
    internal sealed class OpenAiResponder : IResponder
    {
        private readonly ILogger<OpenAiResponder> _logger;
        private readonly OpenAIAPI _openAi;

        public OpenAiResponder(ILogger<OpenAiResponder> logger, OpenAIAPI openAi)
        {
            _logger = logger;
            _openAi = openAi;
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            const string trigger = "dalle";
            if (!message.Content.StartsWith(trigger, StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogInformation("Ignoring message {Message}", message.Content);
                return;
            }

            var prompt = message.Content[trigger.Length..].Trim();

            _logger.LogInformation("Prompting DALL-E with {Prompt}", prompt);

            using (message.Channel.EnterTypingState())
            {
                var result = await _openAi.ImageGenerations.CreateImageAsync(new ImageGenerationRequest
                {
                    Size = ImageSize._256,
                    NumOfImages = 1,
                    ResponseFormat = ImageResponseFormat.Url,
                    Prompt = prompt
                });

                _logger.LogInformation("Got response from OpenAI {Response}", JsonConvert.SerializeObject(result));

                await message.Channel.SendMessageAsync(result.Object, options: token.ToRequestOptions());
            }
        }
    }
}
