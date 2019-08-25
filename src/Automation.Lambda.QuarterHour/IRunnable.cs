using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Lambda.QuarterHour
{
    public interface IRunnable
    {
        IEnumerable<Task> RunAsync(CancellationToken token);
    }
}
