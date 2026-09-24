namespace EnterpriseAiAssistant.Domain.Jobs;

/// <summary>
/// A named production field. A Field contains one or more Wells.
/// Part of the SQL Server JOB database (the single source of truth for
/// all structured job data).
/// </summary>
public sealed class Field
{
    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string Region { get; private set; }

    private Field()
    {
        Name = string.Empty;
        Region = string.Empty;
    }

    public Field(Guid id, string name, string region)
    {
        Id = id;
        Name = name;
        Region = region;
    }
}

/// <summary>
/// A physical well within a Field. Jobs are performed on a Well.
/// </summary>
public sealed class Well
{
    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public Guid FieldId { get; private set; }

    public Field? Field { get; private set; }

    public double? DepthFeet { get; private set; }

    private Well()
    {
        Name = string.Empty;
    }

    public Well(Guid id, string name, Guid fieldId, double? depthFeet)
    {
        Id = id;
        Name = name;
        FieldId = fieldId;
        DepthFeet = depthFeet;
    }
}
