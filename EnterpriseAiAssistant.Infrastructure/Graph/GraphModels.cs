using Newtonsoft.Json;

namespace EnterpriseAiAssistant.Infrastructure.Graph;

/// <summary>
/// The fixed catalog of node types in the job relationship graph, per
/// the requirements: Job, Operation, Run, Personnel, Product,
/// QualityIncident.
/// </summary>
internal static class GraphNodeTypes
{
    public const string Job = "Job";
    public const string Operation = "Operation";
    public const string Run = "Run";
    public const string Personnel = "Personnel";
    public const string Product = "Product";
    public const string QualityIncident = "QualityIncident";
}

internal static class GraphRelationshipTypes
{
    public const string HasOperation = "HasOperation";
    public const string HasRun = "HasRun";
    public const string HasPersonnel = "HasPersonnel";
    public const string HasProduct = "HasProduct";
    public const string HasQualityIncident = "HasQualityIncident";
}

/// <summary>
/// A directed edge to another node in the graph. Stored inline on the
/// source node's document — the graph is modeled as an adjacency list
/// over Cosmos DB's Core (SQL) API rather than the separate Gremlin
/// API, so the whole application can authenticate with a single
/// Managed Identity / DefaultAzureCredential and a single SDK
/// (Microsoft.Azure.Cosmos). See the Phase 2 README for the rationale.
/// </summary>
public sealed class GraphEdge
{
    [JsonProperty("relationshipType")]
    public string RelationshipType { get; set; } = string.Empty;

    [JsonProperty("targetNodeType")]
    public string TargetNodeType { get; set; } = string.Empty;

    [JsonProperty("targetBusinessId")]
    public string TargetBusinessId { get; set; } = string.Empty;
}

/// <summary>
/// A node in the job relationship graph. "id" and "partitionKey" follow
/// Cosmos DB's required shape; nodes are partitioned by NodeType so
/// same-type traversals (e.g. "all Operations for a Job", read by
/// point-read of the Job node's edges) stay within a single partition.
/// </summary>
public sealed class GraphNodeDocument
{
    /// <summary>Cosmos item id: "{NodeType}|{BusinessId}".</summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("partitionKey")]
    public string PartitionKey { get; set; } = string.Empty;

    [JsonProperty("nodeType")]
    public string NodeType { get; set; } = string.Empty;

    [JsonProperty("businessId")]
    public string BusinessId { get; set; } = string.Empty;

    /// <summary>
    /// Business attributes such as clientName, mobilizationDate,
    /// jobStatus, etc. — kept as a flat string dictionary so the schema
    /// stays uniform across node types without needing a Cosmos schema
    /// migration whenever a new attribute is added.
    /// </summary>
    [JsonProperty("properties")]
    public Dictionary<string, string> Properties { get; set; } = new();

    [JsonProperty("edges")]
    public List<GraphEdge> Edges { get; set; } = new();

    public static string BuildId(string nodeType, string businessId) => $"{nodeType}|{businessId}";
}
