using Microsoft.Extensions.Configuration;
using System;
using System.Linq;

namespace Estranged.Automation.Runner.Discord
{
    public static class ConfigurationExtensions
    {
        public static string MakeMcpReplacements(this IConfiguration configuration, string mcpResponse)
        {
            var mcpReplacements = configuration["MCP_REPLACEMENTS"];

            // split into tuples separated by ; each tuple is separated by ,
            var replacements = mcpReplacements?.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Where(x => x.Length == 2)
                .Select(x => (x[0], x[1]))
                .ToList() ?? [];

            foreach (var (oldValue, newValue) in replacements)
            {
                mcpResponse = mcpResponse.Replace(oldValue, newValue, StringComparison.InvariantCultureIgnoreCase);
            }

            return mcpResponse;
        }
    }
}
