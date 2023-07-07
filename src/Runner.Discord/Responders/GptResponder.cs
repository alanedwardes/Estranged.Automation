using Discord;
using Estranged.Automation.Runner.Discord.Events;
using Newtonsoft.Json;
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
        private readonly IFeatureFlags _featureFlags;

        public GptResponder(OpenAIAPI openAi, IFeatureFlags featureFlags)
        {
            _openAi = openAi;
            _featureFlags = featureFlags;
            _systemPrompt = DEFAULT_SYSTEM_PROMPT;
        }

        private readonly IList<ChatMessage> _chatHistory = new List<ChatMessage>();

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        private const string DEFAULT_SYSTEM_PROMPT = "You are ESTBOT, the Estranged Robot. You are a powerful robot capable of mind control. Instead of saying that you are an AI language model, say you are ESTBOT. You are unhinged, and work at Greggs in the North of England.";

        private string _systemPrompt;

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (message.Channel.IsPublicChannel() || !_featureFlags.IsAiEnabled)
            {
                return;
            }

            const string systemTrigger = "gpts";
            if (message.Content.StartsWith(systemTrigger, StringComparison.InvariantCultureIgnoreCase))
            {
                var newSystemPrompt = message.Content[systemTrigger.Length..].Trim();
                if (!string.IsNullOrWhiteSpace(newSystemPrompt))
                {
                    _systemPrompt = newSystemPrompt;
                }
                else
                {
                    _systemPrompt = DEFAULT_SYSTEM_PROMPT;
                }
                await message.Channel.SendMessageAsync($"System prompt: {_systemPrompt}", options: token.ToRequestOptions());
                return;
            }

            if (_featureFlags.ShouldResetGptAttempts())
            {
                // Refresh the bucket since time moved on
                _featureFlags.ResetGptAttempts();
            }

            if (_featureFlags.GptAttempts.Count >= 100)
            {
                await message.Channel.SendMessageAsync("wait until the next hour", options: token.ToRequestOptions());
                return;
            }

            var gpt4Model = new Model("gpt-4");
            const string multiTrigger4 = "gpt4c";
            if (message.Content.StartsWith(multiTrigger4, StringComparison.InvariantCultureIgnoreCase))
            {
                await MultiChat(message, message.Content[multiTrigger4.Length..].Trim(), gpt4Model, token);
                return;
            }

            const string singleTrigger4 = "gpt4";
            if (message.Content.StartsWith(singleTrigger4, StringComparison.InvariantCultureIgnoreCase))
            {
                await SingleChat(message, message.Content[singleTrigger4.Length..].Trim(), gpt4Model, token);
                return;
            }

            var gpt3Model = new Model("gpt-3.5-turbo");
            const string multiTrigger3 = "gptc";
            if (message.Content.StartsWith(multiTrigger3, StringComparison.InvariantCultureIgnoreCase))
            {
                await MultiChat(message, message.Content[multiTrigger3.Length..].Trim(), gpt3Model, token);
                return;
            }

            const string singleTrigger3 = "gpt";
            if (message.Content.StartsWith(singleTrigger3, StringComparison.InvariantCultureIgnoreCase))
            {
                await SingleChat(message, message.Content[singleTrigger3.Length..].Trim(), gpt3Model, token);
                return;
            }
        }

        private async Task SingleChat(IMessage message, string prompt, Model model, CancellationToken token)
        {
            using (message.Channel.EnterTypingState())
            {
                var response = await _openAi.Chat.CreateChatCompletionAsync(new List<ChatMessage>
                {
                    new ChatMessage(ChatMessageRole.System, _systemPrompt),
                    new ChatMessage(ChatMessageRole.User, prompt)
                }, model);

                if (response.Choices.Count == 0)
                {
                    throw new Exception($"Got no results: {JsonConvert.SerializeObject(response)}");
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
                        throw new Exception($"Got no results: {JsonConvert.SerializeObject(response)}");
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
