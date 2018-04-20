using System.Linq;
using Estranged.Automation.Shared;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Collections.Generic;
using System.Threading;
using Narochno.Slack;
using Narochno.Slack.Entities.Requests;
using System;
using Microsoft.Extensions.Logging;

namespace Estranged.Automation.Runner.Syndication
{
    public class SyndicationRunner : PeriodicRunner
    {
        private readonly ILogger<SyndicationRunner> logger;
        private readonly ISeenItemRepository seenItemRepository;
        private readonly HttpClient httpClient;
        private readonly ISlackClient slackClient;

        public override TimeSpan Period => TimeSpan.FromMinutes(15);

        public SyndicationRunner(ILogger<SyndicationRunner> logger, ISeenItemRepository seenItemRepository, HttpClient httpClient)
        {
            this.logger = logger;
            this.seenItemRepository = seenItemRepository;
            this.httpClient = httpClient;
            slackClient = new SlackClient(new SlackConfig { WebHookUrl = Environment.GetEnvironmentVariable("SYNDICATION_WEB_HOOK_URL"), HttpClient = httpClient });
        }

        public string GetUniqueId(XmlNode node) => node["guid"]?.InnerText ?? node["link"].InnerText;

        public async Task GatherSyndication(string feed)
        {
            logger.LogInformation("Gathering syndication for {0}", feed);

            var stream = await httpClient.GetStreamAsync(feed);

            var document = new XmlDocument();
            document.Load(stream);

            var channel = document["rss"]["channel"];

            var feedName = channel["title"].InnerText;

            var itemIds = new List<string>();

            foreach (XmlNode item in document.SelectNodes("/rss/channel/item"))
            {
                itemIds.Add(GetUniqueId(item));
            }

            var seenItems = await seenItemRepository.GetSeenItems(itemIds.ToArray(), CancellationToken.None);

            logger.LogInformation("Found {0} items, {1} of which are seen", itemIds.Count, seenItems.Length);

            foreach (XmlNode item in document.SelectNodes("/rss/channel/item"))
            {
                string uniqueId = GetUniqueId(item);
                if (seenItems.Contains(uniqueId))
                {
                    continue;
                }

                string link = item["link"].InnerText;

                await slackClient.IncomingWebHook(new IncomingWebHookRequest
                {
                    Username = feedName,
                    Text = link
                });

                logger.LogInformation("Marking {0} as read", link);

                await seenItemRepository.SetItemSeen(uniqueId, CancellationToken.None);
            }

            logger.LogInformation("Syndication completed for {0}", feed);
        }

        public async override Task RunPeriodically(CancellationToken token)
        {
            await GatherSyndication("http://feeds.feedburner.com/GamasutraNews");
            await GatherSyndication("https://www.unrealengine.com/rss");
        }
    }
}
