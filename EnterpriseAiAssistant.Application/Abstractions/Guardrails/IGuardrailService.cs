namespace EnterpriseAiAssistant.Application.Abstractions.Guardrails;

/// <summary>
/// Applies safety and data-privacy rules before a message is sent to the
/// model (input guardrails) and after the model responds (output
/// guardrails).
/// </summary>
public interface IGuardrailService
{
    /// <summary>
    /// Validates and masks the incoming user message before it is sent
    /// to the model or persisted. Returns the (possibly masked) message.
    /// Throws <see cref="GuardrailViolationException"/> if the request
    /// contains data that must be blocked outright (e.g. credentials).
    /// </summary>
    Task<string> ValidateInputAsync(
        string userMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and masks the model's output before it is persisted and
    /// shown to the user. Throws <see cref="GuardrailViolationException"/>
    /// if the content must be blocked outright.
    /// </summary>
    Task<string> ValidateOutputAsync(
        string assistantMessage,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Thrown when a guardrail determines a request/response must be
/// blocked entirely rather than masked.
/// </summary>
public sealed class GuardrailViolationException(string reason)
    : Exception(reason)
{
    public string Reason { get; } = reason;
}