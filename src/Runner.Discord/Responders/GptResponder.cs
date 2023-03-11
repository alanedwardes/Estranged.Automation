using Discord;
using Estranged.Automation.Runner.Discord.Events;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Responders
{
    internal sealed class GptResponder : IResponder
    {
        private readonly OpenAIAPI _openAi;

        public GptResponder(OpenAIAPI openAi) => _openAi = openAi;

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            const string trigger = "gpt";
            if (!message.Content.StartsWith(trigger, StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            var prompt = message.Content[trigger.Length..].Trim();

            var response = await _openAi.Chat.CreateChatCompletionAsync(new List<ChatMessage>
            {
                new ChatMessage(ChatMessageRole.User, prompt)
            }, new Model("gpt-3.5-turbo"));

            foreach (var completion in response.Choices)
            {
                const int discordMessageLimit = 2000;

                if (completion.Message.Content.Length > discordMessageLimit)
                {
                    await message.Channel.SendMessageAsync(completion.Message.Content[..discordMessageLimit], options: token.ToRequestOptions());

                    // Assume not longer than 4000k
                    await message.Channel.SendMessageAsync(completion.Message.Content[discordMessageLimit..completion.Message.Content.Length], options: token.ToRequestOptions());
                }
                else
                {
                    await message.Channel.SendMessageAsync(completion.Message.Content, options: token.ToRequestOptions());
                }
            }
        }
    }
}
