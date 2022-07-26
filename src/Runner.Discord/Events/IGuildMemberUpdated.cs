using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Estranged.Automation.Runner.Discord.Events
{
    public interface IGuildMemberUpdated
    {
        Task GuildMemberUpdated(Cacheable<SocketGuildUser, ulong> oldMember, SocketGuildUser newMember, CancellationToken token);
    }
}
