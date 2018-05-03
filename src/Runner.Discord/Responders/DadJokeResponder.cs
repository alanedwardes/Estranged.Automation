using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.Logging;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public class DadJokeResponder : IResponder
    {
        private readonly ILogger<DadJokeResponder> logger;
        private readonly HttpClient httpClient;

        public DadJokeResponder(ILogger<DadJokeResponder> logger, HttpClient httpClient)
        {
            this.logger = logger;
            this.httpClient = httpClient;
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (!message.Content.ToLower().Contains("dad joke") &&
                !message.Content.ToLower().Contains("tell me a joke"))
            {
                return;
            }

            logger.LogInformation("Fetching dad joke.");

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://icanhazdadjoke.com/")
            };

            request.Headers.TryAddWithoutValidation("User-Agent", "Estranged.Automation(https://github.com/alanedwardes/Estranged.Automation)");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

            using (message.Channel.EnterTypingState(token.ToRequestOptions()))
            {
                var response = await httpClient.SendAsync(request, token);
                var joke = await response.Content.ReadAsStringAsync();

                logger.LogInformation("Sending dad joke: {0}", joke);
                await message.Channel.SendMessageAsync(joke, options: token.ToRequestOptions());
            }
        }
    }
}
