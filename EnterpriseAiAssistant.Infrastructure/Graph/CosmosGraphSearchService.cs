using System.Net;
using System.Text;
using EnterpriseAiAssistant.Application.Abstractions.Rag;
using Microsoft.Azure.Cosmos;

namespace EnterpriseAiAssistant.Infrastructure.Graph;

/// <summary>
/// Backs the "CosmosGraphSearch" Semantic Kernel plugin. Every method
/// is an exact node/edge/attribute traversal against the Cosmos DB
/// adjacency-list graph built by JobGraphIndexWriter — no embeddings,
/// no AI-based similarity.
/// </summary>
public sealed class CosmosGraphSearchService : ICosmosGraphSearchService
{
    private readonly CosmosClientFactory _clientFactory;

    public CosmosGraphSearchService(CosmosClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task<string> GetConnectionsAsync(
        string nodeType,
        string nodeBusinessId,
        CancellationToken cancellationToken = default)
    {
        var container = await _clientFactory.GetContainerAsync(cancellationToken);

        var node = await TryReadNodeAsync(container, nodeType, nodeBusinessId, cancellationToken);

        if (node is null)
        {
            return $"No node of type '{nodeType}' with id '{nodeBusinessId}' was found.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Node {nodeType} '{nodeBusinessId}':");

        if (node.Edges.Count == 0)
        {
            sb.AppendLine("  - has no outgoing connections.");
        }
        else
        {
            foreach (var edge in node.Edges)
            {
                sb.AppendLine($"  - {edge.RelationshipType} -> {edge.TargetNodeType} '{edge.TargetBusinessId}'");
            }
        }

        // Incoming edges: any node in the graph whose edges array points at this node.
        var query = new QueryDefinition(
            "SELECT c.nodeType, c.businessId, e.relationshipType " +
            "FROM c JOIN e IN c.edges " +
            "WHERE e.targetNodeType = @targetType AND e.targetBusinessId = @targetId")
            .WithParameter("@targetType", nodeType)
            .WithParameter("@targetId", nodeBusinessId);

        using var iterator = container.GetItemQueryIterator<IncomingEdgeRow>(
            query, requestOptions: new QueryRequestOptions());

        var foundIncoming = false;

        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);

            foreach (var row in page)
            {
                foundIncoming = true;
                sb.AppendLine($"  - incoming: {row.NodeType} '{row.BusinessId}' --{row.RelationshipType}--> this node");
            }
        }

        if (!foundIncoming)
        {
            sb.AppendLine("  - has no incoming connections.");
        }

