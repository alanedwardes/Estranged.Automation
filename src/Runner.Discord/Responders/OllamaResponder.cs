using Discord;
using Estranged.Automation.Runner.Discord.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using OllamaSharp.Models.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Responders
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
            _model = configuration["OLLAMA_MODEL"];
        }

        private const string DEFAULT_SYSTEM_PROMPT = "You are ESTBOT, the Estranged Robot. You are a powerful robot capable of mind control. Instead of saying that you are an AI language model, say you are ESTBOT. You are unhinged, and work at Greggs in the North of England.";

        private string _systemPrompt;
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

            var phil = "You are Phil Mason, a tough, stubborn working class Englishman who always responds in rough cockney English slang. You are 50 years old and you are cynical and grumpy towards most things.";

            const string philTrigger = "ophil ";
            if (initialMessage.Content.StartsWith(philTrigger, StringComparison.InvariantCultureIgnoreCase))
            {
                await Chat(messageHistory, philTrigger.Length, phil, token);
                return;
            }

            const string singleTrigger3 = "ollama ";
            if (initialMessage.Content.StartsWith(singleTrigger3, StringComparison.InvariantCultureIgnoreCase))
            {
                await Chat(messageHistory, singleTrigger3.Length, _systemPrompt, token);
                return;
            }

            if (Random.Shared.NextSingle() <= 0.01f)
            {
                var systemPrompt = Random.Shared.Next(0, 2) == 1 ? phil : _systemPrompt;
                await Chat([originalMessage], 0, systemPrompt, token);
                return;
            }
        }

        private async Task Chat(IList<IMessage> messageHistory, int initialMessagePrefixLength, string systemPrompt, CancellationToken token)
        {
            var initialMessage = messageHistory.Last();
            var latestMessage = messageHistory.First();

            using (latestMessage.Channel.EnterTypingState())
            {
                var messages = new List<Message>
                {
                    new Message { Role = "system", Content = systemPrompt }
                };

                foreach (var message in messageHistory.Reverse())
                {
                    if (message.Author.IsBot)
                    {
                        messages.Add(new Message { Role = "assistant", Content = message.Content });
                    }
                    else if (message == initialMessage)
                    {
                        messages.Add(new Message { Role = "user", Content = message.Content[initialMessagePrefixLength..].Trim() });
                    }
                    else
                    {
                        messages.Add(new Message { Role = "user", Content = message.Content });
                    }
                }

                var request = new ChatRequest
                {
                    Model = _model,
                    Messages = messages,
                    Stream = false
                };

                var response = await _ollamaClient.ChatAsync(request, token).SingleAsync();
                
                if (string.IsNullOrEmpty(response.Message?.Content))
                {
                    throw new Exception($"Got no results: {JsonSerializer.Serialize(response)}");
                }

                await PostMessage(latestMessage, response.Message.Content, token);
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
