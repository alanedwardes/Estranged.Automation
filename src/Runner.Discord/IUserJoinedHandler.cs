using System.Threading.Tasks;
using System.Threading;
using Discord.WebSocket;

namespace Estranged.Automation.Runner.Discord
{
    public interface IUserJoinedHandler
    {
        Task UserJoined(SocketGuildUser user, CancellationToken token);
    }
}
