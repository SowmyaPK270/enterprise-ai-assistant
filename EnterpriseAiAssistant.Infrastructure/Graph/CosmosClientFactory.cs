using Microsoft.Azure.Cosmos;
using EnterpriseAiAssistant.Infrastructure.AI.AzureOpenAI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseAiAssistant.Infrastructure.Graph;

/// <summary>
/// Builds the singleton <see cref="CosmosClient"/> used for the job
/// relationship graph, authenticated with the same Managed Identity /
/// DefaultAzureCredential policy used everywhere else in the app, and
/// exposes a helper to ensure the database/container exist. Uses the
/// Cosmos DB Core (SQL) API rather than the Gremlin API — see the
/// Phase 2 README for why.
/// </summary>
public sealed class CosmosClientFactory : IAsyncDisposable
{
    private readonly CosmosGraphOptions _options;
    private readonly CosmosClient _client;

    public CosmosClientFactory(
      IOptions<CosmosGraphOptions> options,
      ILogger<CosmosClientFactory> logger,
      IHostEnvironment environment)
    {
        _options = options.Value;

        var endpoint = _options.Endpoint?.Trim();

        if (string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException("CosmosDb:Endpoint is not configured.");

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out _))
            throw new InvalidOperationException($"CosmosDb:Endpoint is invalid: '{_options.Endpoint}'.");

        var credential = AzureCredentialFactory.Create(environment, _options.TenantId, logger);

        _client = new CosmosClient(
            endpoint,
            credential,
            new CosmosClientOptions { ApplicationName = "EnterpriseAiAssistant" });
    }

    public async Task<Container> GetContainerAsync(CancellationToken cancellationToken = default)
    {
        var database = await _client.CreateDatabaseIfNotExistsAsync(
            _options.DatabaseName, cancellationToken: cancellationToken);

        var containerResponse = await database.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(_options.ContainerName, "/partitionKey"),
            cancellationToken: cancellationToken);

        return containerResponse.Container;
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }
}
