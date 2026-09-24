namespace EnterpriseAiAssistant.Infrastructure.Search;

/// <summary>
/// Values stored in the shared job-knowledge index's SourceType field,
/// used to filter AzureVectorSearch queries to just job-evidence or
/// just documents even though both live in the same index.
/// </summary>
internal static class JobKnowledgeSourceTypes
{
    public const string JobEvidence = "JobEvidence";

    public const string Document = "Document";
}
