using Google.Cloud.Translation.V2;
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
using Estranged.Automation.Shared;
using System.Threading;

namespace Estranged.Automation.Runner.Reviews
{
    public class ReviewsRunner
    {
        private readonly ILogger<ReviewsRunner> logger;
        private readonly ISteamClient steam;
        private readonly ISlackClient slack;
        private readonly ISeenItemRepository seenItemRepository;
        private readonly TranslationClient translation;
        private const string StateTableName = "EstrangedAutomationState";
        private const string ItemIdKey = "ItemId";
        private const string EnglishLanguage = "en";

        public ReviewsRunner(ILogger<ReviewsRunner> logger, ISteamClient steam, ISlackClient slack, ISeenItemRepository seenItemRepository, TranslationClient translation)
        {
            this.logger = logger;
            this.steam = steam;
            this.slack = slack;
            this.seenItemRepository = seenItemRepository;
            this.translation = translation;
        }

        public async Task GatherReviews(string product, uint appId)
        {
            GetReviewsResponse response = await steam.GetReviews(new GetReviewsRequest(appId) { Filter = ReviewFilter.Recent });
            logger.LogInformation("Got {0} recent reviews", response.Reviews.Count);

            IDictionary<uint, Review> reviews = response.Reviews.ToDictionary(x => x.RecommendationId, x => x);

            uint[] recentReviewIds = reviews.Keys.ToArray();

            uint[] seenReviewIds = (await seenItemRepository.GetSeenItems(recentReviewIds.Select(x => x.ToString()).ToArray(), CancellationToken.None)).Select(x => uint.Parse(x)).ToArray();

            uint[] unseenReviewIds = recentReviewIds.Except(seenReviewIds).ToArray();
            logger.LogInformation("Of which {0} are unseen", unseenReviewIds.Length);

            Review[] unseenReviews = reviews.Where(x => unseenReviewIds.Contains(x.Key))
                .Select(x => x.Value)
                .OrderBy(x => x.Created)
                .ToArray();

            foreach (Review unseenReview in unseenReviews)
            {
                string reviewUrl = $"https://steamcommunity.com/profiles/{unseenReview.Author.SteamId}/recommended/{appId}";

                logger.LogInformation("Posting review {0} to Slack", reviewUrl);

                TranslationResult translationResponse = await translation.TranslateTextAsync(unseenReview.Comment, EnglishLanguage);

                var fields = new List<Field>
                {
                    new Field
                    {
                        Title = $"Original Text ({translationResponse.DetectedSourceLanguage ?? "unknown"})",
                        Value = unseenReview.Comment,
                        Short = false
                    },
                    new Field
                    {
                        Title = "Play Time Total",
                        Value = unseenReview.Author.PlayTimeForever.Humanize(),
                        Short = true
                    },
                    new Field
                    {
                        Title = "Play Time Last 2 Weeks",
                        Value = unseenReview.Author.PlayTimeLastTwoWeeks.Humanize(),
                        Short = true
                    }
                };

                if (translationResponse.DetectedSourceLanguage != EnglishLanguage)
                {
                    fields.Insert(1, new Field
                    {
                        Title = $"Auto-Translated Text",
                        Value = translationResponse.TranslatedText,
                        Short = false
                    });
                }

                await slack.IncomingWebHook(new IncomingWebHookRequest
                {
                    Channel = "#reviews",
                    Emoji = ":steam:",
                    Username = $"{product} ({appId}) Reviews",
                    Attachments = new List<Attachment>
                    {
                        new Attachment
                        {
                            AuthorIcon = "https://steamcommunity-a.akamaihd.net/public/shared/images/userreviews/" + (unseenReview.VotedUp ? "icon_thumbsUp.png" : "icon_thumbsDown.png"),
                            AuthorName = (unseenReview.VotedUp ? "Recommended" : "Not Recommended") + " (open review)",
                            AuthorLink = reviewUrl,
                            Fields = fields
                        }
                    }
                });

                logger.LogInformation("Inserting review {0} into DynamoDB", unseenReview.RecommendationId);
                await seenItemRepository.SetItemSeen(unseenReview.RecommendationId.ToString(), CancellationToken.None);
            }

            logger.LogInformation("Finished posting reviews.");
        }
    }
}
