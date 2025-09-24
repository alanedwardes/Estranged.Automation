using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Estranged.Automation
{
    public static class ConfigurationExtensions
    {
        public static IList<(string, string)> GetTuples(this IConfiguration configuration, string key)
        {
            // split into tuples separated by ; each tuple is separated by ,
            return configuration[key]?.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(','))
                .Where(x => x.Length == 2)
                .Select(x => (x[0], x[1]))
                .ToList() ?? [];
        }

        public static IList<(string, string, string)> GetTriples(this IConfiguration configuration, string key)
        {
            // split into triple separated by ; each triple is separated by ,
            return configuration[key]?.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(','))
                .Where(x => x.Length == 3)
                .Select(x => (x[0], x[1], x[2]))
                .ToList() ?? [];
        }

        public static string MakeMcpReplacements(this IConfiguration configuration, string mcpResponse)
        {
            foreach (var (oldValue, newValue) in configuration.GetTuples("MCP_REPLACEMENTS"))
            {
                mcpResponse = mcpResponse.Replace(oldValue, newValue, StringComparison.InvariantCultureIgnoreCase);
            }

            return mcpResponse;
        }
    }
}
