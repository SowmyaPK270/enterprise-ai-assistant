namespace EnterpriseAiAssistant.Infrastructure.Search;

public sealed class AzureAiSearchOptions
{
    public const string SectionName = "AzureAiSearch";

    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Single index shared by both job-evidence (derived from SQL
    /// Server JOB data) and uploaded documents/PDFs. The two use cases
    /// are distinguished by the SourceType field rather than split into
    /// separate indexes,in which both
    /// paths converge on one Azure AI Search store.
    /// </summary>
    public string IndexName { get; set; } = "job-knowledge-index";

    public string TenantId { get; set; } = string.Empty;
}
