using Discord;
using Estranged.Automation.Runner.Discord.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Discord.Responders
{
    internal sealed class FeatureFlagResponder : IResponder, IFeatureFlags
    {
        public sealed class AttemptsBucket
        {
            public AttemptsBucket(DateTime bucket) => Bucket = bucket;

            public int Count;
            public DateTime Bucket;
        }

        public FeatureFlagResponder()
        {
            ResetDalleAttempts();
            ResetGptAttempts();
        }

        private DateTime CurrentDalleBucket
        {
            get
            {
                var now = DateTime.UtcNow;
                return new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
            }
        }
        public bool ShouldResetDalleAttempts() => DalleAttempts.Bucket != CurrentDalleBucket;
        public void ResetDalleAttempts() => DalleAttempts = new AttemptsBucket(CurrentDalleBucket);
        public AttemptsBucket DalleAttempts { get; private set; }

        private DateTime CurrentGptBucket
        {
            get
            {
                var now = DateTime.UtcNow;
                return new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
            }
        }
        public bool ShouldResetGptAttempts() => GptAttempts.Bucket != CurrentGptBucket;
        public void ResetGptAttempts() => GptAttempts = new AttemptsBucket(CurrentGptBucket);
        public AttemptsBucket GptAttempts { get; private set; }

        public bool IsAiEnabled { get; private set; } = true;

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
