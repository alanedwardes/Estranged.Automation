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

namespace Estranged.Automation.Runner.Syndication
{
    public class DiscordRunner : IRunner
    {
        private readonly ILogger<DiscordRunner> logger;
        private readonly ILoggerFactory loggerFactory;
        private IServiceProvider responderProvider;

        public DiscordRunner(ILogger<DiscordRunner> logger, ILoggerFactory loggerFactory, HttpClient httpClient, TranslationClient translationClient, LanguageServiceClient languageServiceClient)
        {
            this.logger = logger;
            this.loggerFactory = loggerFactory;

            responderProvider = new ServiceCollection()
                .AddSingleton(loggerFactory)
                .AddLogging()
                .AddSingleton(httpClient)
                .AddSingleton(translationClient)
                .AddSingleton(languageServiceClient)
                .AddSingleton<IDiscordClient, DiscordSocketClient>()
                .AddSingleton<IResponder, TextResponder>()
                .AddSingleton<IResponder, HoistedRoleResponder>()
                .AddSingleton<IResponder, DadJokeResponder>()
                .AddSingleton<IResponder, PullTheLeverResponder>()
                .AddSingleton<IResponder, EnglishTranslationResponder>()
                .AddSingleton<IResponder, TranslationResponder>()
                .AddSingleton<IResponder, NaturalLanguageResponder>()
                .AddSingleton<IResponder, DogResponder>()
                .AddSingleton<IResponder, RegionResponder>()
                .BuildServiceProvider();
        }

        public async Task Run(CancellationToken token)
        {
            var socketClient = (DiscordSocketClient)responderProvider.GetRequiredService<IDiscordClient>();

            socketClient.Log += ClientLog;
            socketClient.MessageReceived += message => WrapTask(ClientMessageReceived(message, token));
            socketClient.UserJoined += user => WrapTask(UserJoined(user, socketClient, token));

            await socketClient.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"));
            await socketClient.StartAsync();
            await Task.Delay(-1, token);
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

            // #general channel ID
            var welcomeChannel = await client.GetGroupChannelAsync(368117881000427540);

            var interestingChannels = new[]
            {
                "* #act-i - Estranged: Act I discussion",
                "* #act-ii - Estranged: Act II discussion",
                "* #screenshots - work in progress development screenshots"
            };

            var welcome = $"Welcome to the Estranged Discord server {user}! See #rules for the server rules, you might also be interested in these channels:\n{string.Join("\n", interestingChannels)}";

            await welcomeChannel.SendMessageAsync(welcome, options: token.ToRequestOptions());
        }

        private async Task ClientMessageReceived(SocketMessage socketMessage, CancellationToken token)
        {
            logger.LogTrace("Message received: {0}", socketMessage);
            if (socketMessage.Author.IsBot || socketMessage.Author.IsWebhook || string.IsNullOrWhiteSpace(socketMessage.Content))
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
            logger.Log(GetLogLevel(logMessage.Severity), -1, logMessage.Message, logMessage.Exception, null);
            return null;
        }

        private LogLevel GetLogLevel(LogSeverity severity)
        {
            switch (severity)
            {
                case LogSeverity.Critical:
                    return LogLevel.Critical;
                case LogSeverity.Debug:
                    return LogLevel.Debug;
                case LogSeverity.Error:
                    return LogLevel.Error;
                case LogSeverity.Info:
                    return LogLevel.Information;
                case LogSeverity.Verbose:
                    return LogLevel.Trace;
                case LogSeverity.Warning:
                    return LogLevel.Warning;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
