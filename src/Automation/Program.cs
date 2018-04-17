using Amazon;
using Amazon.DynamoDBv2;
using Estranged.Automation.Runner.Reviews;
using Estranged.Automation.Runner.Syndication;
using Estranged.Automation.Shared;
using Google.Cloud.Translation.V2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Narochno.Steam;
using System.Net.Http;

namespace Estranged.Automation
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var httpClient = new HttpClient();

            var provider = new ServiceCollection()
                .AddSteam()
                .AddLogging()
                .AddSingleton(httpClient)
                .AddTransient<RunnerManager>()
                .AddTransient<ReviewsRunner>()
                .AddTransient<SyndicationRunner>()
                .AddTransient<IAmazonDynamoDB>(x => new AmazonDynamoDBClient(RegionEndpoint.EUWest1))
                .AddTransient<ISeenItemRepository, SeenItemRepository>()
                .AddSingleton(TranslationClient.Create())
                .BuildServiceProvider();

            provider.GetRequiredService<ILoggerFactory>()
                .AddConsole();

            provider.GetRequiredService<RunnerManager>()
                .Run()
                .GetAwaiter()
                .GetResult();

            return 0;
        }
    }
}
