using System.ComponentModel;
using EnterpriseAiAssistant.Application.Abstractions.Rag;
using Microsoft.SemanticKernel;

namespace EnterpriseAiAssistant.Plugins.Tools;

/// <summary>
/// Semantic Kernel plugin wrapper around <see cref="ICosmosGraphSearchService"/>.
/// Relationship-oriented retrieval over the Cosmos DB graph that is
/// synchronized from the SQL Server JOB data (Job -> Operation -> Run ->
/// Personnel, and other relationships). No embeddings or AI-based
/// similarity search are used — retrieval is exact structural/attribute
/// traversal.
/// </summary>
public sealed class CosmosGraphSearchPlugin
{
    private readonly ICosmosGraphSearchService _cosmosGraphSearchService;

    public CosmosGraphSearchPlugin(ICosmosGraphSearchService cosmosGraphSearchService)
    {
        _cosmosGraphSearchService = cosmosGraphSearchService;
    }

    [KernelFunction("get_connections")]
    [Description("Returns everything directly connected to a node in the job relationship graph (both what it points to and what points to it). Use this for open-ended 'what is related to X' questions.")]
    public Task<string> GetConnectionsAsync(
        [Description("The node type: Job, Operation, Run, Personnel, Product, or QualityIncident.")] string nodeType,
        [Description("The node's business identifier, e.g. a job number for a Job node.")] string nodeBusinessId,
        CancellationToken cancellationToken = default)
        => _cosmosGraphSearchService.GetConnectionsAsync(nodeType, nodeBusinessId, cancellationToken);

    [KernelFunction("get_operations_for_job")]
    [Description("Returns the operations that belong to a job, in sequence, via the relationship graph. Use this for questions like 'what operations were performed on job Y'.")]
    public Task<string> GetOperationsForJobAsync(
        [Description("The exact job number, e.g. 'JOB-2026-0001'.")] string jobNumber,
        CancellationToken cancellationToken = default)
        => _cosmosGraphSearchService.GetOperationsForJobAsync(jobNumber, cancellationToken);

    [KernelFunction("get_personnel_for_run")]
    [Description("Returns the personnel (crew) associated with a named run via the relationship graph. Use this for questions like 'who worked on this run'.")]
    public Task<string> GetPersonnelForRunAsync(
        [Description("The run's name, e.g. 'Run 1'.")] string runName,
        CancellationToken cancellationToken = default)
        => _cosmosGraphSearchService.GetPersonnelForRunAsync(runName, cancellationToken);

    [KernelFunction("get_full_job_graph")]
    [Description("Walks the complete job relationship graph (operations, runs with results, personnel, products, and quality incidents with full description text) for a single job. Use for deep 'everything about job X' questions, or as a fallback when SQL is unavailable.")]
    public Task<string> GetFullJobGraphAsync(
        [Description("The exact job number, e.g. 'JOB-2026-0004'.")] string jobNumber,
        CancellationToken cancellationToken = default)
        => _cosmosGraphSearchService.GetFullJobGraphAsync(jobNumber, cancellationToken);
}