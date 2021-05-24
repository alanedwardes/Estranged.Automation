using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
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
            "no"
        };

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (!message.Content.ToLowerInvariant().Contains("how are you"))
            {
                return;
            }

            if (!message.MentionedUserIds.Contains(437014310078906378ul))
            {
                return;
            }

            if (RandomExtensions.PercentChance(25))
            {
                return;
            }

            await message.Channel.SendMessageAsync(string.Format(RESPONSES.OrderBy(x => Guid.NewGuid()).First(), $"<@{message.Author.Id}>"));
        }
    }
}
