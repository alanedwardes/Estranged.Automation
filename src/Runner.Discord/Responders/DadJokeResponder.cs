using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public class DadJokeResponder : IResponder
    {
        private readonly HttpClient httpClient;

        public DadJokeResponder(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://icanhazdadjoke.com/")
            };

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

            var response = await httpClient.SendAsync(request, token);

            var joke = await response.Content.ReadAsStringAsync();

            await message.Channel.SendMessageAsync(joke, options: token.ToRequestOptions());
        }
    }
}
