using Estranged.Automation.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation
{
    public class RunnerManager
    {
        private readonly ILogger<RunnerManager> _logger;
        private readonly IServiceProvider _serviceProvider;

        public RunnerManager(ILogger<RunnerManager> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task Run(CancellationToken token)
        {
            var tasks = _serviceProvider.GetServices<IRunner>()
                                       .Select(x => RunForever(x, token));

            await Task.WhenAll(tasks);
        }

        public async Task RunForever(IRunner runner, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await runner.Run(token);
                }
                catch (TaskCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception from task");
                    await Task.Delay(TimeSpan.FromMinutes(1), token);
                }
            }
        }
    }
}
