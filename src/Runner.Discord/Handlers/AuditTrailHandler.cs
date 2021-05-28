using Discord;
using Discord.WebSocket;
using Estranged.Automation.Runner.Discord.Events;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Handlers
{
    public sealed class AuditTrailHandler : IGuildMemberUpdated, IMessageDeleted, IMessageUpdated, IUserJoinedHandler, IUserLeftHandler, IUserUpdated
    {
        private readonly ILogger<AuditTrailHandler> _logger;

        public AuditTrailHandler(ILogger<AuditTrailHandler> logger) => _logger = logger;

        public Task GuildMemberUpdated(SocketGuildUser oldMember, SocketGuildUser newMember, CancellationToken token)
        {
            _logger.LogInformation("User {OldMember} was updated to {NewMember}", oldMember, newMember);
            return Task.CompletedTask;
        }

        public Task UserUpdated(SocketUser oldUser, SocketUser newUser, CancellationToken token)
        {
            _logger.LogInformation("User {OldUser} was updated to {NewUser}", oldUser, newUser);
            return Task.CompletedTask;
        }

        public Task MessageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel, CancellationToken token)
        {
            _logger.LogInformation("Message {MessageId} was deleted in {Channel}", message.Id, channel);
            return Task.CompletedTask;
        }

        public Task MessageUpdated(Cacheable<IMessage, ulong> message, SocketMessage socketMessage, ISocketMessageChannel channel, CancellationToken token)
        {
            _logger.LogInformation("Message {MessageId} was updated to {NewMessage} in {Channel}", message.Id, socketMessage, channel);
            return Task.CompletedTask;
        }

        public Task UserJoined(SocketGuildUser user, CancellationToken token)
        {
            _logger.LogInformation("User {User} joined", user);
            return Task.CompletedTask;
        }

        public Task UserLeft(SocketGuildUser user, CancellationToken token)
        {
            _logger.LogInformation("User {User} left", user);
            return Task.CompletedTask;
        }
    }
}
