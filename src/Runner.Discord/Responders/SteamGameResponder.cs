using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public sealed class SteamGameResponder : IResponder
    {
        public class App
        {
            public int Appid { get; set; }
            public string Name { get; set; }
        }

        public class Applist
        {
            public List<App> Apps { get; set; } = new List<App>();
        }

        public class SteamAppListRoot
        {
            public Applist Applist { get; set; }
        }

        private readonly string[] commands = new string[] { "what to play", "recommend me a game", "what to buy", "oh man it sure it annoying I don't have anything to play" };
        private const string steamStoreUrl = "https://store.steampowered.com/app/";

        private readonly Lazy<Task<SteamAppListRoot>> steamList;

        public SteamGameResponder(HttpClient httpClient)
        {
            steamList = new Lazy<Task<SteamAppListRoot>>(async () =>
            {
                HttpResponseMessage response = await httpClient.GetAsync("https://api.steampowered.com/ISteamApps/GetAppList/v2/");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<SteamAppListRoot>(responseBody);
            });
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {            
            if (!commands.Any(x => message.Content.Contains(x, StringComparison.InvariantCultureIgnoreCase)))
            {
                return;
            }

            if (message.Author.Id == 269883106792701952)
            {
                int totallyRandomAppId = RandomNumberGenerator.GetInt32(0, 2) == 0 ? 261820 : 582890;
                await message.Channel.SendMessageAsync($"You should try this: {steamStoreUrl}{totallyRandomAppId}", options: token.ToRequestOptions());
                return;
            }

            if (RandomNumberGenerator.GetInt32(0, 101) >= 95)
            {
                await message.Channel.SendMessageAsync("Hmm, read a book?", options: token.ToRequestOptions());
                return;
            }

            var steamApps = await steamList.Value;
            var randomApp = steamApps.Applist.Apps[RandomNumberGenerator.GetInt32(0, steamApps.Applist.Apps.Count)];
            var randomGame = $"You should try {randomApp.Name}\nFind it here: {steamStoreUrl}{randomApp.Appid}";
            await message.Channel.SendMessageAsync(randomGame, options: token.ToRequestOptions());
        }
    }
}
