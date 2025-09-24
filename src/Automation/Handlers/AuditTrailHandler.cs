using Discord;
using Discord.WebSocket;
using Estranged.Automation.Events;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Handlers
{
    public sealed class AuditTrailHandler : IMessageDeleted, IMessageUpdated, IUserJoinedHandler, IUserLeftHandler
    {
        private readonly ILogger<AuditTrailHandler> _logger;

        public AuditTrailHandler(ILogger<AuditTrailHandler> logger) => _logger = logger;

        private const ulong AuditTrailChannelId = 845981918130733106;

        public async Task MessageDeleted(Cacheable<IMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, CancellationToken token)
        {
            return;

            if (channel.Id == AuditTrailChannelId)
            {
                return;
            }

            try
            {
                await Task.WhenAll(message.DownloadAsync(), channel.DownloadAsync());
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to download deleted message {MessageId} in channel {ChannelId}", message.Id, channel.Id);
                return;
            }

            if (message.HasValue && message.Value.Author.IsBot)
            {
                return;
            }

            if (message.HasValue)
            {
                _logger.LogInformation("Message {Message} was deleted in {Channel}", message.Value, channel);
            }
            else
            {
                _logger.LogInformation("Message {Message} was deleted in {Channel} (message not cached)", message.Id, channel);
            }
        }

        public async Task MessageUpdated(Cacheable<IMessage, ulong> message, SocketMessage socketMessage, ISocketMessageChannel channel, CancellationToken token)
        {
            if (channel.Id == AuditTrailChannelId)
            {
                return;
            }

            if (!socketMessage.EditedTimestamp.HasValue)
            {
                // This edit was made by Discord
                return;
            }

            if (message.HasValue)
            {
                _logger.LogInformation("Message {Original} was updated to {Updated} in {Channel}", message.Value, await message.DownloadAsync(), channel);
            }
            else
            {
                _logger.LogInformation("Message {Message} was updated in {Channel} (message not cached)", await message.DownloadAsync(), channel);
            }
        }

        public Task UserJoined(SocketGuildUser user, CancellationToken token)
        {
            _logger.LogInformation("User {User} joined", user);
            return Task.CompletedTask;
        }

        public Task UserLeft(SocketUser user, CancellationToken token)
        {
            _logger.LogInformation("User {User} left", user);
            return Task.CompletedTask;
        }
    }
}
