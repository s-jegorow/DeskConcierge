namespace DeskConcierge.Core.Abstractions;

public interface IDocumentStorage
{
    Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default);
}
