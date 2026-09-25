using System.Runtime.CompilerServices;
using EnterpriseAiAssistant.Application.Abstractions.AI;
using EnterpriseAiAssistant.Application.Abstractions.Cost;
using EnterpriseAiAssistant.Application.Abstractions.Rag;
using EnterpriseAiAssistant.Domain.Chat;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace EnterpriseAiAssistant.Plugins;

/// <summary>
/// Replaces the direct Azure OpenAI IAIClient with one that goes
/// through Semantic Kernel, so a single user question can be answered
/// by the LLM calling one or more of the MsSqlSearch, CosmosGraphSearch,
/// and AzureVectorSearch plugins before synthesizing a final grounded
/// response. Semantic Kernel handles the tool-selection round trips
/// internally (FunctionChoiceBehavior.Auto).
///
///  - Query Planner: before any plugin can be invoked, IQueryPlanner
///    decides whether the question should be decomposed, and — when it
///    should — the sub-queries are injected as guidance for the model's
///    own tool selection.
///  - Result Validator + citations: handled by ResultValidationFilter,
///    attached to the Kernel in KernelFactory, which runs on every tool
///    call this class triggers.
///  - Cost tracking: after the completion call(s) return, this class
///    walks the ChatHistory it passed in — which Semantic Kernel mutates
///    in place with every intermediate tool-calling round trip 
/// </summary>
public sealed class SemanticKernelAIClient : IAIClient
{
    private const string ChatCompletionCallSite = "chat-completion";

    private readonly KernelFactory _kernelFactory;
    private readonly IQueryPlanner _queryPlanner;
    private readonly ICostAccumulator _costAccumulator;

    public SemanticKernelAIClient(
        KernelFactory kernelFactory,
        IQueryPlanner queryPlanner,
        ICostAccumulator costAccumulator)
    {
        _kernelFactory = kernelFactory;
        _queryPlanner = queryPlanner;
        _costAccumulator = costAccumulator;
    }

    public async Task<AIResponse> CompleteAsync(
        AIRequest request,
        CancellationToken cancellationToken = default)
    {
        var kernel = _kernelFactory.Build();
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var history = await BuildHistoryAsync(request, cancellationToken);
        var settings = BuildExecutionSettings();

        var result = await chatCompletionService.GetChatMessageContentAsync(
            history, settings, kernel, cancellationToken);

        RecordChatHistoryUsage(history);

        var content = result.Content;

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Semantic Kernel returned an empty response.");
        }

        return new AIResponse(content);
    }

    public async IAsyncEnumerable<string> StreamCompleteAsync(
        AIRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var kernel = _kernelFactory.Build();
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var history = await BuildHistoryAsync(request, cancellationToken);
        var settings = BuildExecutionSettings();

        var updates = chatCompletionService.GetStreamingChatMessageContentsAsync(
            history, settings, kernel, cancellationToken);

        await foreach (var update in updates)
        {
            if (!string.IsNullOrEmpty(update.Content))
            {
                yield return update.Content;
            }
        }

        RecordChatHistoryUsage(history);
    }

    /// <summary>
    /// FunctionChoiceBehavior.Auto lets the model decide, per turn,
    /// whether zero, one, or multiple of the registered plugin
    /// functions are needed — Semantic Kernel executes them and feeds
    /// the (validated — see ResultValidationFilter) results back to the
    /// model automatically before the final answer is produced or
    /// streamed.
    /// </summary>
    private static OpenAIPromptExecutionSettings BuildExecutionSettings() => new()
    {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    };

    /// <summary>
    /// Query Planner / Context-Builder stage: runs BEFORE any plugin is
    /// ever invoked. For a genuinely compound question, IQueryPlanner
    /// returns a small set of sub-queries which are injected here as an
    /// extra system message — steering the model's own tool selection
    /// toward covering every part of the question rather than picking one tool
    /// call that only half-answers it. For an ordinary question,
    /// planning is skipped at negligible cost (see LlmQueryPlanner).
    /// </summary>
    private async Task<ChatHistory> BuildHistoryAsync(
        AIRequest request,
        CancellationToken cancellationToken)
    {
        var history = new ChatHistory();

        foreach (var message in request.Messages)
        {
            switch (message.Role)
            {
                case ChatRole.System:
                    history.AddSystemMessage(message.Content);
                    break;

                case ChatRole.User:
                    history.AddUserMessage(message.Content);
                    break;

                case ChatRole.Assistant:
                    history.AddAssistantMessage(message.Content);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported chat role: {message.Role}");
            }
        }

        var latestQuestion = request.Messages.LastOrDefault(m => m.Role == ChatRole.User)?.Content;

        if (!string.IsNullOrWhiteSpace(latestQuestion))
        {
            var plan = await _queryPlanner.PlanAsync(latestQuestion, cancellationToken);

            if (plan.NeedsDecomposition)
            {
                history.AddSystemMessage(
                    "This question may require checking multiple things. Before " +
                    "giving a final answer, consider gathering evidence for each " +
                    "of the following using the available tools as needed:\n" +
                    string.Join('\n', plan.SubQueries.Select((q, i) => $"{i + 1}. {q}")));
            }
        }

        return history;
    }

    /// <summary>
    /// Cost tracking: Semantic Kernel's auto function-calling loop can
    /// make several completion round trips within a single call (one
    /// per turn the model asks for another tool). It mutates the
    /// ChatHistory instance passed into it with every round, so walking
    /// it after the call returns lets us sum the actual usage metadata
    /// the underlying Azure OpenAI connector reported for every round,
    /// not just the final one — i.e. the turn's real total.
    /// </summary>
    private void RecordChatHistoryUsage(ChatHistory history)
    {
        foreach (var message in history)
        {
            if (message.Role != AuthorRole.Assistant || message.Metadata is null)
            {
                continue;
            }

            if (message.Metadata.TryGetValue("Usage", out var usageObj) &&
                usageObj is OpenAI.Chat.ChatTokenUsage usage)
            {
                _costAccumulator.Record(
                    ChatCompletionCallSite,
                    _kernelFactory.DeploymentName,
                    usage.InputTokenCount,
                    usage.OutputTokenCount);
            }
        }
    }
}
