using DeskConcierge.Core.Abstractions;
using DeskConcierge.Core.Domain;
using DeskConcierge.Core.Pipeline;

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

    public DocumentIntakeService(IDocumentRepository repository, IDocumentStorage storage, IOcrEngine ocr, FieldExtractor extractor)
    {
        _repository = repository;
        _storage = storage;
        _ocr = ocr;
        _extractor = extractor;
    }

    public async Task<IngestResult> IngestAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var hash = Sha256Hasher.Compute(content);

        var existing = await _repository.FindByHashAsync(hash, cancellationToken);
        if (existing is not null)
            return new IngestResult(IngestOutcome.Duplicate, existing);

        // hash before writing, so a duplicate never touches the inbox
        content.Position = 0;
        var storedPath = await _storage.SaveAsync(content, fileName, cancellationToken);

        var document = new Document(storedPath, hash);

        // ocr inline on upload for now — really a background worker's job
        var ocr = await _ocr.ReadAsync(storedPath, cancellationToken);
        document.ApplyOcr(ocr.Text, ocr.MeanConfidence);
        document.ApplyExtraction(_extractor.Extract(ocr.Text));

        await _repository.AddAsync(document, cancellationToken);
        return new IngestResult(IngestOutcome.Created, document);
    }
}
