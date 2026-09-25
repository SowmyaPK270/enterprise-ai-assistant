using EnterpriseAiAssistant.Application.Abstractions.Cost;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseAiAssistant.Infrastructure.Cost;

public sealed class TokenPricingProvider : ITokenPricingProvider
{
    private readonly TokenPricingOptions _options;
    private readonly ILogger<TokenPricingProvider> _logger;

    public TokenPricingProvider(
        IOptions<TokenPricingOptions> options,
        ILogger<TokenPricingProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public decimal CalculateCost(string modelOrDeployment, int inputTokens, int outputTokens)
    {
        var pricing = ResolvePricing(modelOrDeployment);

        if (pricing is null)
        {
            return 0m;
        }

        var inputCost = inputTokens / 1_000_000m * pricing.InputPerMillionUsd;
        var outputCost = outputTokens / 1_000_000m * pricing.OutputPerMillionUsd;

        return inputCost + outputCost;
    }

    private ModelPricing? ResolvePricing(string modelOrDeployment)
    {
        if (!string.IsNullOrWhiteSpace(modelOrDeployment) &&
            _options.Models.TryGetValue(modelOrDeployment, out var pricing))
        {
            return pricing;
        }

        if (!string.IsNullOrWhiteSpace(_options.DefaultModel) &&
            _options.Models.TryGetValue(_options.DefaultModel, out var fallback))
        {
            _logger.LogDebug(
                "No pricing configured for '{Model}'; using default pricing for '{Default}'.",
                modelOrDeployment, _options.DefaultModel);

            return fallback;
        }

        _logger.LogWarning(
            "No token pricing configured for '{Model}' (and no TokenPricing:DefaultModel " +
            "set); its cost will report as $0. Add it under TokenPricing:Models in configuration.",
            modelOrDeployment);

        return null;
    }
}
