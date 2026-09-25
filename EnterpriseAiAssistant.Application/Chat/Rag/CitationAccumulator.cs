using EnterpriseAiAssistant.Application.Abstractions.Rag;

namespace EnterpriseAiAssistant.Application.Chat.Rag;

/// <summary>
/// Thread-safe, in-memory accumulator for the citations produced while
/// answering one chat turn. See <see cref="ICitationAccumulator"/> for
/// why <see cref="Reset"/> must be called explicitly at the start of
/// every turn rather than relying on DI scope disposal.
/// </summary>
public sealed class CitationAccumulator : ICitationAccumulator
{
    private readonly object _lock = new();
    private readonly List<Citation> _citations = [];

    public void Reset()
    {
        lock (_lock)
        {
            _citations.Clear();
        }
    }

    public void Record(Citation citation)
    {
        lock (_lock)
        {
            _citations.Add(citation);
        }
    }

    public IReadOnlyList<Citation> GetCitations()
    {
        lock (_lock)
        {
            return _citations.ToList();
        }
    }
}
