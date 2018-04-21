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
        private IDiscordClient discordClient;

        public DiscordRunner(ILogger<DiscordRunner> logger)
        {
            this.logger = logger;
        }

        public async Task Run(CancellationToken token)
        {
            var client = new DiscordSocketClient();
            discordClient = client;

            client.Log += ClientLog;

            await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"));
            await client.StartAsync();

            client.MessageReceived += ClientMessageReceived;

            await Task.Delay(-1, token);
        }

        private async Task ClientMessageReceived(SocketMessage socketMessage)
        {
            if (socketMessage.Author.IsBot || socketMessage.Author.IsWebhook || string.IsNullOrWhiteSpace(socketMessage.Content))
            {
                return;
            }

            string content = socketMessage.Content.Trim();
            string contentLower = content.ToLower();

            if (contentLower.StartsWith("/botname"))
            {
                string newName = content.Substring(8).Trim();
                logger.LogInformation("Changing name to {0}", newName);
                await discordClient.CurrentUser.ModifyAsync(x => x.Username = newName);
            }

            if (contentLower.Contains("linux") && !contentLower.Contains("gnu/linux"))
            {
                logger.LogInformation("Sending Linux text");
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
