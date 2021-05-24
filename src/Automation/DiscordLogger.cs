using Discord;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Humanizer;
using System.Threading;

namespace Estranged.Automation
{
    public sealed class DiscordLogger : ILogger
    {
        private readonly IDiscordClient _discordClient;
        private readonly string _categoryName;

        public DiscordLogger(IDiscordClient discordClient, string categoryName)
        {
            _discordClient = discordClient;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var colours = new Dictionary<LogLevel, Color>
            {
                { LogLevel.Debug, Color.Blue },
                { LogLevel.Information, Color.Green },
                { LogLevel.Trace, Color.Teal },
                { LogLevel.Warning, Color.Orange },
                { LogLevel.Error, Color.Red },
                { LogLevel.Critical, Color.DarkRed }
            };

            var embed = new EmbedBuilder
            {
                Color = colours[logLevel],
                Title = _categoryName,
                Description = formatter(state, exception),
                Timestamp = DateTimeOffset.UtcNow
            };

            EMBEDS.Enqueue((exception?.ToString(), embed.Build()));

            _ = PostMessagesBestEffort();
        }

        private static readonly ConcurrentQueue<(string, Embed)> EMBEDS = new ConcurrentQueue<(string, Embed)>();
        private static readonly SemaphoreSlim SEMAPHORE = new SemaphoreSlim(1, 1);

        public async Task PostMessagesBestEffort()
        {
            var guild = await _discordClient.GetGuildAsync(368117880547573760);
            if (guild == null)
            {
                return;
            }

            var channel = await guild.GetTextChannelAsync(845981918130733106);
            if (channel == null)
            {
                return;
            }

            while (!EMBEDS.IsEmpty)
            {
                if (EMBEDS.TryDequeue(out var item))
                {
                    string text = null;
                    if (item.Item1 != null)
                    {
                        text = "```\n" + item.Item1.Truncate(1024) + "\n```";
                    }

                    await SEMAPHORE.WaitAsync();
                    try
                    {
                        await channel.SendMessageAsync(text: text, embed: item.Item2);
                        await Task.Delay(TimeSpan.FromMilliseconds(500));
                    }
                    finally
                    {
                        SEMAPHORE.Release();
                    }
                }
            }
        }
    }
}
