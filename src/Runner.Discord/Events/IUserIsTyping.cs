using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace Estranged.Automation.Runner.Discord.Events
{
    public interface IUserIsTyping
    {
        Task UserIsTyping(Cacheable<IUser, ulong> user, Cacheable<IMessageChannel, ulong> channel, CancellationToken token);
    }
}
