using EnterpriseAiAssistant.Domain.Chat;
using EnterpriseAiAssistant.Domain.Users;
using EnterpriseAiAssistant.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAiAssistant.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the dedicated Conversation SQL database.
/// This database holds chat/user/session data
/// (Users, Conversations, ChatMessages)
/// </summary>
public sealed class ConversationDbContext : DbContext
{
    public ConversationDbContext(DbContextOptions<ConversationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<ChatSession> Conversations => Set<ChatSession>();

    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new ChatSessionConfiguration());
        modelBuilder.ApplyConfiguration(new ChatMessageConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
