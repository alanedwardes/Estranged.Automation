using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Estranged.Automation.Runner.Discord.Events
{
    public interface IRoleUpdated
    {
        Task RoleUpdated(SocketRole oldRole, SocketRole newRole, CancellationToken token);
    }
}
