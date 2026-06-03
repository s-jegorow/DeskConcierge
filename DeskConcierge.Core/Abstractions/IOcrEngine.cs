namespace DeskConcierge.Core.Abstractions;

public interface IOcrEngine
{
    Task<OcrResult> ReadAsync(string filePath, CancellationToken cancellationToken = default);
}

public sealed record OcrResult(string Text, float MeanConfidence);
