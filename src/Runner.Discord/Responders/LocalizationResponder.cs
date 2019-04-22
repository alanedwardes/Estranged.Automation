using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json;
using Octokit;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public class LocalizationResponder : IResponder
    {
        private readonly IGitHubClient gitHubClient;
        private readonly HttpClient httpClient;

        public LocalizationResponder(IGitHubClient gitHubClient, HttpClient httpClient)
        {
            this.gitHubClient = gitHubClient;
            this.httpClient = httpClient;
        }

        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            const string owner = "alanedwardes";
            const string repository = "Insulam.Localization";
            const string masterReference = "heads/master";

            // Find the attachment called "Game.po"
            var translationAttachment = message.Attachments.FirstOrDefault(x => x.Filename == "Game.po");
            if (translationAttachment == null)
            {
                return;
            }

            // Get the permissions manifest from GitHub
            var permissions = (await gitHubClient.Repository.Content.GetAllContentsByRef(owner, repository, "permissions.json", "refs/" + masterReference)).SingleOrDefault();

            // Deserialize to JSON
            var userLocaleMapping = JsonConvert.DeserializeObject<IDictionary<ulong, string>>(permissions.Content);

            // Try to match the user ID to a locale
            if (!userLocaleMapping.TryGetValue(message.Author.Id, out string localeId))
            {
                return;
            }

            // Download the attachment
            var translation = await httpClient.GetStringAsync(translationAttachment.Url);

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
