using System.Text;
using DocumentFormat.OpenXml.Packaging;
using EnterpriseAiAssistant.Application.Ingestion;
using UglyToad.PdfPig;

namespace EnterpriseAiAssistant.Infrastructure.Documents;

/// <summary>
/// Handles .txt/.md uploads with no parsing required. Registered first
/// as the cheapest, most common case.
/// </summary>
public sealed class PlainTextDocumentExtractor : IDocumentTextExtractor
{
    public bool CanHandle(string fileName, string contentType) =>
        fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
        fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
        contentType.Equals("text/plain", StringComparison.OrdinalIgnoreCase);

    public async Task<string> ExtractTextAsync(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(content, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}

/// <summary>
/// Extracts the visible body text from a .docx maintenance manual using
/// the OpenXml SDK — no conversion, no external process required.
/// </summary>
public sealed class DocxDocumentExtractor : IDocumentTextExtractor
{
    public bool CanHandle(string fileName, string contentType) =>
        fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase);

    public Task<string> ExtractTextAsync(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        using var document = WordprocessingDocument.Open(content, isEditable: false);

        var body = document.MainDocumentPart?.Document?.Body;

        return Task.FromResult(body?.InnerText ?? string.Empty);
    }
}

/// <summary>
/// Extracts text page-by-page from a .pdf maintenance manual using
/// PdfPig (pure managed code, no native dependency).
/// </summary>
public sealed class PdfDocumentExtractor : IDocumentTextExtractor
{
    public bool CanHandle(string fileName, string contentType) =>
        fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
        contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);

    public Task<string> ExtractTextAsync(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        using var document = PdfDocument.Open(content);
        var sb = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            sb.AppendLine(page.Text);
        }

        return Task.FromResult(sb.ToString());
    }
}
