namespace EnterpriseAiAssistant.Domain.Jobs;

/// <summary>
/// A crew member associated with one or more Runs.
/// </summary>
public sealed class Personnel
{
    public Guid Id { get; private set; }

    public string FullName { get; private set; }

    public string Role { get; private set; }

    /// <summary>Ids of the Runs this person was assigned to.</summary>
    public Guid RunId { get; private set; }

    private Personnel()
    {
        FullName = string.Empty;
        Role = string.Empty;
    }

    public Personnel(
        Guid id,
        string fullName,
        string role,
        Guid runId)
    {
        Id = id;
        FullName = fullName;
        Role = role;
        RunId = runId;
    }
}
