namespace EnterpriseAiAssistant.Application.Chat.Models;

/// <summary>
/// Represents the request coming from the UI. The application layer
/// reloads the full conversation history from the Conversation store
/// using SessionId, so the client only needs to send the new message
/// text (the persisted history is always the source of truth).
/// </summary>
public sealed record ChatRequest(
    Guid SessionId,
    Guid UserId,
    string Message);