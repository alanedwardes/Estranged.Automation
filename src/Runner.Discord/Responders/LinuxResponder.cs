using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.Logging;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public class LinuxResponder : IResponder
    {
        private readonly ILogger<LinuxResponder> logger;

        public LinuxResponder(ILogger<LinuxResponder> logger)
        {
            this.logger = logger;
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            string contentLower = message.Content.ToLower();

            var words = message.Content.ToLower().Split(new[] { ' ' });

            if (words.Contains("linux") && !words.Contains("gnu"))
            {
                logger.LogInformation("Sending Linux text");
                await message.Channel.SendMessageAsync("I'd just like to interject for a moment. What you're referring to as Linux, is in fact, GNU/Linux, or as I've recently taken to calling it, GNU plus Linux.", false, null, token.ToRequestOptions());
            }
        }
    }
}
