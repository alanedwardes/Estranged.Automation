using Discord;
using Estranged.Automation.Runner.Discord.Events;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Estranged.Automation.Runner.Discord.Responders
{
    internal class ReplyTest : IResponder
    {
        private readonly ILogger<ReplyTest> _logger;

        public ReplyTest(ILogger<ReplyTest> logger)
        {
            _logger = logger;
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            var replies = await GetRepliesRecursive(message, token);

            foreach (var reply in replies)
            {
                _logger.LogInformation(reply.ToString());
            }
        }

        private async Task<IList<IMessage>> GetRepliesRecursive(IMessage message, CancellationToken cancellation)
        {
            IMessage? current = message;
            var messages = new List<IMessage> { current };

            while (current?.Reference == null || !current.Reference.MessageId.IsSpecified)
            {
                current = await message.Channel.GetMessageAsync(message.Reference.MessageId.Value, options: cancellation.ToRequestOptions());
                if (current != null)
                {
                    messages.Add(current);
                }
            }

            return messages;
        }
    }
}
