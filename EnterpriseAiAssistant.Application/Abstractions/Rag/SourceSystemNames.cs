namespace EnterpriseAiAssistant.Application.Abstractions.Rag;

/// <summary>
/// Mapping from a function name to the
/// human-readable source-system name shown in citations. Kept here
/// (Application layer) rather than duplicated in the Plugins project so
/// there is exactly one place that defines "what a source is called."
/// </summary>
public static class SourceSystemNames
{
    public const string SqlJobDatabase = "SQL Job Database";
    public const string CosmosJobGraph = "Cosmos JobGraph";
    public const string AzureVectorSearchJobEvidence = "Azure Vector Search (Job Evidence)";
    public const string AzureVectorSearchDocuments = "Uploaded Documents (PDF/DOCX)";

    private const string MsSqlSearchPluginName = "MsSqlSearch";
    private const string CosmosGraphSearchPluginName = "CosmosGraphSearch";
    private const string AzureVectorSearchPluginName = "AzureVectorSearch";
    private const string SearchDocumentsFunctionName = "search_documents";

    public static string Resolve(string pluginName, string functionName) => pluginName switch
    {
        MsSqlSearchPluginName => SqlJobDatabase,
        CosmosGraphSearchPluginName => CosmosJobGraph,
        AzureVectorSearchPluginName => functionName == SearchDocumentsFunctionName
            ? AzureVectorSearchDocuments
            : AzureVectorSearchJobEvidence,
        _ => pluginName
    };
}
