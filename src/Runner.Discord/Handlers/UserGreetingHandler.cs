using Discord;
using Estranged.Automation.Runner.Discord.Events;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Handlers
{
    public sealed class UserGreetingHandler : IUserIsTyping
    {
        public async Task UserIsTyping(Cacheable<IUser, ulong> user, Cacheable<IMessageChannel, ulong> channel, CancellationToken token)
        {
            if ((await channel.GetOrDownloadAsync()).IsProtectedChannel())
            {
                return;
            }

            if (!RandomExtensions.PercentChance(1))
            {
                return;
            }

            await (await channel.GetOrDownloadAsync()).SendMessageAsync($"Hello <@{user.Id}>", options: token.ToRequestOptions());
        }
    }
}
