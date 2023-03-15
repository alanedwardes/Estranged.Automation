using Discord;
using Estranged.Automation.Runner.Discord.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Responders
{
    internal sealed class FeatureFlagResponder : IResponder
    {
        internal class AttemptsBucket
        {
            public AttemptsBucket(DateTime bucket) => Bucket = bucket;

            public int Count;
            public DateTime Bucket;
        }

        static FeatureFlagResponder()
        {
            ResetDalleAttempts();
            ResetGptAttempts();
        }

        public static DateTime CurrentDalleBucket
        {
            get
            {
                var now = DateTime.UtcNow;
                return new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
            }
        }
        internal static bool ShouldResetDalleAttempts() => DalleAttempts.Bucket != CurrentDalleBucket;
        internal static void ResetDalleAttempts() => DalleAttempts = new AttemptsBucket(CurrentDalleBucket);
        internal static AttemptsBucket DalleAttempts { get; private set; }

        public static DateTime CurrentGptBucket
        {
            get
            {
                var now = DateTime.UtcNow;
                return new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
            }
        }
        internal static bool ShouldResetGptAttempts() => GptAttempts.Bucket != CurrentGptBucket;
        internal static void ResetGptAttempts() => GptAttempts = new AttemptsBucket(CurrentGptBucket);
        internal static AttemptsBucket GptAttempts { get; private set; }

        public static bool IsAiEnabled { get; private set; }

        public Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (message.Author.Id != 269883106792701952)
            {
                return Task.CompletedTask;
            }

            if (message.Content == "ff ai toggle")
            {
                IsAiEnabled = !IsAiEnabled;
                message.Channel.SendMessageAsync($"IsAiEnabled: {IsAiEnabled}");
                return Task.CompletedTask;
            }

            if (message.Content == "ff dalle reset")
            {
                ResetDalleAttempts();
                message.Channel.SendMessageAsync($"Reset dalle attempts");
                return Task.CompletedTask;
            }

            if (message.Content == "ff gpt reset")
            {
                ResetGptAttempts();
                message.Channel.SendMessageAsync($"Reset gpt attempts");
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
    }
}
