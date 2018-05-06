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

        private string[] breeds = new[] { "shiba", "corgi", "samoyed" };

        private string[] allBreeds = new[] { "affenpinscher", "african", "airedale", "akita", "appenzeller", "basenji", "beagle", "bluetick", "borzoi", "bouvier", "boxer", "brabancon", "briard", "bulldog", "bullterrier", "cairn", "chihuahua", "chow", "clumber", "collie", "coonhound", "corgi", "dachshund", "dalmatian", "dane", "deerhound", "dhole", "dingo", "doberman", "elkhound", "entlebucher", "eskimo", "germanshepherd", "greyhound", "groenendael", "hound", "husky", "keeshond", "kelpie", "komondor", "kuvasz", "labrador", "leonberg", "lhasa", "malamute", "malinois", "maltese", "mastiff", "mexicanhairless", "mix", "mountain", "newfoundland", "otterhound", "papillon", "pekinese", "pembroke", "pinscher", "pointer", "pomeranian", "poodle", "pug", "pyrenees", "redbone", "retriever", "ridgeback", "rottweiler", "saluki", "samoyed", "schipperke", "schnauzer", "setter", "sheepdog", "shiba", "shihtzu", "spaniel", "springer", "stbernard", "terrier", "vizsla", "weimaraner", "whippet", "wolfhound" };

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

            var intersections = words.Intersect(allBreeds).ToArray();
            if (intersections.Any())
            {
                await SendPhoto(intersections.First(), message.Channel, token);
                return;
            }

            // Send random breed
            await SendPhoto(breeds.OrderBy(x => Guid.NewGuid()).First(), message.Channel, token);
        }

        private async Task SendPhoto(string breed, IMessageChannel channel, CancellationToken token)
        {
            using (channel.EnterTypingState(token.ToRequestOptions()))
            {
                var dog = JObject.Parse(await httpClient.GetStringAsync($"https://dog.ceo/api/breed/{breed}/images/random"));
                await channel.SendMessageAsync(dog.Value<string>("message"));
            }
        }
    }
}
