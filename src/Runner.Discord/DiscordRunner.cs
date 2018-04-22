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

            var responderProvider = new ServiceCollection()
                .AddSingleton(loggerFactory)
                .AddLogging()
                .AddSingleton(discordClient)
                .AddSingleton<TextResponder>()
                .AddSingleton<HoistedRoleResponder>()
                .BuildServiceProvider();

            client.Log += ClientLog;

            await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"));
            await client.StartAsync();

            client.MessageReceived += message => ClientMessageReceived(responderProvider, message, token);

            await Task.Delay(-1, token);
        }

        private Task ClientMessageReceived(IServiceProvider provider, SocketMessage socketMessage, CancellationToken token)
        {
            if (socketMessage.Author.IsBot || socketMessage.Author.IsWebhook || string.IsNullOrWhiteSpace(socketMessage.Content))
            {
                return Task.CompletedTask;
            }

            // Run all message processors in parallel, don't wait for completion
            Task.WhenAll(provider.GetServices<IResponder>().Select(x => x.ProcessMessage(socketMessage, token)));

            return Task.CompletedTask;
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
