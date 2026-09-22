using EnterpriseAiAssistant.Domain.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseAiAssistant.Infrastructure.Persistence.Configurations;

public sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("ChatMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        // Shadow FK added purely for persistence — the domain ChatMessage
        // entity itself does not need to know which conversation it
        // belongs to; ChatSession owns that relationship.
        builder.Property<Guid>("ConversationId")
            .IsRequired();

        builder.Property(m => m.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.Content)
            .IsRequired(); // no MaxLength -> maps to nvarchar(max)

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.HasIndex("ConversationId");
    }
}
