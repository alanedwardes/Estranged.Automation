using Discord;
using Estranged.Automation.Runner.Discord.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public sealed class MarioResponder : IResponder
    {
        private readonly IReadOnlyList<Uri> _library = new[]
        {
            new Uri("https://alan.gdn/3eb70cea-8059-4797-b1aa-734a29e6779b.jpg"), // Seth Rogan
            new Uri("https://alan.gdn/eb05b40e-6d98-418e-96a2-de2b69d2be24.jpg"), // Chris Pratt
            new Uri("https://alan.gdn/65391db7-2c91-4c32-8b53-6610dddc5685.jpg")  // Jack Black
        };

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            async Task PostFromLibrary()
            {
                await message.Channel.SendMessageAsync("IT'S A ME-A");
                await message.Channel.SendMessageAsync(_library.OrderBy(x => Guid.NewGuid()).First().ToString());
            }

            if (message.Content.Contains("mario", StringComparison.InvariantCultureIgnoreCase) && RandomExtensions.PercentChance(10))
            {
                await PostFromLibrary();
            }

            if (RandomExtensions.PercentChance(1))
            {
                await PostFromLibrary();
            }
        }
    }
}
