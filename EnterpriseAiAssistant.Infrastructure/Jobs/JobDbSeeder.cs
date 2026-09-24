using EnterpriseAiAssistant.Domain.Jobs;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAiAssistant.Infrastructure.Jobs;

/// <summary>
/// Seeds a small, deterministic set of prototype job records — per the
/// requirements, ~10 records are sufficient to exercise the full
/// ingestion → embedding → retrieval → Semantic Kernel → answer flow.
/// Runs once at startup and is a no-op once any Job already exists.
/// </summary>
public static class JobDbSeeder
{
    public static async Task SeedAsync(
        JobDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.Jobs.AnyAsync(cancellationToken))
        {
            return;
        }

        var permianField = new Field(Guid.NewGuid(), "Permian Basin", "West Texas");
        var baccaField = new Field(Guid.NewGuid(), "Bakken", "North Dakota");

        dbContext.Fields.AddRange(permianField, baccaField);

        var wells = new[]
        {
            new Well(Guid.NewGuid(), "PB-101", permianField.Id, 9800),
            new Well(Guid.NewGuid(), "PB-102", permianField.Id, 10250),
            new Well(Guid.NewGuid(), "PB-103", permianField.Id, 9650),
            new Well(Guid.NewGuid(), "BK-201", baccaField.Id, 11020),
            new Well(Guid.NewGuid(), "BK-202", baccaField.Id, 10740),
        };

        dbContext.Wells.AddRange(wells);

        var jobTypes = new[] { "Perforation", "Stimulation", "Workover", "Completion", "Wireline Logging" };
        var clients = new[] { "Apex Energy", "Northstar Resources", "Summit Oil & Gas", "Meridian Petroleum" };
        var random = new Random(42); // fixed seed: deterministic prototype data

        var jobs = new List<Job>();
        var operations = new List<Operation>();
        var runs = new List<Run>();
        var personnel = new List<Personnel>();
        var products = new List<Product>();
        var incidents = new List<QualityIncident>();

        for (var i = 1; i <= 10; i++)
        {
            var job = new Job(
                id: Guid.NewGuid(),
                jobNumber: $"JOB-2026-{i:D4}",
                jobType: jobTypes[i % jobTypes.Length],
                status: i <= 7 ? JobStatus.Completed : JobStatus.InProgress,
                wellId: wells[i % wells.Length].Id,
                clientName: clients[i % clients.Length],
                mobilizationDate: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(i * 5),
                completionDate: i <= 7
                    ? new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(i * 5 + 3)
                    : null);

            jobs.Add(job);

            var operationNames = new[] { "Rig-Up", "Primary Operation", "Flowback" };

            for (var opIndex = 0; opIndex < operationNames.Length; opIndex++)
            {
                var operation = new Operation(
                    id: Guid.NewGuid(),
                    jobId: job.Id,
                    name: operationNames[opIndex],
                    description: $"{operationNames[opIndex]} phase for {job.JobNumber}.",
                    sequenceNumber: opIndex + 1);

                operations.Add(operation);

                var run = new Run(
                    id: Guid.NewGuid(),
                    operationId: operation.Id,
                    name: "Run 1",
                    startedAt: job.MobilizationDate.AddHours(opIndex * 6),
                    completedAt: job.CompletionDate?.AddHours(opIndex * 6 + 4),
                    result: (i == 4 && opIndex == 1) || (i == 8 && opIndex == 2)
                        ? "Failure"
                        : "Success"
                    );

                runs.Add(run);

                personnel.Add(new Personnel(Guid.NewGuid(), $"Crew Lead {i}-{opIndex}", "Field Engineer", run.Id));
                personnel.Add(new Personnel(Guid.NewGuid(), $"Operator {i}-{opIndex}", "Equipment Operator", run.Id));
            }

            products.Add(new Product(Guid.NewGuid(), job.Id, "Proppant - 100 Mesh", 40000 + random.Next(0, 5000), "lb"));
            products.Add(new Product(Guid.NewGuid(), job.Id, "Friction Reducer", 500 + random.Next(0, 100), "gal"));

            if (i % 4 == 0)
            {
                incidents.Add(new QualityIncident(
                    Guid.NewGuid(),
                    job.Id,
                    "Minor",
                    $"Pressure deviation observed during Primary Operation on {job.JobNumber}; resolved on site.",
                    job.MobilizationDate.AddDays(1)));
            }
        }

        dbContext.Jobs.AddRange(jobs);
        dbContext.Operations.AddRange(operations);
        dbContext.Runs.AddRange(runs);
        dbContext.Personnel.AddRange(personnel);
        dbContext.Products.AddRange(products);
        dbContext.QualityIncidents.AddRange(incidents);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
