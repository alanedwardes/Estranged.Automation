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
			var tokens = argText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (tokens.Length == 0)
			{
				await originalMessage.Channel.SendMessageAsync("Usage: purgeold <#channel>|<channelId>|#name <count>", messageReference: new MessageReference(originalMessage.Id), options: token.ToRequestOptions());
				return;
			}

			// Parse target channel from first token
			var channelArg = tokens[0];
			var guild = textChannel.Guild;
			ITextChannel targetChannel = null;
			if (channelArg.StartsWith("<#") && channelArg.EndsWith(">") && channelArg.Length > 3)
			{
				var inner = channelArg.Substring(2, channelArg.Length - 3);
				if (ulong.TryParse(inner, out var mentionedId))
				{
					targetChannel = await guild.GetTextChannelAsync(mentionedId);
				}
			}
			else if (ulong.TryParse(channelArg, out var channelIdFromNumber))
			{
				targetChannel = await guild.GetTextChannelAsync(channelIdFromNumber);
			}
			else
			{
				var name = channelArg.StartsWith("#") && channelArg.Length > 1 ? channelArg[1..] : channelArg;
				var allTextChannels = await guild.GetTextChannelsAsync();
				targetChannel = allTextChannels.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.InvariantCultureIgnoreCase));
			}

			if (targetChannel == null)
			{
				await originalMessage.Channel.SendMessageAsync("Channel not found. Specify a valid channel (mention, ID, or #name).", messageReference: new MessageReference(originalMessage.Id), options: token.ToRequestOptions());
				return;
			}

			if (targetChannel.IsPublicChannel())
			{
				await originalMessage.Channel.SendMessageAsync("Refusing to purge a public channel.", messageReference: new MessageReference(originalMessage.Id), options: token.ToRequestOptions());
				return;
			}

			// Parse count from second token
			int requestedCount = 0;
			if (tokens.Length > 1)
			{
				_ = int.TryParse(tokens[1], out requestedCount);
			}

			if (requestedCount <= 0)
			{
				await originalMessage.Channel.SendMessageAsync("Please specify a positive count (e.g., purgeold #my-channel 100).", messageReference: new MessageReference(originalMessage.Id), options: token.ToRequestOptions());
				return;
			}

            var deleteDelayMs = 5_000;

			var cutoff = DateTimeOffset.UtcNow - TimeSpan.FromDays(30);

			int deleted = 0;
			await originalMessage.Channel.SendMessageAsync($"Purging up to {requestedCount} messages older than 30 days in #{targetChannel.Name}...", messageReference: new MessageReference(originalMessage.Id), options: token.ToRequestOptions());

			var channel = targetChannel;
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

				if (messages.Count == 0)
				{
					break;
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
						var requestOptions = token.ToRequestOptions();
						requestOptions.RetryMode = RetryMode.Retry502 | RetryMode.RetryTimeouts;
						await message.DeleteAsync(options: requestOptions);
						deleted++;
						deletedInThisChannel++;
						lastPurgedTimestamp = message.Timestamp;
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Failed to delete message {MessageId} in channel {ChannelId}", message.Id, channel.Id);
					}

					await Task.Delay(deleteDelayMs, token);
				}
			}

			if (deletedInThisChannel > 0)
			{
				await originalMessage.Channel.SendMessageAsync($"Last message purged timestamp: {lastPurgedTimestamp.Value:O}", messageReference: new MessageReference(originalMessage.Id), options: token.ToRequestOptions());
			}

			await originalMessage.Channel.SendMessageAsync($"Purged {deleted} message(s) in #{channel.Name}.", messageReference: new MessageReference(originalMessage.Id), options: token.ToRequestOptions());
        }
    }
}


