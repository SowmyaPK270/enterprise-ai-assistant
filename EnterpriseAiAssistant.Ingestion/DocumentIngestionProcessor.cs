using EnterpriseAiAssistant.Application.Ingestion;
using EnterpriseAiAssistant.Domain.Ingestion;
using Microsoft.Extensions.Logging;

namespace EnterpriseAiAssistant.Ingestion;

/// <summary>
/// Extracts text from an uploaded PDF/DOCX/plain-text document and
/// writes it into the Azure AI Search document index. The extractor
/// used is chosen from every registered IDocumentTextExtractor by
/// CanHandle(), so adding support for a new file type only means
/// registering another extractor — this processor never changes.
/// </summary>
public sealed class DocumentIngestionProcessor
{
    private readonly IEnumerable<IDocumentTextExtractor> _extractors;
    private readonly IDocumentIndexWriter _documentIndexWriter;
    private readonly IIngestionStatusStore _statusStore;
    private readonly ILogger<DocumentIngestionProcessor> _logger;

    public DocumentIngestionProcessor(
        IEnumerable<IDocumentTextExtractor> extractors,
        IDocumentIndexWriter documentIndexWriter,
        IIngestionStatusStore statusStore,
        ILogger<DocumentIngestionProcessor> logger)
    {
        _extractors = extractors;
        _documentIndexWriter = documentIndexWriter;
        _statusStore = statusStore;
        _logger = logger;
    }

    public async Task ProcessAsync(
        string fileName,
        string contentType,
        byte[] content,
        CancellationToken cancellationToken)
    {
        var run = await _statusStore.StartAsync(IngestionSourceType.Document, fileName, cancellationToken);

        try
        {
            var extractor = _extractors.FirstOrDefault(e => e.CanHandle(fileName, contentType));

            if (extractor is null)
            {
                throw new NotSupportedException(
                    $"No document text extractor is registered for '{fileName}' ({contentType}).");
            }

            string extractedText;

            using (var stream = new MemoryStream(content))
            {
                extractedText = await extractor.ExtractTextAsync(stream, cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                throw new InvalidOperationException($"No text could be extracted from '{fileName}'.");
            }

            await _documentIndexWriter.IndexDocumentAsync(fileName, extractedText, cancellationToken);

            await _statusStore.MarkSucceededAsync(run.Id, cancellationToken);

            _logger.LogInformation("Ingested document {FileName} into Azure AI Search.", fileName);
        }
        catch (Exception ex)
        {
            await _statusStore.MarkFailedAsync(run.Id, ex.Message, cancellationToken);

            _logger.LogError(ex, "Failed to ingest document {FileName}.", fileName);

            throw;
        }
    }
}
