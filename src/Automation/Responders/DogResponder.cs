using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Estranged.Automation.Events;
using Newtonsoft.Json.Linq;

namespace Estranged.Automation.Responders
{
    public class DogResponder : IResponder
    {
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly string[] breeds = new[] { "shiba", "corgi", "samoyed" };

        private readonly string[] allBreeds = new[] { "affenpinscher", "african", "airedale", "akita", "appenzeller", "basenji", "beagle", "bluetick", "borzoi", "bouvier", "boxer", "brabancon", "briard", "bulldog", "bullterrier", "cairn", "chihuahua", "chow", "clumber", "collie", "coonhound", "corgi", "dachshund", "dalmatian", "dane", "deerhound", "dhole", "dingo", "doberman", "elkhound", "entlebucher", "eskimo", "germanshepherd", "greyhound", "groenendael", "hound", "husky", "keeshond", "kelpie", "komondor", "kuvasz", "labrador", "leonberg", "lhasa", "malamute", "malinois", "maltese", "mastiff", "mexicanhairless", "mix", "mountain", "newfoundland", "otterhound", "papillon", "pekinese", "pembroke", "pinscher", "pointer", "pomeranian", "poodle", "pug", "pyrenees", "redbone", "retriever", "ridgeback", "rottweiler", "saluki", "samoyed", "schipperke", "schnauzer", "setter", "sheepdog", "shiba", "shihtzu", "spaniel", "springer", "stbernard", "terrier", "vizsla", "weimaraner", "whippet", "wolfhound" };

        public DogResponder(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (message.Channel.IsProtectedChannel())
            {
                return;
            }

            IList<string> words = message.Content.ToLower().Split(' ');
            var intersections = allBreeds.Intersect(words).ToArray();
            if (intersections.Any())
            {
                await SendPhoto(intersections.First(), message.Channel, token);
                return;
            }

            if (words.Contains("dog"))
            {
                await SendPhoto(breeds.OrderBy(x => Guid.NewGuid()).First(), message.Channel, token);
            }
        }

        private async Task SendPhoto(string breed, IMessageChannel channel, CancellationToken token)
        {
            using (channel.EnterTypingState(token.ToRequestOptions()))
            using (var httpClient = _httpClientFactory.CreateClient(DiscordHttpClientConstants.RESPONDER_CLIENT))
            {
                var dog = JObject.Parse(await httpClient.GetStringAsync($"https://dog.ceo/api/breed/{breed}/images/random"));
                await channel.SendMessageAsync(Uri.EscapeUriString(dog.Value<string>("message")));
            }
        }
    }
}
