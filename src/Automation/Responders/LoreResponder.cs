using Discord;
using Estranged.Automation.Events;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Responders
{
    internal sealed class LoreResponder : IResponder
    {
        private readonly ILogger<LoreResponder> _logger;
        private readonly IConfiguration _configuration;
        private readonly IFeatureFlags _featureFlags;
        private readonly IChatClientFactory _chatClientFactory;

        public LoreResponder(ILogger<LoreResponder> logger, IConfiguration configuration, IFeatureFlags featureFlags, IChatClientFactory chatClientFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _featureFlags = featureFlags;
            _chatClientFactory = chatClientFactory;
        }

        public async Task ProcessMessage(IMessage originalMessage, CancellationToken token)
        {
            var messageHistory = await originalMessage.GetFullConversation(token);

            if (messageHistory.Any(x => x.Channel != originalMessage.Channel))
            {
                _logger.LogWarning("Some of the message history is from other channels");
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

            var initialMessage = messageHistory.Last();

            const string loreTrigger = "lore ";
            if (initialMessage.Content.StartsWith(loreTrigger, StringComparison.InvariantCultureIgnoreCase))
            {
                await Chat(messageHistory, loreTrigger.Length, await GetTools(), token);
                return;
            }
        }

        private async Task<IList<McpClientTool>> GetTools()
        {
            var httpTransport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = new Uri(_configuration["ESTRANGED_WIKI_MCP"]),
                TransportMode = HttpTransportMode.StreamableHttp
            });

            var mcpClient = await McpClient.CreateAsync(httpTransport);

            return await mcpClient.ListToolsAsync();
        }

        private async Task Chat(IList<IMessage> messageHistory, int initialMessagePrefixLength, IList<McpClientTool> tools, CancellationToken token)
        {
            using IChatClient chatClient = _chatClientFactory.CreateClient("urn:ollama:qwen3:8b")
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();

            var initialMessage = messageHistory.Last();
            var latestMessage = messageHistory.First();

            using (latestMessage.Channel.EnterTypingState())
            {
                IList<ChatMessage> chatMessages = MessageExtensions.BuildChatMessages(messageHistory, initialMessagePrefixLength, initialMessage, _configuration["ESTRANGED_WIKI_PROMPT"]);

                var chatResponse = await chatClient.GetResponseAsync(chatMessages, new() { Tools = [.. tools] }, token);

                var inputTokens = chatResponse.Usage.InputTokenCount;
                var outputTokens = chatResponse.Usage.OutputTokenCount;

                const float usdPerMillionInputTokens = 4f;
                const float usdPerMillionOutputTokens = 16f;

                // Log price in usd
                var price = inputTokens / 1_000_000f * usdPerMillionInputTokens + outputTokens / 1_000_000f * usdPerMillionOutputTokens;
                _logger.LogInformation($"Lore request complete, price: ${price:0.00000} (input: {inputTokens} tokens, output: {outputTokens} tokens)");

                await MessageExtensions.PostChatMessages(latestMessage, chatResponse.Messages, token);
            }
        }
    }
}
