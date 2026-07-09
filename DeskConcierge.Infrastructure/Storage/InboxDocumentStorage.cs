using DeskConcierge.Core.Abstractions;

namespace DeskConcierge.Infrastructure.Storage;

public sealed class InboxDocumentStorage : IDocumentStorage
{
    // TODO: inbox path is hardcoded for now, move to config
    private const string InboxPath = "inbox";

    public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(InboxPath);

        // fixed it: strip any directory parts from the filename, otherwise something like ../../etc/passwd would escape the inbox
        var safeName = Path.GetFileName(fileName);
        var storedPath = Path.Combine(InboxPath, $"{Guid.NewGuid():N}_{safeName}");

        await using var target = File.Create(storedPath);
        await content.CopyToAsync(target, cancellationToken);

        return Path.GetFullPath(storedPath);
    }
}
