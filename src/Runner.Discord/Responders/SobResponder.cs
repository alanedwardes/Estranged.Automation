using Discord;
using Estranged.Automation.Runner.Discord.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public sealed class SobResponder : IResponder
    {
        private const string SOB = "😭";

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (!message.Content.Contains(SOB))
            {
                return;
            }

            if (RandomExtensions.PercentChance(95))
            {
                return;
            }

            using (message.Channel.EnterTypingState())
            {
                await Task.Delay(TimeSpan.FromSeconds(1), token);
                await message.Channel.SendMessageAsync(SOB, options: token.ToRequestOptions());
            }
        }
    }
}
