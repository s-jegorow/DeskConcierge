using DeskConcierge.Core.Domain;

namespace DeskConcierge.Core.Abstractions;

public interface IDocumentAnalyzer
{
    // null when there's nothing to analyse (empty text)
    Task<DocumentAnalysis?> AnalyzeAsync(string ocrText, CancellationToken cancellationToken = default);
}
