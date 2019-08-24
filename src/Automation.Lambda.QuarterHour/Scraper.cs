using Estranged.Automation.Shared;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Lambda.QuarterHour
{
    public class Scraper
    {
        private readonly HttpClient httpClient;
        private readonly ISeenItemRepository seenItemRepository;

        public Scraper(HttpClient httpClient, ISeenItemRepository seenItemRepository)
        {
            this.httpClient = httpClient;
            this.seenItemRepository = seenItemRepository;
        }

        public async Task GatherUnseenUrls(string pattern, string url, Func<string, Task> itemTask, CancellationToken token)
        {
            var regex = new Regex(pattern);
            var html = await httpClient.GetStringAsync(url);

            var foundUrls = regex.Matches(html)
                .OfType<Match>()
                .Select(x => x.Value)
                .Distinct()
                .ToArray();

            if (foundUrls.Length == 0)
            {
                return;
            }

            var seenUrls = await seenItemRepository.GetSeenItems(foundUrls, token);

            foreach (string foundUrl in foundUrls)
            {
                if (seenUrls.Contains(foundUrl))
                {
                    continue;
                }

                await itemTask(foundUrl);
                await seenItemRepository.SetItemSeen(foundUrl, token);
            }
        }
    }
}
