namespace Estranged.Automation.Runner.Discord.Responders
{
    internal interface IFeatureFlags
    {
        FeatureFlagResponder.AttemptsBucket DalleHqAttempts { get; }
        FeatureFlagResponder.AttemptsBucket DalleAttempts { get; }
        FeatureFlagResponder.AttemptsBucket GptAttempts { get; }
        bool IsAiEnabled { get; }

        void ResetDalleHqAttempts();
        void ResetDalleAttempts();
        void ResetGptAttempts();
        bool ShouldResetDalleHqAttempts();
        bool ShouldResetDalleAttempts();
        bool ShouldResetGptAttempts();
    }
}