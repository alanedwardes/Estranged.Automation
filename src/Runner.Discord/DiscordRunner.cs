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
using System.Collections.Generic;

namespace Estranged.Automation.Runner.Syndication
{
    public class DiscordRunner : IRunner
    {
        private readonly ILogger<DiscordRunner> logger;
        private readonly ILoggerFactory loggerFactory;
        private IDiscordClient discordClient;

        public DiscordRunner(ILogger<DiscordRunner> logger, ILoggerFactory loggerFactory)
        {
            this.logger = logger;
            this.loggerFactory = loggerFactory;
        }

        public async Task Run(CancellationToken token)
        {
            var client = new DiscordSocketClient();
            discordClient = client;

            logger.LogInformation("Created client.");

            var responderProvider = new ServiceCollection()
                .AddSingleton(loggerFactory)
                .AddLogging()
                .AddSingleton(discordClient)
                .AddSingleton<IResponder, TextResponder>()
                .AddSingleton<IResponder, HoistedRoleResponder>()
                .AddSingleton<IResponder, DadJokeResponder>()
                .BuildServiceProvider();

            client.Log += ClientLog;

            await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"));
            await client.StartAsync();

            client.MessageReceived += message => ClientMessageReceived(responderProvider, message, token);

            while (true)
            {
                logger.LogInformation("Connection status: {0}", client.ConnectionState);
                await Task.Delay(TimeSpan.FromSeconds(10), token);
            }
        }

        private Task ClientMessageReceived(IServiceProvider provider, SocketMessage socketMessage, CancellationToken token)
        {
            if (socketMessage.Author.IsBot || socketMessage.Author.IsWebhook)
            {
                return Task.CompletedTask;
            }

            RunResponders(provider.GetServices<IResponder>(), socketMessage, token);
            return Task.CompletedTask;
        }

        private async Task RunResponders(IEnumerable<IResponder> responders, IMessage message, CancellationToken token)
        {
            var tasks = responders.Select(x => x.ProcessMessage(message, token)).ToArray();
            logger.LogInformation("Waiting for completion of {0} tasks", tasks.Length);
            await Task.WhenAll(tasks);
            logger.LogInformation("Completed {0} tasks for message: {1}", tasks.Length, message);
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
