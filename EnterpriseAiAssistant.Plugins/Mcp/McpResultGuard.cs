using EnterpriseAiAssistant.Application.Abstractions.Rag;
using Microsoft.Extensions.Logging;

namespace EnterpriseAiAssistant.Plugins.Mcp;

/// <summary>
/// MCP counterpart of <see cref="Filters.ResultValidationFilter"/>.
/// The Semantic Kernel filter only runs for tool calls made by this app's
/// own orchestrator; calls arriving over MCP bypass the Kernel entirely, so
/// every MCP tool result is passed through the same <see cref="IResultValidator"/>
/// (empty/"not found" handling, truncation, indirect prompt-injection
/// neutralization) before it is returned to the external client.
/// Citations are intentionally not recorded: they belong to a chat turn,
/// and an MCP call has no ChatService turn to attach them to.
/// </summary>
public sealed class McpResultGuard
{
    private readonly IResultValidator _validator;
    private readonly ILogger<McpResultGuard> _logger;

    public McpResultGuard(IResultValidator validator, ILogger<McpResultGuard> logger)
    {
        _validator = validator;
        _logger = logger;
    }

    public string Apply(string sourceSystem, string rawResult, string query)
    {
        var outcome = _validator.Validate(sourceSystem, rawResult, query);

        if (outcome.FlaggedPossiblePromptInjection)
        {
            _logger.LogWarning(
                "Possible prompt injection neutralized in content retrieved from {Source} for an MCP client.",
                sourceSystem);
        }

        if (outcome.WasTruncated)
        {
            _logger.LogInformation(
                "Result from {Source} was truncated before being returned to an MCP client.",
                sourceSystem);
        }

        return outcome.SanitizedContent;
    }
}
