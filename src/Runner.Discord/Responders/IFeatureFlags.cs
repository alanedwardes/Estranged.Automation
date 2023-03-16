namespace Estranged.Automation.Runner.Discord.Responders
{
    internal interface IFeatureFlags
    {
        FeatureFlagResponder.AttemptsBucket DalleAttempts { get; }
        FeatureFlagResponder.AttemptsBucket GptAttempts { get; }
        bool IsAiEnabled { get; }

        void ResetDalleAttempts();
        void ResetGptAttempts();
        bool ShouldResetDalleAttempts();
        bool ShouldResetGptAttempts();
    }
}