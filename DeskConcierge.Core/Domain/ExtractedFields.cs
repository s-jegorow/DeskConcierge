namespace DeskConcierge.Core.Domain;

public sealed class ExtractedFields
{
    public ExtractedField? Iban { get; set; }
    public ExtractedField? Date { get; set; }
    public ExtractedField? Amount { get; set; }
    public ExtractedField? InvoiceNumber { get; set; }
}
