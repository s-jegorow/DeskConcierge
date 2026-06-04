using System.Text.RegularExpressions;
using DeskConcierge.Core.Domain;

namespace DeskConcierge.Core.Pipeline;

public sealed class FieldExtractor
{
    // german iban: DE + 2 check digits + 18 digits, spaces allowed
    private static readonly Regex IbanPattern = new(@"DE\d{2}(?:\s?\d){18}", RegexOptions.Compiled);

    // dates like 30.06.2026
    private static readonly Regex DatePattern = new(@"\b\d{1,2}\.\d{1,2}\.\d{2,4}\b", RegexOptions.Compiled);

    // german amounts like 1.234,56
    private static readonly Regex AmountPattern = new(@"\b\d{1,3}(?:\.\d{3})*,\d{2}\b", RegexOptions.Compiled);

    private static readonly Regex InvoicePattern = new(
        @"(?:Rechnungsnummer|Rechnungs-?Nr\.?|Rg\.?-?Nr\.?|Rechnung\s+Nr\.?)\s*[:.]?\s*([A-Za-z0-9][A-Za-z0-9\-/]*)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ExtractedFields Extract(string text)
    {
        var fields = new ExtractedFields();
        if (string.IsNullOrWhiteSpace(text))
            return fields;

        fields.Iban = FindIban(text);
        fields.Date = FindDate(text);
        fields.Amount = FindAmount(text);
        fields.InvoiceNumber = FindInvoiceNumber(text);
        return fields;
    }

    private static ExtractedField? FindIban(string text)
    {
        foreach (Match match in IbanPattern.Matches(text))
        {
            var candidate = match.Value.Replace(" ", "");
            if (IsValidIban(candidate))
                return new ExtractedField(candidate, 0.95f);
        }
        return null;
    }

    private static ExtractedField? FindDate(string text)
    {
        var matches = DatePattern.Matches(text);
        if (matches.Count == 0)
            return null;

        // letters often carry several dates — take the first, trust it less
        var confidence = matches.Count == 1 ? 0.8f : 0.5f;
        return new ExtractedField(matches[0].Value, confidence);
    }

    private static ExtractedField? FindAmount(string text)
    {
        var matches = AmountPattern.Matches(text);
        if (matches.Count == 0)
            return null;

        // same as dates — several are common, take the first
        var confidence = matches.Count == 1 ? 0.7f : 0.5f;
        return new ExtractedField(matches[0].Value, confidence);
    }

    private static ExtractedField? FindInvoiceNumber(string text)
    {
        var match = InvoicePattern.Match(text);
        if (!match.Success)
            return null;

        // label's there, so fairly sure
        return new ExtractedField(match.Groups[1].Value, 0.85f);
    }

    private static bool IsValidIban(string iban)
    {
        if (iban.Length != 22)
            return false;

        // iban mod-97 check (ISO 7064)
        var rearranged = iban[4..] + iban[..4];
        var remainder = 0;
        foreach (var ch in rearranged)
        {
            int value;
            if (char.IsDigit(ch))
                value = ch - '0';
            else if (char.IsLetter(ch))
                value = char.ToUpperInvariant(ch) - 'A' + 10;
            else
                return false;

            remainder = value > 9
                ? (remainder * 100 + value) % 97
                : (remainder * 10 + value) % 97;
        }
        return remainder == 1;
    }
}
