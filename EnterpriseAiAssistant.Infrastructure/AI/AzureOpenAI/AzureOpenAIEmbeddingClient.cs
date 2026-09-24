using EnterpriseAiAssistant.Application.Abstractions.Embeddings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace EnterpriseAiAssistant.Infrastructure.AI.AzureOpenAI;

/// <summary>
/// Generates embeddings via the Azure OpenAI embedding deployment. Used
/// at ingestion time to embed job-evidence/document chunks, and at
/// query time (by AzureVectorSearchService) to embed the user's
/// question before it is compared against those pre-computed vectors.
/// </summary>
public sealed class AzureOpenAIEmbeddingClient : IEmbeddingGenerator
{
    private readonly EmbeddingClient _embeddingClient;

    public AzureOpenAIEmbeddingClient(
        IOptions<AzureOpenAIOptions> options,
        ILogger<AzureOpenAIEmbeddingClient> logger,
        IHostEnvironment environment)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.Endpoint))
            throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured.");

        if (string.IsNullOrWhiteSpace(settings.EmbeddingDeploymentName))
            throw new InvalidOperationException("AzureOpenAI:EmbeddingDeploymentName is not configured.");

        var endpoint = settings.Endpoint.Trim().TrimEnd('/');

        const string openAiV1Suffix = "/openai/v1";
        if (endpoint.EndsWith(openAiV1Suffix, StringComparison.OrdinalIgnoreCase))
            endpoint = endpoint[..^openAiV1Suffix.Length];

        var credential = AzureCredentialFactory.Create(
            environment, settings.TenantId, logger);

        var azureClient = new Azure.AI.OpenAI.AzureOpenAIClient(
            new Uri(endpoint),
            credential);

        _embeddingClient = azureClient.GetEmbeddingClient(
            settings.EmbeddingDeploymentName);
    }

    public async Task<ReadOnlyMemory<float>> GenerateAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text to embed cannot be empty.", nameof(text));
        }

        var result = await _embeddingClient.GenerateEmbeddingAsync(
            text,
            cancellationToken: cancellationToken);

        return result.Value.ToFloats();
    }
}
