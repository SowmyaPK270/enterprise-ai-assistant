using EnterpriseAiAssistant.Application.Abstractions.AI;
using EnterpriseAiAssistant.Application.Chat.Interfaces;
using EnterpriseAiAssistant.Infrastructure.AI.AzureOpenAI;
using EnterpriseAiAssistant.Infrastructure.Persistence;
using EnterpriseAiAssistant.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
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

        var conversationConnectionString =
            configuration.GetConnectionString("ConversationDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'ConversationDatabase' is not configured.");

        services.AddDbContext<ConversationDbContext>(options =>
            options.UseSqlServer(
                conversationConnectionString,
                sql => sql.MigrationsAssembly(
                    typeof(ConversationDbContext).Assembly.FullName)));

        services.AddScoped<IConversationRepository, ConversationRepository>();

        return services;
    }
}