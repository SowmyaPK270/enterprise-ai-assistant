using EnterpriseAiAssistant.Application.Abstractions.AI;
using EnterpriseAiAssistant.Application.Chat.Services;
using EnterpriseAiAssistant.Infrastructure.AI.AzureOpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseAiAssistant.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Azure OpenAI configuration
        services.Configure<AzureOpenAIOptions>(
            configuration.GetSection(
                AzureOpenAIOptions.SectionName));

        // Azure OpenAI
        services.AddSingleton<AzureOpenAIClient>();

        services.AddScoped<IAIClient, AzureOpenAIService>();

        return services;
    }
}