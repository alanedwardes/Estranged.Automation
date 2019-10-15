using Discord;

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
    }
}
