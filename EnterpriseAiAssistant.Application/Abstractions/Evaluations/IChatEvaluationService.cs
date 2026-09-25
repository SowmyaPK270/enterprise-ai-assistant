namespace EnterpriseAiAssistant.Application.Abstractions.Evaluation;

public sealed record ChatTurnEvaluationContext(
    Guid SessionId,
    Guid UserId,
    string UserMessage,
    string AssistantMessage,
    TimeSpan Latency,
    bool WasBlockedByGuardrail,
    string? GuardrailReason,
    IReadOnlyList<string>? CitedSources = null,
    decimal? EstimatedCostUsd = null);

public sealed record ChatTurnEvaluationResult(
    bool IsRelevant,
    bool IsGrounded,
    bool AppearsToHallucinate,
    bool RefusalWasCorrect,
    IReadOnlyDictionary<string, double> Scores);

/// <summary>
/// Records and evaluates each chat turn so quality/safety metrics can be
/// tracked over time. Kept independent from ChatService so evaluation
/// logic (and its cost) can evolve without touching the request path.
/// </summary>
public interface IChatEvaluationService
{
    /// <summary>
    /// Fire-and-record: always logs the turn (latency, blocked/not, etc.)
    /// for later analysis, regardless of whether deeper evaluation runs.
    /// </summary>
    Task RecordTurnAsync(
        ChatTurnEvaluationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs (optionally sampled) heuristic/LLM-based evaluation of a
    /// turn and returns scoring results.
    /// </summary>
    Task<ChatTurnEvaluationResult> EvaluateTurnAsync(
        ChatTurnEvaluationContext context,
        CancellationToken cancellationToken = default);
}