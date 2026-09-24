namespace EnterpriseAiAssistant.Domain.Jobs;

/// <summary>
/// A discrete phase of work carried out as part of a Job
/// (e.g. "Perforation", "Stimulation", "Flowback"). A Job has one or
/// more Operations, and an Operation has one or more Runs.
/// </summary>
public sealed class Operation
{
    public Guid Id { get; private set; }

    public Guid JobId { get; private set; }

    public string Name { get; private set; }

    public string Description { get; private set; }

    public int SequenceNumber { get; private set; }

    private Operation()
    {
        Name = string.Empty;
        Description = string.Empty;
    }

    public Operation(
        Guid id,
        Guid jobId,
        string name,
        string description,
        int sequenceNumber)
    {
        Id = id;
        JobId = jobId;
        Name = name;
        Description = description;
        SequenceNumber = sequenceNumber;
    }
}
