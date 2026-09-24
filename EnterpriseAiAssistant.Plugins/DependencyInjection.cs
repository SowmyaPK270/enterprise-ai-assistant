using EnterpriseAiAssistant.Application.Abstractions.AI;
using EnterpriseAiAssistant.Plugins.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseAiAssistant.Plugins;

public static class DependencyInjection
{
    /// <summary>
    /// Registers Semantic Kernel orchestration and the three RAG
    /// plugins, and overrides IAIClient to route through them.
    /// </summary>
    public static IServiceCollection AddSemanticKernelPlugins(
        this IServiceCollection services)
    {
        services.AddScoped<MsSqlSearchPlugin>();
        services.AddScoped<CosmosGraphSearchPlugin>();
        services.AddScoped<AzureVectorSearchPlugin>();

        services.AddScoped<KernelFactory>();

        services.AddScoped<IAIClient, SemanticKernelAIClient>();

        return services;
    }
}
