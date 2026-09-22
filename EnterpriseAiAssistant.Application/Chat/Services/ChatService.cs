using System.Runtime.CompilerServices;
using System.Text;
using EnterpriseAiAssistant.Application.Abstractions.AI;
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

    public ChatService(
        IAIClient aiClient,
        IConversationRepository repository)
    {
        _aiClient = aiClient;
        _repository = repository;
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

        // Persist the user's message first so history survives even if
        // the AI call or the stream is interrupted partway through.
        var userMessage = ChatMessage.Create(ChatRole.User, request.Message);

        session.AddMessage(userMessage);

        await _repository.AddMessageAsync(session.Id, userMessage, cancellationToken);

        if (session.Title == DefaultTitle)
        {
            var title = request.Message.Length > 30
                ? request.Message[..30] + "..."
                : request.Message;

            session.Rename(title);

            await _repository.RenameSessionAsync(session.Id, title, cancellationToken);
        }

        var aiRequest = new AIRequest(session.Messages.ToList());

        var buffer = new StringBuilder();

        try
        {
            await foreach (var chunk in _aiClient.StreamCompleteAsync(aiRequest, cancellationToken))
            {
                if (string.IsNullOrEmpty(chunk))
                    continue;

                buffer.Append(chunk);

                yield return chunk;
            }
        }
        finally
        {
            // Persist whatever the model produced, even a partial reply
            // if the caller cancelled or the connection dropped mid-stream.
            var fullContent = buffer.ToString();

            if (!string.IsNullOrWhiteSpace(fullContent))
            {
                var assistantMessage = ChatMessage.Create(ChatRole.Assistant, fullContent);

                session.AddMessage(assistantMessage);

                await _repository.AddMessageAsync(
                    session.Id,
                    assistantMessage,
                    CancellationToken.None);
            }
        }
    }
}
