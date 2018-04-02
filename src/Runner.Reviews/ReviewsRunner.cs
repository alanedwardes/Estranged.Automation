using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Humanizer;
using Microsoft.Extensions.Logging;
using Narochno.Slack;
using Narochno.Slack.Entities;
using Narochno.Slack.Entities.Requests;
using Narochno.Steam;
using Narochno.Steam.Entities;
using Narochno.Steam.Entities.Requests;
using Narochno.Steam.Entities.Responses;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Estranged.Automation.Runner.Reviews
{
    public class ReviewsRunner
    {
        private readonly ILogger<ReviewsRunner> logger;
        private readonly ISteamClient steam;
        private readonly ISlackClient slack;
        private readonly IAmazonDynamoDB dynamo;
        private const string StateTableName = "EstrangedAutomationState";
        private const string ItemIdKey = "ItemId";

        public ReviewsRunner(ILogger<ReviewsRunner> logger, ISteamClient steam, ISlackClient slack, IAmazonDynamoDB dynamo)
        {
            this.logger = logger;
            this.steam = steam;
            this.slack = slack;
            this.dynamo = dynamo;
        }

        public async Task GatherReviews(string product, uint appId)
        {
            GetReviewsResponse response = await steam.GetReviews(new GetReviewsRequest(appId) { Filter = ReviewFilter.Recent });
            logger.LogInformation("Got {0} recent reviews", response.Reviews.Count);

            IDictionary<string, Review> reviews = response.Reviews.ToDictionary(x => x.RecommendationId, x => x);

            string[] recentReviewIds = reviews.Keys.ToArray();

            BatchGetItemResponse items = await dynamo.BatchGetItemAsync(new BatchGetItemRequest
            {
                RequestItems = new Dictionary<string, KeysAndAttributes>
                {
                    {
                        StateTableName,
                        new KeysAndAttributes
                        {
                            Keys = recentReviewIds.Select(x => new Dictionary<string, AttributeValue> { { ItemIdKey, new AttributeValue(x) } }).ToList()
                        }
                    }
                }
            });

            string[] seenReviewIds = items.Responses[StateTableName].Select(x => x[ItemIdKey].S).ToArray();

            string[] unseenReviewIds = recentReviewIds.Except(seenReviewIds).ToArray();
            logger.LogInformation("Of which {0} are unseen", unseenReviewIds.Length);

            Review[] unseenReviews = reviews.Where(x => unseenReviewIds.Contains(x.Key))
                .Select(x => x.Value)
                .OrderBy(x => x.Created)
                .ToArray();

            foreach (Review unseenReview in unseenReviews)
            {
                string reviewUrl = $"https://steamcommunity.com/profiles/{unseenReview.Author.SteamId}/recommended/{appId}";

                logger.LogInformation("Posting review {0} to Slack", reviewUrl);
                await slack.IncomingWebHook(new IncomingWebHookRequest
                {
                    Channel = "#reviews",
                    Emoji = ":steam:",
                    Username = $"{product} ({appId}) Reviews",
                    Attachments = new List<Attachment>
                    {
                        new Attachment
                        {
                            Text = unseenReview.Comment,
                            AuthorIcon = "https://steamcommunity-a.akamaihd.net/public/shared/images/userreviews/" + (unseenReview.VotedUp ? "icon_thumbsUp.png" : "icon_thumbsDown.png"),
                            AuthorName = (unseenReview.VotedUp ? "Recommended" : "Not Recommended") + " (open review)",
                            AuthorLink = reviewUrl,
                            Fields = new List<Field>
                            {
                                new Field
                                {
                                    Title = "Play Time Total",
                                    Value = unseenReview.Author.PlayTimeForever.Humanize(),
                                    Short = true
                                },
                                new Field
                                {
                                    Title = "Play Time Last 2 Weeks",
                                    Value = unseenReview.Author.PlaytimeLastTwoWeeks.Humanize(),
                                    Short = true
                                },
                                new Field
                                {
                                    Title = "Language",
                                    Value = unseenReview.Language,
                                    Short = true
                                },
                                new Field
                                {
                                    Title = "Created",
                                    Value = unseenReview.Created.Humanize(),
                                    Short = true
                                }
                            }
                        }
                    }
                });

                logger.LogInformation("Inserting review {0} into DynamoDB", unseenReview.RecommendationId);
                await dynamo.PutItemAsync(StateTableName, new Dictionary<string, AttributeValue> { { ItemIdKey, new AttributeValue(unseenReview.RecommendationId) } });
            }

            logger.LogInformation("Finished posting reviews.");
        }
    }
}
