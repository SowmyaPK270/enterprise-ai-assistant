namespace EnterpriseAiAssistant.Infrastructure.Search;

/// <summary>
/// A deliberately simple character-based chunker: fixed-size windows
/// with a small overlap so context isn't lost at chunk boundaries. 
/// </summary>
internal static class TextChunker
{
    public static IReadOnlyList<string> Chunk(
        string text,
        int maxChunkSize = 1200,
        int overlap = 150)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var normalized = text.Trim();

        if (normalized.Length <= maxChunkSize)
        {
            return [normalized];
        }

        var chunks = new List<string>();
        var start = 0;

        while (start < normalized.Length)
        {
            var length = Math.Min(maxChunkSize, normalized.Length - start);
            chunks.Add(normalized.Substring(start, length));

            if (start + length >= normalized.Length)
            {
                break;
            }

            start += maxChunkSize - overlap;
        }

        return chunks;
    }
}
