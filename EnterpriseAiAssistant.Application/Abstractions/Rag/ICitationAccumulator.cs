namespace EnterpriseAiAssistant.Application.Abstractions.Rag;

/// <summary>
/// Retrieval tool call that returned usable data, ready to be shown
/// to the user as a source citation.
/// </summary>
public sealed record Citation(
    string SourceSystem,
    string Query,
    string Preview);

/// <summary>
/// Collects citations across every plugin call made while answering the
/// current chat turn, so ChatService can show the User exactly which
/// systems (SQL Job Database, Cosmos JobGraph, Azure Vector Search,
/// uploaded documents) contributed to the answer.
/// </summary>
public interface ICitationAccumulator
{
    void Reset();

    void Record(Citation citation);

    IReadOnlyList<Citation> GetCitations();
}
