using Anthropic;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OllamaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation
{
	public interface IChatClientFactory
	{
		IChatClient CreateClient(string urn);
		Task<IList<string>> GetModels(string urn, CancellationToken token);
    }

	internal sealed class ChatClientFactory : IChatClientFactory
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IConfiguration _configuration;
        private readonly IAnthropicClient _anthropicClient;

        public ChatClientFactory(IHttpClientFactory httpClientFactory, IAnthropicClient anthropicClient, IConfiguration configuration)
		{
			_httpClientFactory = httpClientFactory;
			_configuration = configuration;
            _anthropicClient = anthropicClient;
        }

		public IChatClient CreateClient(string urn)
		{
			var parts = urn.Split(':', 3, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length != 3 || !parts[0].Equals("urn", StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException("Invalid URN format. Expected 'urn:<provider>:<model>'.", nameof(urn));
			}

			var provider = parts[1].ToLowerInvariant();
			var model = parts[2];

			switch (provider)
			{
				case "ollama":
					{
						var httpClient = _httpClientFactory.CreateClient();
						httpClient.Timeout = TimeSpan.FromHours(1);
						httpClient.BaseAddress = new Uri(_configuration["OLLAMA_HOST"]);
						return new OllamaApiClient(httpClient, model);
					}
				case "anthropic":
					{
						return _anthropicClient.AsIChatClient(model);
                    }
                default:
					throw new NotSupportedException($"Provider '{provider}' is not supported.");
			}
		}

		public async Task<IList<string>> GetModels(string urn, CancellationToken token)
		{
            var parts = urn.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !parts[0].Equals("urn", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Invalid URN format. Expected 'urn:<provider>:<model>'.", nameof(urn));
            }

            var provider = parts[1].ToLowerInvariant();

			switch (provider)
			{
				case "ollama":
					{
                        using var httpClient = _httpClientFactory.CreateClient();
                        httpClient.Timeout = TimeSpan.FromHours(1);
                        httpClient.BaseAddress = new Uri(_configuration["OLLAMA_HOST"]);
                        using var client = new OllamaApiClient(httpClient);
						var ollamaModels = await client.ListLocalModelsAsync(token);
						return [.. ollamaModels.Select(x => x.Name)];
                    }
				case "anthropic":
					{
                        var models = await _anthropicClient.Models.List(null, token);
						return [.. models.Items.Select(x => x.ID)];
                    }
                default:
                    throw new NotSupportedException($"Provider '{provider}' is not supported.");
            }
        }

	}
}


