using Azure.Search.Documents.Models;
using EnterpriseAiAssistant.Application.Abstractions.Embeddings;
using EnterpriseAiAssistant.Application.Ingestion;

namespace EnterpriseAiAssistant.Infrastructure.Search;

/// <summary>
/// Ingestion-time writer for the document half of the shared Azure AI
/// Search index. Uploaded PDFs/DOCX files are chunked and embedded
/// directly — unlike job-evidence, there is no SQL Server data behind
/// them, so their text is indexed close to as-extracted.
/// </summary>
public sealed class DocumentIndexWriter : IDocumentIndexWriter
{
    private readonly SearchClientFactory _clientFactory;
    private readonly IEmbeddingGenerator _embeddingGenerator;

    public DocumentIndexWriter(
        SearchClientFactory clientFactory,
        IEmbeddingGenerator embeddingGenerator)
    {
        _clientFactory = clientFactory;
        _embeddingGenerator = embeddingGenerator;
    }

    public async Task IndexDocumentAsync(
        string fileName,
        string extractedText,
        CancellationToken cancellationToken = default)
    {
        var chunks = TextChunker.Chunk(extractedText);

        if (chunks.Count == 0)
        {
            return;
        }

        var documents = new List<SearchDocument>(chunks.Count);

        for (var i = 0; i < chunks.Count; i++)
        {
            var vector = await _embeddingGenerator.GenerateAsync(chunks[i], cancellationToken);

            documents.Add(new SearchDocument
            {
                ["Id"] = $"document-{SanitizeId(fileName)}-{i}",
                ["Content"] = chunks[i],
                ["SourceType"] = JobKnowledgeSourceTypes.Document,
                ["JobNumber"] = null,
                ["DocumentName"] = fileName,
                ["CreatedAt"] = DateTimeOffset.UtcNow,
                ["ContentVector"] = vector.ToArray()
            });
        }

        var searchClient = _clientFactory.GetSearchClient();
        var batch = IndexDocumentsBatch.MergeOrUpload(documents);

        await searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);
    }

    /// <summary>Azure AI Search document keys allow only a restricted character set.</summary>
    private static string SanitizeId(string fileName)
    {
        var chars = fileName.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        return new string(chars);
    }
}
