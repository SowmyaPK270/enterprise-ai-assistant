using System.Threading.Channels;

namespace EnterpriseAiAssistant.Ingestion;

/// <summary>
/// A job to ingest, enqueued by either the initial-sync startup task,
/// the JOB data sync path, or a document upload endpoint. Kept as a
/// small discriminated union (via the two static factories) rather than
/// a class hierarchy, since the queue only ever needs to know which
/// processor to hand the item to.
/// </summary>
public sealed class IngestionWorkItem
{
    public IngestionWorkItemKind Kind { get; }

    /// <summary>Job number, when Kind is JobSync.</summary>
    public string? JobNumber { get; }

    /// <summary>Uploaded file name, when Kind is DocumentSync.</summary>
    public string? DocumentFileName { get; }

    /// <summary>Uploaded file content, when Kind is DocumentSync.</summary>
    public byte[]? DocumentContent { get; }

    /// <summary>Uploaded file content type, when Kind is DocumentSync.</summary>
    public string? DocumentContentType { get; }

    private IngestionWorkItem(
        IngestionWorkItemKind kind,
        string? jobNumber,
        string? documentFileName,
        byte[]? documentContent,
        string? documentContentType)
    {
        Kind = kind;
        JobNumber = jobNumber;
        DocumentFileName = documentFileName;
        DocumentContent = documentContent;
        DocumentContentType = documentContentType;
    }

    public static IngestionWorkItem ForJob(string jobNumber) =>
        new(IngestionWorkItemKind.JobSync, jobNumber, null, null, null);

    public static IngestionWorkItem ForDocument(string fileName, string contentType, byte[] content) =>
        new(IngestionWorkItemKind.DocumentSync, null, fileName, content, contentType);
}

public enum IngestionWorkItemKind
{
    JobSync,
    DocumentSync
}

/// <summary>
/// Thin wrapper over an unbounded <see cref="Channel{T}"/> so ingestion
/// work can be enqueued from anywhere (startup sync, a future
/// SQL-change trigger, an upload endpoint) without blocking on the
/// background worker, and dequeued one item at a time by
/// BackgroundIngestionWorker. This decoupling is what keeps ingestion
/// off the live chat request path.
/// </summary>
public sealed class IngestionQueue
{
    private readonly Channel<IngestionWorkItem> _channel =
        Channel.CreateUnbounded<IngestionWorkItem>();

    public void Enqueue(IngestionWorkItem item) => _channel.Writer.TryWrite(item);

    public IAsyncEnumerable<IngestionWorkItem> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
