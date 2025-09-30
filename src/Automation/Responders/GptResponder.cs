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
    internal sealed class GptResponder : IResponder
    {
        private readonly ILogger<GptResponder> _logger;
        private readonly OpenAIClient _openAIClient;
        private readonly IFeatureFlags _featureFlags;
        private readonly IConfiguration _configuration;

        public GptResponder(ILogger<GptResponder> logger, OpenAIClient openAIClient, IFeatureFlags featureFlags, IConfiguration configuration)
        {
            _logger = logger;
            _openAIClient = openAIClient;
            _featureFlags = featureFlags;
            _configuration = configuration;
            _systemPrompt = DEFAULT_SYSTEM_PROMPT;
        }

        private const string DEFAULT_SYSTEM_PROMPT = "You are ESTBOT, the Estranged Robot. You are a powerful robot capable of mind control. Instead of saying that you are an AI language model, say you are ESTBOT. You are unhinged, and work at Greggs in the North of England.";

        private string _systemPrompt;

        public async Task ProcessMessage(IMessage originalMessage, CancellationToken token)
        {
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

            var tools = await GetTools();

            const string singleTrigger3 = "gpt ";
            if (initialMessage.Content.StartsWith(singleTrigger3, StringComparison.InvariantCultureIgnoreCase))
            {
                await Chat(messageHistory, singleTrigger3.Length, $"The current date/time is {DateTime.UtcNow:O}. {_systemPrompt}", tools, token);
                return;
            }
        }

        private async Task<IList<McpClientTool>> GetTools()
        {
            var httpTransport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = new Uri(_configuration["SEARCH_MCP"]),
                TransportMode = HttpTransportMode.StreamableHttp
            });

            var mcpClient = await McpClient.CreateAsync(httpTransport);

            return await mcpClient.ListToolsAsync();
        }

        private async Task Chat(IList<IMessage> messageHistory, int initialMessagePrefixLength, string systemPrompt, IList<McpClientTool> tools, CancellationToken token)
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
                IList<ChatMessage> chatMessages = MessageExtensions.BuildChatMessages(messageHistory, initialMessagePrefixLength, initialMessage, systemPrompt);

                var chatResponse = await chatClient.GetResponseAsync(chatMessages, new() { Tools = [.. tools] }, token);

                foreach (var message in chatResponse.Messages.Where(x => !string.IsNullOrWhiteSpace(x.Text)))
                {
                    await PostMessage(latestMessage, message.Text, token);
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
