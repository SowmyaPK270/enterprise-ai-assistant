namespace EnterpriseAiAssistant.Application.Abstractions.Cost;

/// <summary>One LLM call's token usage and its estimated dollar cost.</summary>
public sealed record LlmCallUsage(
    string CallSite,
    string ModelOrDeployment,
    int InputTokens,
    int OutputTokens,
    decimal EstimatedCostUsd);

/// <summary>The full picture of what a chat turn cost, across every LLM call it made.</summary>
public sealed record ExecutionCostSummary(
    int TotalInputTokens,
    int TotalOutputTokens,
    decimal TotalEstimatedCostUsd,
    IReadOnlyList<LlmCallUsage> Calls);

/// <summary>
/// Accumulates every LLM call's token usage for the current chat turn —
/// the query planner call and every chat-completion round trip Semantic
/// Kernel makes while auto-invoking tools — so a single "total execution
/// cost" can be reported once the turn completes. This is populated by
/// intercepting the usage metadata the LLM provider actually returns, 
/// not estimated from text length.
public interface ICostAccumulator
{
    void Reset();

    void Record(string callSite, string modelOrDeployment, int inputTokens, int outputTokens);

    ExecutionCostSummary GetSummary();
}
