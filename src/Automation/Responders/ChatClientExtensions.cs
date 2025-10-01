using Discord;
using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Responders
{
    public static class ChatClientExtensions
    {
        public static async Task StreamResponse(this IChatClient chatClient, IMessage latestMessage, IEnumerable<ChatMessage> chatMessages, ChatOptions chatOptions, CancellationToken token)
        {
            IMessageChannel channel = latestMessage.Channel;
            IMessage lastThreadMessage = latestMessage;

            var sb = new StringBuilder();

            await foreach (var chatResponse in chatClient.GetStreamingResponseAsync(chatMessages, chatOptions, token))
            {
                sb.Append(chatResponse.Text);

                if (sb.Length >= 1000)
                {
                    lastThreadMessage = await channel.SendMessageAsync(sb.ToString(), messageReference: new MessageReference(lastThreadMessage.Id), flags: MessageFlags.SuppressEmbeds, options: token.ToRequestOptions());
                    sb.Clear();
                }
            }

            if (sb.Length > 0)
            {
                await channel.SendMessageAsync(sb.ToString(), messageReference: new MessageReference(lastThreadMessage.Id), flags: MessageFlags.SuppressEmbeds, options: token.ToRequestOptions());
            }
        }
    }
}
