using Estranged.Automation.Events;
using Estranged.Automation.Handlers;
using Estranged.Automation.Responders;
using Microsoft.Extensions.DependencyInjection;

namespace Estranged.Automation
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddResponderServices(this IServiceCollection services)
        {
            return services.AddSingleton<IResponder, LocalizationResponder>()
                           .AddSingleton<IResponder, DadJokeResponder>()
                           .AddSingleton<IResponder, PullTheLeverResponder>()
                           .AddSingleton<IResponder, DogResponder>()
                           .AddSingleton<IResponder, HelloResponder>()
                           .AddSingleton<IResponder, QuoteResponder>()
                           .AddSingleton<IResponder, RtxResponder>()
                           .AddSingleton<IResponder, SteamGameResponder>()
                           .AddSingleton<IResponder, SobResponder>()
                           .AddSingleton<IResponder, RepeatPhraseResponder>()
                           .AddSingleton<IResponder, EmojiReactionResponder>()
                           .AddSingleton<IResponder, HowAreYouResponder>()
                           .AddSingleton<IResponder, CopyReactionEmoji>()
                           .AddSingleton<IResponder, SpammerResponder>()
                           .AddSingleton<IResponder, DalleResponder>()
                           .AddSingleton<IResponder, StableDiffusionResponder>()
                           .AddSingleton<IResponder, GptResponder>()
                           .AddSingleton<IResponder, OllamaResponder>()
                           .AddSingleton<IResponder, RetentionResponder>()
                           .AddSingleton<IResponder, LoreResponder>()
                           .AddSingleton<IResponder, LlmResponder>()
                           .AddSingleton<IResponder, CloneWarsResponder>()
                           .AddSingleton<FeatureFlagResponder>()
                           .AddSingleton<IResponder>(x => x.GetRequiredService<FeatureFlagResponder>())
                           .AddSingleton<IFeatureFlags>(x => x.GetRequiredService<FeatureFlagResponder>())
                           .AddSingleton<IMessageDeleted, DeletedMessageQuoter>()
                           .AddSingleton<IUserLeftHandler, LeftMessageHandler>()
                           .AddSingleton<IReactionAddedHandler, VerifiedUserHandler>()
                           .AddSingleton<IUserJoinedHandler, WelcomeMessageHandler>()
                           .AddSingleton<IReactionAddedHandler, CopyReactionEmoji>()
                           .AddSingleton<IUserIsTyping, UserGreetingHandler>()
                           .AddSingleton<IMessageDeleted, AuditTrailHandler>()
                           .AddSingleton<IMessageUpdated, AuditTrailHandler>()
                           .AddSingleton<IUserJoinedHandler, AuditTrailHandler>()
                           .AddSingleton<IUserLeftHandler, AuditTrailHandler>();
        }
    }
}
