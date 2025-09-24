using Discord;
using Humanizer;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation
{
    public sealed class DiscordLogMessage
    {
        public string Category { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
        public IMessage AssociatedMessage { get; set; }
    }

    public sealed class DiscordLoggerProvider : ILoggerProvider
    {
        private readonly IReadOnlyDictionary<LogLevel, string> _logLevelEmoji = new Dictionary<LogLevel, string>
        {
            { LogLevel.Debug, "🟪" },
            { LogLevel.Information, "🟩" },
            { LogLevel.Trace, "🟦" },
            { LogLevel.Warning, "🟨" },
            { LogLevel.Error, "🟥" },
            { LogLevel.Critical, "🟥" }
        };

        private readonly IDiscordClient _discordClient;
        private readonly ConcurrentQueue<DiscordLogMessage> _messageQueue = new ConcurrentQueue<DiscordLogMessage>();
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public DiscordLoggerProvider(IDiscordClient discordClient)
        {
            _discordClient = discordClient;
            _ = PumpMessages();
        }

        public ILogger CreateLogger(string categoryName) => new DiscordLogger(_messageQueue, categoryName);

        private async Task PumpMessages()
        {
            while (!_tokenSource.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), _tokenSource.Token);

                try
                {
                    await PostMessage();
                }
                catch
                {
                    // Do nothing
                }
            }
        }

        private async Task PostMessage()
        {
            var guild = await _discordClient.GetGuildAsync(368117880547573760, CacheMode.AllowDownload, new RequestOptions { CancelToken = _tokenSource.Token });
            if (guild == null)
            {
                return;
            }

            var channel = await guild.GetTextChannelAsync(845981918130733106, CacheMode.AllowDownload, new RequestOptions { CancelToken = _tokenSource.Token });
            if (channel == null)
            {
                return;
            }

            if (!_messageQueue.TryDequeue(out DiscordLogMessage logMessage))
            {
                return;
            }

            string text = $"{_logLevelEmoji[logMessage.Level]} `{logMessage.Category}` {logMessage.Message}";
            if (logMessage.Exception != null)
            {
                text += "```\n" + logMessage.Exception.ToString().Truncate(1024) + "\n```";
            }

            await channel.SendMessageAsync(text, embed: logMessage.AssociatedMessage?.QuoteMessage(), options: new RequestOptions { CancelToken = _tokenSource.Token });
        }

        public void Dispose() => _tokenSource.Cancel();
    }
}
