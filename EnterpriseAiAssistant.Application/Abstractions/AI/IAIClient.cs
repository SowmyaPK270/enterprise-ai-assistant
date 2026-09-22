namespace EnterpriseAiAssistant.Application.Abstractions.AI;

public interface IAIClient
{
    /// <summary>
    /// Non-streaming completion. Kept for callers that need a single,
    /// fully-formed answer (e.g. background jobs, tests).
    /// </summary>
    Task<AIResponse> CompleteAsync(
        AIRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams the completion as it is generated, one text delta at a
    /// time, so the UI can render tokens as they arrive.
    /// </summary>
    IAsyncEnumerable<string> StreamCompleteAsync(
        AIRequest request,
        CancellationToken cancellationToken = default);
}
