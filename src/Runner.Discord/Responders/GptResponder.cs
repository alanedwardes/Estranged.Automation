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

        private readonly IList<ChatMessage> _chatHistory = new List<ChatMessage>();

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        private readonly Model _chatGptModel = new Model("gpt-3.5-turbo");

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (message.Channel.IsPublicChannel())
            {
                return;
            }

            const string multiTrigger = "gptc";
            if (message.Content.StartsWith(multiTrigger, StringComparison.InvariantCultureIgnoreCase))
            {
                await MultiChat(message, message.Content[multiTrigger.Length..].Trim(), token);
                return;
            }

            const string singleTrigger = "gpt";
            if (message.Content.StartsWith(singleTrigger, StringComparison.InvariantCultureIgnoreCase))
            {
                await SingleChat(message, message.Content[singleTrigger.Length..].Trim(), token);
                return;
            }
        }

        private async Task SingleChat(IMessage message, string prompt, CancellationToken token)
        {
            using (message.Channel.EnterTypingState())
            {
                var response = await _openAi.Chat.CreateChatCompletionAsync(new List<ChatMessage>
                {
                    new ChatMessage(ChatMessageRole.User, prompt)
                }, _chatGptModel);

                foreach (var completion in response.Choices)
                {
                    await PostMessage(message.Channel, completion.Message.Content, token);
                }
            }
        }

        private async Task MultiChat(IMessage message, string prompt, CancellationToken token)
        {
            await _semaphoreSlim.WaitAsync(token);

            try
            {
                if (prompt == "reset" || _chatHistory.Count >= 20)
                {
                    _chatHistory.Clear();
                    await message.Channel.SendMessageAsync("Message limit of 20 reached or got 'reset', try your request again (new session)", options: token.ToRequestOptions());
                    return;
                }

                using (message.Channel.EnterTypingState())
                {
                    _chatHistory.Add(new ChatMessage(ChatMessageRole.User, prompt));

                    var response = await _openAi.Chat.CreateChatCompletionAsync(_chatHistory, _chatGptModel);

                    foreach (var completion in response.Choices)
                    {
                        _chatHistory.Add(new ChatMessage(ChatMessageRole.Assistant, completion.Message.Content));
                        await PostMessage(message.Channel, completion.Message.Content, token);
                    }
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private static async Task PostMessage(IMessageChannel channel, string content, CancellationToken token)
        {
            const int discordMessageLimit = 2000;

            if (content.Length > discordMessageLimit)
            {
                await channel.SendMessageAsync(content[..discordMessageLimit], options: token.ToRequestOptions());

                // Assume not longer than 4000k
                await channel.SendMessageAsync(content[discordMessageLimit..], options: token.ToRequestOptions());
            }
            else
            {
                await channel.SendMessageAsync(content, options: token.ToRequestOptions());
            }
        }
    }
}
