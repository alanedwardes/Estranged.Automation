using Discord;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

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

            _embeds.Enqueue(embed.Build());

            _ = PostMessagesBestEffort(logLevel, eventId, state, exception, formatter);
        }

        private readonly ConcurrentQueue<Embed> _embeds = new ConcurrentQueue<Embed>();

        public async Task PostMessagesBestEffort<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
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

            while (!_embeds.IsEmpty)
            {
                if (_embeds.TryDequeue(out Embed embed))
                {
                    try
                    {
                        await channel.SendMessageAsync(embed: embed);
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                    catch (Exception)
                    {
                        _embeds.Enqueue(embed);
                    }
                }
            }
        }
    }
}
