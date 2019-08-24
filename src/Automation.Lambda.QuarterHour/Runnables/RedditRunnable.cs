using Narochno.Slack;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Lambda.QuarterHour.Runnables
{
    public class RedditRunnable : IRunnable
    {
        private readonly SlackClient slack;
        private readonly Scraper scraper;

        public RedditRunnable(Function.FunctionConfig config, HttpClient httpClient, Scraper scraper)
        {
            slack = new SlackClient(new SlackConfig { WebHookUrl = config.EstrangedDiscordGamingWebhook, HttpClient = httpClient });
            this.scraper = scraper;
        }

        public async Task RunAsync(CancellationToken token)
        {
            // r/gaming
            await scraper.GatherUnseenUrls("https://www.reddit.com/r/gaming/comments/[a-z0-9]+/[a-z_0-9]+/", "https://www.reddit.com/r/gaming/top/", x => slack.PostText(x), token);
        }
    }
}
