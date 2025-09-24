using Discord;
using Estranged.Automation.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Responders
{
    public sealed class SobResponder : IResponder
    {
        private const string SOB = "😭";

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (message.Channel.IsProtectedChannel())
            {
                return;
            }

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
