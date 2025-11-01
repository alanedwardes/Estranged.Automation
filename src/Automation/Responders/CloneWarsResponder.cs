using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Estranged.Automation.Events;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Estranged.Automation.Responders
{
	internal sealed class CloneWarsResponder : IResponder
	{
		private readonly ILogger<CloneWarsResponder> _logger;
		private readonly IChatClientFactory _chatFactory;

		public CloneWarsResponder(ILogger<CloneWarsResponder> logger, IChatClientFactory chatFactory)
		{
			_logger = logger;
			_chatFactory = chatFactory;
		}

		private enum SessionStep
		{
			AskUrnA,
			AskUrnB,
			AskSystemA,
			AskSystemB,
			AskInitialA,
			AskInitialB,
			AwaitContinue,
			Running,
			Completed
		}

		private sealed class Session
		{
			public ulong ChannelId { get; init; }
			public ulong UserId { get; init; }
			public SessionStep Step { get; set; } = SessionStep.AskUrnA;
			public string UrnA { get; set; }
			public string UrnB { get; set; }
			public string SystemA { get; set; }
			public string SystemB { get; set; }
			public string InitialA { get; set; }
			public string InitialB { get; set; }
			public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
			public List<ChatMessage> HistoryA { get; } = new();
			public List<ChatMessage> HistoryB { get; } = new();
			public int TurnsA { get; set; }
			public int TurnsB { get; set; }
			public bool NextIsA { get; set; } = true;
		}

		private static readonly ConcurrentDictionary<(ulong ChannelId, ulong UserId), Session> _sessions = new();
		private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(10);

		public async Task ProcessMessage(IMessage originalMessage, CancellationToken token)
		{
			if (originalMessage.Channel.IsPublicChannel())
			{
				return;
			}

			if (originalMessage.Channel is not ITextChannel)
			{
				return;
			}

			var key = (originalMessage.Channel.Id, originalMessage.Author.Id);
			_sessions.TryGetValue(key, out var existing);
			if (existing != null && DateTimeOffset.UtcNow - existing.LastUpdated > SessionTimeout)
			{
				_sessions.TryRemove(key, out _);
				existing = null;
			}

			var content = (originalMessage.Content ?? string.Empty).Trim();

			const string trigger = "clonewars";
			if (existing == null)
			{
				if (!content.StartsWith(trigger, StringComparison.InvariantCultureIgnoreCase))
				{
					return;
				}

				var session = new Session
				{
					ChannelId = originalMessage.Channel.Id,
					UserId = originalMessage.Author.Id,
					Step = SessionStep.AskUrnA
				};
				_sessions[key] = session;
				await originalMessage.Channel.SendMessageAsync("Enter URN for Agent A (e.g., urn:openai:gpt-4o-mini or urn:ollama:llama3):", messageReference: new MessageReference(originalMessage.Id), options: token.ToRequestOptions());
				return;
			}

			// We have an active session; process the wizard inputs
			if (!content.StartsWith(trigger, StringComparison.InvariantCultureIgnoreCase))
			{
				await HandleWizard(existing, originalMessage, content, token);
			}
		}

		private async Task HandleWizard(Session session, IMessage message, string content, CancellationToken token)
		{
			content = content.Trim();

			if (string.Equals(content, "cancel", StringComparison.InvariantCultureIgnoreCase))
			{
				_sessions.TryRemove((session.ChannelId, session.UserId), out _);
				await message.Channel.SendMessageAsync("Cancelled clone wars session.", messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
				return;
			}

			if (string.Equals(content, "redo", StringComparison.InvariantCultureIgnoreCase))
			{
				await StepBack(session, message, token);
				return;
			}

			session.LastUpdated = DateTimeOffset.UtcNow;

			switch (session.Step)
			{
				case SessionStep.AskUrnA:
					if (!await TryValidateUrn(content, message, token)) return;
					session.UrnA = content;
					session.Step = SessionStep.AskUrnB;
					await message.Channel.SendMessageAsync("Enter URN for Agent B:", messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
					break;

				case SessionStep.AskUrnB:
					if (!await TryValidateUrn(content, message, token)) return;
					session.UrnB = content;
					session.Step = SessionStep.AskSystemA;
					await message.Channel.SendMessageAsync("Enter system prompt for Agent A:", messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
					break;

				case SessionStep.AskSystemA:
					session.SystemA = content; // allow empty
					session.Step = SessionStep.AskSystemB;
					await message.Channel.SendMessageAsync("Enter system prompt for Agent B:", messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
					break;

				case SessionStep.AskSystemB:
					session.SystemB = content; // allow empty
					session.Step = SessionStep.AskInitialA;
					await message.Channel.SendMessageAsync("Enter initial message for Agent A:", messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
					break;

				case SessionStep.AskInitialA:
					if (string.IsNullOrWhiteSpace(content))
					{
						await message.Channel.SendMessageAsync("Initial message cannot be empty. Type it or 'cancel'.", messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
						return;
					}
					session.InitialA = content;
					session.Step = SessionStep.AskInitialB;
					await message.Channel.SendMessageAsync("Enter initial message for Agent B:", messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
					break;

				case SessionStep.AskInitialB:
					if (string.IsNullOrWhiteSpace(content))
					{
						await message.Channel.SendMessageAsync("Initial message cannot be empty. Type it or 'cancel'.", messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
						return;
					}
					session.InitialB = content;
					// Initialize histories and prompt for explicit continue
					session.HistoryA.Clear();
					session.HistoryB.Clear();
					if (!string.IsNullOrWhiteSpace(session.SystemA))
					{
						session.HistoryA.Add(new ChatMessage(ChatRole.System, session.SystemA));
					}
					session.HistoryA.Add(new ChatMessage(ChatRole.User, session.InitialA));
					if (!string.IsNullOrWhiteSpace(session.SystemB))
					{
						session.HistoryB.Add(new ChatMessage(ChatRole.System, session.SystemB));
					}
					session.HistoryB.Add(new ChatMessage(ChatRole.User, session.InitialB));
					session.TurnsA = 0;
					session.TurnsB = 0;
					session.NextIsA = true; // A starts
					session.Step = SessionStep.AwaitContinue;
					await message.Channel.SendMessageAsync("Type 'continue' to start, or 'cancel' to abort.", messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
					break;

				case SessionStep.AwaitContinue:
					if (string.Equals(content, "continue", StringComparison.InvariantCultureIgnoreCase))
					{
						session.Step = SessionStep.Running;
						var completed = await RunOneTurn(session, message, token);
						if (!completed)
						{
							session.Step = SessionStep.AwaitContinue;
							await message.Channel.SendMessageAsync("Type 'continue' to proceed, or 'cancel' to stop.", messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
						}
						break;
					}
					await message.Channel.SendMessageAsync("Awaiting 'continue' or 'cancel'.", messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
					break;

				case SessionStep.Running:
					// Ignore stray inputs while running
					break;
			}
		}

		private async Task<bool> TryValidateUrn(string urn, IMessage message, CancellationToken token)
		{
			try
			{
				using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
				cts.CancelAfter(TimeSpan.FromSeconds(5));
				var _ = _chatFactory.CreateClient(urn);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Invalid URN: {Urn}", urn);
				await message.Channel.SendMessageAsync("Invalid URN. Expected 'urn:<provider>:<model>' (providers: openai, ollama). Try again or 'cancel'.", messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
				return false;
			}
		}

		private async Task StepBack(Session session, IMessage message, CancellationToken token)
		{
			switch (session.Step)
			{
				case SessionStep.AskUrnA:
					await message.Channel.SendMessageAsync("Enter URN for Agent A:", messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
					break;
				case SessionStep.AskUrnB:
					session.UrnA = null;
					session.Step = SessionStep.AskUrnA;
					await message.Channel.SendMessageAsync("Enter URN for Agent A:", messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
					break;
				case SessionStep.AskSystemA:
					session.UrnB = null;
					session.Step = SessionStep.AskUrnB;
					await message.Channel.SendMessageAsync("Enter URN for Agent B:", messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
					break;
				case SessionStep.AskSystemB:
					session.SystemA = null;
					session.Step = SessionStep.AskSystemA;
					await message.Channel.SendMessageAsync("Enter system prompt for Agent A:", messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
					break;
				case SessionStep.AskInitialA:
					session.SystemB = null;
					session.Step = SessionStep.AskSystemB;
					await message.Channel.SendMessageAsync("Enter system prompt for Agent B:", messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
					break;
				case SessionStep.AskInitialB:
					session.InitialA = null;
					session.Step = SessionStep.AskInitialA;
					await message.Channel.SendMessageAsync("Enter initial message for Agent A:", messageReference: new MessageReference(message.Id), options: token.ToRequestOptions());
					break;
			}
		}

		private async Task<bool> RunOneTurn(Session session, IMessage initialMessage, CancellationToken token)
		{
			try
			{
				var options = new ChatOptions
				{
					AdditionalProperties = new AdditionalPropertiesDictionary { { "Think", false } }
				};

				if (session.NextIsA)
				{
					var clientA = _chatFactory.CreateClient(session.UrnA);
					var response = await clientA.GetResponseAsync(session.HistoryA, options, token);
					var text = response.Text ?? string.Empty;
					if (string.IsNullOrWhiteSpace(text))
					{
						await initialMessage.Channel.SendMessageAsync("Agent A produced no response. Ending.", messageReference: new MessageReference(initialMessage.Id), options: token.ToRequestOptions());
						_sessions.TryRemove((session.ChannelId, session.UserId), out _);
						session.Step = SessionStep.Completed;
						return true;
					}
					session.HistoryA.Add(new ChatMessage(ChatRole.Assistant, text));
					session.HistoryB.Add(new ChatMessage(ChatRole.User, text));
					session.TurnsA++;
					session.NextIsA = false;
					await MessageExtensions.PostChatMessages(initialMessage, new List<ChatMessage> { new(ChatRole.Assistant, $"A> {text}") }, token);
				}
				else
				{
					var clientB = _chatFactory.CreateClient(session.UrnB);
					var response = await clientB.GetResponseAsync(session.HistoryB, options, token);
					var text = response.Text ?? string.Empty;
					if (string.IsNullOrWhiteSpace(text))
					{
						await initialMessage.Channel.SendMessageAsync("Agent B produced no response. Ending.", messageReference: new MessageReference(initialMessage.Id), options: token.ToRequestOptions());
						_sessions.TryRemove((session.ChannelId, session.UserId), out _);
						session.Step = SessionStep.Completed;
						return true;
					}
					session.HistoryB.Add(new ChatMessage(ChatRole.Assistant, text));
					session.HistoryA.Add(new ChatMessage(ChatRole.User, text));
					session.TurnsB++;
					session.NextIsA = true;
					await MessageExtensions.PostChatMessages(initialMessage, new List<ChatMessage> { new(ChatRole.Assistant, $"B> {text}") }, token);
				}
				return false;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Clone wars conversation failed");
				await initialMessage.Channel.SendMessageAsync("An error occurred during clone wars. Session ended.", messageReference: new MessageReference(initialMessage.Id), options: token.ToRequestOptions());
				_sessions.TryRemove((session.ChannelId, session.UserId), out _);
				session.Step = SessionStep.Completed;
				return true;
			}
		}
	}
}


