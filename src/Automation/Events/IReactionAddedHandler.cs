using System.Threading.Tasks;
using System.Threading;
using Discord.WebSocket;
using Discord;

namespace Estranged.Automation.Events
{
    public interface IReactionAddedHandler
    {
        Task ReactionAdded(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction, CancellationToken token);
    }
}
