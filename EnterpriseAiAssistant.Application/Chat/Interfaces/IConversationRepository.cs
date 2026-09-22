using EnterpriseAiAssistant.Domain.Chat;
using EnterpriseAiAssistant.Domain.Users;

namespace EnterpriseAiAssistant.Application.Chat.Interfaces;

/// <summary>
/// Persistence boundary for conversation history, backed by the
/// dedicated Conversation SQL store. This store is intentionally kept
/// separate from any RAG/knowledge datastore (Azure AI Search, the SQL
/// Server JOB database, Cosmos DB) introduced in later phases.
/// </summary>
public interface IConversationRepository
{
    Task<User> GetOrCreateUserAsync(
        string externalId,
        string displayName,
        CancellationToken cancellationToken = default);

    Task AddSessionAsync(
        ChatSession session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a conversation together with its full, chronologically
    /// ordered message history.
    /// </summary>
    Task<ChatSession?> GetSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads all of a user's conversations (each with its messages),
    /// most recently updated first, for the sidebar.
    /// </summary>
    Task<IReadOnlyList<ChatSession>> GetSessionsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddMessageAsync(
        Guid sessionId,
        ChatMessage message,
        CancellationToken cancellationToken = default);

    Task RenameSessionAsync(
        Guid sessionId,
        string title,
        CancellationToken cancellationToken = default);
}
