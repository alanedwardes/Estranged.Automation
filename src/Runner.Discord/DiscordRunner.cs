using Estranged.Automation.Shared;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Discord.WebSocket;
using Discord;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Estranged.Automation.Runner.Discord;
using System.Diagnostics;
using System.Collections.Generic;

namespace Estranged.Automation.Runner.Syndication
{
    public class DiscordRunner : IRunner
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

        public async Task Run(CancellationToken token)
        {
            _discordSocketClient.Log += ClientLog;
            _discordSocketClient.MessageReceived += message => WrapTask(MessageReceived(message, token));
            _discordSocketClient.MessageDeleted += (message, channel) => WrapTask(MessageDeleted(message.Id, channel, _discordSocketClient, token));
            _discordSocketClient.UserJoined += user => WrapTask(UserJoined(user, _discordSocketClient, token));
            _discordSocketClient.UserLeft += user => WrapTask(UserLeft(user, _discordSocketClient, token));
            _discordSocketClient.ReactionAdded += (message, channel, reaction) => ReactionAdded(message, _discordSocketClient, channel, reaction, token);

            await _discordSocketClient.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"));
            await _discordSocketClient.StartAsync();
            await Task.Delay(-1);
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> message, DiscordSocketClient client, ISocketMessageChannel channel, SocketReaction reaction, CancellationToken token)
        {
            if (channel.Name != "rules")
            {
                return;
            }

            var guild = client.Guilds.Single(x => x.Name == "ESTRANGED");

            // Get the "members" role
            var role = guild.GetRole(845401897204580412);

            // Get the user that added the reaction
            var user = guild.GetUser(reaction.UserId);

            if (role.Members.Contains(user))
            {
                // The member already has the role
                return;
            }

            // Add the role to the user
            await user.AddRoleAsync(role, options: token.ToRequestOptions());
        }

        private async Task WrapTask(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Exception from event handler task");
            }
        }

        private async Task UserJoined(SocketGuildUser user, DiscordSocketClient client, CancellationToken token)
        {
            _logger.LogInformation("User joined: {0}", user);

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
            _logger.LogInformation("User left: {0}", user);

            var goodbye = $"User {user} left the server!";

            await client.GetChannelByName("goodbyes").SendMessageAsync(goodbye, options: token.ToRequestOptions());
        }

        private async Task MessageDeleted(ulong messageId, ISocketMessageChannel channel, DiscordSocketClient client, CancellationToken token)
        {
            _logger.LogInformation("Message deleted: {0}", messageId);

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

        private async Task MessageReceived(SocketMessage socketMessage, CancellationToken token)
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

            _logger.LogTrace("Message received: {0}", socketMessage);
            if (socketMessage.Author.IsBot || socketMessage.Author.IsWebhook)
            {
                return;
            }

            _logger.LogTrace("Finding responder services");
            var responders = _serviceProvider.GetServices<IResponder>().ToArray();
            _logger.LogTrace("Invoking {0} responders", responders.Length);
            await Task.WhenAll(responders.Select(x => RunResponder(x, socketMessage, token)));
        }

        private async Task RunResponder(IResponder responder, IMessage message, CancellationToken token)
        {
            var stopwatch = new Stopwatch();
            _logger.LogTrace("Running responder {0} for message {1}", responder.GetType().Name, message);
            stopwatch.Start();
            try
            {
                await responder.ProcessMessage(message, token);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Got exception from responder caused by message \"{0}\"", message);
            }
            _logger.LogTrace("Completed responder {0} in {1} for message: {2}", responder.GetType().Name, stopwatch.Elapsed, message);
        }

        private Task ClientLog(LogMessage logMessage)
        {
            _logger.Log(GetLogLevel(logMessage.Severity), logMessage.Exception, logMessage.Message);
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
