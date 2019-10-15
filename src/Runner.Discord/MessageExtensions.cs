using Discord;
using System.Collections.Generic;
using System.Linq;

namespace Estranged.Automation.Runner.Discord
{
    internal static class MessageExtensions
    {
        public static Embed QuoteMessage(this IMessage quotedMessage, IUser quoter = null)
        {
            var builder = new EmbedBuilder()
                .WithTimestamp(quotedMessage.CreatedAt)
                .WithAuthor(quotedMessage.Author.Username, quotedMessage.Author.GetAvatarUrl())
                .WithDescription(quotedMessage.Content);

            if (quoter == null)
            {
                builder.WithFooter($"Originally posted in #{quotedMessage.Channel.Name}");
            }
            else
            { 
                builder.WithFooter($"Quoted by {quoter.Username}, originally posted in #{quotedMessage.Channel.Name}");
            }

            foreach (var embed in quotedMessage.Embeds)
            {
                if (!string.IsNullOrWhiteSpace(embed.Title) && !string.IsNullOrWhiteSpace(embed.Description))
                {
                    builder.AddField(embed.Title, embed.Description);
                }

                foreach (var field in embed.Fields)
                {
                    if (!string.IsNullOrWhiteSpace(field.Name) && !string.IsNullOrWhiteSpace(field.Value))
                    {
                        builder.AddField(field.Name, field.Value, field.Inline);
                    }
                }
            }

            return builder.Build();
        }

        public static bool IsPublicChannel(this IChannel channel)
        {
            IEnumerable<ulong> publicChannels = new ulong[]
            {
                368117881000427540, // #general
                479405352207777795, // #gaming
                437311972917248022, // #act-i
                437312012603752458, // #act-ii
                439742315016486922, // #dev-screenshots
                454937488000024577, // #bugs,
                457813004889751553, // #ideas
                535160312320753664, // #bots
                549182701630914561, // #aaaaaaaaaaaaaaaaaaaa
            };

            return publicChannels.Contains(channel.Id);
        }
    }
}
