using Azure.AI.OpenAI;
using EnterpriseAiAssistant.Infrastructure.AI.AzureOpenAI;
using EnterpriseAiAssistant.Plugins.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace EnterpriseAiAssistant.Plugins;

/// <summary>
/// Builds a Semantic Kernel <see cref="Kernel"/> configured with the
/// Azure OpenAI chat completion connector and the three retrieval
/// plugins (MsSqlSearch, CosmosGraphSearch, AzureVectorSearch). A new
/// Kernel is built per chat request (it wraps already-constructed
/// singleton clients) so plugin instances can safely take scoped
/// dependencies such as JobDbContext.
/// </summary>
public sealed class KernelFactory
{
    private readonly AzureOpenAIOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IHostEnvironment _environment;
    private readonly MsSqlSearchPlugin _msSqlSearchPlugin;
    private readonly CosmosGraphSearchPlugin _cosmosGraphSearchPlugin;
    private readonly AzureVectorSearchPlugin _azureVectorSearchPlugin;

    public KernelFactory(
        IOptions<AzureOpenAIOptions> options,
        ILoggerFactory loggerFactory,
        IHostEnvironment environment,
        MsSqlSearchPlugin msSqlSearchPlugin,
        CosmosGraphSearchPlugin cosmosGraphSearchPlugin,
        AzureVectorSearchPlugin azureVectorSearchPlugin)
    {
        _options = options.Value;
        _loggerFactory = loggerFactory;
        _environment = environment;
        _msSqlSearchPlugin = msSqlSearchPlugin;
        _cosmosGraphSearchPlugin = cosmosGraphSearchPlugin;
        _azureVectorSearchPlugin = azureVectorSearchPlugin;
    }

    public Kernel Build()
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
            throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured.");

        if (string.IsNullOrWhiteSpace(_options.DeploymentName))
            throw new InvalidOperationException("AzureOpenAI:DeploymentName is not configured.");

        var endpoint = _options.Endpoint.Trim().TrimEnd('/');

        const string openAiV1Suffix = "/openai/v1";
        if (endpoint.EndsWith(openAiV1Suffix, StringComparison.OrdinalIgnoreCase))
            endpoint = endpoint[..^openAiV1Suffix.Length];

        var credential = AzureCredentialFactory.Create(
            _environment, _options.TenantId, _loggerFactory.CreateLogger<KernelFactory>());

        // Create Azure OpenAI client with managed identity credential
        var azureOpenAiClient = new Azure.AI.OpenAI.AzureOpenAIClient(new Uri(endpoint), credential);

        var builder = Kernel.CreateBuilder();

        builder.Services.AddSingleton(_loggerFactory);
        builder.Services.AddSingleton(azureOpenAiClient);

        builder.AddAzureOpenAIChatCompletion(_options.DeploymentName);

        var kernel = builder.Build();

        kernel.Plugins.AddFromObject(_msSqlSearchPlugin, "MsSqlSearch");
        kernel.Plugins.AddFromObject(_cosmosGraphSearchPlugin, "CosmosGraphSearch");
        kernel.Plugins.AddFromObject(_azureVectorSearchPlugin, "AzureVectorSearch");

        return kernel;
    }
}