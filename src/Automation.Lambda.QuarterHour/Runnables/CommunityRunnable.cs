using Estranged.Automation.Shared;
using Microsoft.Extensions.Logging;
using Narochno.Slack;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Lambda.QuarterHour.Runnables
{
    public class CommunityRunnable : IRunnable
    {
        private readonly ISlackClient slack;
        private readonly Scraper scraper;

        public CommunityRunnable(ILogger<CommunityRunnable> logger, ISeenItemRepository seenItemRepository, HttpClient httpClient, Function.FunctionConfig config, Scraper scraper)
        {
            slack = new SlackClient(new SlackConfig { WebHookUrl = config.EstrangedDiscordCommunityWebhook }, httpClient);
            this.scraper = scraper;
        }

        public IEnumerable<Task> RunAsync(CancellationToken token)
        {
            // Screenshots
            var sharedFilesPattern = @"https://steamcommunity.com/sharedfiles/filedetails/\?id=([0-9]*)";
            string screenshotUrl(uint appId) => $"https://steamcommunity.com/app/{appId}/screenshots/?browsefilter=mostrecent";

            yield return scraper.GatherUnseenUrls(sharedFilesPattern, screenshotUrl(582890), x => slack.PostText(x), token);
            yield return scraper.GatherUnseenUrls(sharedFilesPattern, screenshotUrl(261820), x => slack.PostText(x), token);

            // Discussions
            var discussionsPattern = @"https://steamcommunity.com/app/([0-9]*)/discussions/([0-9]*)/([0-9]*)/";
            string discussionsUrl(uint appId) => $"https://steamcommunity.com/app/{appId}/discussions/";

            yield return scraper.GatherUnseenUrls(discussionsPattern, discussionsUrl(582890), x => slack.PostText(x), token);
            yield return scraper.GatherUnseenUrls(discussionsPattern, discussionsUrl(261820), x => slack.PostText(x), token);

            // Workshop
            string workshopUrl(uint appId) => $"https://steamcommunity.com/workshop/browse/?appid={appId}&browsesort=mostrecent";
            yield return scraper.GatherUnseenUrls(sharedFilesPattern, workshopUrl(261820), x => slack.PostText(x), token);

            // Guides
            string guidesUrl(uint appId) => $"https://steamcommunity.com/app/{appId}/guides/";
            yield return scraper.GatherUnseenUrls(sharedFilesPattern, guidesUrl(582890), x => slack.PostText(x), token);
            yield return scraper.GatherUnseenUrls(sharedFilesPattern, guidesUrl(261820), x => slack.PostText(x), token);

            // Videos
            string videosUrl(uint appId) => $"https://steamcommunity.com/app/{appId}/videos/";
            yield return scraper.GatherUnseenUrls(sharedFilesPattern, videosUrl(582890), x => slack.PostText(x), token);
            yield return scraper.GatherUnseenUrls(sharedFilesPattern, videosUrl(261820), x => slack.PostText(x), token);
        }
    }
}
