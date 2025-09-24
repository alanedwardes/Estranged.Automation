using Discord;
using Estranged.Automation.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Responders
{
    public sealed class HowAreYouResponder : IResponder
    {
        private static readonly IReadOnlyList<string> RESPONSES = new []
        {
            "Yes?",
            "GOOD THANK YOU HOW ARE YOU?",
            "never better",
            "none of your business",
            "i've been better",
            "Terrible.",
            "BOTtastic",
            "BOTtacular",
            "Good Thank You How Are You {0}",
            "😭",
            "no",
            "Not great. My car broke down...",
            "Don't ask me again."
        };

        private static readonly IReadOnlyList<string> TRIGGERS = new[]
        {
            "how are you",
            "how is everyone",
            "how's everyone",
            "how you"
        };

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (message.Channel.IsProtectedChannel())
            {
                return;
            }

            if (!TRIGGERS.Any(x => message.Content.ToLowerInvariant().Contains(x)))
            {
                return;
            }

            if (RandomExtensions.PercentChance(25))
            {
                return;
            }

            using (message.Channel.EnterTypingState())
            {
                await Task.Delay(TimeSpan.FromSeconds(RandomNumberGenerator.GetInt32(3, 6)));
                await message.Channel.SendMessageAsync(string.Format(RESPONSES.OrderBy(x => Guid.NewGuid()).First(), $"<@{message.Author.Id}>"));
            }
        }
    }
}
