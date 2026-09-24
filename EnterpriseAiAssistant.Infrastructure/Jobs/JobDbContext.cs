using EnterpriseAiAssistant.Domain.Jobs;
using EnterpriseAiAssistant.Infrastructure.Jobs.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAiAssistant.Infrastructure.Jobs;

/// <summary>
/// EF Core context for the JOB database: the single source of truth for
/// all structured job data (Wells, Fields, Jobs, Operations, Runs,
/// Personnel, Products, QualityIncidents). This is a distinct database
/// from ConversationDbContext — conversation history is a separate
/// concern from the operational JOB data that feeds RAG. Both Cosmos DB
/// and Azure AI Search are ingested FROM this database; neither is ever
/// written to directly by the chat path.
///</summary>
public sealed class JobDbContext : DbContext
{
    public JobDbContext(DbContextOptions<JobDbContext> options)
        : base(options)
    {
    }

    public DbSet<Field> Fields => Set<Field>();

    public DbSet<Well> Wells => Set<Well>();

    public DbSet<Job> Jobs => Set<Job>();

    public DbSet<Operation> Operations => Set<Operation>();

    public DbSet<Run> Runs => Set<Run>();

    public DbSet<Personnel> Personnel => Set<Personnel>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<QualityIncident> QualityIncidents => Set<QualityIncident>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new FieldConfiguration());
        modelBuilder.ApplyConfiguration(new WellConfiguration());
        modelBuilder.ApplyConfiguration(new JobConfiguration());
        modelBuilder.ApplyConfiguration(new OperationConfiguration());
        modelBuilder.ApplyConfiguration(new RunConfiguration());
        modelBuilder.ApplyConfiguration(new PersonnelConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new QualityIncidentConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
