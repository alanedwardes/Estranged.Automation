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
using System.Text.RegularExpressions;
using Estranged.Automation.Lambda.QuarterHour;

namespace Estranged.Automation.Runner.Syndication
{
    public class SyndicationRunnable : IRunnable
    {
        private readonly ILogger<SyndicationRunnable> logger;
        private readonly ISeenItemRepository seenItemRepository;
        private readonly HttpClient httpClient;
        private readonly ISlackClient slackClient;

        public SyndicationRunnable(ILogger<SyndicationRunnable> logger, ISeenItemRepository seenItemRepository, HttpClient httpClient)
        {
            this.logger = logger;
            this.seenItemRepository = seenItemRepository;
            this.httpClient = httpClient;
            slackClient = new SlackClient(new SlackConfig { WebHookUrl = Environment.GetEnvironmentVariable("SYNDICATION_WEB_HOOK_URL"), HttpClient = httpClient });
        }

        public string GetUniqueId(XmlNode node) => node["guid"]?.InnerText ?? node["link"].InnerText;

        public async Task GatherSyndication(string feed, CancellationToken token)
        {
            logger.LogInformation("Gathering syndication for {0}", feed);

            var response = await httpClient.GetAsync(feed, token);

            var content = await response.Content.ReadAsStringAsync();

            var contentCleaned = Regex.Replace(content, @"[^\x09\x0A\x0D\x20-\uD7FF\uE000-\uFFFD\u10000-\u10FFFF]", string.Empty);

            var document = new XmlDocument();
            document.LoadXml(contentCleaned);

            var channel = document["rss"]["channel"];

            var feedName = channel["title"].InnerText;

            var itemIds = new List<string>();

            foreach (XmlNode item in document.SelectNodes("/rss/channel/item"))
            {
                itemIds.Add(GetUniqueId(item));
            }

            var seenItems = await seenItemRepository.GetSeenItems(itemIds.ToArray(), token);

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
                }, token);

                logger.LogInformation("Marking {0} as read", link);

                await seenItemRepository.SetItemSeen(uniqueId, token);
            }

            logger.LogInformation("Syndication completed for {0}", feed);
        }

        public async Task RunAsync(CancellationToken token)
        {
            await GatherSyndication("https://feeds.feedburner.com/GamasutraNews", token);
        }
    }
}
