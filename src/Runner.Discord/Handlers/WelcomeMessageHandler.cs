using Discord;
using Discord.WebSocket;
using Estranged.Automation.Runner.Discord.Events;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Handlers
{
    public sealed class WelcomeMessageHandler : IUserJoinedHandler
    {
        private readonly ILogger<WelcomeMessageHandler> _logger;
        private readonly DiscordSocketClient _discordClient;

        public WelcomeMessageHandler(ILogger<WelcomeMessageHandler> logger, DiscordSocketClient discordClient)
        {
            _logger = logger;
            _discordClient = discordClient;
        }

        public async Task UserJoined(SocketGuildUser user, CancellationToken token)
        {
            _logger.LogInformation("User joined: {0}", user);

            var guild = _discordClient.Guilds.Single(x => x.Name == "ESTRANGED");

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
    }
}
