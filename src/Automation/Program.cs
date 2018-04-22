using Amazon;
using Amazon.DynamoDBv2;
using Estranged.Automation.Runner.Reviews;
using Estranged.Automation.Runner.Syndication;
using Estranged.Automation.Shared;
using Google.Cloud.Translation.V2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Narochno.Steam;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var httpClient = new HttpClient();

            var provider = new ServiceCollection()
                .AddSteam()
                .AddLogging(x => x.SetMinimumLevel(LogLevel.Trace))
                .AddSingleton(httpClient)
                .AddTransient<RunnerManager>()
                .AddTransient<IRunner, ReviewsRunner>()
                .AddTransient<IRunner, SyndicationRunner>()
                .AddTransient<IRunner, DiscordRunner>()
                .AddTransient<IAmazonDynamoDB>(x => new AmazonDynamoDBClient(RegionEndpoint.EUWest1))
                .AddTransient<ISeenItemRepository, SeenItemRepository>()
                .AddSingleton(TranslationClient.Create())
                .BuildServiceProvider();

            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

            loggerFactory.AddConsole(LogLevel.Trace);

            var source = new CancellationTokenSource(TimeSpan.FromHours(1));

            AppDomain.CurrentDomain.ProcessExit += (sender, ev) => source.Cancel();

            try
            {
                provider.GetRequiredService<RunnerManager>()
                        .Run(source.Token)
                        .GetAwaiter()
                        .GetResult();
            }
            catch (TaskCanceledException e)
            {
                loggerFactory.CreateLogger(nameof(Program)).LogInformation(e, "Task cancelled.");
            }

            return 0;
        }
    }
}
