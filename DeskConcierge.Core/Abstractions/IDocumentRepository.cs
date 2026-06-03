using DeskConcierge.Core.Domain;

namespace DeskConcierge.Core.Abstractions;

public interface IDocumentRepository
{
    Task AddAsync(Document document, CancellationToken cancellationToken = default);

    Task<Document?> FindByHashAsync(string contentHash, CancellationToken cancellationToken = default);
}
