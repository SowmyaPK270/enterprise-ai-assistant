namespace EnterpriseAiAssistant.Domain.Jobs;

/// <summary>
/// A quality or safety incident recorded against a Job.
/// </summary>
public sealed class QualityIncident
{
    public Guid Id { get; private set; }

    public Guid JobId { get; private set; }

    public string Severity { get; private set; }

    public string Description { get; private set; }

    public DateTimeOffset ReportedAt { get; private set; }

    private QualityIncident()
    {
        Severity = string.Empty;
        Description = string.Empty;
    }

    public QualityIncident(
        Guid id,
        Guid jobId,
        string severity,
        string description,
        DateTimeOffset reportedAt)
    {
        Id = id;
        JobId = jobId;
        Severity = severity;
        Description = description;
        ReportedAt = reportedAt;
    }
}
