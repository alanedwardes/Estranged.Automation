using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Discord;
using Octokit;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public class LocalizationResponder : IResponder
    {
        private readonly IGitHubClient gitHubClient;
        private readonly HttpClient httpClient;
        private readonly IAmazonDynamoDB dynamoDb;

        public LocalizationResponder(IGitHubClient gitHubClient, IAmazonDynamoDB dynamoDb, HttpClient httpClient)
        {
            this.gitHubClient = gitHubClient;
            this.httpClient = httpClient;
            this.dynamoDb = dynamoDb;
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            const string owner = "alanedwardes";
            const string repository = "Insulam.Localization";
            const string masterReference = "heads/master";

            // Find the attachment called "Game.po"
            var translationAttachment = message.Attachments.FirstOrDefault(x => x.Filename.Contains("Game.po"));
            if (translationAttachment == null)
            {
                return;
            }

            // Validate permissions against the DynamoDB table
            var permissionItem = await dynamoDb.GetItemAsync("EstrangedAutomationTranslators", new Dictionary<string, AttributeValue> {{"UserId", new AttributeValue(message.Author.Id.ToString())}}, token);
            if (permissionItem.Item == null)
            {
                return;
            }

            // Pull the locale ID from the row
            var localeId = permissionItem.Item["LanguageId"].S;

            // Download the attachment and normalise line endings
            var translation = (await httpClient.GetStringAsync(translationAttachment.Url)).Replace("\r\n", "\n");

            // Figure out the path
            var translationPath = $"Game/{localeId}/Game.po";

            // Get the current master reference
            var master = await gitHubClient.Git.Reference.Get(owner, repository, masterReference);

            // Create a new branch
            var newBranch = await gitHubClient.Git.Reference.Create(owner, repository, new NewReference("refs/heads/" + Guid.NewGuid(), master.Object.Sha));

            // Get the existing file reference
            var existingFile = (await gitHubClient.Repository.Content.GetAllContentsByRef(owner, repository, translationPath, newBranch.Ref)).SingleOrDefault();

            // Update the existing file
            await gitHubClient.Repository.Content.UpdateFile(owner, repository, translationPath, new UpdateFileRequest($"Updates Game.po for {localeId}", translation, existingFile.Sha, newBranch.Ref));

            // Create a pull request
            var pullRequest = await gitHubClient.PullRequest.Create(owner, repository, new NewPullRequest($"Updates {localeId} Localization", newBranch.Ref, "refs/" + masterReference));

            // Send a message with PR link
            await message.Channel.SendMessageAsync($"Opened PR on behalf of <@{message.Author.Id}>: {pullRequest.HtmlUrl} (CC <@269883106792701952>)");
        }
    }
}
