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

        public override TimeSpan Period => TimeSpan.FromMinutes(30);

        public CommunityRunner(ILogger<CommunityRunner> logger, ISeenItemRepository seenItemRepository, HttpClient httpClient)
        {
            this.logger = logger;
            this.slack = new SlackClient(new SlackConfig { WebHookUrl = Environment.GetEnvironmentVariable("COMMUNITY_WEB_HOOK_URL"), HttpClient = httpClient });
            this.seenItemRepository = seenItemRepository;
            this.httpClient = httpClient;
        }

        public async Task GatherUrls(string pattern, string url, CancellationToken token)
        {
            var fileUrlRegex = new Regex(pattern);
            var html = await httpClient.GetStringAsync(url);

            var screenshotUrls = fileUrlRegex.Matches(html).OfType<Match>().Select(x => x.Value).Distinct().ToArray();
            var seenUrls = await seenItemRepository.GetSeenItems(screenshotUrls, token);

            foreach (string screenshotUrl in screenshotUrls)
            {
                if (seenUrls.Contains(screenshotUrl))
                {
                    continue;
                }

                logger.LogInformation("Posting community content {0} to Slack", screenshotUrl);
                await slack.PostText(screenshotUrl, token);
                await seenItemRepository.SetItemSeen(screenshotUrl, token);
            }
        }

        public async override Task RunPeriodically(CancellationToken token)
        {
            var screenshotPattern = @"https://steamcommunity.com/sharedfiles/filedetails/\?id=([0-9]*)";
            string screenshotUrl(uint appId) => $"https://steamcommunity.com/app/{appId}/screenshots/?browsefilter=mostrecent";

            await GatherUrls(screenshotPattern, screenshotUrl(582890), token);
            await GatherUrls(screenshotPattern, screenshotUrl(261820), token);

            var discussionsPattern = @"https://steamcommunity.com/app/([0-9]*)/discussions/([0-9]*)/([0-9]*)/";
            string discussionsUrl(uint appId) => $"https://steamcommunity.com/app/{appId}/discussions/";

            await GatherUrls(discussionsPattern, discussionsUrl(582890), token);
            await GatherUrls(discussionsPattern, discussionsUrl(261820), token);
        }
    }
}
