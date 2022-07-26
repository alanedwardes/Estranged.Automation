using System.Threading.Tasks;
using System.Threading;
using Discord;

namespace Estranged.Automation.Runner.Discord.Events
{
    public interface IMessageDeleted
    {
        Task MessageDeleted(Cacheable<IMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, CancellationToken token);
    }
}
