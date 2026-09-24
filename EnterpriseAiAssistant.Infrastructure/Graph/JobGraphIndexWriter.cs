using EnterpriseAiAssistant.Application.Ingestion;
using Microsoft.Azure.Cosmos;

namespace EnterpriseAiAssistant.Infrastructure.Graph;

/// <summary>
/// Transforms a JobRecord into the Job → Operation → Run → Personnel
/// (plus Product/QualityIncident) relationship graph and upserts it
/// into Cosmos DB. Every node document is fully rebuilt from the
/// current JobRecord on every sync, so re-running ingestion for a job
/// can never leave stale edges behind. No embeddings are used.
/// </summary>
public sealed class JobGraphIndexWriter : IJobGraphIndexWriter
{
    private readonly CosmosClientFactory _clientFactory;

    public JobGraphIndexWriter(CosmosClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task SyncJobGraphAsync(
        JobRecord job,
        CancellationToken cancellationToken = default)
    {
        var container = await _clientFactory.GetContainerAsync(cancellationToken);
        var nodes = BuildGraph(job);

        foreach (var node in nodes)
        {
            await container.UpsertItemAsync(
                node,
                new PartitionKey(node.PartitionKey),
                cancellationToken: cancellationToken);
        }
    }

    private static List<GraphNodeDocument> BuildGraph(JobRecord job)
    {
        var nodes = new List<GraphNodeDocument>();

        var jobNode = NewNode(GraphNodeTypes.Job, job.JobNumber, new Dictionary<string, string>
        {
            ["jobType"] = job.JobType,
            ["jobStatus"] = job.Status,
            ["clientName"] = job.ClientName,
            ["wellName"] = job.WellName,
            ["fieldName"] = job.FieldName,
            ["mobilizationDate"] = job.MobilizationDate.ToString("O"),
            ["completionDate"] = job.CompletionDate?.ToString("O") ?? string.Empty
        });

        foreach (var operation in job.Operations)
        {
            var operationBusinessId = $"{job.JobNumber}::{operation.SequenceNumber}::{operation.Name}";

            jobNode.Edges.Add(new GraphEdge
            {
                RelationshipType = GraphRelationshipTypes.HasOperation,
                TargetNodeType = GraphNodeTypes.Operation,
                TargetBusinessId = operationBusinessId
            });

            var operationNode = NewNode(GraphNodeTypes.Operation, operationBusinessId, new Dictionary<string, string>
            {
                ["name"] = operation.Name,
                ["description"] = operation.Description,
                ["sequenceNumber"] = operation.SequenceNumber.ToString(),
                ["jobNumber"] = job.JobNumber
            });

            foreach (var run in operation.Runs)
            {
                var runBusinessId = $"{operationBusinessId}::{run.Name}";

                operationNode.Edges.Add(new GraphEdge
                {
                    RelationshipType = GraphRelationshipTypes.HasRun,
                    TargetNodeType = GraphNodeTypes.Run,
                    TargetBusinessId = runBusinessId
                });

                var runNode = NewNode(GraphNodeTypes.Run, runBusinessId, new Dictionary<string, string>
                {
                    ["name"] = run.Name,
                    ["operationName"] = operation.Name,
                    ["jobNumber"] = job.JobNumber,
                    ["startedAt"] = run.StartedAt.ToString("O"),
                    ["completedAt"] = run.CompletedAt?.ToString("O") ?? string.Empty,
                    ["result"] = run.Result
                });

                foreach (var person in run.Personnel)
                {
                    var personnelBusinessId = $"{runBusinessId}::{person.FullName}";

                    runNode.Edges.Add(new GraphEdge
                    {
                        RelationshipType = GraphRelationshipTypes.HasPersonnel,
                        TargetNodeType = GraphNodeTypes.Personnel,
                        TargetBusinessId = personnelBusinessId
                    });

                    nodes.Add(NewNode(GraphNodeTypes.Personnel, personnelBusinessId, new Dictionary<string, string>
                    {
                        ["fullName"] = person.FullName,
                        ["role"] = person.Role,
                        ["runName"] = run.Name,
                        ["jobNumber"] = job.JobNumber
                    }));
                }

                nodes.Add(runNode);
            }

            nodes.Add(operationNode);
        }

        foreach (var product in job.Products)
        {
            var productBusinessId = $"{job.JobNumber}::{product.Name}";

            jobNode.Edges.Add(new GraphEdge
            {
                RelationshipType = GraphRelationshipTypes.HasProduct,
                TargetNodeType = GraphNodeTypes.Product,
                TargetBusinessId = productBusinessId
            });

            nodes.Add(NewNode(GraphNodeTypes.Product, productBusinessId, new Dictionary<string, string>
            {
                ["name"] = product.Name,
                ["quantity"] = product.Quantity.ToString("G"),
                ["unitOfMeasure"] = product.UnitOfMeasure,
                ["jobNumber"] = job.JobNumber
            }));
        }

        foreach (var incident in job.QualityIncidents)
        {
            var incidentBusinessId = $"{job.JobNumber}::{incident.ReportedAt:O}";

            jobNode.Edges.Add(new GraphEdge
            {
                RelationshipType = GraphRelationshipTypes.HasQualityIncident,
                TargetNodeType = GraphNodeTypes.QualityIncident,
                TargetBusinessId = incidentBusinessId
            });

            nodes.Add(NewNode(GraphNodeTypes.QualityIncident, incidentBusinessId, new Dictionary<string, string>
            {
                ["severity"] = incident.Severity,
                ["description"] = incident.Description,
                ["reportedAt"] = incident.ReportedAt.ToString("O"),
                ["jobNumber"] = job.JobNumber
            }));
        }

        nodes.Add(jobNode);

        return nodes;
    }

    private static GraphNodeDocument NewNode(
        string nodeType,
        string businessId,
        Dictionary<string, string> properties) => new()
    {
        Id = GraphNodeDocument.BuildId(nodeType, businessId),
        PartitionKey = nodeType,
        NodeType = nodeType,
        BusinessId = businessId,
        Properties = properties,
        Edges = []
    };
}
