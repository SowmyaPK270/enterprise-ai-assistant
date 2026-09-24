using System.Text;
using Azure.Search.Documents.Models;
using EnterpriseAiAssistant.Application.Abstractions.Embeddings;
using EnterpriseAiAssistant.Application.Ingestion;

namespace EnterpriseAiAssistant.Infrastructure.Search;

/// <summary>
/// Ingestion-time writer for the job-evidence. so semantically similar questions can retrieve
/// evidence even when they don't use the same words as the structured
/// SQL data. This is the "SQL data → chunking/extraction → embeddings"
/// path.
/// </summary>
public sealed class JobEvidenceIndexWriter : IJobEvidenceIndexWriter
{
    private readonly SearchClientFactory _clientFactory;
    private readonly IEmbeddingGenerator _embeddingGenerator;

    public JobEvidenceIndexWriter(
        SearchClientFactory clientFactory,
        IEmbeddingGenerator embeddingGenerator)
    {
        _clientFactory = clientFactory;
        _embeddingGenerator = embeddingGenerator;
    }

    public async Task IndexJobEvidenceAsync(
        JobRecord job,
        CancellationToken cancellationToken = default)
    {
        var passages = BuildEvidencePassages(job);
        var documents = new List<SearchDocument>(passages.Count);

        for (var i = 0; i < passages.Count; i++)
        {
            var vector = await _embeddingGenerator.GenerateAsync(passages[i], cancellationToken);

            documents.Add(new SearchDocument
            {
                ["Id"] = $"job-evidence-{job.JobNumber}-{i}",
                ["Content"] = passages[i],
                ["SourceType"] = JobKnowledgeSourceTypes.JobEvidence,
                ["JobNumber"] = job.JobNumber,
                ["DocumentName"] = null,
                ["CreatedAt"] = DateTimeOffset.UtcNow,
                ["ContentVector"] = vector.ToArray()
            });
        }

        if (documents.Count == 0)
        {
            return;
        }

        var searchClient = _clientFactory.GetSearchClient();
        var batch = IndexDocumentsBatch.MergeOrUpload(documents);

        await searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);
    }

    private static List<string> BuildEvidencePassages(JobRecord job)
    {
        var passages = new List<string>();

        var summary = new StringBuilder();
        summary.Append($"Job {job.JobNumber} is a {job.JobType} job for client {job.ClientName} ");
        summary.Append($"performed on well {job.WellName} in the {job.FieldName} field. ");
        summary.Append($"Status: {job.Status}. Mobilized on {job.MobilizationDate:yyyy-MM-dd}");
        summary.Append(job.CompletionDate is not null
            ? $" and completed on {job.CompletionDate:yyyy-MM-dd}."
            : " and not yet completed.");

        passages.Add(summary.ToString());

        foreach (var operation in job.Operations)
        {
            var opText = new StringBuilder();
            opText.Append($"During job {job.JobNumber}, the '{operation.Name}' operation ");
            opText.Append($"(step {operation.SequenceNumber}) covered: {operation.Description} ");

            foreach (var run in operation.Runs)
            {
                opText.Append($"{run.Name} started {run.StartedAt:yyyy-MM-dd} with result: {run.Result}. ");

                if (run.Personnel.Count > 0)
                {
                    var names = string.Join(", ", run.Personnel.Select(p => $"{p.FullName} ({p.Role})"));
                    opText.Append($"Personnel involved: {names}. ");
                }
            }

            passages.Add(opText.ToString());
        }

        if (job.QualityIncidents.Count > 0)
        {
            var incidentText = new StringBuilder();
            incidentText.Append($"Job {job.JobNumber} had the following quality/safety incidents: ");

            foreach (var incident in job.QualityIncidents)
            {
                incidentText.Append(
                    $"[{incident.Severity}, {incident.ReportedAt:yyyy-MM-dd}] {incident.Description} ");
            }

            passages.Add(incidentText.ToString());
        }

        if (job.Products.Count > 0)
        {
            var productText = string.Join(", ", job.Products.Select(p => $"{p.Quantity} {p.UnitOfMeasure} of {p.Name}"));
            passages.Add($"Job {job.JobNumber} consumed the following products: {productText}.");
        }

        return passages;
    }
}
