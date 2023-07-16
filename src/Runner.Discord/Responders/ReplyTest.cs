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
            await foreach (var reply in message.GetReplies(token))
            {
                _logger.LogInformation(reply.ToString());
            }
        }
    }
}
