namespace EnterpriseAiAssistant.Domain.Jobs;

/// <summary>
/// A single execution/attempt within an Operation
/// (e.g. "Run 1", "Run 2"). Personnel are assigned to a Run.
/// </summary>
public sealed class Run
{
    public Guid Id { get; private set; }

    public Guid OperationId { get; private set; }

    public string Name { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public string Result { get; private set; }

    private Run()
    {
        Name = string.Empty;
        Result = string.Empty;
    }

    public Run(
        Guid id,
        Guid operationId,
        string name,
        DateTimeOffset startedAt,
        DateTimeOffset? completedAt,
        string result)
    {
        Id = id;
        OperationId = operationId;
        Name = name;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        Result = result;
    }
}
