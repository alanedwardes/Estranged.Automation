using Discord;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Estranged.Automation
{
    public sealed class DiscordLogger : ILogger
    {
        private readonly ConcurrentQueue<DiscordLogMessage> _messageQueue;
        private readonly string _categoryName;

        public DiscordLogger(ConcurrentQueue<DiscordLogMessage> messageQueue, string categoryName)
        {
            _messageQueue = messageQueue;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        private IEnumerable<IMessage> GetAssociatedMessages(object state)
        {
            foreach (KeyValuePair<string, object> stateValue in state as IReadOnlyList<KeyValuePair<string, object>>)
            {
                if (stateValue.Value is IMessage message)
                {
                    yield return message;
                }
            }
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            DiscordLogMessage CreateLogMessage(string message, IMessage associatedMessage) => new DiscordLogMessage
            {
                Level = logLevel,
                Category = _categoryName,
                Message = message,
                Exception = exception,
                AssociatedMessage = associatedMessage
            };

            var associatedMessages = GetAssociatedMessages(state).ToArray();

            _messageQueue.Enqueue(CreateLogMessage(formatter(state, exception), associatedMessages.FirstOrDefault()));

            foreach (var associatedMessage in associatedMessages.Skip(1))
            {
                _messageQueue.Enqueue(CreateLogMessage(null, associatedMessage));
            }
        }
    }
}
