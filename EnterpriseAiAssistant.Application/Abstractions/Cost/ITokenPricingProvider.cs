namespace EnterpriseAiAssistant.Application.Abstractions.Cost;

/// <summary>
/// Resolves $/token pricing for a given model 
/// </summary>
public interface ITokenPricingProvider
{
    decimal CalculateCost(string modelOrDeployment, int inputTokens, int outputTokens);
}
