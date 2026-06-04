namespace DeskConcierge.Core.Domain;

public sealed class Document
{
    public Guid Id { get; }
    public string OriginalPath { get; }
    public DateTimeOffset CreatedAt { get; }
    public string ContentHash { get; }
    public string? OcrText { get; private set; }
    public float? OcrConfidence { get; private set; }

    // flat columns on the document for now, own extraction table can come later
    public string? Iban { get; private set; }
    public float? IbanConfidence { get; private set; }
    public string? Date { get; private set; }
    public float? DateConfidence { get; private set; }
    public string? Amount { get; private set; }
    public float? AmountConfidence { get; private set; }
    public string? InvoiceNumber { get; private set; }
    public float? InvoiceNumberConfidence { get; private set; }

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

    public void ApplyExtraction(ExtractedFields fields)
    {
        Iban = fields.Iban?.Value;
        IbanConfidence = fields.Iban?.Confidence;
        Date = fields.Date?.Value;
        DateConfidence = fields.Date?.Confidence;
        Amount = fields.Amount?.Value;
        AmountConfidence = fields.Amount?.Confidence;
        InvoiceNumber = fields.InvoiceNumber?.Value;
        InvoiceNumberConfidence = fields.InvoiceNumber?.Confidence;
    }
}
