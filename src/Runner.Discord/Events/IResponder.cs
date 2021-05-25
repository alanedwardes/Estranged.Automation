using Discord;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Events
{
    public interface IResponder
    {
        Task ProcessMessage(IMessage message, CancellationToken token);
    }
}
