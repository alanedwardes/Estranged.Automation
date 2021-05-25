using System.Threading.Tasks;
using System.Threading;
using Discord.WebSocket;
using Discord;

namespace Estranged.Automation.Runner.Discord.Events
{
    public interface IMessageDeleted
    {
        Task MessageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel, CancellationToken token);
    }
}
