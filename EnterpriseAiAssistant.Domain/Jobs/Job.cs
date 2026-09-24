namespace EnterpriseAiAssistant.Domain.Jobs;

/// <summary>
/// A field job performed on a Well for a client. This is the aggregate
/// root of the SQL Server JOB database, which is the single source of
/// truth for all structured job data. Both Cosmos DB (relationship/graph
/// representation) and Azure AI Search (job-evidence semantic index) are
/// derived, read-optimized projections built from this data during
/// ingestion; neither is ever written to directly.
/// </summary>
public sealed class Job
{
    public Guid Id { get; private set; }

    /// <summary>Human-readable job number, e.g. "JOB-2026-0001".</summary>
    public string JobNumber { get; private set; }

    public string JobType { get; private set; }

    public JobStatus Status { get; private set; }

    public Guid WellId { get; private set; }

    public Well? Well { get; private set; }

    public string ClientName { get; private set; }

    public DateTimeOffset MobilizationDate { get; private set; }

    public DateTimeOffset? CompletionDate { get; private set; }

    private Job()
    {
        JobNumber = string.Empty;
        JobType = string.Empty;
        ClientName = string.Empty;
    }

    public Job(
        Guid id,
        string jobNumber,
        string jobType,
        JobStatus status,
        Guid wellId,
        string clientName,
        DateTimeOffset mobilizationDate,
        DateTimeOffset? completionDate)
    {
        Id = id;
        JobNumber = jobNumber;
        JobType = jobType;
        Status = status;
        WellId = wellId;
        ClientName = clientName;
        MobilizationDate = mobilizationDate;
        CompletionDate = completionDate;
    }
}
