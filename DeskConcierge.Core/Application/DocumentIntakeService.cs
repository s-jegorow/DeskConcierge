using DeskConcierge.Core.Abstractions;
using DeskConcierge.Core.Domain;
using DeskConcierge.Core.Pipeline;
using Microsoft.Extensions.Logging;

namespace DeskConcierge.Core.Application;

public enum IngestOutcome
{
    Created,
    Duplicate
}

public sealed record IngestResult(IngestOutcome Outcome, Document Document);

public sealed class DocumentIntakeService
{
    private readonly IDocumentRepository _repository;
    private readonly IDocumentStorage _storage;
    private readonly IOcrEngine _ocr;
    private readonly FieldExtractor _extractor;
    private readonly IDocumentAnalyzer _analyzer;
    private readonly IDocumentArchive _archive;
    private readonly ILogger<DocumentIntakeService> _logger;

    public DocumentIntakeService(IDocumentRepository repository, IDocumentStorage storage, IOcrEngine ocr, FieldExtractor extractor, IDocumentAnalyzer analyzer, IDocumentArchive archive, ILogger<DocumentIntakeService> logger)
    {
        _repository = repository;
        _storage = storage;
        _ocr = ocr;
        _extractor = extractor;
        _analyzer = analyzer;
        _archive = archive;
        _logger = logger;
    }

    public async Task<IngestResult> IngestAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var hash = Sha256Hasher.Compute(content);

        var existing = await _repository.FindByHashAsync(hash, cancellationToken);
        if (existing is not null)
        {
            _logger.LogInformation("duplicate upload ignored: {FileName} (hash {Hash})", fileName, hash[..8]);
            return new IngestResult(IngestOutcome.Duplicate, existing);
        }

        // hash before writing, so a duplicate never touches the inbox
        content.Position = 0;
        var storedPath = await _storage.SaveAsync(content, fileName, cancellationToken);
        _logger.LogInformation("stored {FileName} → {Path}", fileName, storedPath);

        var document = new Document(storedPath, hash);

        // ocr inline on upload for now — really a background worker's job
        var ocr = await _ocr.ReadAsync(storedPath, cancellationToken);
        document.ApplyOcr(ocr.Text, ocr.MeanConfidence);
        document.ApplyExtraction(_extractor.Extract(ocr.Text));

        // llm understanding — a fallback, so a model that's down must not sink the upload
        try
        {
            var analysis = await _analyzer.AnalyzeAsync(ocr.Text, cancellationToken);
            if (analysis is not null)
                document.ApplyAnalysis(analysis);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "llm analysis failed for {FileName} — document saved without analysis", fileName);
        }

        // move the original out of the inbox into the readable archive + drop a sidecar
        var archivedPath = await _archive.FileAsync(document, cancellationToken);
        document.Relocate(archivedPath);

        await _repository.AddAsync(document, cancellationToken);
        _logger.LogInformation("ingested {FileName} → {ArchivedPath} (ocr {Confidence:P0})", fileName, archivedPath, ocr.MeanConfidence / 100f);
        return new IngestResult(IngestOutcome.Created, document);
    }
}
