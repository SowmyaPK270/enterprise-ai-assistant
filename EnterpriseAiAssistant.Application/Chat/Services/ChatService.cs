using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using EnterpriseAiAssistant.Application.Abstractions.AI;
using EnterpriseAiAssistant.Application.Abstractions.Cost;
using EnterpriseAiAssistant.Application.Abstractions.Evaluation;
using EnterpriseAiAssistant.Application.Abstractions.Guardrails;
using EnterpriseAiAssistant.Application.Abstractions.Rag;
using EnterpriseAiAssistant.Application.Chat.Interfaces;
using EnterpriseAiAssistant.Application.Chat.Models;
using EnterpriseAiAssistant.Domain.Chat;
using EnterpriseAiAssistant.Domain.Users;

// Sits inbetween ChatRequest and AIRequest.
// Main application use-case service: also owns persistence of
// conversation history to the dedicated Conversation SQL store.
namespace EnterpriseAiAssistant.Application.Chat.Services;

public sealed class ChatService : IChatService
{
    private const string DefaultTitle = "New conversation";

    private readonly IAIClient _aiClient;
    private readonly IConversationRepository _repository;
    private readonly IGuardrailService _guardrails;
    private readonly IChatEvaluationService _evaluation;
    private readonly ICitationAccumulator _citations;
    private readonly ICostAccumulator _cost;

    public ChatService(
        IAIClient aiClient,
        IConversationRepository repository,
        IGuardrailService guardrails,
        IChatEvaluationService evaluation,
        ICitationAccumulator citations,
        ICostAccumulator cost)
    {
        _aiClient = aiClient;
        _repository = repository;
        _guardrails = guardrails;
        _evaluation = evaluation;
        _citations = citations;
        _cost = cost;
    }

    public Task<User> EnsureUserAsync(
        string externalId,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new ArgumentException(
                "External id cannot be empty.",
                nameof(externalId));
        }

