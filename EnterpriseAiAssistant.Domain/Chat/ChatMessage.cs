using System;
using System.Collections.Generic;
using System.Text;

namespace EnterpriseAiAssistant.Domain.Chat;

public sealed class ChatMessage
{
    public Guid Id { get; private set; }

    public ChatRole Role { get; private set; }

    public string Content { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private ChatMessage()
    {
        Content = string.Empty;
    }

    private ChatMessage(
        Guid id,
        ChatRole role,
        string content,
        DateTimeOffset createdAt)
    {
        Id = id;
        Role = role;
        Content = content;
        CreatedAt = createdAt;
    }

    public static ChatMessage Create(
        ChatRole role,
        string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException(
                "Message content cannot be empty.",
                nameof(content));
        }

        return new ChatMessage(
            Guid.NewGuid(),
            role,
            content.Trim(),
            DateTimeOffset.UtcNow);
    }
}