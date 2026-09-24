namespace EnterpriseAiAssistant.Infrastructure.Graph;

public sealed class CosmosGraphOptions
{
    public const string SectionName = "CosmosDb";

    public string Endpoint { get; set; } = string.Empty;

    public string DatabaseName { get; set; } = string.Empty;

    public string ContainerName { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;
}
