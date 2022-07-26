using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Narochno.Slack;
using Estranged.Automation.Lambda.QuarterHour;
using System.Collections.Generic;

namespace Estranged.Automation.Runner.Syndication
{
    public class SyndicationRunnable : IRunnable
    {
        private readonly SlackClient slack;
        private readonly Scraper scraper;

        public SyndicationRunnable(Function.FunctionConfig config, HttpClient httpClient, Scraper scraper)
        {
            slack = new SlackClient(new SlackConfig { WebHookUrl = config.EstrangedDiscordSyndicationWebhook }, httpClient);
            this.scraper = scraper;
        }

        public IEnumerable<Task> RunAsync(CancellationToken token)
        {
            // Gamasutra
            yield return scraper.GatherUnseenUrls("/view/news/[0-9]+/[A-Za-z0-9_]+.php", "https://www.gamasutra.com/", x => slack.PostText($"https://www.gamasutra.com{x}"), token);

            // Unreal News / Blog
            yield return scraper.GatherUnseenUrls("/en-US/(blog|news)/[A-Za-z0-9-]+", "https://www.unrealengine.com/en-US/feed", x => slack.PostText($"https://www.unrealengine.com{x}"), token);
        }
    }
}
