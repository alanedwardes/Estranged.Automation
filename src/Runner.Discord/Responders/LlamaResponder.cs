using Discord;
using Estranged.Automation.Runner.Discord.Events;
using LLama.Common;
using LLama;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Linq;

namespace Estranged.Automation.Runner.Discord.Responders
{
    internal sealed class LlamaResponder : IResponder
    {
        private readonly Lazy<LLamaModel> _model = new Lazy<LLamaModel>(() =>
        {
            string modelPath = Environment.GetEnvironmentVariable("LLAMA_MODEL_PATH");
            var modelParams = new ModelParams(modelPath);
            return new LLamaModel(modelParams);
        });
        private readonly IFeatureFlags _featureFlags;

        public LlamaResponder(IFeatureFlags featureFlags)
        {
            _featureFlags = featureFlags;
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (message.Channel.IsPublicChannel() || !_featureFlags.IsAiEnabled)
            {
                return;
            }

            const string singleTrigger = "llama";
            if (message.Content.StartsWith(singleTrigger, StringComparison.InvariantCultureIgnoreCase))
            {
                await SingleChat(message, message.Content[singleTrigger.Length..].Trim(), _model.Value, token);
                return;
            }
        }

        private async Task SingleChat(IMessage message, string prompt, LLamaModel model, CancellationToken token)
        {
            using (message.Channel.EnterTypingState())
            {
                using var timeoutCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellation.Token, token);

                var systemPrompt = "Transcript of a dialog, where the User interacts with an Assistant named Bob. Bob is helpful, kind, honest, good at writing, and never fails to answer the User's requests immediately and with precision.";

                var executor = new InteractiveExecutor(model);
                var session = new ChatSession(executor);

                var inferenceParams = new InferenceParams
                {
                    AntiPrompts = new List<string> { "User:" }
                };

                // Apply the system prompt
                await session.ChatAsync(systemPrompt, inferenceParams, linkedCancellation.Token).ToArrayAsync();

                // Apply the user prompt
                var sb = new StringBuilder();
                await foreach (var text in session.ChatAsync(prompt, inferenceParams, linkedCancellation.Token))
                {
                    sb.Append(text);
                }

                await message.Channel.SendMessageAsync(sb.ToString(), options: token.ToRequestOptions());
            }
        }
    }
}
