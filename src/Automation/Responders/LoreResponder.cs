using Discord;
using Estranged.Automation.Events;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Responders
{
    internal sealed class LoreResponder : IResponder
    {
        private readonly ILogger<GptResponder> _logger;
        private readonly IConfiguration _configuration;
        private readonly IFeatureFlags _featureFlags;
        private readonly OpenAIClient _openAIClient;

        public LoreResponder(ILogger<GptResponder> logger, IConfiguration configuration, IFeatureFlags featureFlags, OpenAIClient openAIClient)
        {
            _logger = logger;
            _configuration = configuration;
            _featureFlags = featureFlags;
            _openAIClient = openAIClient;
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
            var openAIClient = _openAIClient.GetChatClient("gpt-4o-mini");

            using IChatClient chatClient = openAIClient.AsIChatClient()
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

                foreach (var message in chatResponse.Messages.Where(x => !string.IsNullOrWhiteSpace(x.Text)))
                {
                    await latestMessage.Channel.SendMessageAsync(_configuration.MakeMcpReplacements(message.Text), messageReference: new MessageReference(latestMessage.Id), flags: MessageFlags.SuppressEmbeds, options: token.ToRequestOptions());
                }
            }
        }
    }
}
