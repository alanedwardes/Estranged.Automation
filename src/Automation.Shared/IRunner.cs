using System;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Shared
{
    public interface IRunner
    {
        Task Run(CancellationToken token);
    }

    public abstract class PeriodicRunner : IRunner
    {
        public abstract TimeSpan Period { get; }

        public abstract Task RunPeriodically(CancellationToken token);

        public virtual async Task Run(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await RunPeriodically(token);
                await Task.Delay(Period, token);
            }
        }
    }
}
