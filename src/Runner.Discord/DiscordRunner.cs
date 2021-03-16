using Estranged.Automation.Shared;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Discord.WebSocket;
using Discord;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Estranged.Automation.Runner.Discord.Responders;
using Estranged.Automation.Runner.Discord;
using System.Diagnostics;
using System.Net.Http;
using Google.Cloud.Translation.V2;
using Google.Cloud.Language.V1;
using System.Collections.Generic;
using Amazon;
using Amazon.DynamoDBv2;
using Octokit;

namespace Estranged.Automation.Runner.Syndication
{
    public class DiscordRunner : IRunner
    {
        private readonly ILogger<DiscordRunner> logger;
        private IServiceProvider responderProvider;

        public DiscordRunner(ILogger<DiscordRunner> logger, ILoggerFactory loggerFactory, HttpClient httpClient, TranslationClient translationClient, LanguageServiceClient languageServiceClient, IGitHubClient gitHubClient)
        {
            this.logger = logger;

            responderProvider = new ServiceCollection()
                .AddSingleton(loggerFactory)
                .AddLogging()
                .AddSingleton(httpClient)
                .AddSingleton(translationClient)
                .AddSingleton(languageServiceClient)
                .AddSingleton(gitHubClient)
                .AddSingleton<IResponder, LocalizationResponder>()
                .AddSingleton<IRateLimitingRepository, RateLimitingRepository>()
                .AddSingleton<IAmazonDynamoDB>(new AmazonDynamoDBClient(RegionEndpoint.EUWest1))
                .AddSingleton<IDiscordClient, DiscordSocketClient>()
                .AddSingleton<IResponder, DadJokeResponder>()
                .AddSingleton<IResponder, PullTheLeverResponder>()
                .AddSingleton<IResponder, EnglishTranslationResponder>()
                .AddSingleton<IResponder, TranslationResponder>()
                .AddSingleton<IResponder, NaturalLanguageResponder>()
                .AddSingleton<IResponder, DogResponder>()
                .AddSingleton<IResponder, HelloResponder>()
                .AddSingleton<IResponder, QuoteResponder>()
                .AddSingleton<IResponder, RtxResponder>()
                .AddSingleton<IResponder, TwitchResponder>()
                .AddSingleton<IResponder, SteamGameResponder>()
                .AddSingleton<IResponder, SobResponder>()
                .AddSingleton<IResponder, RepeatPhraseResponder>()
                .BuildServiceProvider();
        }

        public async Task Run(CancellationToken token)
        {
            var socketClient = (DiscordSocketClient)responderProvider.GetRequiredService<IDiscordClient>();

            socketClient.Log += ClientLog;
            socketClient.MessageReceived += message => WrapTask(ClientMessageReceived(message, token));
            socketClient.MessageDeleted += (message, channel) => WrapTask(ClientMessageDeleted(message.Id, channel, socketClient, token));
            socketClient.UserJoined += user => WrapTask(UserJoined(user, socketClient, token));
            socketClient.UserLeft += user => WrapTask(UserLeft(user, socketClient, token));

            await socketClient.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"));
            await socketClient.StartAsync();
            await Task.Delay(-1);
        }

        private async Task WrapTask(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                logger.LogCritical(e, "Exception from event handler task");
            }
        }

        private async Task UserJoined(SocketGuildUser user, DiscordSocketClient client, CancellationToken token)
        {
            logger.LogInformation("User joined: {0}", user);

            var guild = client.Guilds.Single(x => x.Name == "ESTRANGED");

            var welcomeChannel = guild.TextChannels.Single(x => x.Name == "welcome");
            var rulesChannel = guild.TextChannels.Single(x => x.Name == "rules");

            var welcome = $"Welcome to the Estranged Discord server <@{user.Id}>! " +
                          $"See <#{rulesChannel.Id}> for the server rules, you might also be interested in these channels:";

            var interestingChannels = string.Join("\n", new[]
            {
                $"* <#437311972917248022> - Estranged: Act I discussion",
                $"* <#437312012603752458> - Estranged: The Departure discussion",
                $"* <#439742315016486922> - Work in progress development screenshots"
            });

            var moderators = guild.Roles.Single(x => x.Name == "moderators")
                                        .Members.Where(x => !x.IsBot)
                                        .OrderBy(x => x.Nickname)
                                        .Select(x => $"<@{x.Id}>");

            var moderatorList = $"Your moderators are {string.Join(", ", moderators)}.";

            await welcomeChannel.SendMessageAsync($"{welcome}\n{interestingChannels}\n{moderatorList}", options: token.ToRequestOptions());
        }

        private async Task UserLeft(SocketGuildUser user, DiscordSocketClient client, CancellationToken token)
        {
            logger.LogInformation("User left: {0}", user);

            var goodbye = $"User {user} left the server!";

            await client.GetChannelByName("goodbyes").SendMessageAsync(goodbye, options: token.ToRequestOptions());
        }

        private async Task ClientMessageDeleted(ulong messageId, ISocketMessageChannel channel, DiscordSocketClient client, CancellationToken token)
        {
            logger.LogInformation("Message deleted: {0}", messageId);

            const string deletionsChannel = "deletions";
            if (!channel.IsPublicChannel() && channel.Name != deletionsChannel)
            {
                return;
            }

            var message = _publicMessageHistory.Single(x => x.Id == messageId);
            await client.GetChannelByName(deletionsChannel).SendMessageAsync("Deleted:", false, message.QuoteMessage(), token.ToRequestOptions());
        }

        private int messageCount;

        private IList<IMessage> _publicMessageHistory = new List<IMessage>();

        private async Task ClientMessageReceived(SocketMessage socketMessage, CancellationToken token)
        {
            messageCount++;

            if (socketMessage.Channel.IsPublicChannel())
            {
                _publicMessageHistory.Add(socketMessage);

                if (_publicMessageHistory.Count > 100)
                {
                    _publicMessageHistory.RemoveAt(0);
                }
            }

            logger.LogTrace("Message received: {0}", socketMessage);
            if (socketMessage.Author.IsBot || socketMessage.Author.IsWebhook)
            {
                return;
            }

            logger.LogTrace("Finding responder services");
            var responders = responderProvider.GetServices<IResponder>().ToArray();
            logger.LogTrace("Invoking {0} responders", responders.Length);
            await Task.WhenAll(responders.Select(x => RunResponder(x, socketMessage, token)));
        }

        private async Task RunResponder(IResponder responder, IMessage message, CancellationToken token)
        {
            var stopwatch = new Stopwatch();
            logger.LogTrace("Running responder {0} for message {1}", responder.GetType().Name, message);
            stopwatch.Start();
            try
            {
                await responder.ProcessMessage(message, token);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Got exception from responder caused by message \"{0}\"", message);
            }
            logger.LogTrace("Completed responder {0} in {1} for message: {2}", responder.GetType().Name, stopwatch.Elapsed, message);
        }

        private Task ClientLog(LogMessage logMessage)
        {
            logger.Log(GetLogLevel(logMessage.Severity), logMessage.Exception, logMessage.Message);
            return null;
        }

        private LogLevel GetLogLevel(LogSeverity severity)
        {
            return severity switch
            {
                LogSeverity.Critical => LogLevel.Critical,
                LogSeverity.Debug => LogLevel.Debug,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Info => LogLevel.Information,
                LogSeverity.Verbose => LogLevel.Trace,
                LogSeverity.Warning => LogLevel.Warning,
                _ => throw new NotImplementedException(),
            };
        }
    }
}
