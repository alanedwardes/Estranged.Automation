using Estranged.Automation.Shared;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Discord.WebSocket;
using Discord;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Estranged.Automation.Runner.Discord.Events;
using Humanizer;

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

        private readonly DateTimeOffset _startTime = DateTimeOffset.UtcNow;
        private DateTimeOffset _connectTime = DateTimeOffset.UtcNow;

        public async Task Run(CancellationToken token)
        {
            _discordSocketClient.Connected += () =>
            {
                _logger.LogTrace("Connected. Uptime {Uptime}, previous connection time {PreviousConnectionTime}", (DateTimeOffset.UtcNow - _startTime).Humanize(), (DateTimeOffset.UtcNow - _connectTime).Humanize());
                _connectTime = DateTimeOffset.UtcNow;
                return Task.CompletedTask;
            };

            _discordSocketClient.MessageReceived += message =>
            {
                if (message.Author.IsBot || message.Author.IsWebhook)
                {
                    return Task.CompletedTask;
                }

                FireAndForgetHandlers<IResponder>(responder => responder.ProcessMessage(message, token));
                return Task.CompletedTask;
            };

            _discordSocketClient.MessageDeleted += (message, channel) =>
            {
                FireAndForgetHandlers<IMessageDeleted>(handler => handler.MessageDeleted(message, channel, token));
                return Task.CompletedTask;
            };
            _discordSocketClient.MessageUpdated += (message, socketMessage, channel) =>
            {
                FireAndForgetHandlers<IMessageUpdated>(handler => handler.MessageUpdated(message, socketMessage, channel, token));
                return Task.CompletedTask;
            };
            _discordSocketClient.UserJoined += user =>
            {
                FireAndForgetHandlers<IUserJoinedHandler>(handler => handler.UserJoined(user, token));
                return Task.CompletedTask;
            };
            _discordSocketClient.UserLeft += (guild, user) =>
            {
                FireAndForgetHandlers<IUserLeftHandler>(handler => handler.UserLeft(user, token));
                return Task.CompletedTask;
            };
            _discordSocketClient.ReactionAdded += (message, channel, reaction) =>
            {
                FireAndForgetHandlers<IReactionAddedHandler>(handler => handler.ReactionAdded(message, channel, reaction, token));
                return Task.CompletedTask;
            };
            _discordSocketClient.GuildMemberUpdated += (oldMember, newMember) =>
            {
                FireAndForgetHandlers<IGuildMemberUpdated>(handler => handler.GuildMemberUpdated(oldMember, newMember, token));
                return Task.CompletedTask;
            };
            _discordSocketClient.RoleUpdated += (oldRole, newRole) =>
            {
                FireAndForgetHandlers<IRoleUpdated>(handler => handler.RoleUpdated(oldRole, newRole, token));
                return Task.CompletedTask;
            };
            _discordSocketClient.UserUpdated += (oldUser, newUser) =>
            {
                FireAndForgetHandlers<IUserUpdated>(handler => handler.UserUpdated(oldUser, newUser, token));
                return Task.CompletedTask;
            };
            _discordSocketClient.UserIsTyping += (socketUser, channel) =>
            {
                FireAndForgetHandlers<IUserIsTyping>(handler => handler.UserIsTyping(socketUser, channel, token));
                return Task.CompletedTask;
            };

            await _discordSocketClient.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"));
            await _discordSocketClient.StartAsync();
            await Task.Delay(-1);
        }

        private void FireAndForgetHandlers<T>(Func<T, Task> handlerAction)
        {
            foreach (var handler in _serviceProvider.GetServices<T>())
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await handlerAction(handler);
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical(e, "Exception from event handler");
                    }
                });
            }
        }
    }
}
