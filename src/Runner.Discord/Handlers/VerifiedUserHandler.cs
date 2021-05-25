using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Discord.WebSocket;
using Discord;
using System.Linq;
using Estranged.Automation.Runner.Discord.Events;

namespace Estranged.Automation.Runner.Discord.Handlers
{
    public sealed class VerifiedUserHandler : IReactionAddedHandler
    {
        private readonly ILogger<VerifiedUserHandler> _logger;
        private readonly DiscordSocketClient _discordClient;

        public VerifiedUserHandler(ILogger<VerifiedUserHandler> logger, DiscordSocketClient discordClient)
        {
            _logger = logger;
            _discordClient = discordClient;
        }

        public async Task ReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction, CancellationToken token)
        {
            if (channel.Name != "verification")
            {
                return;
            }

            var guild = _discordClient.Guilds.Single(x => x.Name == "ESTRANGED");

            // The "verified" role
            var role = guild.GetRole(845401897204580412);

            var user = await _discordClient.Rest.GetGuildUserAsync(guild.Id, reaction.UserId);
            if (user.RoleIds.Contains(role.Id))
            {
                _logger.LogWarning("User {User} already has role {Role}", user, role);
                return;
            }

            _logger.LogInformation("Adding role {Role} to {User}", role, user);
            await user.AddRoleAsync(role);

            user = await _discordClient.Rest.GetGuildUserAsync(guild.Id, reaction.UserId);

            _logger.LogInformation("{User} now has roles {Roles}", user, string.Join(", ", user.RoleIds.Select(guild.GetRole).Select(x => x.Name)));
        }
    }
}
