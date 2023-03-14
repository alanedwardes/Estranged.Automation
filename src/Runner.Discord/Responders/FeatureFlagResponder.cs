using Discord;
using Estranged.Automation.Runner.Discord.Events;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Responders
{
    internal sealed class FeatureFlagResponder : IResponder
    {
        public static bool IsAiEnabled { get; private set; }

        public Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (message.Author.Id != 269883106792701952)
            {
                return Task.CompletedTask;
            }

            if (message.Content == "ff ai toggle")
            {
                IsAiEnabled = !IsAiEnabled;
                message.Channel.SendMessageAsync($"IsAiEnabled: {IsAiEnabled}");
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
    }
}
