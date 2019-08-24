using Estranged.Automation.Shared;
using Microsoft.Extensions.Logging;
using Narochno.Slack;
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
            slack = new SlackClient(new SlackConfig { WebHookUrl = config.EstrangedDiscordCommunityWebhook, HttpClient = httpClient });
            this.scraper = scraper;
        }

        public async Task RunAsync(CancellationToken token)
        {
            // Screenshots
            var sharedFilesPattern = @"https://steamcommunity.com/sharedfiles/filedetails/\?id=([0-9]*)";
            string screenshotUrl(uint appId) => $"https://steamcommunity.com/app/{appId}/screenshots/?browsefilter=mostrecent";

            await scraper.GatherUnseenUrls(sharedFilesPattern, screenshotUrl(582890), x => slack.PostText(x), token);
            await scraper.GatherUnseenUrls(sharedFilesPattern, screenshotUrl(261820), x => slack.PostText(x), token);

            // Discussions
            var discussionsPattern = @"https://steamcommunity.com/app/([0-9]*)/discussions/([0-9]*)/([0-9]*)/";
            string discussionsUrl(uint appId) => $"https://steamcommunity.com/app/{appId}/discussions/";

            await scraper.GatherUnseenUrls(discussionsPattern, discussionsUrl(582890), x => slack.PostText(x), token);
            await scraper.GatherUnseenUrls(discussionsPattern, discussionsUrl(261820), x => slack.PostText(x), token);

            // Workshop
            string workshopUrl(uint appId) => $"https://steamcommunity.com/workshop/browse/?appid={appId}&browsesort=mostrecent";

            await scraper.GatherUnseenUrls(sharedFilesPattern, workshopUrl(261820), x => slack.PostText(x), token);
        }
    }
}
