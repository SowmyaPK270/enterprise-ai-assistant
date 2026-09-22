using EnterpriseAiAssistant.Application.Chat.Models;
using EnterpriseAiAssistant.Domain.Chat;
using EnterpriseAiAssistant.Domain.Users;

namespace EnterpriseAiAssistant.Application.Chat.Interfaces;

public interface IChatService
{
    /// <summary>
    /// Looks up the local User row for the signed-in identity, creating
    /// it on first sign-in. Called once when the chat page loads.
    /// </summary>
    Task<User> EnsureUserAsync(
        string externalId,
        string displayName,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatSession>> GetSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ChatSession> CreateSessionAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message and streams the assistant's reply back one chunk
    /// at a time. Both the user's message and the assistant's full reply
    /// are persisted to the Conversation store as part of this call.
    /// </summary>
    IAsyncEnumerable<string> StreamMessageAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default);
}