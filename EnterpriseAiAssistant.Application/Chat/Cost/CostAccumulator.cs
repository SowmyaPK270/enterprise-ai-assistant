using EnterpriseAiAssistant.Application.Abstractions.Cost;

namespace EnterpriseAiAssistant.Application.Chat.Cost;

/// <summary>
/// Thread-safe, in-memory implementation of <see cref="ICostAccumulator"/>.
/// <see cref="Reset"/> must be called
/// explicitly at the start of every turn rather than relying on DI
/// scope disposal in a Blazor Server app.
/// </summary>
public sealed class CostAccumulator : ICostAccumulator
{
    private readonly ITokenPricingProvider _pricingProvider;
    private readonly object _lock = new();
    private readonly List<LlmCallUsage> _calls = [];

    public CostAccumulator(ITokenPricingProvider pricingProvider)
    {
        _pricingProvider = pricingProvider;
    }

    public void Reset()
    {
        lock (_lock)
        {
            _calls.Clear();
        }
    }

    public void Record(string callSite, string modelOrDeployment, int inputTokens, int outputTokens)
    {
        var estimatedCost = _pricingProvider.CalculateCost(modelOrDeployment, inputTokens, outputTokens);

        lock (_lock)
        {
            _calls.Add(new LlmCallUsage(callSite, modelOrDeployment, inputTokens, outputTokens, estimatedCost));
        }
    }

    public ExecutionCostSummary GetSummary()
    {
        lock (_lock)
        {
            return new ExecutionCostSummary(
                _calls.Sum(c => c.InputTokens),
                _calls.Sum(c => c.OutputTokens),
                _calls.Sum(c => c.EstimatedCostUsd),
                _calls.ToList());
        }
    }
}
