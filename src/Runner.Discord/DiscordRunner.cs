using Estranged.Automation.Shared;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Discord.WebSocket;
using Discord;
using System;

namespace Estranged.Automation.Runner.Syndication
{
    public class DiscordRunner : IRunner
    {
        private readonly ILogger<DiscordRunner> logger;

        public DiscordRunner(ILogger<DiscordRunner> logger)
        {
            this.logger = logger;
        }

        public async Task Run(CancellationToken token)
        {
            var client = new DiscordSocketClient();

            client.Log += ClientLog;

            await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"));
            await client.StartAsync();

            client.MessageReceived += ClientMessageReceived;

            await Task.Delay(-1);
        }

        private async Task ClientMessageReceived(SocketMessage socketMessage)
        {
            if (socketMessage.Author.IsBot || socketMessage.Author.IsWebhook)
            {
                return;
            }

            logger.LogInformation("Received message: {0}", socketMessage);

            if (socketMessage.Content != null && socketMessage.Content.ToLower().Contains("linux"))
            {
                await socketMessage.Channel.SendMessageAsync("I'd just like to interject for a moment. What you’re referring to as Linux, is in fact, GNU/Linux, or as I’ve recently taken to calling it, GNU plus Linux.");
            }
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
