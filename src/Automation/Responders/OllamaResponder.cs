using Discord;
using Estranged.Automation.Events;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using OllamaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Responders
{
    internal sealed class OllamaResponder : IResponder
    {
        private readonly ILogger<OllamaResponder> _logger;
        private readonly OllamaApiClient _ollamaClient;

        public OllamaResponder(ILogger<OllamaResponder> logger, IConfiguration configuration, OllamaApiClient ollamaClient)
        {
            _logger = logger;
            _ollamaClient = ollamaClient;
            _systemPrompt = DEFAULT_SYSTEM_PROMPT;
            _configuration = configuration;
            _model = configuration["OLLAMA_MODEL"];
        }

        private const string DEFAULT_SYSTEM_PROMPT = "You are ESTBOT, the Estranged Robot. You are a powerful robot capable of mind control. Instead of saying that you are an AI language model, say you are ESTBOT. You are unhinged, and work at Greggs in the North of England.";

        private string _systemPrompt;
        private readonly IConfiguration _configuration;
        private string _model;

        public async Task ProcessMessage(IMessage originalMessage, CancellationToken token)
        {
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

            const string modelTrigger = "ollamam";
            if (initialMessage.Content.StartsWith(modelTrigger, StringComparison.InvariantCultureIgnoreCase))
            {
                var newModel = initialMessage.Content[modelTrigger.Length..].Trim();
                if (!string.IsNullOrWhiteSpace(newModel))
                {
                    _model = newModel;
                    await initialMessage.Channel.SendMessageAsync($"Model: {_model}", options: token.ToRequestOptions());
                }
                return;
            }

            const string systemTrigger = "ollamas";
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

            var tools = await GetTools();

            const string singleTrigger3 = "ollama ";
            if (initialMessage.Content.StartsWith(singleTrigger3, StringComparison.InvariantCultureIgnoreCase))
            {
                await Chat(messageHistory, singleTrigger3.Length, _systemPrompt, _model, tools, token);
                return;
            }

            foreach ((var trigger, var model, var systemPrompt) in _configuration.GetTriples("OLLAMA_TRIGGERS"))
            {
                if (initialMessage.Content.StartsWith(trigger, StringComparison.InvariantCultureIgnoreCase))
                {
                    await Chat(messageHistory, trigger.Length, systemPrompt, model, tools, token);
                    return;
                }
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

        private async Task Chat(IList<IMessage> messageHistory, int initialMessagePrefixLength, string systemPrompt, string model, IList<McpClientTool> tools, CancellationToken token)
        {
            using IChatClient chatClient = ((IChatClient)_ollamaClient)
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();

            var initialMessage = messageHistory.Last();
            var latestMessage = messageHistory.First();

            using (latestMessage.Channel.EnterTypingState())
            {
                IList<ChatMessage> chatMessages = MessageExtensions.BuildChatMessages(messageHistory, initialMessagePrefixLength, initialMessage, $"The current date/time is {DateTime.UtcNow:O}. {systemPrompt}");

                var chatResponse = await chatClient.GetResponseAsync(chatMessages, new() { Tools = [.. tools], ModelId = model }, token);

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

                await message.Channel.SendMessageAsync(content[discordMessageLimit..], messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
            }
            else
            {
                await message.Channel.SendMessageAsync(content, messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
            }
        }
    }
}
