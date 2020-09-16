using Estranged.Automation.Shared;
using Google;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Lambda.QuarterHour.Runnables
{
    public class ReviewsRunnable : IRunnable
    {
        private readonly ILogger<CommunityRunnable> logger;
        private readonly ISlackClient slack;
        private readonly ISeenItemRepository seenItemRepository;
        private readonly TranslationClient translation;
        private readonly ISteamClient steam;

        public ReviewsRunnable(ILogger<CommunityRunnable> logger, ISeenItemRepository seenItemRepository, HttpClient httpClient, Function.FunctionConfig config, TranslationClient translation, ISteamClient steam)
        {
            this.logger = logger;
            this.slack = new SlackClient(new SlackConfig { WebHookUrl = config.EstrangedDiscordReviewsWebhook, HttpClient = httpClient });
            this.seenItemRepository = seenItemRepository;
            this.translation = translation;
            this.steam = steam;
        }

        public IEnumerable<Task> RunAsync(CancellationToken token)
        {
            yield return GatherReviews("Estranged: Act I", 261820, token);
            yield return GatherReviews("Estranged: The Departure", 582890, token);
        }

        public async Task GatherReviews(string product, uint appId, CancellationToken token)
        {
            logger.LogInformation("Gathering reviews for app {0}", appId);

            GetReviewsResponse response = await steam.GetReviews(new GetReviewsRequest(appId) { Filter = ReviewFilter.Recent }, token);
            logger.LogInformation("Got {0} recent reviews", response.Reviews.Count);

            IDictionary<uint, Review> reviews = response.Reviews.ToDictionary(x => x.RecommendationId, x => x);

            uint[] recentReviewIds = reviews.Keys.ToArray();

            uint[] seenReviewIds = (await seenItemRepository.GetSeenItems(recentReviewIds.Select(x => x.ToString()).ToArray(), token)).Select(x => uint.Parse(x)).ToArray();

            uint[] unseenReviewIds = recentReviewIds.Except(seenReviewIds).ToArray();
            logger.LogInformation("Of which {0} are unseen", unseenReviewIds.Length);

            Review[] unseenReviews = reviews.Where(x => unseenReviewIds.Contains(x.Key))
                .Select(x => x.Value)
                .OrderBy(x => x.Created)
                .ToArray();

            foreach (Review unseenReview in unseenReviews)
            {
                var reviewContent = unseenReview.Comment.Truncate(512);

                string reviewUrl = $"https://steamcommunity.com/profiles/{unseenReview.Author.SteamId}/recommended/{appId}";

                logger.LogInformation("Posting review {0} to Slack", reviewUrl);

                TranslationResult translationResponse = null;
                try
                {
                    translationResponse = await translation.TranslateTextAsync(reviewContent, "en", null, null, token);
                }
                catch (GoogleApiException e)
                {
                    logger.LogError(e, "Encountered error translating review.");
                }

                var reviewFlags = new List<string>
                {
                    unseenReview.SteamPurchase ? "Steam Activation" : "Key Activation",
                    unseenReview.WrittenDuringEarlyAccess ? "Early Access" : "Released"
                };

                if (unseenReview.ReceivedForFree)
                {
                    reviewFlags.Add("Received for Free");
                }

                var fields = new List<Field>
                {
                    new Field
                    {
                        Title = $"Original Text ({translationResponse?.DetectedSourceLanguage ?? "unknown"})",
                        Value = reviewContent,
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
                    },
                    new Field
                    {
                        Title = "Games Owned",
                        Value = unseenReview.Author.NumGamesOwned.ToString("N"),
                        Short = true
                    },
                    new Field
                    {
                        Title = "Type",
                        Value = string.Join(", ", reviewFlags)
                    }
                };

                if (translationResponse != null && translationResponse.DetectedSourceLanguage != "en")
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
                    Username = $"{product}",
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
                }, token);

                logger.LogInformation("Inserting review {0} into DynamoDB", unseenReview.RecommendationId);
                await seenItemRepository.SetItemSeen(unseenReview.RecommendationId.ToString(), token);
            }

            logger.LogInformation("Finished posting reviews.");
        }
    }
}
