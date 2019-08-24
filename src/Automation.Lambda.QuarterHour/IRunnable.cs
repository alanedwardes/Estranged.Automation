using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Lambda.QuarterHour
{
    public interface IRunnable
    {
        Task RunAsync(CancellationToken token);
    }
}
