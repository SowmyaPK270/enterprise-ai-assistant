namespace EnterpriseAiAssistant.Web.Mcp;

/// <summary>
/// Settings for the MCP server endpoint (configuration section "Mcp").
/// </summary>
public sealed class McpOptions
{
    public const string SectionName = "Mcp";

    /// <summary>Master switch. When false, no MCP endpoint is mapped.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Route the Streamable HTTP endpoint is served on.</summary>
    public string Path { get; set; } = "/mcp";

    /// <summary>
    /// Shared secret MCP clients must send as <c>Authorization: Bearer &lt;key&gt;</c>
    /// (or <c>X-Api-Key: &lt;key&gt;</c>). Supply it through user secrets or App
    /// Service configuration (<c>Mcp__ApiKey</c>), never appsettings.json.
    /// If empty, the endpoint is only mapped in the Development environment
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
