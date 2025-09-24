using System.Threading.Tasks;
using System.Threading;
using Discord.WebSocket;

namespace Estranged.Automation.Events
{
    public interface IUserLeftHandler
    {
        Task UserLeft(SocketUser user, CancellationToken token);
    }
}
