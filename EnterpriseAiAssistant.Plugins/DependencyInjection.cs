using EnterpriseAiAssistant.Application.Abstractions.AI;
using EnterpriseAiAssistant.Application.Abstractions.Rag;
using EnterpriseAiAssistant.Plugins.Filters;
using EnterpriseAiAssistant.Plugins.Planning;
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

        services.AddScoped<IQueryPlanner, LlmQueryPlanner>();
        services.AddScoped<ResultValidationFilter>();

        services.AddScoped<KernelFactory>();

        services.AddScoped<IAIClient, SemanticKernelAIClient>();

        return services;
    }
}
