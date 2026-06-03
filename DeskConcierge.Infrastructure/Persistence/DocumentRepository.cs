using DeskConcierge.Core.Abstractions;
using DeskConcierge.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace DeskConcierge.Infrastructure.Persistence;

public sealed class DocumentRepository : IDocumentRepository
{
    private readonly DeskConciergeDbContext _db;

    public DocumentRepository(DeskConciergeDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        _db.Documents.Add(document);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<Document?> FindByHashAsync(string contentHash, CancellationToken cancellationToken = default)
    {
        return _db.Documents.FirstOrDefaultAsync(d => d.ContentHash == contentHash, cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // SQLite can't ORDER BY a DateTimeOffset, so we sort in memory — fine for a small archive
        var documents = await _db.Documents
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return documents
            .OrderByDescending(d => d.CreatedAt)
            .ToList();
    }

    public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }
}
