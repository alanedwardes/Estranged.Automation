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

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (!(message.Author is SocketGuildUser guildUser))
            {
                return;
            }

            if (!guildUser.Roles.Any(x => x.IsHoisted))
            {
                return;
            }

            if (!message.Content.StartsWith("/"))
            {
                return;
            }

            string command = message.Content.Substring(1).ToLower();

            if (command.StartsWith("botname"))
            {
                string newName = message.Content.Substring(8).Trim();
                logger.LogInformation("Changing name to {0}", newName);
                await discordClient.CurrentUser.ModifyAsync(x => x.Username = newName);
                await message.DeleteAsync();
                await message.Channel.SendMessageAsync($"{message.Author} changed my name to `{newName}`");
                return;
            }

            logger.LogInformation("{0} sent unknown command {1}", message.Author, message);
            await message.Channel.SendMessageAsync($"{message.Author}, I do not understand `{message.Content}`");
        }
    }
}
