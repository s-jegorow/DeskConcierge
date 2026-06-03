using DeskConcierge.Core.Domain;

namespace DeskConcierge.Core.Pipeline;

public sealed class IngestStage
{
    public Document Ingest(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = Sha256Hasher.Compute(stream);
        return new Document(filePath, hash);
    }
}
