using Estranged.Automation.Shared;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Discord.WebSocket;
using Discord;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Estranged.Automation.Runner.Discord
{
    public sealed class DiscordRunner : IRunner
    {
        private readonly ILogger<DiscordRunner> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly DiscordSocketClient _discordSocketClient;

        public DiscordRunner(ILogger<DiscordRunner> logger, IServiceProvider serviceProvider, DiscordSocketClient discordSocketClient)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _discordSocketClient = discordSocketClient;
        }

        private static readonly IReadOnlyDictionary<LogSeverity, LogLevel> LOG_LEVEL_MAPPING = new Dictionary<LogSeverity, LogLevel>
        {
            { LogSeverity.Critical, LogLevel.Critical },
            { LogSeverity.Debug, LogLevel.Debug },
            { LogSeverity.Error, LogLevel.Error },
            { LogSeverity.Info, LogLevel.Information },
            { LogSeverity.Verbose, LogLevel.Trace },
            { LogSeverity.Warning, LogLevel.Warning }
        };

        public async Task Run(CancellationToken token)
        {
            _discordSocketClient.Log += logMessage =>
            {
                _logger.Log(LOG_LEVEL_MAPPING[logMessage.Severity], logMessage.Exception, logMessage.Message);
                return Task.CompletedTask;
            };

            _discordSocketClient.MessageReceived += message =>
            {
                if (message.Author.IsBot || message.Author.IsWebhook)
                {
                    return Task.CompletedTask;
                }

                return Task.WhenAll(_serviceProvider.GetServices<IResponder>().Select(x => WrapTask(x.ProcessMessage(message, token))));
            };

            _discordSocketClient.MessageDeleted += (message, channel) =>
            {
                return Task.WhenAll(_serviceProvider.GetServices<IMessageDeleted>().Select(x => WrapTask(x.MessageDeleted(message, channel, token))));
            };

            _discordSocketClient.UserJoined += user =>
            {
                return Task.WhenAll(_serviceProvider.GetServices<IUserJoinedHandler>().Select(x => WrapTask(x.UserJoined(user, token))));
            };

            _discordSocketClient.UserLeft += user =>
            {
                return Task.WhenAll(_serviceProvider.GetServices<IUserLeftHandler>().Select(x => WrapTask(x.UserLeft(user, token))));
            };

            _discordSocketClient.ReactionAdded += (message, channel, reaction) =>
            {
                return Task.WhenAll(_serviceProvider.GetServices<IReactionAddedHandler>().Select(x => WrapTask(x.ReactionAdded(message, channel, reaction, token))));
            };

            await _discordSocketClient.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"));
            await _discordSocketClient.StartAsync();
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
                _logger.LogCritical(e, "Exception from event handler");
            }
        }
    }
}
