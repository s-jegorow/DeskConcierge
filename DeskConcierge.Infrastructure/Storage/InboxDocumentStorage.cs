using DeskConcierge.Core.Abstractions;

namespace DeskConcierge.Infrastructure.Storage;

public sealed class InboxDocumentStorage : IDocumentStorage
{
    // TODO: inbox path is hardcoded for now — move to config
    private const string InboxPath = "inbox";

    public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(InboxPath);

        // keeping the original filename verbatim — might need sanitizing later (path separators, duplicates)
        var storedPath = Path.Combine(InboxPath, $"{Guid.NewGuid():N}_{fileName}");

        await using var target = File.Create(storedPath);
        await content.CopyToAsync(target, cancellationToken);

        return Path.GetFullPath(storedPath);
    }
}
