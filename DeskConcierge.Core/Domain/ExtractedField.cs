namespace DeskConcierge.Core.Domain;

public sealed class ExtractedField
{
    public string Value { get; }
    public float Confidence { get; }

    public ExtractedField(string value, float confidence)
    {
        Value = value;
        Confidence = confidence;
    }
}
