using DeskConcierge.Core.Domain;

namespace DeskConcierge.Core.Abstractions;

public interface IDocumentArchive
{
    // moves the document's file into the human-readable archive, writes a sidecar, returns the new path
    Task<string> FileAsync(Document document, CancellationToken cancellationToken = default);
}
