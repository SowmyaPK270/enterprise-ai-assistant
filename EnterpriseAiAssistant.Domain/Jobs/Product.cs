namespace EnterpriseAiAssistant.Domain.Jobs;

/// <summary>
/// A product/material consumed as part of a Job (e.g. a chemical or
/// proppant used during an Operation).
/// </summary>
public sealed class Product
{
    public Guid Id { get; private set; }

    public Guid JobId { get; private set; }

    public string Name { get; private set; }

    public double Quantity { get; private set; }

    public string UnitOfMeasure { get; private set; }

    private Product()
    {
        Name = string.Empty;
        UnitOfMeasure = string.Empty;
    }

    public Product(
        Guid id,
        Guid jobId,
        string name,
        double quantity,
        string unitOfMeasure)
    {
        Id = id;
        JobId = jobId;
        Name = name;
        Quantity = quantity;
        UnitOfMeasure = unitOfMeasure;
    }
}
