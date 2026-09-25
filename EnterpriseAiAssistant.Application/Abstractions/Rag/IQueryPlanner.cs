namespace EnterpriseAiAssistant.Application.Abstractions.Rag;

/// <summary>
/// The outcome of the planning stage: whether the question needed to be
/// broken into smaller retrieval sub-queries, and what those are.
/// </summary>
public sealed record QueryPlan(
    bool NeedsDecomposition,
    IReadOnlyList<string> SubQueries,
    string Rationale)
{
    public static QueryPlan NoDecomposition(string rationale) => new(false, [], rationale);
}

/// <summary>
/// Query Planner / Context-Builder stage. Runs BEFORE any of the
/// MsSqlSearch/CosmosGraphSearch/AzureVectorSearch plugins are invoked:
/// given the user's raw question, decides whether it is a compound
/// question that should be rewritten into smaller, independent
/// retrieval sub-queries so downstream tools can answer each part
/// precisely, rather than the model trying to satisfy everything with
/// one imprecise tool call.
///
/// This is  a planning-only step — it does not call any
/// retrieval tool itself. The plan it produces is used to steer the
/// model's own tool selection (Semantic Kernel's function-calling loop
/// remains the actual executor).
/// </summary>
public interface IQueryPlanner
{
    Task<QueryPlan> PlanAsync(
        string question,
        CancellationToken cancellationToken = default);
}
