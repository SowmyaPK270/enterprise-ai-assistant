namespace EnterpriseAiAssistant.Application.Ingestion;

/// <summary>
/// A flattened, ingestion-friendly projection of a Job and everything
/// hanging off it (Operations, Runs, Personnel, Products,
/// QualityIncidents) as read from the SQL Server source of truth. Both
/// the Cosmos DB graph writer and the Azure AI Search job-evidence
/// writer are built from this single shape, so the two derived
/// representations can never drift from each other or from SQL Server.
/// </summary>
public sealed record JobRecord(
    string JobNumber,
    string JobType,
    string Status,
    string ClientName,
    string WellName,
    string FieldName,
    DateTimeOffset MobilizationDate,
    DateTimeOffset? CompletionDate,
    IReadOnlyList<OperationRecord> Operations,
    IReadOnlyList<ProductRecord> Products,
    IReadOnlyList<QualityIncidentRecord> QualityIncidents);

public sealed record OperationRecord(
    string Name,
    string Description,
    int SequenceNumber,
    IReadOnlyList<RunRecord> Runs);

public sealed record RunRecord(
    string Name,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string Result,
    IReadOnlyList<PersonnelRecord> Personnel);

public sealed record PersonnelRecord(
    string FullName,
    string Role);

public sealed record ProductRecord(
    string Name,
    double Quantity,
    string UnitOfMeasure);

public sealed record QualityIncidentRecord(
    string Severity,
    string Description,
    DateTimeOffset ReportedAt);

/// <summary>
/// A document (PDF/DOCX/etc.) submitted for ingestion into the Azure AI
/// Search document index. Content is the raw file bytes; extraction and
/// chunking happen downstream in the ingestion pipeline.
/// </summary>
public sealed record DocumentIngestionRequest(
    string FileName,
    string ContentType,
    byte[] Content);