        return _repository.GetOrCreateUserAsync(
            externalId,
            displayName,
            cancellationToken);
    }

    public Task<IReadOnlyList<ChatSession>> GetSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetSessionsForUserAsync(userId, cancellationToken);
    }

    public async Task<ChatSession> CreateSessionAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var session = ChatSession.Create(userId);

        await _repository.AddSessionAsync(session, cancellationToken);

        return session;
    }


    public async IAsyncEnumerable<string> StreamMessageAsync(
    ChatRequest request,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException(
                "Message cannot be empty.",
                nameof(request));
        }

        var session = await _repository.GetSessionAsync(request.SessionId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Conversation '{request.SessionId}' was not found.");

        if (session.UserId != request.UserId)
        {
            throw new InvalidOperationException(
                "This conversation does not belong to the requesting user.");
        }

        // A DI scope in this Blazor Server app spans the whole browser
        // circuit, not a single message, so these MUST be reset here at
        // the start of every turn rather than relying on scope disposal
        // to clear whatever the previous turn (or a previous session on
        // the same circuit) recorded.
        _citations.Reset();
        _cost.Reset();

        var stopwatch = Stopwatch.StartNew();

        // --- Input guardrail: validate/mask before we touch the model or store ---
        string? inputBlockReason = null;
        var maskedUserMessage = request.Message;

        try
        {
            maskedUserMessage = await _guardrails.ValidateInputAsync(
                request.Message, cancellationToken);
        }
        catch (GuardrailViolationException ex)
        {
            inputBlockReason = ex.Reason;
        }

        if (inputBlockReason is not null)
        {
            var userMessage = ChatMessage.Create(ChatRole.User, request.Message);
            session.AddMessage(userMessage);
            await _repository.AddMessageAsync(session.Id, userMessage, cancellationToken);

            var refusal = ChatMessage.Create(ChatRole.Assistant, inputBlockReason);
            session.AddMessage(refusal);
            await _repository.AddMessageAsync(session.Id, refusal, cancellationToken);

            stopwatch.Stop();

            var blockedContext = new ChatTurnEvaluationContext(
                session.Id,
                request.UserId,
                request.Message,
                inputBlockReason,
                stopwatch.Elapsed,
                WasBlockedByGuardrail: true,
                GuardrailReason: inputBlockReason);

            await _evaluation.RecordTurnAsync(blockedContext, CancellationToken.None);
            await _evaluation.EvaluateTurnAsync(blockedContext, CancellationToken.None);

            yield return inputBlockReason;

            yield break;
        }

        // Store the MASKED version so raw PII never enters history or
        // gets sent back to the model as context in later turns.
        var validatedUserMessage = ChatMessage.Create(ChatRole.User, maskedUserMessage);

        session.AddMessage(validatedUserMessage);

        await _repository.AddMessageAsync(session.Id, validatedUserMessage, cancellationToken);

        if (session.Title == DefaultTitle)
        {
            var title = maskedUserMessage.Length > 30
                ? maskedUserMessage[..30] + "..."
                : maskedUserMessage;

            session.Rename(title);

            await _repository.RenameSessionAsync(session.Id, title, cancellationToken);
        }

        var aiRequest = new AIRequest(session.Messages.ToList());

        // Buffer the FULL response server-side first. We cannot
        // safely release partial chunks to the client because a
        // guardrail match (e.g. an email address, a secret) can span
        // multiple chunks, and redaction after the fact can't "unsend"
        // what the client already rendered. While this streams, the
        // active IAIClient implementation (SemanticKernelAIClient) is
        // populating _citations and _cost as it plans the query, calls
        // retrieval tools, and completes the response. ---
        var buffer = new StringBuilder();

        await foreach (var chunk in _aiClient.StreamCompleteAsync(aiRequest, cancellationToken))
        {
            if (string.IsNullOrEmpty(chunk))
                continue;

            buffer.Append(chunk);
        }

        var rawContent = buffer.ToString();
        var wasOutputBlocked = false;
        string? outputBlockReason = null;
        var finalContent = rawContent;
        IReadOnlyList<Citation> turnCitations = [];
        ExecutionCostSummary? turnCostSummary = null;

        if (!string.IsNullOrWhiteSpace(rawContent))
        {
            try
            {
                // --- Output guardrail: redact/validate the complete reply ---
                finalContent = await _guardrails.ValidateOutputAsync(
                    rawContent, CancellationToken.None);
            }
            catch (GuardrailViolationException ex)
            {
                wasOutputBlocked = true;
                outputBlockReason = ex.Reason;
                finalContent = ex.Reason;
            }

            // Traceability & cost: append which systems the answer is
            // grounded in and what it cost
            if (!wasOutputBlocked)
            {
                turnCitations = _citations.GetCitations();
                turnCostSummary = _cost.GetSummary();

                var footer = BuildTraceabilityFooter(turnCitations, turnCostSummary);

                if (!string.IsNullOrEmpty(footer))
                {
                    finalContent += footer;
                }
            }

            var assistantMessage = ChatMessage.Create(ChatRole.Assistant, finalContent);

            session.AddMessage(assistantMessage);

            await _repository.AddMessageAsync(
                session.Id,
                assistantMessage,
                CancellationToken.None);

            stopwatch.Stop();

            var turnContext = new ChatTurnEvaluationContext(
                session.Id,
                request.UserId,
                request.Message,
                finalContent,
                stopwatch.Elapsed,
                WasBlockedByGuardrail: wasOutputBlocked,
                GuardrailReason: outputBlockReason,
                CitedSources: turnCitations.Select(c => c.SourceSystem).Distinct().ToList(),
                EstimatedCostUsd: turnCostSummary?.TotalEstimatedCostUsd);

            await _evaluation.RecordTurnAsync(turnContext, CancellationToken.None);
            await _evaluation.EvaluateTurnAsync(turnContext, CancellationToken.None);
        }

        // Re-chunk the VALIDATED content so the UI still gets a
        // streaming-like typing effect, but only ever sees safe text.
        const int chunkSize = 20;

        for (var i = 0; i < finalContent.Length; i += chunkSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var length = Math.Min(chunkSize, finalContent.Length - i);

            yield return finalContent.Substring(i, length);

            await Task.Delay(15, cancellationToken); // small delay for a natural typing feel
        }
    }

    /// <summary>
    /// Renders the Citation/Cost stage results into a short
    /// footer appended to the assistant's message.
    /// </summary>
    private static string BuildTraceabilityFooter(
        IReadOnlyList<Citation> citations,
        ExecutionCostSummary? cost)
    {
        if (citations.Count == 0 && (cost is null || cost.Calls.Count == 0))
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.Append("\n\n---");

        if (citations.Count > 0)
        {
            sb.Append("\n**Sources:**");

            foreach (var citation in citations)
            {
                sb.Append($"\n- {citation.SourceSystem}");

                if (!string.IsNullOrWhiteSpace(citation.Query))
                {
                    sb.Append($" — \"{citation.Query}\"");
                }
            }
        }

        if (cost is not null && cost.Calls.Count > 0)
        {
            sb.Append(
                $"\n\n*Estimated cost: ${cost.TotalEstimatedCostUsd:0.000000} " +
                $"({cost.TotalInputTokens} input / {cost.TotalOutputTokens} output tokens " +
                $"across {cost.Calls.Count} LLM call(s))*");
        }

        return sb.ToString();
    }
}