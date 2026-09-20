using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace EnterpriseAiAssistant.Infrastructure.AI.AzureOpenAI;

public sealed class AzureOpenAIClient
{
    private readonly ChatClient _chatClient;

    public AzureOpenAIClient(
        IOptions<AzureOpenAIOptions> options,
        ILogger<AzureOpenAIClient> logger,
        IHostEnvironment environment)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.Endpoint))
            throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured.");

        if (string.IsNullOrWhiteSpace(settings.DeploymentName))
            throw new InvalidOperationException("AzureOpenAI:DeploymentName is not configured.");

        var endpoint = settings.Endpoint.Trim().TrimEnd('/');

        const string openAiV1Suffix = "/openai/v1";
        if (endpoint.EndsWith(openAiV1Suffix, StringComparison.OrdinalIgnoreCase))
            endpoint = endpoint[..^openAiV1Suffix.Length];

        TokenCredential credential = environment.IsDevelopment()
            ? new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                TenantId = settings.TenantId,
                ExcludeVisualStudioCredential = false,
                ExcludeAzureCliCredential = false
            })
            : new ManagedIdentityCredential();

        logger.LogInformation("Using Azure credential: {CredentialType}", credential.GetType().Name);

        var azureClient = new Azure.AI.OpenAI.AzureOpenAIClient(
            new Uri(endpoint),
            credential);

        _chatClient = azureClient.GetChatClient(settings.DeploymentName);
    }

    public ChatClient ChatClient => _chatClient;
}