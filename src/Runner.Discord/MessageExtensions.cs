﻿using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.CompilerServices;

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

                899774965895811082, // #test1
                899775011261399131, // #test2
                899775057331642448, // #test3

                435131444931723264, // #friends
            };

            return publicChannels.Contains(channel.Id);
        }

        public static bool IsProtectedChannel(this IChannel channel)
        {
            IEnumerable<ulong> protectedChannels = new ulong[]
            {
                1036776421584027688, // #game-screenshots
                435094509953744907, // #announcements
                454937488000024577, // #bugs
                439742315016486922, // #dev-screenshots
                457813004889751553, // #ideas
                883750008141279253, // #game-dev
                455012497775132673, // #community
                435152590209286145, // #reviews
                633725420285591571, // #deletions
                633724305871470593, // #goodbyes
                437311972917248022, // #act-i
                437312012603752458, // #the-departure
                802974954160128011, // #arctic-cold
            };

            return protectedChannels.Contains(channel.Id);
        }

        public static SocketGuild GetEstrangedGuild(this DiscordSocketClient client)
        {
            return client.Guilds.Single(x => x.Name == "ESTRANGED");
        }

        public static SocketTextChannel GetChannelByName(this DiscordSocketClient client, string channelName)
        {
            return client.GetEstrangedGuild().TextChannels.Single(x => x.Name == channelName);
        }

        public static async IAsyncEnumerable<IMessage> GetFullConversation(this IMessage message, [EnumeratorCancellation] CancellationToken cancellation)
        {
            IMessage current = message;

            if (current != null)
            {
                yield return current;
            }

            while (current != null && current.Reference != null && current.Reference.MessageId.IsSpecified)
            {
                current = await current.Channel.GetMessageAsync(current.Reference.MessageId.Value, options: cancellation.ToRequestOptions());
                if (current != null)
                {
                    yield return current;
                }
            }
        }
    }
}
