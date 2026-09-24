namespace EnterpriseAiAssistant.Domain.Ingestion;

/// <summary>
/// The origin of the data being ingested. JobSqlData produces both the
/// Cosmos DB graph representation and the Azure AI Search job-evidence
/// index; Document produces only the Azure AI Search document index.
/// </summary>
public enum IngestionSourceType
{
    JobSqlData,
    Document
}

public enum IngestionStatus
{
    Pending,
    Running,
    Succeeded,
    Failed
}

/// <summary>
/// Tracks a single ingestion attempt (one Job's sync, or one document's
/// processing) so ingestion progress/status can be observed and failed
/// items can be identified for retry, independent of the live chat path.
/// </summary>
public sealed class IngestionRun
{
    public Guid Id { get; private set; }

    public IngestionSourceType SourceType { get; private set; }

    /// <summary>Job number or file name being processed, for display.</summary>
    public string Target { get; private set; }

    public IngestionStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public string? ErrorMessage { get; private set; }

    private IngestionRun()
    {
        Target = string.Empty;
    }

    public static IngestionRun Start(IngestionSourceType sourceType, string target)
    {
        return new IngestionRun
        {
            Id = Guid.NewGuid(),
            SourceType = sourceType,
            Target = target,
            Status = IngestionStatus.Running,
            AttemptCount = 1,
            StartedAt = DateTimeOffset.UtcNow
        };
    }

    public void RetryAttempt()
    {
        AttemptCount++;
        Status = IngestionStatus.Running;
        ErrorMessage = null;
    }

    public void MarkSucceeded()
    {
        Status = IngestionStatus.Succeeded;
        CompletedAt = DateTimeOffset.UtcNow;
        ErrorMessage = null;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = IngestionStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
        ErrorMessage = errorMessage;
    }
}
