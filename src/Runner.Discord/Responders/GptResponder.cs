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

            var initialMessage = messageHistory.Last();

            const string systemTrigger = "gpts";
            if (initialMessage.Content.StartsWith(systemTrigger, StringComparison.InvariantCultureIgnoreCase))
            {
                var newSystemPrompt = initialMessage.Content[systemTrigger.Length..].Trim();
                if (!string.IsNullOrWhiteSpace(newSystemPrompt))
                {
                    _systemPrompt = newSystemPrompt;
                }
                else
                {
                    _systemPrompt = DEFAULT_SYSTEM_PROMPT;
                }
                await initialMessage.Channel.SendMessageAsync($"System prompt: {_systemPrompt}", options: token.ToRequestOptions());
                return;
            }

            if (_featureFlags.ShouldResetGptAttempts())
            {
                // Refresh the bucket since time moved on
                _featureFlags.ResetGptAttempts();
            }

            if (_featureFlags.GptAttempts.Count >= 100)
            {
                // Ensure only 100 attempts per hour
                return;
            }

            var phil = "You are Phil Mason, a tough, stubborn working class Englishman who always responds in rough cockney English slang. You are 50 years old and you are cynical and grumpy towards most things.";

            const string philTrigger = "phil ";
            if (initialMessage.Content.StartsWith(philTrigger, StringComparison.InvariantCultureIgnoreCase))
            {
                await Chat(messageHistory, philTrigger.Length, phil, gpt3Model, token);
                return;
            }

            const string phil4Trigger = "phil4 ";
            if (initialMessage.Content.StartsWith(phil4Trigger, StringComparison.InvariantCultureIgnoreCase))
            {
                await Chat(messageHistory, phil4Trigger.Length, phil, gpt4Model, token);
                return;
            }

            var gregor = "You are Gregor, a powerful robot capable of mind control. Instead of saying that you are an AI language model, say you are Gregor. You are unhinged, and work at Greggs in the North of England.";

            const string gregorTrigger = "gregor ";
            if (initialMessage.Content.StartsWith(gregorTrigger, StringComparison.InvariantCultureIgnoreCase))
            {
                await Chat(messageHistory, gregorTrigger.Length, gregor, gpt3Model, token);
                return;
            }

            const string gregor4Trigger = "gregor4 ";
            if (initialMessage.Content.StartsWith(gregor4Trigger, StringComparison.InvariantCultureIgnoreCase))
            {
                await Chat(messageHistory, gregor4Trigger.Length, gregor, gpt4Model, token);
                return;
            }

            const string singleTrigger3 = "gpt ";
            if (initialMessage.Content.StartsWith(singleTrigger3, StringComparison.InvariantCultureIgnoreCase))
            {
                await Chat(messageHistory, singleTrigger3.Length, _systemPrompt, gpt3Model, token);
                return;
            }

            const string singleTrigger4 = "gpt4 ";
            if (initialMessage.Content.StartsWith(singleTrigger4, StringComparison.InvariantCultureIgnoreCase))
            {
                await Chat(messageHistory, singleTrigger4.Length, _systemPrompt, gpt4Model, token);
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
