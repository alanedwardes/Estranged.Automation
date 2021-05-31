using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;

namespace Estranged.Automation.Runner.Discord
{
    public static class MessageExtensions
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

            foreach (var attachment in quotedMessage.Attachments)
            {
                builder.WithImageUrl(attachment.Url);
            }

            foreach (var embed in quotedMessage.Embeds)
            {
                if (!string.IsNullOrWhiteSpace(embed.Description))
                {
                    builder.AddField(embed.Title ?? embed.Author.Value.Name, embed.Description.Length > 1024 ? embed.Description.Substring(0, 1024) : embed.Description);
                }

                if (embed.Image.HasValue)
                {
                    builder.WithImageUrl(embed.Image.Value.Url);
                }

                if (embed.Thumbnail.HasValue)
                {
                    builder.WithImageUrl(embed.Thumbnail.Value.Url);
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
                435094509953744907, // #announcements
                453209462438887435, // #welcome
                633724305871470593, // #goodbyes
                633725420285591571, // #deletions
                435152590209286145, // #reviews
                470513916393291777, // #feedback
                435913619662831616, // #syndication
                455012497775132673, // #community
                435097044433240065, // #git

                368117881000427540, // #general
                479405352207777795, // #gaming
                437311972917248022, // #act-i
                437312012603752458, // #the-departure
                439742315016486922, // #dev-screenshots
                454937488000024577, // #bugs,
                457813004889751553, // #ideas
                535160312320753664, // #bots
                549182701630914561, // #aaaaaaaaaaaaaaaaaaaa

                435131444931723264, // #friends
            };

            return publicChannels.Contains(channel.Id);
        }

        public static SocketTextChannel GetChannelByName(this DiscordSocketClient client, string channelName)
        {
            var guild = client.Guilds.Single(x => x.Name == "ESTRANGED");

            return guild.TextChannels.Single(x => x.Name == channelName);
        }
    }
}