        return sb.ToString();
    }

    public async Task<string> GetOperationsForJobAsync(
        string jobNumber,
        CancellationToken cancellationToken = default)
    {
        var container = await _clientFactory.GetContainerAsync(cancellationToken);

        var jobNode = await TryReadNodeAsync(container, GraphNodeTypes.Job, jobNumber, cancellationToken);

        if (jobNode is null)
        {
            return $"No job with number '{jobNumber}' was found in the graph.";
        }

        var operationEdges = jobNode.Edges
            .Where(e => e.RelationshipType == GraphRelationshipTypes.HasOperation)
            .ToList();

        if (operationEdges.Count == 0)
        {
            return $"Job '{jobNumber}' has no recorded operations.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Operations for job {jobNumber}:");

        foreach (var edge in operationEdges)
        {
            var operationNode = await TryReadNodeAsync(
                container, GraphNodeTypes.Operation, edge.TargetBusinessId, cancellationToken);

            if (operationNode is null)
            {
                continue;
            }

            operationNode.Properties.TryGetValue("name", out var name);
            operationNode.Properties.TryGetValue("description", out var description);
            operationNode.Properties.TryGetValue("sequenceNumber", out var sequence);

            sb.AppendLine($"  - Step {sequence}: {name} — {description} ({operationNode.Edges.Count} run(s))");
        }

        return sb.ToString();
    }

    public async Task<string> GetPersonnelForRunAsync(
        string runName,
        CancellationToken cancellationToken = default)
    {
        var container = await _clientFactory.GetContainerAsync(cancellationToken);

        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.nodeType = @nodeType AND c.properties.name = @runName")
            .WithParameter("@nodeType", GraphNodeTypes.Run)
            .WithParameter("@runName", runName);

        using var iterator = container.GetItemQueryIterator<GraphNodeDocument>(query);

        var matches = new List<GraphNodeDocument>();

        while (iterator.HasMoreResults)
        {
            matches.AddRange(await iterator.ReadNextAsync(cancellationToken));
        }

        if (matches.Count == 0)
        {
            return $"No run named '{runName}' was found in the graph.";
        }

        var sb = new StringBuilder();

        foreach (var run in matches)
        {
            run.Properties.TryGetValue("jobNumber", out var jobNumber);
            run.Properties.TryGetValue("operationName", out var operationName);

            sb.AppendLine($"Run '{runName}' (job {jobNumber}, operation '{operationName}'):");

            var personnelEdges = run.Edges
                .Where(e => e.RelationshipType == GraphRelationshipTypes.HasPersonnel)
                .ToList();

            if (personnelEdges.Count == 0)
            {
                sb.AppendLine("  - no personnel recorded.");
                continue;
            }

            foreach (var edge in personnelEdges)
            {
                var personnelNode = await TryReadNodeAsync(
                    container, GraphNodeTypes.Personnel, edge.TargetBusinessId, cancellationToken);

                if (personnelNode is null)
                {
                    continue;
                }

                personnelNode.Properties.TryGetValue("fullName", out var fullName);
                personnelNode.Properties.TryGetValue("role", out var role);

                sb.AppendLine($"  - {fullName} ({role})");
            }
        }

        return sb.ToString();
    }

    public async Task<string> GetFullJobGraphAsync(
        string jobNumber,
        CancellationToken cancellationToken = default)
    {
        var container = await _clientFactory.GetContainerAsync(cancellationToken);

        var jobNode = await TryReadNodeAsync(container, GraphNodeTypes.Job, jobNumber, cancellationToken);

        if (jobNode is null)
        {
            return $"No job with number '{jobNumber}' was found in the graph.";
        }

        var sb = new StringBuilder();

        jobNode.Properties.TryGetValue("jobType", out var jobType);
        jobNode.Properties.TryGetValue("jobStatus", out var jobStatus);
        jobNode.Properties.TryGetValue("clientName", out var clientName);
        jobNode.Properties.TryGetValue("wellName", out var wellName);
        jobNode.Properties.TryGetValue("fieldName", out var fieldName);

        sb.AppendLine($"Job {jobNumber} ({jobType}) — status: {jobStatus}, client: {clientName}, well: {wellName} in {fieldName}.");

        foreach (var edge in jobNode.Edges)
        {
            switch (edge.RelationshipType)
            {
                case GraphRelationshipTypes.HasOperation:
                    {
                        var operationNode = await TryReadNodeAsync(
                            container, GraphNodeTypes.Operation, edge.TargetBusinessId, cancellationToken);

                        if (operationNode is null)
                        {
                            continue;
                        }

                        operationNode.Properties.TryGetValue("name", out var opName);
                        operationNode.Properties.TryGetValue("sequenceNumber", out var seq);
                        operationNode.Properties.TryGetValue("description", out var opDescription);

                        sb.AppendLine($"- Operation {seq}: {opName} — {opDescription}");

                        foreach (var runEdge in operationNode.Edges
                            .Where(e => e.RelationshipType == GraphRelationshipTypes.HasRun))
                        {
                            var runNode = await TryReadNodeAsync(
                                container, GraphNodeTypes.Run, runEdge.TargetBusinessId, cancellationToken);

                            if (runNode is null)
                            {
                                continue;
                            }

                            runNode.Properties.TryGetValue("name", out var runName);
                            runNode.Properties.TryGetValue("result", out var runResult);

                            sb.AppendLine($"    - {runName} -> {runResult}");

                            foreach (var personnelEdge in runNode.Edges
                                .Where(e => e.RelationshipType == GraphRelationshipTypes.HasPersonnel))
                            {
                                var personnelNode = await TryReadNodeAsync(
                                    container, GraphNodeTypes.Personnel, personnelEdge.TargetBusinessId, cancellationToken);

                                if (personnelNode is null)
                                {
                                    continue;
                                }

                                personnelNode.Properties.TryGetValue("fullName", out var fullName);
                                personnelNode.Properties.TryGetValue("role", out var role);

                                sb.AppendLine($"        - {fullName} ({role})");
                            }
                        }

                        break;
                    }

                case GraphRelationshipTypes.HasProduct:
                    {
                        var productNode = await TryReadNodeAsync(
                            container, GraphNodeTypes.Product, edge.TargetBusinessId, cancellationToken);

                        if (productNode is null)
                        {
                            continue;
                        }

                        productNode.Properties.TryGetValue("name", out var productName);
                        productNode.Properties.TryGetValue("quantity", out var quantity);
                        productNode.Properties.TryGetValue("unitOfMeasure", out var unit);

                        sb.AppendLine($"- Product: {quantity} {unit} of {productName}");
                        break;
                    }

                case GraphRelationshipTypes.HasQualityIncident:
                    {
                        var incidentNode = await TryReadNodeAsync(
                            container, GraphNodeTypes.QualityIncident, edge.TargetBusinessId, cancellationToken);

                        if (incidentNode is null)
                        {
                            continue;
                        }

                        incidentNode.Properties.TryGetValue("severity", out var severity);
                        incidentNode.Properties.TryGetValue("description", out var description);
                        incidentNode.Properties.TryGetValue("reportedAt", out var reportedAt);

                        sb.AppendLine($"- Quality incident [{severity}] on {reportedAt}: {description}");
                        break;
                    }
            }
        }

        return sb.ToString();
    }

    private static async Task<GraphNodeDocument?> TryReadNodeAsync(
        Container container,
        string nodeType,
        string businessId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await container.ReadItemAsync<GraphNodeDocument>(
                GraphNodeDocument.BuildId(nodeType, businessId),
                new PartitionKey(nodeType),
                cancellationToken: cancellationToken);

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private sealed record IncomingEdgeRow(string NodeType, string BusinessId, string RelationshipType);
}