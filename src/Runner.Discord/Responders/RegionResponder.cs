using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public class RegionResponder : IResponder
    {
        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            return;

            string[] words = message.Content.ToLower().Split(' ');

            IDictionary<CultureInfo, RegionInfo> regionAndCultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                                                                                .ToDictionary(x => x, x => new RegionInfo(x.Name));

            var foundCultures = regionAndCultures.Where(x => words.Contains(x.Key.EnglishName, StringComparer.InvariantCultureIgnoreCase));
            foreach (var culture in foundCultures)
            {
                await PrintRegionAndCulture(message.Channel, culture);
            }

            var foundRegions = regionAndCultures.Where(x => words.Contains(x.Value.EnglishName, StringComparer.InvariantCultureIgnoreCase));
            foreach (var region in foundRegions)
            {
                await PrintRegionAndCulture(message.Channel, region);
            }
        }

        public async Task PrintRegionAndCulture(IMessageChannel channel, KeyValuePair<CultureInfo, RegionInfo> pair)
        {
            await channel.SendMessageAsync($"Ah yes, {pair.Key.EnglishName} spoken in {pair.Value.EnglishName}, with the currency {pair.Value.CurrencyEnglishName} ({pair.Value.CurrencySymbol})");
        }
    }
}
