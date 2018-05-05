using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public class RegionResponder : IResponder
    {
        public Task ProcessMessage(IMessage message, CancellationToken token)
        {
            RegionInfo[] regions = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                                              .Select(x => new RegionInfo(x.Name))
                                              .ToArray();

            string[] words = message.Content.Split(' ');

            RegionInfo[] foundRegions = regions.Where(x => words.Contains(x.EnglishName))
                                               .ToArray();
            foreach (RegionInfo region in foundRegions)
            {
                message.Channel.SendMessageAsync($"Ah yes! {region.EnglishName} ({region.NativeName})! They use the {region.CurrencyEnglishName} ({region.CurrencySymbol}).");
            }

            return Task.CompletedTask;
        }
    }
}
