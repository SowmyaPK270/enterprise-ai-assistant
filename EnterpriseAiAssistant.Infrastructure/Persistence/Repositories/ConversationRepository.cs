using System.Linq;
using EnterpriseAiAssistant.Application.Chat.Interfaces;
using EnterpriseAiAssistant.Domain.Chat;
using EnterpriseAiAssistant.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAiAssistant.Infrastructure.Persistence.Repositories;

public sealed class ConversationRepository : IConversationRepository
{
    private readonly ConversationDbContext _dbContext;

    public ConversationRepository(ConversationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User> GetOrCreateUserAsync(
        string externalId,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == externalId, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var user = User.Create(externalId, displayName);

        _dbContext.Users.Add(user);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return user;
    }

    public async Task AddSessionAsync(
        ChatSession session,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Conversations.Add(session);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.Entry(session).State = EntityState.Detached;
    }

    public Task<ChatSession?> GetSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Conversations
            .AsNoTracking()
            .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
    }

    public async Task<IReadOnlyList<ChatSession>> GetSessionsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Conversations
            .AsNoTracking()
            .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddMessageAsync(
        Guid sessionId,
        ChatMessage message,
        CancellationToken cancellationToken = default)
    {
        _dbContext.ChatMessages.Add(message);

        _dbContext.Entry(message).Property("ConversationId").CurrentValue = sessionId;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _dbContext.Conversations
            .Where(s => s.Id == sessionId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(s => s.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken);
    }

    public async Task RenameSessionAsync(
        Guid sessionId,
        string title,
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.Conversations
            .Where(s => s.Id == sessionId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(s => s.Title, title)
                    .SetProperty(s => s.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken);

        if (rows == 0)
        {
            throw new InvalidOperationException(
                $"Conversation '{sessionId}' was not found.");
        }
    }
}