using EnterpriseAiAssistant.Application.Abstractions.AI;
using EnterpriseAiAssistant.Application.Abstractions.Cost;
using EnterpriseAiAssistant.Application.Abstractions.Embeddings;
using EnterpriseAiAssistant.Application.Abstractions.Rag;
using EnterpriseAiAssistant.Application.Chat.Interfaces;
using EnterpriseAiAssistant.Application.Ingestion;
using EnterpriseAiAssistant.Infrastructure.AI.AzureOpenAI;
using EnterpriseAiAssistant.Infrastructure.Cost;
using EnterpriseAiAssistant.Infrastructure.Documents;
using EnterpriseAiAssistant.Infrastructure.Graph;
using EnterpriseAiAssistant.Infrastructure.Ingestion;
using EnterpriseAiAssistant.Infrastructure.Jobs;
using EnterpriseAiAssistant.Infrastructure.Persistence;
using EnterpriseAiAssistant.Infrastructure.Persistence.Repositories;
using EnterpriseAiAssistant.Infrastructure.Search;
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
        AddAzureOpenAI(services, configuration);
        AddConversationStore(services, configuration);
        AddJobSqlStore(services, configuration);
        AddAzureAiSearch(services, configuration);
        AddCosmosGraphStore(services, configuration);
        AddDocumentExtraction(services);
        AddCostTracking(services, configuration);

        services.AddSingleton<IIngestionStatusStore, InMemoryIngestionStatusStore>();

        return services;
    }

    private static void AddAzureOpenAI(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AzureOpenAIOptions>(
            configuration.GetSection(AzureOpenAIOptions.SectionName));

        services.AddSingleton<AzureOpenAIClient>();
        services.AddSingleton<AzureOpenAIEmbeddingClient>();

        // Default IAIClient: direct non-orchestrated Azure OpenAI chat
        // completion. The Plugins project registers
        // SemanticKernelAIClient afterwards, which — because the last
        // registration wins in Microsoft.Extensions.DependencyInjection —
        // becomes the effective IAIClient once AddSemanticKernelPlugins()
        // is called in Program.cs. This keeps the Web app functional even
        // if the Plugins project is not wired up.
        services.AddScoped<IAIClient, AzureOpenAIService>();
        services.AddSingleton<IEmbeddingGenerator, AzureOpenAIEmbeddingClient>();
    }

    private static void AddConversationStore(IServiceCollection services, IConfiguration configuration)
    {
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
    }

    /// <summary>
    /// The JOB SQL Server database: the single source of truth for
    /// structured job data. Intentionally a separate DbContext/database
    /// from ConversationDbContext (see requirements: conversation
    /// storage is a separate concern from RAG). The two can point at
    /// different databases on the same Azure SQL logical server.
    /// </summary>
    private static void AddJobSqlStore(IServiceCollection services, IConfiguration configuration)
    {
        var jobConnectionString =
            configuration.GetConnectionString("JobDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'JobDatabase' is not configured.");

        services.AddDbContext<JobDbContext>(options =>
            options.UseSqlServer(jobConnectionString));

        services.AddScoped<IJobSqlSearchService, JobSqlSearchService>();
        services.AddScoped<IJobDataSourceReader, JobDataSourceReader>();
    }

    private static void AddAzureAiSearch(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AzureAiSearchOptions>(
            configuration.GetSection(AzureAiSearchOptions.SectionName));

        services.AddSingleton<SearchClientFactory>();
        services.AddSingleton<JobKnowledgeIndexInitializer>();

        services.AddScoped<IAzureVectorSearchService, AzureVectorSearchService>();
        services.AddScoped<IJobEvidenceIndexWriter, JobEvidenceIndexWriter>();
        services.AddScoped<IDocumentIndexWriter, DocumentIndexWriter>();
    }

    private static void AddCosmosGraphStore(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CosmosGraphOptions>(
            configuration.GetSection(CosmosGraphOptions.SectionName));

        services.AddSingleton<CosmosClientFactory>();
        services.AddScoped<ICosmosGraphSearchService, CosmosGraphSearchService>();
        services.AddScoped<IJobGraphIndexWriter, JobGraphIndexWriter>();
    }

    private static void AddDocumentExtraction(IServiceCollection services)
    {
        // Order matters only in that the first extractor whose
        // CanHandle() matches is used by the ingestion pipeline.
        services.AddSingleton<IDocumentTextExtractor, PlainTextDocumentExtractor>();
        services.AddSingleton<IDocumentTextExtractor, DocxDocumentExtractor>();
        services.AddSingleton<IDocumentTextExtractor, PdfDocumentExtractor>();
    }

    /// <summary>
    /// Cost tracking: ITokenPricingProvider turns the raw token
    /// counts SemanticKernelAIClient/LlmQueryPlanner record into an
    /// estimated $ cost. 
    /// </summary>
    private static void AddCostTracking(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TokenPricingOptions>(
            configuration.GetSection(TokenPricingOptions.SectionName));

        services.AddSingleton<ITokenPricingProvider, TokenPricingProvider>();
    }
}
