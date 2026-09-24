namespace EnterpriseAiAssistant.Domain.Jobs;

/// <summary>
/// Lifecycle status of a Job in the SQL Server source-of-truth database.
/// </summary>
public enum JobStatus
{
    Scheduled,
    Mobilized,
    InProgress,
    Completed,
    Cancelled
}
