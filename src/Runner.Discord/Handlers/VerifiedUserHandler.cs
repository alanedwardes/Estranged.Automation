using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Discord.WebSocket;
using Discord;
using System.Linq;
using System.Collections.Concurrent;

namespace Estranged.Automation.Runner.Discord.Handlers
{
    public sealed class VerifiedUserHandler : IReactionAddedHandler
    {
        private readonly IProducerConsumerCollection<ulong> _usersWithMembersRole = new ConcurrentBag<ulong>();
        private readonly ILogger<VerifiedUserHandler> _logger;
        private readonly IDiscordClient _discordClient;

        public VerifiedUserHandler(ILogger<VerifiedUserHandler> logger, IDiscordClient discordClient)
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

            if (_usersWithMembersRole.Contains(reaction.UserId))
            {
                return;
            }

            var guild = ((DiscordSocketClient)_discordClient).Guilds.Single(x => x.Name == "ESTRANGED");

            // Get the "members" role
            var role = guild.GetRole(845401897204580412);

            // Get a list of all users in the server
            _logger.LogInformation("Getting a list of all guild members because {UserId} reacted in the rules channel", reaction.UserId);
            var bufferedUsers = await guild.GetUsersAsync(options: token.ToRequestOptions()).FlattenAsync();

            // Get the user that added the reaction
            var user = bufferedUsers.Single(x => x.Id == reaction.UserId);

            // Add the role to the user
            _logger.LogInformation("Adding role {Role} to {User}", role, user);
            await user.AddRoleAsync(role, options: token.ToRequestOptions());
            _usersWithMembersRole.TryAdd(user.Id);
        }
    }
}
