namespace EnterpriseAiAssistant.Infrastructure.Cost;

public sealed class ModelPricing
{
    public decimal InputPerMillionUsd { get; set; }

    public decimal OutputPerMillionUsd { get; set; }
}

public sealed class TokenPricingOptions
{
    public const string SectionName = "TokenPricing";

    public string DefaultModel { get; set; } = string.Empty;

    public Dictionary<string, ModelPricing> Models { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}
