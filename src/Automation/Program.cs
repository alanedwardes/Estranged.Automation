using Ae.Steam.Client;
using Amazon;
using Amazon.DynamoDBv2;
using Discord;
using Discord.WebSocket;
using Estranged.Automation.Runner.Discord;
using Estranged.Automation.Shared;
using Google.Cloud.Language.V1;
using Google.Cloud.Translation.V2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("Bootstrapping");

            var productHeader = new ProductInfoHeaderValue("Estranged-Automation", "1.0.0");

            var gitHubClient = new GitHubClient(new Octokit.ProductHeaderValue(productHeader.Product.Name, productHeader.Product.Version))
            {
                Credentials = new Credentials("estranged-automation", Environment.GetEnvironmentVariable("GITHUB_PASSWORD"))
            };

            var discordSocketClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                HandlerTimeout = null,
                MessageCacheSize = 1024
            });

            var services = new ServiceCollection()
                .AddLogging(options =>
                {
                    options.AddConsole()
                           .AddProvider(new DiscordLoggerProvider(discordSocketClient))
                           .SetMinimumLevel(LogLevel.Information);
                })
                .AddTransient<RunnerManager>()
                .AddTransient<IRunner, DiscordRunner>()
                .AddSingleton<IGitHubClient>(gitHubClient)
                .AddSingleton(discordSocketClient)
                .AddSingleton<IDiscordClient>(discordSocketClient)
                .AddTransient<IAmazonDynamoDB>(x => new AmazonDynamoDBClient(RegionEndpoint.EUWest1))
                .AddTransient<ISeenItemRepository, SeenItemRepository>()
                .AddSingleton<IRateLimitingRepository, RateLimitingRepository>()
                .AddSingleton(TranslationClient.Create())
                .AddSingleton(LanguageServiceClient.Create())
                .AddResponderServices();

            var builder1 = services.AddHttpClient(DiscordHttpClientConstants.RESPONDER_CLIENT, x => x.DefaultRequestHeaders.UserAgent.Add(productHeader));
            builder1.Services.RemoveAll<IHttpMessageHandlerBuilderFilter>();

            var builder2 = services.AddHttpClient<ISteamClient, SteamClient>(x => x.DefaultRequestHeaders.UserAgent.Add(productHeader));
            builder2.Services.RemoveAll<IHttpMessageHandlerBuilderFilter>();

            var provider = services.BuildServiceProvider();

            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

            var source = new CancellationTokenSource();

            AppDomain.CurrentDomain.ProcessExit += (sender, ev) => source.Cancel();

            try
            {
                Console.WriteLine("Starting manager");
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
