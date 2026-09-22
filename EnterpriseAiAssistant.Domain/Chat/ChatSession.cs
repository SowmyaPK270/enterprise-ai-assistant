namespace EnterpriseAiAssistant.Domain.Chat;

public sealed class ChatSession
{
    private readonly List<ChatMessage> _messages = [];

    public Guid Id { get; private set; }

    /// <summary>
    /// The owning user's Id (EnterpriseAiAssistant.Domain.Users.User.Id).
    /// Kept as a plain Guid rather than a navigation reference so the
    /// Chat aggregate does not need to depend on the Users namespace.
    /// </summary>
    public Guid UserId { get; private set; }

    public string Title { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<ChatMessage> Messages =>
        _messages.AsReadOnly();

    private ChatSession()
    {
        Title = string.Empty;
    }

    private ChatSession(
        Guid id,
        Guid userId,
        string title,
        DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        Title = title;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public static ChatSession Create(
        Guid userId,
        string title = "New conversation")
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "A conversation must belong to a user.",
                nameof(userId));
        }

        var now = DateTimeOffset.UtcNow;

        return new ChatSession(
            Guid.NewGuid(),
            userId,
            title,
            now);
    }

    public void Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException(
                "Session title cannot be empty.",
                nameof(title));

        Title = title.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddMessage(ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        _messages.Add(message);

        UpdatedAt = DateTimeOffset.UtcNow;
    }
}