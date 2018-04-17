using Amazon;
using Amazon.DynamoDBv2;
using Estranged.Automation.Runner.Reviews;
using Estranged.Automation.Shared;
using Google.Cloud.Translation.V2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Narochno.Slack;
using Narochno.Steam;
using System;

namespace Estranged.Automation
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var provider = new ServiceCollection()
                .AddSteam()
                .AddLogging()
                .AddTransient<RunnerManager>()
                .AddTransient<ReviewsRunner>()
                .AddTransient<IAmazonDynamoDB>(x => new AmazonDynamoDBClient(RegionEndpoint.EUWest1))
                .AddTransient<ISeenItemRepository, SeenItemRepository>()
                .AddSlack(new SlackConfig { WebHookUrl = Environment.GetEnvironmentVariable("SLACK_WEB_HOOK_URL") })
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
