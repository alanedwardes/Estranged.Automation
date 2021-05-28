using Discord.WebSocket;
using Estranged.Automation.Runner.Discord.Events;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Handlers
{
    public sealed class UserGreetingHandler : IUserIsTyping
    {
        public async Task UserIsTyping(SocketUser user, ISocketMessageChannel channel, CancellationToken token)
        {
            if (!RandomExtensions.PercentChance(1))
            {
                return;
            }

            await channel.SendMessageAsync($"Hello <@{user.Id}>");
        }
    }
}
