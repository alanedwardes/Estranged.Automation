using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ae.Steam.Client;
using Ae.Steam.Client.Entities;
using Ae.Steam.Client.Exceptions;
using Discord;
using Microsoft.Extensions.Logging;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public sealed class SteamGameResponder : IResponder
    {
        private readonly string[] commands = new string[] { "what to play", "recommend me a game", "what to buy", "oh man it sure it annoying I don't have anything to play" };
        private readonly Lazy<Task<IReadOnlyList<SteamAppSummary>>> _steamList;
        private readonly ILogger<SteamGameResponder> _logger;
        private readonly ISteamClient _steamClient;

        public SteamGameResponder(ILogger<SteamGameResponder> logger, ISteamClient steamClient)
        {
            _steamList = new Lazy<Task<IReadOnlyList<SteamAppSummary>>>(() => steamClient.GetAppList(CancellationToken.None));
            _logger = logger;
            _steamClient = steamClient;
        }

        private async Task<SteamAppSummary> GetRandomGame(CancellationToken token, bool onlySafeForWork)
        {
            var steamApps = await _steamList.Value;
            var randomApp = steamApps.OrderBy(x => Guid.NewGuid()).First();
            var appId = randomApp.AppId;

            try
            {
                if (onlySafeForWork && await _steamClient.IsAppAdultOnly(appId, token))
                {
                    return null;
                }
            }
            catch (SteamClientException e)
            {
                _logger.LogWarning(e, "Error getting is adult only flag");
                return null;
            }

            SteamAppDetails steamAppDetails;
            try
            {
                steamAppDetails = await _steamClient.GetAppDetails(randomApp.AppId, token);
            }
            catch (SteamClientException e)
            {
                _logger.LogWarning(e, "Error getting app details");
                return null;
            }

            if (steamAppDetails.Type != "game")
            {
                return null;
            }

            if (steamAppDetails.ReleaseDate.ComingSoon)
            {
                return null;
            }

            if (steamAppDetails.RequiredAge >= 18 && onlySafeForWork)
            {
                return null;
            }

            return randomApp;
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {            
            if (!commands.Any(x => message.Content.Contains(x, StringComparison.InvariantCultureIgnoreCase)))
            {
                return;
            }

            var onlySafeForWork = message.Channel.IsPublicChannel();

            // Try 3 times to get something
            var randomGame = await GetRandomGame(token, onlySafeForWork) ?? await GetRandomGame(token, onlySafeForWork) ?? await GetRandomGame(token, onlySafeForWork);

            if (randomGame == null)
            {
                await message.Channel.SendMessageAsync("Hmm, read a book?", options: token.ToRequestOptions());
                return;
            }
            
            await message.Channel.SendMessageAsync($"You should try {randomGame.Name}\nFind it here: {randomGame.StorePage}", options: token.ToRequestOptions());
        }
    }
}
