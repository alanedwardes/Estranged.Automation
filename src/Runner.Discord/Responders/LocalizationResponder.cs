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
            const string masterReference = "heads/master";

            var userLocaleMapping = new Dictionary<ulong, string>
            {
                { 269883106792701952, "en" }, // For test purposes
                { 178765255441121280, "sk" },
                { 231511791228682242, "nl" },
                { 556512268402294825, "de" },
                { 536004191886376960, "km" },
                { 367376322684780544, "tr" },
                { 386296200225226752, "hu" },
                { 352608170268688384, "pt-BR" },
                { 245630696595390466, "pr-PT" }
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
            await message.Channel.SendMessageAsync($"Opened PR on behalf of <@{message.Author.Id}>: {pullRequest.Url} (CC <@269883106792701952>)");

            // Delete original message
            await message.DeleteAsync();
        }
    }
}
