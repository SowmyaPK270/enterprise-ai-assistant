using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseAiAssistant.Plugins.Mcp;

public static class RetrievalMcpExtensions
{
    /// <summary>
    /// Registers the three retrieval tool sets (SQL, graph, vector) and the
    /// shared <see cref="McpResultGuard"/> on an MCP server builder. Transport
    /// and endpoint mapping stay in the composition root (Program.cs).
    /// </summary>
    public static IMcpServerBuilder WithRetrievalTools(this IMcpServerBuilder builder)
    {
        builder.Services.AddScoped<McpResultGuard>();

        return builder
            .WithTools<MsSqlSearchMcpTools>()
            .WithTools<CosmosGraphSearchMcpTools>()
            .WithTools<AzureVectorSearchMcpTools>();
    }
}
