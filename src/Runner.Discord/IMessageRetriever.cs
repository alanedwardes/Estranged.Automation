using Discord;
using Estranged.Automation.Runner.Discord.Events;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord
{
    public sealed class MessageRetriever : IResponder
    {
        //private readonly IDictionary<(ulong, ulong, ulong), IMessage> _messageHistory = new ConcurrentDictionary<ulong, IMessage>();

        public Task<IMessage> GetMessage(ulong guildId, ulong messageId, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }

        public Task ProcessMessage(IMessage message, CancellationToken token)
        {
            //_messageHistory.Add(message.Id, message);

            //if (_messageHistory.Count > 100)
            //{
            //    _messageHistory.Remove(_messageHistory.Keys.First());
            //}

            return Task.CompletedTask;
        }
    }
}
