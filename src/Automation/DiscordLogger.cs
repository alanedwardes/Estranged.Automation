using Discord;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

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

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var logMessage = new DiscordLogMessage
            {
                Level = logLevel,
                Category = _categoryName,
                Message = formatter(state, exception),
                Exception = exception,
            };

            foreach (KeyValuePair<string, object> stateValue in state as IReadOnlyList<KeyValuePair<string, object>>)
            {
                if (stateValue.Value is IMessage message)
                {
                    logMessage.AssociatedMessage = message;
                }
            }

            _messageQueue.Enqueue(logMessage);
        }
    }
}
