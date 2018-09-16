using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public class RtxResponder : IResponder
    {
        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            var trimmed = message.Content.ToLower().Trim();
            if (trimmed.Contains("rtx on") || trimmed.Contains("rtx off"))
            {
                await message.Channel.SendMessageAsync("AND JUST LIKE THAT", options: token.ToRequestOptions());
            }
        }
    }
}
