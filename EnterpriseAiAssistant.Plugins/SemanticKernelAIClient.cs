using System.Runtime.CompilerServices;
using EnterpriseAiAssistant.Application.Abstractions.AI;
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
/// internally (FunctionChoiceBehavior.Auto); this class only needs to
/// convert domain ChatMessages to a ChatHistory and back.
/// </summary>
public sealed class SemanticKernelAIClient : IAIClient
{
    private readonly KernelFactory _kernelFactory;

    private const string SystemInstruction =
        """
        You are answering questions about enterprise job data.
        For factual questions about jobs, clients, wells, fields, operations, runs, products, incidents, counts, or dates:
        - Always use the deterministic retrieval tools before answering.
        - Prefer MsSqlSearch for exact facts, filters, and aggregations.
        - Do not infer seeded data from memory.
        - If no records match a requested filter, say that clearly and include the closest directly relevant supporting records returned by the tool.
        """;

    public SemanticKernelAIClient(KernelFactory kernelFactory)
    {
        _kernelFactory = kernelFactory;
    }

    public async Task<AIResponse> CompleteAsync(
        AIRequest request,
        CancellationToken cancellationToken = default)
    {
        var kernel = _kernelFactory.Build();
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var history = BuildHistory(request);
        var settings = BuildExecutionSettings();

        var result = await chatCompletionService.GetChatMessageContentAsync(
            history, settings, kernel, cancellationToken);

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
        var history = BuildHistory(request);
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
    }

    /// <summary>
    /// FunctionChoiceBehavior.Auto lets the model decide, per turn,
    /// whether zero, one, or multiple of the registered plugin
    /// functions are needed — Semantic Kernel executes them and feeds
    /// the results back to the model automatically before the final
    /// answer is produced or streamed.
    /// </summary>
    private static OpenAIPromptExecutionSettings BuildExecutionSettings() => new()
    {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    };

    private static ChatHistory BuildHistory(AIRequest request)
    {
        var history = new ChatHistory();
        history.AddSystemMessage(SystemInstruction);

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

        return history;
    }
}
