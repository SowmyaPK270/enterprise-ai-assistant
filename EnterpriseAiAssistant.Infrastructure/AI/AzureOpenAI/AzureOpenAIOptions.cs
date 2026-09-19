using System;
using System.Collections.Generic;
using System.Text;

namespace EnterpriseAiAssistant.Infrastructure.AI.AzureOpenAI;

public sealed class AzureOpenAIOptions
{
    public const string SectionName = "AzureOpenAI";

    public string Endpoint { get; set; } = string.Empty;

    public string DeploymentName { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;
}