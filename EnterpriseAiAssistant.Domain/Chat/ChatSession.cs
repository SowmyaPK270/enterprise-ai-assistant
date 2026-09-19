namespace EnterpriseAiAssistant.Domain.Chat;

public sealed class ChatSession
{
    private readonly List<ChatMessage> _messages = [];

    public Guid Id { get; private set; }

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
        string title,
        DateTimeOffset createdAt)
    {
        Id = id;
        Title = title;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public static ChatSession Create(
        string title = "New conversation")
    {
        var now = DateTimeOffset.UtcNow;

        return new ChatSession(
            Guid.NewGuid(),
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