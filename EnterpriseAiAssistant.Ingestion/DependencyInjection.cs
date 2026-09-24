using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EnterpriseAiAssistant.Ingestion;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the ingestion queue, its processors, and the two
    /// hosted services (the continuous background worker and the
    /// one-shot initial sync). Depends on AddInfrastructure() having
    /// already registered the reader/writer/status-store ports.
    /// </summary>
    public static IServiceCollection AddIngestion(this IServiceCollection services)
    {
        services.AddSingleton<IngestionQueue>();

        services.AddScoped<JobDataIngestionProcessor>();
        services.AddScoped<DocumentIngestionProcessor>();

        services.AddHostedService<BackgroundIngestionWorker>();
        services.AddHostedService<InitialSyncHostedService>();

        return services;
    }
}
