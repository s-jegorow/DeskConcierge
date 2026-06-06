using DeskConcierge.Core.Abstractions;
using DeskConcierge.Core.Domain;
using DeskConcierge.Core.Pipeline;

namespace DeskConcierge.Core.Application;

// runs documents that still sit in the inbox back through the pipeline and files them into the archive
public sealed class DocumentReprocessor
{
    private readonly IDocumentRepository _repository;
    private readonly IOcrEngine _ocr;
    private readonly FieldExtractor _extractor;
    private readonly IDocumentAnalyzer _analyzer;
    private readonly IDocumentArchive _archive;

    public DocumentReprocessor(IDocumentRepository repository, IOcrEngine ocr, FieldExtractor extractor, IDocumentAnalyzer analyzer, IDocumentArchive archive)
    {
        _repository = repository;
        _ocr = ocr;
        _extractor = extractor;
        _analyzer = analyzer;
        _archive = archive;
    }

    public async Task<int> ReprocessInboxAsync(CancellationToken cancellationToken = default)
    {
        var documents = await _repository.GetAllAsync(cancellationToken);
        var reprocessed = 0;

        foreach (var document in documents)
        {
            if (!IsInInbox(document.OriginalPath) || !File.Exists(document.OriginalPath))
                continue;

            var ocr = await _ocr.ReadAsync(document.OriginalPath, cancellationToken);
            document.ApplyOcr(ocr.Text, ocr.MeanConfidence);
            document.ApplyExtraction(_extractor.Extract(ocr.Text));

            // same fallback contract as intake: a model that's down must not block the rest
            try
            {
                var analysis = await _analyzer.AnalyzeAsync(ocr.Text, cancellationToken);
                if (analysis is not null)
                    document.ApplyAnalysis(analysis);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
            }

            var archivedPath = await _archive.FileAsync(document, cancellationToken);
            document.Relocate(archivedPath);

            await _repository.UpdateAsync(document, cancellationToken);
            reprocessed++;
        }

        return reprocessed;
    }

    // a leftover lives in the inbox folder; archived files sit under the store
    private static bool IsInInbox(string path)
        => path.Split('/', '\\').Any(segment => segment.Equals("inbox", StringComparison.OrdinalIgnoreCase));
}
