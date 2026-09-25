using System.Text.Json;
using EnterpriseAiAssistant.Application.Abstractions.Cost;
using EnterpriseAiAssistant.Application.Abstractions.Rag;
using EnterpriseAiAssistant.Infrastructure.AI.AzureOpenAI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace EnterpriseAiAssistant.Plugins.Planning;

/// <summary>
/// LLM-backed <see cref="IQueryPlanner"/>. Deliberately built as a raw,
/// tool-free chat completion (its own small AzureOpenAIClient/ChatClient,
/// not a Kernel) rather than routed through Semantic Kernel: planning
/// must run BEFORE any plugin is registered/considered, so it has no
/// business holding a reference to the Kernel or its tools at all.
/// A cheap, deterministic check skips the LLM call entirely for ordinary, 
/// single-intent questions — most of them.
/// </summary>
public sealed class LlmQueryPlanner : IQueryPlanner
{
    private const string PlannerCallSite = "query-planner";
    private const int MaxSubQueries = 4;

    private static readonly string[] ComplexityHints =
    [
        " and ", " also ", " as well as ", " then ", "after that",
        ";", " both ", " compare ", " versus ", " vs "
    ];

    private const string PlannerSystemPrompt =
        "You are a retrieval planning assistant for an oilfield job-records system. " +
        "Given the user's question, decide whether it should be split into smaller, " +
        "independent retrieval sub-queries so downstream search and database tools " +
        "can answer each part precisely. Only recommend decomposition for genuinely " +
        "compound or multi-part questions - most questions do not need it. Respond " +
        "with ONLY a JSON object of the exact shape: " +
        "{\"needsDecomposition\": boolean, \"subQueries\": string[], \"rationale\": string}. " +
        "Return at most 4 subQueries.";

    private readonly ChatClient _chatClient;
    private readonly string _deploymentName;
    private readonly AzureOpenAIOptions _options;
    private readonly ICostAccumulator _costAccumulator;
    private readonly ILogger<LlmQueryPlanner> _logger;

    public LlmQueryPlanner(
        IOptions<AzureOpenAIOptions> options,
        ICostAccumulator costAccumulator,
        ILogger<LlmQueryPlanner> logger,
        IHostEnvironment environment)
    {
        _options = options.Value;
        _costAccumulator = costAccumulator;
        _logger = logger;

        _deploymentName = string.IsNullOrWhiteSpace(_options.PlannerDeploymentName)
            ? _options.DeploymentName
            : _options.PlannerDeploymentName;

        var endpoint = NormalizeEndpoint(_options.Endpoint);
        var credential = AzureCredentialFactory.Create(environment, _options.TenantId, logger);

        var azureClient = new Azure.AI.OpenAI.AzureOpenAIClient(new Uri(endpoint), credential);
        _chatClient = azureClient.GetChatClient(_deploymentName);
    }

    public async Task<QueryPlan> PlanAsync(
        string question,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableQueryPlanning)
        {
            return QueryPlan.NoDecomposition("Query planning is disabled (AzureOpenAI:EnableQueryPlanning=false).");
        }

        if (string.IsNullOrWhiteSpace(question) || !LooksCompound(question))
        {
            return QueryPlan.NoDecomposition("Question appears single-intent; skipped the planning call.");
        }

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(PlannerSystemPrompt),
                new UserChatMessage(question)
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            var completion = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);

            RecordUsage(completion.Value.Usage);

            var json = completion.Value.Content.Count > 0
                ? completion.Value.Content[0].Text
                : null;

            return ParsePlan(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Query planning failed; continuing without decomposition.");
            return QueryPlan.NoDecomposition("Planning call failed; proceeding without decomposition.");
        }
    }

    private void RecordUsage(ChatTokenUsage? usage)
    {
        if (usage is null)
        {
            return;
        }

        _costAccumulator.Record(
            PlannerCallSite, _deploymentName, usage.InputTokenCount, usage.OutputTokenCount);
    }

    private static bool LooksCompound(string question)
    {
        if (question.Length > 160)
        {
            return true;
        }

        if (question.Count(c => c == '?') > 1)
        {
            return true;
        }

        var padded = $" {question.ToLowerInvariant()} ";
        return ComplexityHints.Any(padded.Contains);
    }

    private QueryPlan ParsePlan(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return QueryPlan.NoDecomposition("Planner returned no content.");
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var needsDecomposition =
                root.TryGetProperty("needsDecomposition", out var needsElement) &&
                needsElement.ValueKind == JsonValueKind.True;

            var rationale = root.TryGetProperty("rationale", out var rationaleElement)
                ? rationaleElement.GetString() ?? string.Empty
                : string.Empty;

            var subQueries = new List<string>();

            if (root.TryGetProperty("subQueries", out var subQueriesElement) &&
                subQueriesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in subQueriesElement.EnumerateArray().Take(MaxSubQueries))
                {
                    if (item.ValueKind == JsonValueKind.String &&
                        !string.IsNullOrWhiteSpace(item.GetString()))
                    {
                        subQueries.Add(item.GetString()!);
                    }
                }
            }

            return new QueryPlan(needsDecomposition && subQueries.Count > 0, subQueries, rationale);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Could not parse query planner response as JSON: {Json}", json);
            return QueryPlan.NoDecomposition("Planner response was not valid JSON.");
        }
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        var trimmed = endpoint.Trim().TrimEnd('/');
        const string openAiV1Suffix = "/openai/v1";

        return trimmed.EndsWith(openAiV1Suffix, StringComparison.OrdinalIgnoreCase)
            ? trimmed[..^openAiV1Suffix.Length]
            : trimmed;
    }
}
