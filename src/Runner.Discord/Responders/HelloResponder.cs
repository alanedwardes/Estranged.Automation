using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace Estranged.Automation.Runner.Discord.Responders
{
    public class HelloResponder : IResponder
    {
        public async Task ProcessMessage(IMessage message, CancellationToken token)
        {
            if (!message.Content.ToLowerInvariant().Contains("hello"))
            {
                return;
            }

            if (!message.MentionedUserIds.Contains(437014310078906378ul))
            {
                return;
            }

            var userIds = new ulong[] { 367376322684780544, 93842442498879488 };

            var channelMembers = (await message.Channel.GetUsersAsync(options: token.ToRequestOptions()).Flatten()).ToArray();

            var chosenUser = channelMembers.OrderBy(x => Guid.NewGuid())
                                           .Where(x => x.Id != message.Author.Id && x.Status == UserStatus.Online && userIds.Contains(x.Id))
                                           .FirstOrDefault();

            if (chosenUser == null)
            {
                return;
            }

            await message.Channel.SendMessageAsync($"Hello <@{chosenUser.Id}>", options: token.ToRequestOptions());
        }
    }
}
