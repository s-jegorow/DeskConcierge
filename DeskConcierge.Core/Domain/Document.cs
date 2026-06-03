namespace DeskConcierge.Core.Domain;

public sealed class Document
{
    public Guid Id { get; }
    public string OriginalPath { get; }
    public DateTimeOffset CreatedAt { get; }
    public string ContentHash { get; }
    public string? OcrText { get; private set; }
    public float? OcrConfidence { get; private set; }

    public Document(string originalPath, string contentHash)
    {
        if (string.IsNullOrWhiteSpace(originalPath))
            throw new ArgumentException("Original path must not be empty.", nameof(originalPath));
        if (string.IsNullOrWhiteSpace(contentHash))
            throw new ArgumentException("Content hash must not be empty.", nameof(contentHash));

        Id = Guid.NewGuid();
        OriginalPath = originalPath;
        ContentHash = contentHash;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void ApplyOcr(string text, float confidence)
    {
        OcrText = text;
        OcrConfidence = confidence;
    }
}
