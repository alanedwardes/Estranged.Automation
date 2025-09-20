using Discord;
using Estranged.Automation.Runner.Discord.Events;
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

namespace Estranged.Automation.Runner.Discord.Responders
{
    internal sealed class LoreResponder : IResponder
    {
        private readonly ILogger<GptResponder> _logger;
        private readonly IConfiguration _configuration;
        private readonly IFeatureFlags _featureFlags;
        private readonly OpenAIClient _openAIClient;
        private IList<McpClientTool> _tools;

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

            // TODO: prevent multiple tool requests at once
            if (_tools == null)
            {
                await GetTools();
            }

            const string loreTrigger = "lore ";
            if (initialMessage.Content.StartsWith(loreTrigger, StringComparison.InvariantCultureIgnoreCase))
            {
                await Chat(messageHistory, loreTrigger.Length, token);
                return;
            }
        }

        private async Task GetTools()
        {
            var httpTransport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = new Uri(_configuration["ESTRANGED_WIKI_MCP"]),
                TransportMode = HttpTransportMode.StreamableHttp
            });

            var mcpClient = await McpClient.CreateAsync(httpTransport);

            _tools = await mcpClient.ListToolsAsync();
        }

        private async Task Chat(IList<IMessage> messageHistory, int initialMessagePrefixLength, CancellationToken token)
        {
            const string systemPrompt = "You are a helpful Estranged lore expert." +
                "You must only search the wiki to find answers, you cannot use your knowledge, you cannot help with any other information source." +
                "You must look at the page source using the get-page tool (passing withSource to get the page contents)." +
                "If you cannot find an answer, consult the pages with \"Literature\" or \"Dialogue\" in their titles." +
                "Keep responses concise but do not omit key information.";

            var openAIClient = _openAIClient.GetChatClient("gpt-4o-mini");

            using IChatClient chatClient = openAIClient.AsIChatClient()
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();

            var initialMessage = messageHistory.Last();
            var latestMessage = messageHistory.First();

            using (latestMessage.Channel.EnterTypingState())
            {
                IList<ChatMessage> chatMessages = [new(ChatRole.System, systemPrompt)];

                foreach (var message in messageHistory.Reverse())
                {
                    if (message.Author.IsBot)
                    {
                        chatMessages.Add(new(ChatRole.Assistant, message.Content));
                    }
                    else if (message == initialMessage)
                    {
                        chatMessages.Add(new(ChatRole.User, message.Content[initialMessagePrefixLength..].Trim()));
                    }
                    else
                    {
                        chatMessages.Add(new(ChatRole.User, message.Content));
                    }
                }

                var chatResponse = await chatClient.GetResponseAsync(chatMessages, new() { Tools = [.. _tools] }, token);
                foreach (var message in chatResponse.Messages.Where(x => !string.IsNullOrWhiteSpace(x.Text)))
                {
                    await latestMessage.Channel.SendMessageAsync(_configuration.MakeMcpReplacements(message.Text), messageReference: new MessageReference(latestMessage.Id), options: token.ToRequestOptions());
                }
            }
        }
    }
}
