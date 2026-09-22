using System;

namespace EnterpriseAiAssistant.Domain.Users;

/// <summary>
/// Represents an authenticated principal who owns conversations in the
/// Conversation store. This is a first-class local row (with its own Id)
/// rather than the identity-provider's own identifier, so Conversations
/// and ChatMessages can have a stable foreign key that never changes even
/// if the identity provider or claim shape changes later.
/// </summary>
public sealed class User
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The identifier from the identity provider (e.g. the "oid" or
    /// "sub" claim from Entra ID) used to look up or create this row
    /// the first time a given person signs in.
    /// </summary>
    public string ExternalId { get; private set; }

    public string DisplayName { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private User()
    {
        ExternalId = string.Empty;
        DisplayName = string.Empty;
    }

    private User(
        Guid id,
        string externalId,
        string displayName,
        DateTimeOffset createdAt)
    {
        Id = id;
        ExternalId = externalId;
        DisplayName = displayName;
        CreatedAt = createdAt;
    }

    public static User Create(
        string externalId,
        string displayName)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new ArgumentException(
                "External id cannot be empty.",
                nameof(externalId));
        }

        return new User(
            Guid.NewGuid(),
            externalId.Trim(),
            string.IsNullOrWhiteSpace(displayName)
                ? "Unknown user"
                : displayName.Trim(),
            DateTimeOffset.UtcNow);
    }
}
