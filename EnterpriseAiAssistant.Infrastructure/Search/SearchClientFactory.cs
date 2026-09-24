using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using EnterpriseAiAssistant.Infrastructure.AI.AzureOpenAI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseAiAssistant.Infrastructure.Search;

/// <summary>
/// Creates the <see cref="SearchIndexClient"/> (index management) and
/// <see cref="SearchClient"/> (document upload/query) for the shared
/// job-knowledge index, authenticated with the same Managed Identity /
/// DefaultAzureCredential policy used everywhere else in the app.
/// </summary>
public sealed class SearchClientFactory
{
    private readonly AzureAiSearchOptions _options;
    private readonly SearchIndexClient _indexClient;

    public SearchClientFactory(
        IOptions<AzureAiSearchOptions> options,
        ILogger<SearchClientFactory> logger,
        IHostEnvironment environment)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.Endpoint))
            throw new InvalidOperationException("AzureAiSearch:Endpoint is not configured.");

        var credential = AzureCredentialFactory.Create(environment, _options.TenantId, logger);

        _indexClient = new SearchIndexClient(new Uri(_options.Endpoint), credential);
    }

    public SearchIndexClient IndexClient => _indexClient;

    public SearchClient GetSearchClient() => _indexClient.GetSearchClient(_options.IndexName);

    public string IndexName => _options.IndexName;
}
