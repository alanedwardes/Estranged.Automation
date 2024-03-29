﻿using Estranged.Automation.Shared;
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

                return Task.WhenAll(_serviceProvider.GetServices<IResponder>().Select(x => WrapTask(x.ProcessMessage(message, token))));
            };

            _discordSocketClient.MessageDeleted += (message, channel) => Task.WhenAll(_serviceProvider.GetServices<IMessageDeleted>().Select(x => WrapTask(x.MessageDeleted(message, channel, token))));
            _discordSocketClient.MessageUpdated += (message, socketMessage, channel) => Task.WhenAll(_serviceProvider.GetServices<IMessageUpdated>().Select(x => WrapTask(x.MessageUpdated(message, socketMessage, channel, token))));
            _discordSocketClient.UserJoined += user => Task.WhenAll(_serviceProvider.GetServices<IUserJoinedHandler>().Select(x => WrapTask(x.UserJoined(user, token))));
            _discordSocketClient.UserLeft += (guild, user) => Task.WhenAll(_serviceProvider.GetServices<IUserLeftHandler>().Select(x => WrapTask(x.UserLeft(user, token))));
            _discordSocketClient.ReactionAdded += (message, channel, reaction) => Task.WhenAll(_serviceProvider.GetServices<IReactionAddedHandler>().Select(x => WrapTask(x.ReactionAdded(message, channel, reaction, token))));
            _discordSocketClient.GuildMemberUpdated += (oldMember, newMember) => Task.WhenAll(_serviceProvider.GetServices<IGuildMemberUpdated>().Select(x => WrapTask(x.GuildMemberUpdated(oldMember, newMember, token))));
            _discordSocketClient.RoleUpdated += (oldRole, newRole) => Task.WhenAll(_serviceProvider.GetServices<IRoleUpdated>().Select(x => WrapTask(x.RoleUpdated(oldRole, newRole, token))));
            _discordSocketClient.UserUpdated += (oldUser, newUser) => Task.WhenAll(_serviceProvider.GetServices<IUserUpdated>().Select(x => WrapTask(x.UserUpdated(oldUser, newUser, token))));
            _discordSocketClient.UserIsTyping += (socketUser, channel) => Task.WhenAll(_serviceProvider.GetServices<IUserIsTyping>().Select(x => WrapTask(x.UserIsTyping(socketUser, channel, token))));

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
