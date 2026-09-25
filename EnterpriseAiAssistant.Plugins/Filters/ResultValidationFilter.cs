using EnterpriseAiAssistant.Application.Abstractions.Rag;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace EnterpriseAiAssistant.Plugins.Filters;

/// <summary>
/// Semantic Kernel function-invocation filter that sits between every
/// MsSqlSearch/CosmosGraphSearch/AzureVectorSearch tool call and the
/// model that consumes its result:
///
///  1. Runs the tool as normal (<c>await next(context)</c>).
///  2. Passes the raw result through IResultValidator — the Result
///     Validator stage: empty/"not found" detection, truncation, and
///     neutralizing indirect prompt-injection attempts hiding in
///     retrieved content (most relevant for uploaded documents, which
///     this app does not otherwise vet).
///  3. If validation changed the content, replaces context.Result so
///     the SANITIZED version — not the raw one — is what flows back
///     into the conversation the model uses to synthesize its answer.
///  4. Records a citation for any call that returned real data, so the
///     turn's full citation list is already known before the final
///     answer is even written.
/// </summary>
public sealed class ResultValidationFilter : IFunctionInvocationFilter
{
    private readonly IResultValidator _validator;
    private readonly ICitationAccumulator _citations;
    private readonly ILogger<ResultValidationFilter> _logger;

    public ResultValidationFilter(
        IResultValidator validator,
        ICitationAccumulator citations,
        ILogger<ResultValidationFilter> logger)
    {
        _validator = validator;
        _citations = citations;
        _logger = logger;
    }

    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, Task> next)
    {
        await next(context);

        var rawResult = context.Result?.GetValue<string>();

        if (string.IsNullOrEmpty(rawResult))
        {
            return;
        }

        var pluginName = context.Function.PluginName ?? "Unknown";
        var sourceSystem = SourceSystemNames.Resolve(pluginName, context.Function.Name);
        var primaryArgument = ExtractPrimaryArgument(context.Arguments);

        var outcome = _validator.Validate(sourceSystem, rawResult, primaryArgument);

        if (!string.Equals(outcome.SanitizedContent, rawResult, StringComparison.Ordinal))
        {
            context.Result = new FunctionResult(context.Result!, outcome.SanitizedContent);
        }

        if (outcome.HasUsableData)
        {
            _citations.Record(new Citation(sourceSystem, primaryArgument, Preview(outcome.SanitizedContent)));
        }

        if (outcome.FlaggedPossiblePromptInjection)
        {
            _logger.LogWarning(
                "Possible prompt injection neutralized in content retrieved from {Source} (function {Function}).",
                sourceSystem, context.Function.Name);
        }

        if (outcome.WasTruncated)
        {
            _logger.LogInformation(
                "Result from {Source} (function {Function}) was truncated before reaching the model.",
                sourceSystem, context.Function.Name);
        }
    }

    private static string ExtractPrimaryArgument(KernelArguments? arguments)
    {
        if (arguments is null || arguments.Count == 0)
        {
            return string.Empty;
        }

        // Every plugin function's first parameter is its main query/id
        // argument (likejob number, node id, run name, search query) 
        // good enough for a short, human-readable citation label.
        var first = arguments.Values.FirstOrDefault();
        return first?.ToString() ?? string.Empty;
    }

    private static string Preview(string text) =>
        text.Length <= 160 ? text : text[..160] + "…";
}
