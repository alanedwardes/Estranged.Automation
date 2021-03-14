using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
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

        private string[] commands = new string[] { "what to play", "recommend me a game", "what to buy", "oh man it sure it annoying I don't have anything to play" };
        private const string steamStoreUrl = "https://store.steampowered.com/app/";

        private Lazy<Task<SteamAppListRoot>> steamList;

        public SteamGameResponder(HttpClient httpClient)
        {
            steamList = new Lazy<Task<SteamAppListRoot>>(async () =>
            {
                HttpResponseMessage response = await httpClient.GetAsync("https://api.steampowered.com/ISteamApps/GetAppList/v2/");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                SteamAppListRoot appList = JsonConvert.DeserializeObject<SteamAppListRoot>(responseBody);
                return appList;
            });
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            string randomGame;
            if (message.Author.Id == 269883106792701952)
            {
                int totallyRandomAppId = RandomNumberGenerator.GetInt32(0, 1) == 0 ? 261820 : 582890;
                randomGame = $"You should try this: {steamStoreUrl}{totallyRandomAppId}";
                await message.Channel.SendMessageAsync(randomGame, options: token.ToRequestOptions());
                return;
            }
            var trimmed = message.Content.ToLower().Trim();
            for (int i = 0; i < commands.Length; i++)
            {
                if (trimmed.Contains(commands[i]))
                {
                    var steamApps = await steamList.Value;
                    var randomApp = steamApps.Applist.Apps[RandomNumberGenerator.GetInt32(0, steamApps.Applist.Apps.Count)];
                    var randomFloat = RandomNumberGenerator.GetInt32(0, int.MaxValue) / (double)int.MaxValue;                   
                    randomGame = randomFloat > 0.95 ? $"You should try {randomApp.Name}\nFind it here: {steamStoreUrl}{randomApp.Appid}" : "Hmm, read a book?";

                    await message.Channel.SendMessageAsync(randomGame, options: token.ToRequestOptions());
                }
            }
        }
    }
}
