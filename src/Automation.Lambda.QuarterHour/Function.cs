using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Estranged.Automation.Lambda.QuarterHour.Runnables;
using Estranged.Automation.Runner.Syndication;
using Estranged.Automation.Shared;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Translation.V2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Narochno.Steam;

namespace Estranged.Automation.Lambda.QuarterHour
{
    public class Function
    {
        public class FunctionConfig
        {
            public string EstrangedDiscordCommunityWebhook { get; set; }
            public string EstrangedDiscordReviewsWebhook { get; set; }
            public string EstrangedDiscordGamingWebhook { get; set; }
            public string EstrangedDiscordSyndicationWebhook { get; set; }
        }

        public async Task<Stream> FunctionHandler(Stream input, ILambdaContext context)
        {
            var ssm = new AmazonSimpleSystemsManagementClient();

            const string googleComputeParameter = "/estranged/google/compute";
            const string communityWebhookParameter = "/estranged/discord/webhooks/community";
            const string reviewsWebhookParameter = "/estranged/discord/webhooks/reviews";
            const string gamingWebhookParameter = "/estranged/discord/webhooks/gaming";
            const string syndicationWebhookParameter = "/estranged/discord/webhooks/syndication";

            var parameters = (await ssm.GetParametersAsync(new GetParametersRequest
            {
                Names = new List<string>
                {
                    googleComputeParameter,
                    communityWebhookParameter,
                    reviewsWebhookParameter,
                    gamingWebhookParameter,
                    syndicationWebhookParameter
                }
            })).Parameters.ToDictionary(x => x.Name, x => x.Value);

            var config = new FunctionConfig
            {
                EstrangedDiscordCommunityWebhook = parameters[communityWebhookParameter],
                EstrangedDiscordReviewsWebhook = parameters[reviewsWebhookParameter],
                EstrangedDiscordGamingWebhook = parameters[gamingWebhookParameter],
                EstrangedDiscordSyndicationWebhook = parameters[syndicationWebhookParameter]
            };

            var httpClient = new HttpClient();

            var services = new ServiceCollection()
                .AddLogging(options =>
                {
                    options.AddConsole();
                    options.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Warning);
                })
                .AddSingleton<IRunnable, CommunityRunnable>()
                .AddSingleton<IRunnable, ReviewsRunnable>()
                .AddSingleton<IRunnable, SyndicationRunnable>()
                .AddSingleton(TranslationClient.Create(GoogleCredential.FromJson(parameters[googleComputeParameter])))
                .AddSteam(new SteamConfig { HttpClient = httpClient })
                .AddSingleton<ISeenItemRepository, SeenItemRepository>()
                .AddSingleton<IAmazonDynamoDB, AmazonDynamoDBClient>()
                .AddSingleton<Scraper>()
                .AddSingleton(httpClient)
                .AddSingleton(config);

            var provider = services.BuildServiceProvider();

            await Task.WhenAll(provider.GetServices<IRunnable>().SelectMany(x => x.RunAsync(CancellationToken.None)));

            return input;
        }
    }
}
