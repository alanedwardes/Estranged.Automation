using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json.Linq;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public class DogResponder : IResponder
    {
        private readonly HttpClient httpClient;

        private string[] breeds = new[] { "shiba", "corgi" };

        public DogResponder(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            IList<string> words = message.Content.ToLower().Split(' ');
            if (!words.Contains("dog"))
            {
                return;
            }

            var breed = breeds.OrderBy(x => Guid.NewGuid()).First();

            var dog = JObject.Parse(await httpClient.GetStringAsync($"https://dog.ceo/api/breed/{breed}/images/random"));
            await message.Channel.SendMessageAsync(dog.Value<string>("message"));
        }
    }
}
