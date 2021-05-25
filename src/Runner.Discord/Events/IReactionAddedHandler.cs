using System.Threading.Tasks;
using System.Threading;
using Discord.WebSocket;
using Discord;

namespace Estranged.Automation.Runner.Discord.Events
{
    public interface IReactionAddedHandler
    {
        Task ReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction, CancellationToken token);
    }
}
