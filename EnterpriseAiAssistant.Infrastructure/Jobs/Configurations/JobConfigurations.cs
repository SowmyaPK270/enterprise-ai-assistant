using EnterpriseAiAssistant.Domain.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseAiAssistant.Infrastructure.Jobs.Configurations;

public sealed class FieldConfiguration : IEntityTypeConfiguration<Field>
{
    public void Configure(EntityTypeBuilder<Field> builder)
    {
        builder.ToTable("Fields");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.Name).HasMaxLength(200).IsRequired();
        builder.Property(f => f.Region).HasMaxLength(200).IsRequired();
    }
}

public sealed class WellConfiguration : IEntityTypeConfiguration<Well>
{
    public void Configure(EntityTypeBuilder<Well> builder)
    {
        builder.ToTable("Wells");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();

        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();

        builder.HasOne(w => w.Field)
            .WithMany()
            .HasForeignKey(w => w.FieldId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(w => w.FieldId);
    }
}

public sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("Jobs");

        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).ValueGeneratedNever();

        builder.Property(j => j.JobNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(j => j.JobNumber).IsUnique();

        builder.Property(j => j.JobType).HasMaxLength(100).IsRequired();
        builder.Property(j => j.ClientName).HasMaxLength(200).IsRequired();

        builder.Property(j => j.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(j => j.Well)
            .WithMany()
            .HasForeignKey(j => j.WellId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(j => j.WellId);
        builder.HasIndex(j => j.Status);
    }
}

public sealed class OperationConfiguration : IEntityTypeConfiguration<Operation>
{
    public void Configure(EntityTypeBuilder<Operation> builder)
    {
        builder.ToTable("Operations");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.Name).HasMaxLength(200).IsRequired();
        builder.Property(o => o.Description).HasMaxLength(1000);

        builder.HasIndex(o => o.JobId);
    }
}

public sealed class RunConfiguration : IEntityTypeConfiguration<Run>
{
    public void Configure(EntityTypeBuilder<Run> builder)
    {
        builder.ToTable("Runs");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Result).HasMaxLength(1000);

        builder.HasIndex(r => r.OperationId);
    }
}

public sealed class PersonnelConfiguration : IEntityTypeConfiguration<Personnel>
{
    public void Configure(EntityTypeBuilder<Personnel> builder)
    {
        builder.ToTable("Personnel");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.FullName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Role).HasMaxLength(100).IsRequired();

        builder.HasIndex(p => p.RunId);
    }
}

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.UnitOfMeasure).HasMaxLength(50).IsRequired();

        builder.HasIndex(p => p.JobId);
    }
}

public sealed class QualityIncidentConfiguration : IEntityTypeConfiguration<QualityIncident>
{
    public void Configure(EntityTypeBuilder<QualityIncident> builder)
    {
        builder.ToTable("QualityIncidents");

        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).ValueGeneratedNever();

        builder.Property(q => q.Severity).HasMaxLength(50).IsRequired();
        builder.Property(q => q.Description).HasMaxLength(1000).IsRequired();

        builder.HasIndex(q => q.JobId);
    }
}
