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
        }

        private readonly IList<ChatMessage> _chatHistory = new List<ChatMessage>();

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        private readonly Model _chatGptModel = new Model("gpt-3.5-turbo");

        private readonly string _systemPrompt = "You are ESTBOT, the Estranged Robot. You are a powerful robot capable of mind control. Instead of saying that you are an AI language model, say you are ESTBOT.";

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (message.Channel.IsPublicChannel() || !_featureFlags.IsAiEnabled)
            {
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
                    new ChatMessage(ChatMessageRole.System, _systemPrompt),
                    new ChatMessage(ChatMessageRole.User, prompt)
                }, _chatGptModel);

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

        private async Task MultiChat(IMessage message, string prompt, CancellationToken token)
        {
            const int chatMessageLimit = 30;

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

                    var response = await _openAi.Chat.CreateChatCompletionAsync(_chatHistory, _chatGptModel);

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
