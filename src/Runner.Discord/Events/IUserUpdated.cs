using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Estranged.Automation.Runner.Discord.Events
{
    public interface IUserUpdated
    {
        Task UserUpdated(SocketUser oldUser, SocketUser newUser, CancellationToken token);
    }
}
