using Discord;
using Estranged.Automation.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OllamaSharp.Models.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Responders
{
    internal sealed class RetentionResponder : IResponder
    {
        private readonly ILogger<RetentionResponder> _logger;
        private readonly IConfiguration _configuration;

        public RetentionResponder(ILogger<RetentionResponder> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task ProcessMessage(IMessage originalMessage, CancellationToken token)
        {
            if (originalMessage.Author.Id != 269883106792701952)
            {
                return;
            }

            if (originalMessage.Channel.IsPublicChannel())
            {
                return;
            }

            if (originalMessage.Channel is not ITextChannel textChannel)
            {
                return;
            }

            const string trigger = "purgeold";
            var content = originalMessage.Content?.Trim() ?? string.Empty;
            if (!content.StartsWith(trigger, StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

			var argText = content.Length > trigger.Length ? content[trigger.Length..].Trim() : string.Empty;
			int requestedCount = 0;
			_ = int.TryParse(argText, out requestedCount);

            var deleteDelayMs = 1_000;

			var cutoff = DateTimeOffset.UtcNow - TimeSpan.FromDays(30);

			int deleted = 0;
			var channelBreakdown = new List<string>();
			await originalMessage.Channel.SendMessageAsync($"Purging up to {requestedCount} messages older than 30 days across private channels...", messageReference: new MessageReference(originalMessage.Id), options: token.ToRequestOptions());

			var guild = textChannel.Guild;
			var channels = await guild.GetTextChannelsAsync();
			foreach (var channel in channels.Where(c => !c.IsPublicChannel()))
			{
				if (deleted >= requestedCount)
				{
					break;
				}

				var pinned = await channel.GetPinnedMessagesAsync(options: token.ToRequestOptions());
				var pinnedIds = new HashSet<ulong>(pinned.Select(x => x.Id));

				IMessage fromMessage = null;
				DateTimeOffset? lastPurgedTimestamp = null;
				int deletedInThisChannel = 0;
				while (deleted < requestedCount)
				{
					IList<IMessage> messages = [];
					if (fromMessage == null)
					{
						Console.WriteLine($"Retrieving latest messages for #{channel.Name}...");
                        messages = [.. await channel.GetMessagesAsync().FlattenAsync()];
					}
					else
					{
						Console.WriteLine($"Retrieving messages before {fromMessage.Id} for #{channel.Name}...");
                        messages = [.. await channel.GetMessagesAsync(fromMessage, Direction.Before, 100, options: token.ToRequestOptions()).FlattenAsync()];
                    }

					foreach (var message in messages)
					{
						fromMessage = message;

						if (deleted >= requestedCount)
						{
							break;
						}

						if (pinnedIds.Contains(message.Id))
						{
							continue;
						}

						if (message.Timestamp >= cutoff)
						{
							continue;
						}

						try
						{
							await message.DeleteAsync(options: token.ToRequestOptions());
							deleted++;
							deletedInThisChannel++;
							lastPurgedTimestamp = message.Timestamp;
							await Task.Delay(deleteDelayMs, token);
						}
						catch (Exception ex)
						{
							_logger.LogWarning(ex, "Failed to delete message {MessageId} in channel {ChannelId}", message.Id, channel.Id);
						}
					}
				}

				if (deletedInThisChannel > 0)
				{
					channelBreakdown.Add($"#{channel.Name} ({deletedInThisChannel})");
					await originalMessage.Channel.SendMessageAsync($"Last message purged timestamp: {lastPurgedTimestamp.Value:O}", messageReference: new MessageReference(originalMessage.Id), options: token.ToRequestOptions());
				}
			}

			var breakdownText = channelBreakdown.Count > 0 ? $" Deleted from: {string.Join(", ", channelBreakdown)}" : string.Empty;
			await originalMessage.Channel.SendMessageAsync($"Purged {deleted} message(s) across private channels.{breakdownText}", messageReference: new MessageReference(originalMessage.Id), options: token.ToRequestOptions());
        }
    }
}


