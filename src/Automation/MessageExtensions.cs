using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation
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
                633724305871470593, // #goodbyes
                633725420285591571, // #deletions
                435152590209286145, // #reviews
                455012497775132673, // #community

                368117881000427540, // #general
                479405352207777795, // #gaming
                1036776421584027688,// #game-screenshots
                845428451077783643, // #verification

                437311972917248022, // #act-i
                437312012603752458, // #the-departure
                802974954160128011, // #arctic-cold
                439742315016486922, // #dev-screenshots
                457813004889751553, // #ideas
                454937488000024577, // #bugs
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

        public static async Task<IList<IMessage>> GetFullConversation(this IMessage message, CancellationToken cancellation)
        {
            IMessage current = message;

            var history = new List<IMessage>();

            if (current != null)
            {
                history.Add(current);
            }

            while (current != null && current.Reference != null && current.Reference.MessageId.IsSpecified)
            {
                current = await current.Channel.GetMessageAsync(current.Reference.MessageId.Value, options: cancellation.ToRequestOptions());
                if (current != null)
                {
                    history.Add(current);
                }
            }

            return history;
        }

        public static IList<ChatMessage> BuildChatMessages(IList<IMessage> messageHistory, int initialMessagePrefixLength, IMessage initialMessage, string systemPrompt)
        {
            IList<ChatMessage> chatMessages = [new(ChatRole.System, systemPrompt)];

            foreach (var message in messageHistory.Reverse())
            {
                if (message.Author.IsBot)
                {
                    chatMessages.Add(new(ChatRole.Assistant, message.Content));
                }
                else if (message == initialMessage)
                {
                    chatMessages.Add(new(ChatRole.User, message.Content[initialMessagePrefixLength..].Trim()));
                }
                else
                {
                    chatMessages.Add(new(ChatRole.User, message.Content));
                }
            }

            return chatMessages;
        }

        public static async Task PostChatMessages(IMessage latestMessage, IList<ChatMessage> chatMessages, CancellationToken token)
        {
            foreach (var chatMessage in chatMessages.Where(x => !string.IsNullOrWhiteSpace(x.Text)))
            {
                foreach (var chunk in chatMessage.Text.ChunkBy(2000))
                {
                    await latestMessage.Channel.SendMessageAsync(chunk, messageReference: new MessageReference(latestMessage.Id), flags: MessageFlags.SuppressEmbeds, options: token.ToRequestOptions());
                }
            }
        }

        private static IEnumerable<string> ChunkBy(this string str, int chunkSize)
        {
            for (int i = 0; i < str.Length; i += chunkSize)
            {
                yield return str.Substring(i, Math.Min(chunkSize, str.Length - i));
            }
        }
    }
}
