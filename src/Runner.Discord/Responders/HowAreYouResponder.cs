using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Responders
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

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (!message.Content.ToLowerInvariant().Contains("how are you"))
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
