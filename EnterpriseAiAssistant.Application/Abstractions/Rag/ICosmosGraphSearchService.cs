namespace EnterpriseAiAssistant.Application.Abstractions.Rag;

/// <summary>
/// Relationship-oriented retrieval against the Cosmos DB graph
/// representation that is synchronized from the SQL Server JOB data
/// during ingestion (Job -> Operation -> Run -> Personnel, plus other
/// entity relationships). No embeddings or AI-based similarity search
/// are used here — retrieval is exact structural/attribute traversal.
/// Backs the Semantic Kernel "CosmosGraphSearch" plugin.
/// </summary>
public interface ICosmosGraphSearchService
{
    /// <summary>
    /// Returns every node directly connected to the given node
    /// (e.g. "what is connected to node Z?").
    /// </summary>
    Task<string> GetConnectionsAsync(
        string nodeType,
        string nodeBusinessId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the Operations that belong to a Job
    /// (e.g. "give me the operations for job Y").
    /// </summary>
    Task<string> GetOperationsForJobAsync(
        string jobNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the Personnel associated with a Run
    /// (e.g. "which personnel are associated with this run?").
    /// </summary>
    Task<string> GetPersonnelForRunAsync(
        string runName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Walks the full Job -> Operation -> Run -> Personnel graph (plus
    /// Product and QualityIncident children) and returns everything in
    /// one deterministic traversal, including full incident description
    /// text. Use for deep "everything about job X" questions answerable
    /// purely from the graph, independent of SQL/Search availability.
    /// </summary>
    Task<string> GetFullJobGraphAsync(
        string jobNumber,
        CancellationToken cancellationToken = default);
}