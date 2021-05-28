using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Estranged.Automation.Runner.Discord.Events
{
    public interface IUserIsTyping
    {
        Task UserIsTyping(SocketUser user, ISocketMessageChannel channel, CancellationToken token);
    }
}
