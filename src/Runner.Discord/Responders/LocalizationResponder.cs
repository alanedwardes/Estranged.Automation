using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
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
            const string defaultBranch = "master";

            var userLocaleMapping = new Dictionary<ulong, string>
            {
                { 269883106792701952, "en" }, // For test purposes
                { 178765255441121280, "sk" },
                { 231511791228682242, "nl" }
            };

            // Try to match the user ID to a locale
            if (!userLocaleMapping.TryGetValue(message.Author.Id, out string localeId))
            {
                return;
            }

            // Find the attachment called "Game.po"
            var translationAttachment = message.Attachments.FirstOrDefault(x => x.Filename == "Game.po");
            if (translationAttachment == null)
            {
                return;
            }

            // Download the attachment
            var translation = await httpClient.GetStringAsync(translationAttachment.Url);

            // Figure out the path
            var translationPath = $"Game/{localeId}/Game.po";

            // Get the current master reference
            var master = await gitHubClient.Git.Reference.Get(owner, repository, defaultBranch);

            // Create a new branch
            var newBranch = await gitHubClient.Git.Reference.Create(owner, repository, new NewReference(Guid.NewGuid().ToString(), master.Object.Sha));

            // Get the existing file reference
            var existingFile = (await gitHubClient.Repository.Content.GetAllContentsByRef(owner, repository, translationPath, newBranch.Ref)).SingleOrDefault();

            // Update the existing file
            await gitHubClient.Repository.Content.UpdateFile(owner, repository, translationPath, new UpdateFileRequest($"Updates Game.po for {localeId}", translation, existingFile.Sha));

            // Create a pull request
            await gitHubClient.PullRequest.Create(owner, repository, new NewPullRequest($"Updates {localeId} Localization", newBranch.Ref, defaultBranch));
        }
    }
}
