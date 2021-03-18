﻿using Estranged.Automation.Runner.Discord.Responders;
using Estranged.Automation.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Estranged.Automation.Runner.Discord
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddResponderServices(this IServiceCollection services)
        {
            return services.AddSingleton<IResponder, LocalizationResponder>()
                           .AddSingleton<IResponder, DadJokeResponder>()
                           .AddSingleton<IResponder, PullTheLeverResponder>()
                           .AddSingleton<IResponder, EnglishTranslationResponder>()
                           .AddSingleton<IResponder, TranslationResponder>()
                           .AddSingleton<IResponder, NaturalLanguageResponder>()
                           .AddSingleton<IResponder, DogResponder>()
                           .AddSingleton<IResponder, HelloResponder>()
                           .AddSingleton<IResponder, QuoteResponder>()
                           .AddSingleton<IResponder, RtxResponder>()
                           .AddSingleton<IResponder, TwitchResponder>()
                           .AddSingleton<IResponder, SteamGameResponder>()
                           .AddSingleton<IResponder, SobResponder>()
                           .AddSingleton<IResponder, RepeatPhraseResponder>();
        }
    }
}