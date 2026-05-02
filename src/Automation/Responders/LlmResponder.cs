using Discord;
using Estranged.Automation.Events;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Responders
{
    internal sealed class LlmResponder : IResponder
    {
        private readonly ILogger<LlmResponder> _logger;
        private readonly IChatClientFactory _chatFactory;
        private readonly IConfiguration _configuration;

        public LlmResponder(ILogger<LlmResponder> logger, IChatClientFactory chatFactory, IConfiguration configuration)
        {
            _logger = logger;
            _chatFactory = chatFactory;
            _configuration = configuration;
        }

        public async Task ProcessMessage(IMessage originalMessage, CancellationToken token)
        {
            if (originalMessage.Channel.IsPublicChannel())
                return;

            var messageHistory = await originalMessage.GetFullConversation(token);
            if (messageHistory.Any(x => x.Channel != originalMessage.Channel))
            {
                _logger.LogWarning("Some of the message history is from other channels");
                return;
            }

            var initialMessage = messageHistory.Last();

            foreach (var trigger in _configuration.GetSection("LlmTriggers").Get<LlmTrigger[]>() ?? [])
            {
                if (initialMessage.Content.StartsWith(trigger.Trigger, StringComparison.InvariantCultureIgnoreCase))
                {
                    await Chat(messageHistory, trigger.Trigger.Length, trigger.SystemPrompt, trigger.Urn, token);
                    return;
                }
            }
        }

        private async Task Chat(IList<IMessage> messageHistory, int prefixLength, string systemPrompt, string urn, CancellationToken token)
        {
            using var chatClient = _chatFactory.CreateClient(urn);
            var initialMessage = messageHistory.Last();
            var latestMessage = messageHistory.First();

            using (latestMessage.Channel.EnterTypingState())
            {
                IList<ChatMessage> chatMessages = MessageExtensions.BuildChatMessages(
                    messageHistory, prefixLength, initialMessage,
                    systemPrompt.Replace("{CurrentDate}", $"The current date is {DateTime.Now:D}"));
                await chatClient.StreamResponse(latestMessage, chatMessages,
                    new() { AdditionalProperties = new AdditionalPropertiesDictionary { { "Think", false } } },
                    token);
            }
        }
    }
}
