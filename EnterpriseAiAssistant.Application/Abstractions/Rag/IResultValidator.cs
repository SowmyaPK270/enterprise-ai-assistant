namespace EnterpriseAiAssistant.Application.Abstractions.Rag;

/// <summary>
/// The outcome of validating one piece of retrieved content (one
/// plugin/tool call's result) before it is allowed to flow back into
/// the model's context for final answer synthesis.
/// </summary>
public sealed record ResultValidationOutcome(
    /// <summary>
    /// False when the source returned no real data (an empty result,
    /// an explicit "not found" response, or an error) — such results
    /// are still shown to the model (so it knows the lookup was
    /// attempted) but are excluded from citations since nothing was
    /// actually retrieved.
    /// </summary>
    bool HasUsableData,

    /// <summary>
    /// The content the model should actually see — identical to the
    /// raw result unless truncation or prompt-injection neutralization
    /// was applied.
    /// </summary>
    string SanitizedContent,

    bool FlaggedPossiblePromptInjection,

    bool WasTruncated);

/// <summary>
/// Result Validator stage. Runs on every value returned by
/// MsSqlSearch/CosmosGraphSearch/AzureVectorSearch AFTER the tool
/// executes but BEFORE the result is added back into the conversation
/// the model uses to write its final answer — i.e. exactly the
/// "evaluate retrieved chunks/tool outputs for quality, format,
/// grounding, and injected instructions before synthesis" stage.
///
/// Deliberately implemented as fast, deterministic checks rather than
/// an extra LLM call per tool result: at the volume of tool calls a
/// single chat turn can generate, an LLM-based judge on every result
/// would multiply cost and latency for comparatively small benefit
/// over these checks. The heuristics below are still real quality
/// gates.
/// </summary>
public interface IResultValidator
{
    ResultValidationOutcome Validate(
        string sourceSystem,
        string rawResult,
        string originalQuestion);
}
