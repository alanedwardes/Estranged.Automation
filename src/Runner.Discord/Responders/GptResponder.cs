using Discord;
using Estranged.Automation.Runner.Discord.Events;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI_API;
using OpenAI_API.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Responders
{
    internal sealed class GptResponder : IResponder
    {
        private readonly ILogger<GptResponder> _logger;
        private readonly OpenAIAPI _openAi;

        public GptResponder(ILogger<GptResponder> logger, OpenAIAPI openAi)
        {
            _logger = logger;
            _openAi = openAi;
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            const string trigger = "gpt";
            if (!message.Content.StartsWith(trigger, StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            var prompt = message.Content[trigger.Length..].Trim();

            var response = await _openAi.Completions.CreateCompletionAsync(prompt, new Model("gpt-3.5-turbo"));

            _logger.LogInformation("Got response {Response}", JsonConvert.SerializeObject(response));

            foreach (var completion in response.Completions)
            {
                await message.Channel.SendMessageAsync(completion.Text, options: token.ToRequestOptions());
            }
        }
    }
}
