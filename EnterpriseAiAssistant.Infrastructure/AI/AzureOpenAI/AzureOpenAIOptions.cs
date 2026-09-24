using System;
using System.Collections.Generic;
using System.Text;

namespace EnterpriseAiAssistant.Infrastructure.AI.AzureOpenAI;

public sealed class AzureOpenAIOptions
{
    public const string SectionName = "AzureOpenAI";

    public string Endpoint { get; set; } = string.Empty;

    public string DeploymentName { get; set; } = string.Empty;

    /// <summary>
    /// Deployment name of the Azure OpenAI text-embedding model (e.g.
    /// "text-embedding-3-small") used both when indexing content and
    /// when embedding a user's question for vector search.
    /// </summary>
    public string EmbeddingDeploymentName { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;
}