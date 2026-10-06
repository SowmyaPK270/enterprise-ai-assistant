using System.ComponentModel;
using EnterpriseAiAssistant.Application.Abstractions.Rag;
using ModelContextProtocol.Server;

namespace EnterpriseAiAssistant.Plugins.Mcp;

/// <summary>
/// MCP exposure of <see cref="ICosmosGraphSearchService"/> — the same
/// relationship traversal that <c>CosmosGraphSearchPlugin</c> offers to
/// Semantic Kernel. All tools are read-only.
/// </summary>
[McpServerToolType]
public sealed class CosmosGraphSearchMcpTools
{
    private const string Source = SourceSystemNames.CosmosJobGraph;

    private readonly ICosmosGraphSearchService _graph;
    private readonly McpResultGuard _guard;

    public CosmosGraphSearchMcpTools(ICosmosGraphSearchService graph, McpResultGuard guard)
    {
        _graph = graph;
        _guard = guard;
    }

    [McpServerTool(Name = "get_connections", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("[Graph] Returns everything directly connected to a node in the job relationship graph (both what it points to and what points to it). Use this for open-ended 'what is related to X' questions.")]
    public async Task<string> GetConnections(
        [Description("The node type: Job, Operation, Run, Personnel, Product, or QualityIncident.")] string nodeType,
        [Description("The node's business identifier, e.g. a job number for a Job node.")] string nodeBusinessId,
        CancellationToken cancellationToken = default)
        => _guard.Apply(Source, await _graph.GetConnectionsAsync(nodeType, nodeBusinessId, cancellationToken), nodeBusinessId);

    [McpServerTool(Name = "get_operations_for_job", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("[Graph] Returns the operations that belong to a job, in sequence, via the relationship graph. Use this for questions like 'what operations were performed on job Y'.")]
    public async Task<string> GetOperationsForJob(
        [Description("The exact job number, e.g. 'JOB-2026-0001'.")] string jobNumber,
        CancellationToken cancellationToken = default)
        => _guard.Apply(Source, await _graph.GetOperationsForJobAsync(jobNumber, cancellationToken), jobNumber);

    [McpServerTool(Name = "get_personnel_for_run", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("[Graph] Returns the personnel (crew) associated with a named run via the relationship graph. Use this for questions like 'who worked on this run'.")]
    public async Task<string> GetPersonnelForRun(
        [Description("The run's name, e.g. 'Run 1'.")] string runName,
        CancellationToken cancellationToken = default)
        => _guard.Apply(Source, await _graph.GetPersonnelForRunAsync(runName, cancellationToken), runName);

    [McpServerTool(Name = "get_full_job_graph", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("[Graph] Walks the complete job relationship graph (operations, runs with results, personnel, products, and quality incidents with full description text) for a single job. Use for deep 'everything about job X' questions, or as a fallback when SQL is unavailable.")]
    public async Task<string> GetFullJobGraph(
        [Description("The exact job number, e.g. 'JOB-2026-0004'.")] string jobNumber,
        CancellationToken cancellationToken = default)
        => _guard.Apply(Source, await _graph.GetFullJobGraphAsync(jobNumber, cancellationToken), jobNumber);
}
