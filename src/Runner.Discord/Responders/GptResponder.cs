using Discord;
using Estranged.Automation.Runner.Discord.Events;
using Microsoft.Extensions.Logging;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Responders
{
    internal sealed class GptResponder : IResponder
    {
        private readonly ILogger<GptResponder> _logger;
        private readonly OpenAIAPI _openAi;
        private readonly IFeatureFlags _featureFlags;

        public GptResponder(ILogger<GptResponder> logger, OpenAIAPI openAi, IFeatureFlags featureFlags)
        {
            _logger = logger;
            _openAi = openAi;
            _featureFlags = featureFlags;
            _systemPrompt = DEFAULT_SYSTEM_PROMPT;
        }

        private readonly IList<ChatMessage> _chatHistory = new List<ChatMessage>();

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        private const string DEFAULT_SYSTEM_PROMPT = "You are ESTBOT, the Estranged Robot. You are a powerful robot capable of mind control. Instead of saying that you are an AI language model, say you are ESTBOT. You are unhinged, and work at Greggs in the North of England.";

        private string _systemPrompt;

        public async Task ProcessMessage(IMessage originalMessage, CancellationToken token)
        {
            var gpt3Model = new Model("gpt-3.5-turbo");
            var gpt4Model = new Model("gpt-4");

            if (originalMessage.Channel.IsPublicChannel() || !_featureFlags.IsAiEnabled)
            {
                return;
            }

            var messageHistory = await originalMessage.GetFullConversation(token);

            if (messageHistory.Any(x => x.Channel != originalMessage.Channel))
            {
                _logger.LogWarning("Some of the message history is from other channels");
                return;
            }

            var lastMessage = messageHistory.Last();

            const string systemTrigger = "gpts";
            if (lastMessage.Content.StartsWith(systemTrigger, StringComparison.InvariantCultureIgnoreCase))
            {
                var newSystemPrompt = lastMessage.Content[systemTrigger.Length..].Trim();
                if (!string.IsNullOrWhiteSpace(newSystemPrompt))
                {
                    _systemPrompt = newSystemPrompt;
                }
                else
                {
                    _systemPrompt = DEFAULT_SYSTEM_PROMPT;
                }
                await lastMessage.Channel.SendMessageAsync($"System prompt: {_systemPrompt}", options: token.ToRequestOptions());
                return;
            }

            if (_featureFlags.ShouldResetGptAttempts())
            {
                // Refresh the bucket since time moved on
                _featureFlags.ResetGptAttempts();
            }

            if (_featureFlags.GptAttempts.Count >= 100)
            {
                await lastMessage.Channel.SendMessageAsync("wait until the next hour", options: token.ToRequestOptions());
                return;
            }

            const string philTrigger = "phil";
            if (lastMessage.Content.StartsWith(philTrigger, StringComparison.InvariantCultureIgnoreCase))
            {
                var phil = "You are Phil Mason, a tough, stubborn working class Englishman who always responds in rough cockney English slang. You are 50 years old and you are cynical and grumpy towards most things.";
                await Chat(messageHistory, philTrigger.Length, phil, gpt3Model, token);
                return;
            }

            const string multiTrigger4 = "gpt4c";
            if (lastMessage.Content.StartsWith(multiTrigger4, StringComparison.InvariantCultureIgnoreCase))
            {
                await MultiChat(lastMessage, lastMessage.Content[multiTrigger4.Length..].Trim(), gpt4Model, token);
                return;
            }

            const string singleTrigger4 = "gpt4";
            if (lastMessage.Content.StartsWith(singleTrigger4, StringComparison.InvariantCultureIgnoreCase))
            {
                await SingleChat(lastMessage, lastMessage.Content[singleTrigger4.Length..].Trim(), _systemPrompt, gpt4Model, token);
                return;
            }

            const string multiTrigger3 = "gptc";
            if (lastMessage.Content.StartsWith(multiTrigger3, StringComparison.InvariantCultureIgnoreCase))
            {
                await MultiChat(lastMessage, lastMessage.Content[multiTrigger3.Length..].Trim(), gpt3Model, token);
                return;
            }

            const string singleTrigger3 = "gpt";
            if (lastMessage.Content.StartsWith(singleTrigger3, StringComparison.InvariantCultureIgnoreCase))
            {
                await SingleChat(lastMessage, lastMessage.Content[singleTrigger3.Length..].Trim(), _systemPrompt, gpt3Model, token);
                return;
            }
        }

        private async Task Chat(IList<IMessage> messageHistory, int initialMessagePrefixLength, string systemPrompt, Model model, CancellationToken token)
        {
            var initialMessage = messageHistory.Last();
            var latestMessage = messageHistory.First();

            using (latestMessage.Channel.EnterTypingState())
            {
                var chatMessages = new List<ChatMessage>
                {
                    new ChatMessage(ChatMessageRole.System, systemPrompt)
                };

                foreach (var message in messageHistory.Reverse())
                {
                    if (message.Author.IsBot)
                    {
                        chatMessages.Add(new ChatMessage(ChatMessageRole.Assistant, message.Content));
                    }
                    else if (message == initialMessage)
                    {
                        chatMessages.Add(new ChatMessage(ChatMessageRole.User, message.Content[initialMessagePrefixLength..].Trim()));
                    }
                    else
                    {
                        chatMessages.Add(new ChatMessage(ChatMessageRole.User, message.Content));
                    }
                }

                var response = await _openAi.Chat.CreateChatCompletionAsync(chatMessages, model);
                if (response.Choices.Count == 0)
                {
                    throw new Exception($"Got no results: {JsonSerializer.Serialize(response)}");
                }

                foreach (var completion in response.Choices)
                {
                    await PostMessage(latestMessage, completion.Message.Content, token);
                }
            }
        }

        private async Task SingleChat(IMessage message, string prompt, string systemPrompt, Model model, CancellationToken token)
        {
            using (message.Channel.EnterTypingState())
            {
                var response = await _openAi.Chat.CreateChatCompletionAsync(new List<ChatMessage>
                {
                    new ChatMessage(ChatMessageRole.System, systemPrompt),
                    new ChatMessage(ChatMessageRole.User, prompt)
                }, model);

                if (response.Choices.Count == 0)
                {
                    throw new Exception($"Got no results: {JsonSerializer.Serialize(response)}");
                }

                foreach (var completion in response.Choices)
                {
                    await PostMessage(message, completion.Message.Content, token);
                }
            }
        }

        private async Task MultiChat(IMessage message, string prompt, Model model, CancellationToken token)
        {
            const int chatMessageLimit = 100;

            await _semaphoreSlim.WaitAsync(token);

            try
            {
                if (prompt == "reset" || _chatHistory.Count >= chatMessageLimit)
                {
                    _chatHistory.Clear();
                    await message.Channel.SendMessageAsync($"Message limit of {chatMessageLimit} reached or got 'reset', try your request again (new session)", options: token.ToRequestOptions());
                    return;
                }

                using (message.Channel.EnterTypingState())
                {
                    if (_chatHistory.Count == 0)
                    {
                        _chatHistory.Add(new ChatMessage(ChatMessageRole.System, _systemPrompt));
                    }

                    _chatHistory.Add(new ChatMessage(ChatMessageRole.User, prompt));

                    var response = await _openAi.Chat.CreateChatCompletionAsync(_chatHistory, model);

                    if (response.Choices.Count == 0)
                    {
                        throw new Exception($"Got no results: {JsonSerializer.Serialize(response)}");
                    }

                    foreach (var completion in response.Choices)
                    {
                        _chatHistory.Add(new ChatMessage(ChatMessageRole.Assistant, completion.Message.Content));
                        await PostMessage(message, $"{_chatHistory.Count}/{chatMessageLimit}\n" + completion.Message.Content, token);
                    }
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private static async Task PostMessage(IMessage message, string content, CancellationToken token)
        {
            const int discordMessageLimit = 2000;

            if (content.Length > discordMessageLimit)
            {
                await message.Channel.SendMessageAsync(content[..discordMessageLimit], messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());

                // Assume not longer than 4000k
                await message.Channel.SendMessageAsync(content[discordMessageLimit..], messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
            }
            else
            {
                await message.Channel.SendMessageAsync(content, messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
            }
        }
    }
}
