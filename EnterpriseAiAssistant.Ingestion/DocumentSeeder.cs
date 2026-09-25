using EnterpriseAiAssistant.Application.Ingestion;

namespace EnterpriseAiAssistant.Ingestion;

/// <summary>
/// A small reference documents ingested
/// automatically on startup —  so the Azure AI Search
/// document index is populated out of the box, the same way the
/// job-evidence already is.
/// DocumentIndexWriter writes chunks
/// with a filename-derived, deterministic id and upserts,
/// </summary>
public static class DocumentSeeder
{
    public static IReadOnlyList<DocumentIngestionRequest> GetSeedDocuments()
    {
        var manualText =
            "Flowback Safety Procedure: Before opening the choke manifold during flowback " +
            "operations, crew must verify wellhead pressure has dropped below 500 psi and " +
            "confirm two-way radio contact with the control room.\n\n" +
            "Wireline Logging Tool Maintenance: The manual requires that all wireline logging " +
            "tools be checked every 90 days, or within 30 days following any hard impact or " +
            "dropped-tool event, before being redeployed downhole.";

        return
        [
            new DocumentIngestionRequest(
                FileName: "manual.txt",
                ContentType: "text/plain",
                Content: System.Text.Encoding.UTF8.GetBytes(manualText))
        ];
    }
}
