using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Ae.Steam.Client;
using Ae.Steam.Client.Entities;
using Discord;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public sealed class SteamGameResponder : IResponder
    {
        private readonly string[] commands = new string[] { "what to play", "recommend me a game", "what to buy", "oh man it sure it annoying I don't have anything to play" };
        private readonly Lazy<Task<IReadOnlyList<SteamAppSummary>>> _steamList;
        private readonly ISteamClient _steamClient;

        public SteamGameResponder(ISteamClient steamClient)
        {
            _steamList = new Lazy<Task<IReadOnlyList<SteamAppSummary>>>(() => steamClient.GetAppList(CancellationToken.None));
            _steamClient = steamClient;
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {            
            if (!commands.Any(x => message.Content.Contains(x, StringComparison.InvariantCultureIgnoreCase)))
            {
                return;
            }

            if (message.Author.Id == 269883106792701952)
            {
                const string steamStoreUrl = "https://store.steampowered.com/app/";
                int totallyRandomAppId = RandomNumberGenerator.GetInt32(0, 2) == 0 ? 261820 : 582890;
                await message.Channel.SendMessageAsync($"You should try this: {steamStoreUrl}{totallyRandomAppId}", options: token.ToRequestOptions());
                return;
            }

            if (RandomNumberGenerator.GetInt32(0, 101) >= 95)
            {
                await message.Channel.SendMessageAsync("Hmm, read a book?", options: token.ToRequestOptions());
                return;
            }

            var steamApps = await _steamList.Value;
            var randomApp = steamApps[RandomNumberGenerator.GetInt32(0, steamApps.Count)];
            var randomGame = $"You should try {randomApp.Name}\nFind it here: {randomApp.StorePage}";
            await message.Channel.SendMessageAsync(randomGame, options: token.ToRequestOptions());
        }
    }
}
