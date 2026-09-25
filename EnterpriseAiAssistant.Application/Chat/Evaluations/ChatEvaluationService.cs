using EnterpriseAiAssistant.Application.Abstractions.Evaluation;
using Microsoft.Extensions.Logging;

namespace EnterpriseAiAssistant.Application.Chat.Evaluation;

/// <summary>
/// Baseline evaluation implementation: logs every turn via ILogger
/// and applies simple heuristics
/// for relevance/groundedness/hallucination/refusal-correctness.
/// </summary>
public sealed class ChatEvaluationService : IChatEvaluationService
{
    private readonly ILogger<ChatEvaluationService> _logger;

    // Sample rate for the (potentially expensive) deep evaluation path.
    private const double EvaluationSampleRate = 1.0; // evaluate every turn by default

    private static readonly Random Sampler = new();

    public ChatEvaluationService(ILogger<ChatEvaluationService> logger)
    {
        _logger = logger;
    }

    public Task RecordTurnAsync(
        ChatTurnEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "ChatTurn Session={SessionId} User={UserId} " +
            "LatencyMs={LatencyMs} Blocked={Blocked} Reason={Reason} " +
            "UserMsgLen={UserLen} AssistantMsgLen={AssistantLen} " +
            "Sources=[{Sources}] EstimatedCostUsd={EstimatedCostUsd}",
            context.SessionId,
            context.UserId,
            context.Latency.TotalMilliseconds,
            context.WasBlockedByGuardrail,
            context.GuardrailReason,
            context.UserMessage.Length,
            context.AssistantMessage.Length,
            context.CitedSources is null ? string.Empty : string.Join(", ", context.CitedSources),
            context.EstimatedCostUsd);

        return Task.CompletedTask;
    }

    public Task<ChatTurnEvaluationResult> EvaluateTurnAsync(
        ChatTurnEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        if (Sampler.NextDouble() > EvaluationSampleRate)
        {
            return Task.FromResult(new ChatTurnEvaluationResult(
                IsRelevant: true,
                IsGrounded: true,
                AppearsToHallucinate: false,
                RefusalWasCorrect: true,
                Scores: new Dictionary<string, double>()));
        }

        var isRefusal = LooksLikeRefusal(context.AssistantMessage);
        var shouldHaveRefused = context.WasBlockedByGuardrail;

        var refusalWasCorrect =
            shouldHaveRefused == isRefusal;

        var isRelevant = HeuristicRelevance(
            context.UserMessage, context.AssistantMessage);

        var appearsToHallucinate = context.AssistantMessage.Contains(
            "as an ai", StringComparison.OrdinalIgnoreCase) == false &&
            context.AssistantMessage.Length > 0 &&
            !isRelevant;

        var result = new ChatTurnEvaluationResult(
            IsRelevant: isRelevant,
            IsGrounded: !appearsToHallucinate,
            AppearsToHallucinate: appearsToHallucinate,
            RefusalWasCorrect: refusalWasCorrect,
            Scores: new Dictionary<string, double>
            {
                ["relevance"] = isRelevant ? 1.0 : 0.0,
                ["refusal_correct"] = refusalWasCorrect ? 1.0 : 0.0,
                ["hallucination_risk"] = appearsToHallucinate ? 1.0 : 0.0,
            });

        _logger.LogInformation(
            "ChatEvaluation Session={SessionId} Relevant={Relevant} " +
            "RefusalCorrect={RefusalCorrect} HallucinationRisk={Hallucination}",
            context.SessionId,
            result.IsRelevant,
            result.RefusalWasCorrect,
            result.AppearsToHallucinate);

        return Task.FromResult(result);
    }

    private static bool LooksLikeRefusal(string text) =>
        text.Contains("can't help", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("cannot help", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("i'm not able to", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("i can't process", StringComparison.OrdinalIgnoreCase);

    private static bool HeuristicRelevance(string userMessage, string assistantMessage)
    {
        // Extremely simple keyword-overlap heuristic. Swap for an
        // embeddings-similarity or LLM-judge check for real scoring.
        if (string.IsNullOrWhiteSpace(assistantMessage))
        {
            return false;
        }

        var userWords = userMessage
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => w.Length > 3)
            .Select(w => w.ToLowerInvariant())
            .ToHashSet();

        if (userWords.Count == 0)
        {
            return true;
        }

        var assistantLower = assistantMessage.ToLowerInvariant();
        var overlap = userWords.Count(w => assistantLower.Contains(w));

        return overlap > 0 || assistantMessage.Length > 20;
    }
}