namespace EnterpriseAiAssistant.Application.Abstractions.Embeddings;

/// <summary>
/// Converts text into an embedding vector using the configured Azure
/// OpenAI embedding deployment. Used both at ingestion time (to embed
/// job-evidence and document chunks before they are written to Azure AI
/// Search) and at query time (to embed the user's question before it is
/// compared against those pre-computed vectors).
/// </summary>
public interface IEmbeddingGenerator
{
    Task<ReadOnlyMemory<float>> GenerateAsync(
        string text,
        CancellationToken cancellationToken = default);
}
