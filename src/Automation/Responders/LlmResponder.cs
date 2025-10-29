using Discord;
using Estranged.Automation.Events;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Responders
{
    internal sealed class LlmResponder(ILogger<OllamaResponder> logger, IChatClientFactory chatFactory) : IResponder
    {
        private readonly ILogger<OllamaResponder> _logger = logger;
        private readonly IChatClientFactory _chatFactory = chatFactory;
        private string _urn;
        private string _systemPrompt;

        public async Task ProcessMessage(IMessage originalMessage, CancellationToken token)
        {
            if (originalMessage.Author.Id != 269883106792701952)
            {
                return;
            }

            if (originalMessage.Channel.IsPublicChannel())
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

            const string listModelTrigger = "llmlist ";
            if (initialMessage.Content.StartsWith(listModelTrigger, StringComparison.InvariantCultureIgnoreCase))
            {
                var availableModels = await _chatFactory.GetModels(initialMessage.Content[listModelTrigger.Length..].Trim(), token);
                await initialMessage.Channel.SendMessageAsync($"Available models:\n- {string.Join("\n- ", availableModels)}", options: token.ToRequestOptions());
                return;
            }

            const string modelTrigger = "llmconf ";
            if (initialMessage.Content.StartsWith(modelTrigger, StringComparison.InvariantCultureIgnoreCase))
            {
                var options = initialMessage.Content[modelTrigger.Length..].Trim().Split("|");
                _urn = options[0];
                _systemPrompt = options[1];
                await initialMessage.Channel.SendMessageAsync($"Configured LLM with URN: {_urn}", options: token.ToRequestOptions());
                return;
            }

            const string llmTrigger = "llm ";
            if (initialMessage.Content.StartsWith(llmTrigger, StringComparison.InvariantCultureIgnoreCase))
            {
                var chatClient = _chatFactory.CreateClient(_urn);
                var latestMessage = messageHistory.First();

                using (latestMessage.Channel.EnterTypingState())
                {
                    IList<ChatMessage> chatMessages = MessageExtensions.BuildChatMessages(messageHistory, llmTrigger.Length, initialMessage, _systemPrompt.Replace("{CurrentDate}", $"The current date is {DateTime.Now:D}"));
                    await chatClient.StreamResponse(latestMessage, chatMessages, new() { AdditionalProperties = new AdditionalPropertiesDictionary { { "Think", false } } }, token);
                }
                return;
            }
        }
    }
}