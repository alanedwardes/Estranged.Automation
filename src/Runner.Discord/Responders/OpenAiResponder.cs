using Discord;
using Estranged.Automation.Runner.Discord.Events;
using OpenAI_API;
using OpenAI_API.Images;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Responders
{
    internal sealed class OpenAiResponder : IResponder
    {
        private readonly OpenAIAPI _openAi;

        public OpenAiResponder(OpenAIAPI openAi)
        {
            _openAi = openAi;
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            const string trigger = "dalle";
            if (!message.Content.StartsWith(trigger, StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            using (message.Channel.EnterTypingState())
            {
                var result = await _openAi.ImageGenerations.CreateImageAsync(new ImageGenerationRequest
                {
                    Size = ImageSize._256,
                    NumOfImages = 1,
                    ResponseFormat = ImageResponseFormat.Url,
                    Prompt = message.Content[trigger.Length..].Trim()
                });

                await message.Channel.SendMessageAsync(result.Object);
            }
        }
    }
}
