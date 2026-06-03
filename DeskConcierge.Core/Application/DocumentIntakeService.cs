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

    public DocumentIntakeService(IDocumentRepository repository, IDocumentStorage storage)
    {
        _repository = repository;
        _storage = storage;
    }

    public async Task<IngestResult> IngestAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var hash = Sha256Hasher.Compute(content);

        var existing = await _repository.FindByHashAsync(hash, cancellationToken);
        if (existing is not null)
            return new IngestResult(IngestOutcome.Duplicate, existing);

        // hashing the upload before writing it to disk, so a duplicate never touches the inbox
        // — not 100% sure the extra rewind is worth it for very large files
        content.Position = 0;
        var storedPath = await _storage.SaveAsync(content, fileName, cancellationToken);

        var document = new Document(storedPath, hash);
        await _repository.AddAsync(document, cancellationToken);
        return new IngestResult(IngestOutcome.Created, document);
    }
}
