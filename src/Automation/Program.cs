﻿using Ae.Steam.Client;
using Amazon;
using Amazon.DynamoDBv2;
using Discord;
using Discord.WebSocket;
using Estranged.Automation.Runner.Discord;
using Estranged.Automation.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Octokit;
using OpenAI;
using OllamaSharp;
using System;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace Estranged.Automation
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("Bootstrapping");

            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("config.secret.json", true)
                .Build();

            var productHeader = new ProductInfoHeaderValue("Estranged-Automation", "1.0.0");

            var gitHubClient = new GitHubClient(new Octokit.ProductHeaderValue(productHeader.Product.Name, productHeader.Product.Version))
            {
                Credentials = new Credentials("estranged-automation", configuration["GITHUB_PASSWORD"])
            };

            var discordSocketClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                HandlerTimeout = null,
                MessageCacheSize = 1024,
                GatewayIntents = GatewayIntents.All
            });

            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
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
                .AddSingleton(new OpenAIClient(configuration["OPENAI_APIKEY"]))
                .AddSingleton(provider =>
                {
                    var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient();
                    httpClient.Timeout = TimeSpan.FromHours(1);
                    httpClient.BaseAddress = new Uri(configuration["OLLAMA_HOST"]);
                    return new OllamaApiClient(httpClient);
                })
                .AddSingleton<IDiscordClient>(discordSocketClient)
                .AddTransient<IAmazonDynamoDB>(x => new AmazonDynamoDBClient(RegionEndpoint.EUWest1))
                .AddTransient<ISeenItemRepository, SeenItemRepository>()
                .AddSingleton<IRateLimitingRepository, RateLimitingRepository>()
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
