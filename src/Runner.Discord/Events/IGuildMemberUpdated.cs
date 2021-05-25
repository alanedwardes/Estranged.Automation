using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Estranged.Automation.Runner.Discord.Events
{
    public interface IGuildMemberUpdated
    {
        Task GuildMemberUpdated(SocketGuildUser oldMember, SocketGuildUser newMember, CancellationToken token);
    }
}
