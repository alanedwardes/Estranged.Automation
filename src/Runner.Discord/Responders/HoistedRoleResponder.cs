using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public class HoistedRoleResponder : IResponder
    {
        private readonly ILogger<HoistedRoleResponder> logger;
        private readonly IDiscordClient discordClient;

        public HoistedRoleResponder(ILogger<HoistedRoleResponder> logger, IDiscordClient discordClient)
        {
            this.logger = logger;
            this.discordClient = discordClient;
        }

        private readonly HashSet<ulong> mutedUsers = new HashSet<ulong>();
        private async Task ProcessMutedUsers(IMessage message, CancellationToken token)
        {
            if (mutedUsers.Contains(message.Author.Id))
            {
                logger.LogInformation("Deleting message from muted user {0}: {1}", message.Author, message);
                await message.DeleteAsync(token.ToRequestOptions());
            }
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (!message.Content.StartsWith("/"))
            {
                await ProcessMutedUsers(message, token);
                return;
            }

            logger.LogInformation("{0} sent command {1}", message.Author, message);

            // Delete the command message
            await message.DeleteAsync(token.ToRequestOptions());

            if (!(message.Author is SocketGuildUser guildUser))
            {
                return;
            }

            if (!guildUser.Roles.Any(x => x.IsHoisted))
            {
                return;
            }

            string command = message.Content.Substring(1).ToLower();

            if (command.StartsWith("botname"))
            {
                string newName = message.Content.Substring(8).Trim();
                await discordClient.CurrentUser.ModifyAsync(x => x.Username = newName, token.ToRequestOptions());
                await message.Channel.SendMessageAsync($"{message.Author} changed my name to `{newName}`", options: token.ToRequestOptions());
                return;
            }

            if (command.StartsWith("help"))
            {
                await message.Author.SendMessageAsync($"Available commands:\n* /mute @user1 @user2\n* /unmute @user1 @user2\n* /botname <newname>", options: token.ToRequestOptions());
                return;
            }

            if (command.StartsWith("mute"))
            {
                string mutedUsersDebug = string.Join(", ", message.MentionedUserIds);
                foreach (var userId in message.MentionedUserIds)
                {
                    mutedUsers.Add(userId);
                }
                await message.Channel.SendMessageAsync($"{message.Author} muted user(s) `{mutedUsersDebug}`", options: token.ToRequestOptions());
                return;
            }

            if (command.StartsWith("unmute"))
            {
                string mutedUsersDebug = string.Join(", ", message.MentionedUserIds);
                foreach (var userId in message.MentionedUserIds)
                {
                    mutedUsers.Remove(userId);
                }
                await message.Channel.SendMessageAsync($"{message.Author} unmuted user(s) `{mutedUsersDebug}`", options: token.ToRequestOptions());
                return;
            }

            await message.Channel.SendMessageAsync($"{message.Author}, I do not understand `{message.Content}`", options: token.ToRequestOptions());
        }
    }
}
