using System.Threading.Tasks;
using System.Threading;
using Discord.WebSocket;

namespace Estranged.Automation.Runner.Discord.Events
{
    public interface IUserLeftHandler
    {
        Task UserLeft(SocketGuildUser user, CancellationToken token);
    }
}
