using Azure.Identity;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

//Creates and holds the actual Azure OpenAI SDK client.
//AzureOpenAIClient class is a wrapper that creates the real client object provided by Microsoft's Azure OpenAI SDK and keeps it available for your application to use.
//Create the Microsoft SDK client, configure authentication/settings, get the appropriate deployment's ChatClient, and hold onto it.
namespace EnterpriseAiAssistant.Infrastructure.AI.AzureOpenAI;

public sealed class AzureOpenAIClient
{
    private readonly ChatClient _chatClient;

    public AzureOpenAIClient(IOptions<AzureOpenAIOptions> options)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.Endpoint))
        {
            throw new InvalidOperationException(
                "AzureOpenAI:Endpoint is not configured.");
        }

        if (string.IsNullOrWhiteSpace(settings.DeploymentName))
        {
            throw new InvalidOperationException(
                "AzureOpenAI:DeploymentName is not configured.");
        }

        if (string.IsNullOrWhiteSpace(settings.TenantId))
        {
            throw new InvalidOperationException(
                "AzureOpenAI:TenantId is not configured.");
        }

        var endpoint = settings.Endpoint.Trim().TrimEnd('/');

        const string openAiV1Suffix = "/openai/v1";
        if (endpoint.EndsWith(openAiV1Suffix, StringComparison.OrdinalIgnoreCase))
        {
            endpoint = endpoint[..^openAiV1Suffix.Length];
        }

        var credential = new DefaultAzureCredential(
            new DefaultAzureCredentialOptions
            {
                TenantId = settings.TenantId,
                ExcludeVisualStudioCredential = false,
                ExcludeAzureCliCredential = false
            });

        var azureClient = new Azure.AI.OpenAI.AzureOpenAIClient(  
            new Uri(endpoint),
            credential);

        _chatClient = azureClient.GetChatClient(settings.DeploymentName);  
    } 

    public ChatClient ChatClient => _chatClient;
}