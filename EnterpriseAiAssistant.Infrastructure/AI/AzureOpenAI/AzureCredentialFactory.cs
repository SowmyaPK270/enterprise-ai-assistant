using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnterpriseAiAssistant.Infrastructure.AI.AzureOpenAI;

/// <summary>
/// Builds the single <see cref="TokenCredential"/> used to authenticate
/// against every Azure resource in this application (Azure OpenAI,
/// Azure AI Search, Cosmos DB, Azure SQL). Centralized here so all
/// resources share the exact same "Managed Identity in the cloud,
/// DefaultAzureCredential locally" policy instead of re-implementing it
/// per client.
/// </summary>
public static class AzureCredentialFactory
{
    public static TokenCredential Create(
        IHostEnvironment environment,
        string? tenantId,
        ILogger logger)
    {
        TokenCredential credential = environment.IsDevelopment()
            ? new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                TenantId = tenantId,
                ExcludeVisualStudioCredential = false,
                ExcludeAzureCliCredential = false
            })
            : new ManagedIdentityCredential();

        logger.LogInformation(
            "Using Azure credential: {CredentialType}",
            credential.GetType().Name);

        return credential;
    }
}
