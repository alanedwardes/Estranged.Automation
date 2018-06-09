using Microsoft.Extensions.Logging;
using Narochno.Slack;
using System.Threading.Tasks;
using Estranged.Automation.Shared;
using System.Threading;
using System.Net.Http;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Estranged.Automation.Runner.Community
{
    public class CommunityRunner : PeriodicRunner
    {
        private readonly ILogger<CommunityRunner> logger;
        private readonly ISlackClient slack;
        private readonly ISeenItemRepository seenItemRepository;
        private readonly HttpClient httpClient;

        public override TimeSpan Period => TimeSpan.FromMinutes(15);

        public CommunityRunner(ILogger<CommunityRunner> logger, ISeenItemRepository seenItemRepository, HttpClient httpClient)
        {
            this.logger = logger;
            this.slack = new SlackClient(new SlackConfig { WebHookUrl = Environment.GetEnvironmentVariable("COMMUNITY_WEB_HOOK_URL"), HttpClient = httpClient });
            this.seenItemRepository = seenItemRepository;
            this.httpClient = httpClient;
        }

        public async Task GatherScreenshots(uint appId, CancellationToken token)
        {
            var fileUrlRegex = new Regex(@"https://steamcommunity.com/sharedfiles/filedetails/\?id=([0-9]*)");
            var html = await httpClient.GetStringAsync($"https://steamcommunity.com/app/{appId}/screenshots/?browsefilter=mostrecent");

            var screenshotUrls = fileUrlRegex.Matches(html).OfType<Match>().Select(x => x.Value).ToArray();
            var seenUrls = await seenItemRepository.GetSeenItems(screenshotUrls, token);

            foreach (string screenshotUrl in screenshotUrls)
            {
                if (seenUrls.Contains(screenshotUrl))
                {
                    continue;
                }

                logger.LogInformation("Posting screenshot {0} to Slack", screenshotUrl);
                //await slack.PostText(screenshotUrl, token);
                //await seenItemRepository.SetItemSeen(screenshotUrl, token);
            }
        }

        public async override Task RunPeriodically(CancellationToken token)
        {
            await GatherScreenshots(582890, token);
        }
    }
}
