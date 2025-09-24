using System.Threading;
using System.Threading.Tasks;
using Discord;
using Estranged.Automation.Events;

namespace Estranged.Automation.Responders
{
    public class RtxResponder : IResponder
    {
        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (message.Channel.IsProtectedChannel())
            {
                return;
            }

            var trimmed = message.Content.ToLower().Trim();
            if (trimmed.Contains("rtx on") || trimmed.Contains("rtx off"))
            {
                await message.Channel.SendMessageAsync("AND JUST LIKE THAT", options: token.ToRequestOptions());
            }
        }
    }
}
