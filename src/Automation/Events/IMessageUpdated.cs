using System.Threading.Tasks;
using System.Threading;
using Discord.WebSocket;
using Discord;

namespace Estranged.Automation.Events
{
    public interface IMessageUpdated
    {
        Task MessageUpdated(Cacheable<IMessage, ulong> message, SocketMessage socketMessage, ISocketMessageChannel channel, CancellationToken token);
    }
}
