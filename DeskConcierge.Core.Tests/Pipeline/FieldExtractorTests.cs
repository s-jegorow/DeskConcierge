using DeskConcierge.Core.Pipeline;
using Xunit;

namespace DeskConcierge.Core.Tests.Pipeline;

public class FieldExtractorTests
{
    private readonly FieldExtractor _extractor = new();

    [Fact]
    public void Extract_EmptyText_ReturnsNoFields()
    {
        var fields = _extractor.Extract("");

        Assert.Null(fields.Iban);
        Assert.Null(fields.Date);
        Assert.Null(fields.Amount);
        Assert.Null(fields.InvoiceNumber);
    }

    [Fact]
    public void Extract_ValidIban_IsFoundAndSpacesRemoved()
    {
        var fields = _extractor.Extract("Bitte überweisen auf DE89 3704 0044 0532 0130 00.");

        Assert.NotNull(fields.Iban);
        Assert.Equal("DE89370400440532013000", fields.Iban!.Value);
    }

    [Fact]
    public void Extract_IbanWithBrokenChecksum_IsIgnored()
    {
        var fields = _extractor.Extract("IBAN DE89370400440532013001");

        Assert.Null(fields.Iban);
    }

    [Fact]
    public void Extract_SingleDate_HasHigherConfidenceThanAmbiguous()
    {
        var single = _extractor.Extract("Rechnungsdatum: 30.06.2026");
        var several = _extractor.Extract("Datum 01.01.2026, fällig am 15.02.2026.");

        Assert.NotNull(single.Date);
        Assert.NotNull(several.Date);
        Assert.Equal("30.06.2026", single.Date!.Value);
        Assert.Equal("01.01.2026", several.Date!.Value);
        Assert.True(single.Date.Confidence > several.Date.Confidence);
    }

    [Fact]
    public void Extract_PrefersFutureDateOverPast()
    {
        // past date appears first in text — should still return the future one (the deadline)
        var fields = _extractor.Extract("Ausgestellt am 01.01.2026, Zahlungsziel 30.12.2026.");

        Assert.NotNull(fields.Date);
        Assert.Equal("30.12.2026", fields.Date!.Value);
    }

    [Fact]
    public void Extract_InvalidOcrDate_IsDiscarded()
    {
        var fields = _extractor.Extract("Datum: 32.13.2026");

        Assert.Null(fields.Date);
    }

    [Fact]
    public void Extract_Amount_ReadsGermanFormat()
    {
        var fields = _extractor.Extract("Gesamtbetrag: 1.234,56 EUR");

        Assert.NotNull(fields.Amount);
        Assert.Equal("1.234,56", fields.Amount!.Value);
    }

    [Fact]
    public void Extract_Amount_PrefersLabelledTotalOverFirstLineItem()
    {
        var fields = _extractor.Extract("Grundgebühr 12,90 EUR Verbrauch 85,44 EUR Gesamtbetrag 1.234,56 EUR");

        Assert.NotNull(fields.Amount);
        Assert.Equal("1.234,56", fields.Amount!.Value);
    }

    [Fact]
    public void Extract_Amount_WithoutLabel_PicksLargest()
    {
        var fields = _extractor.Extract("Posten A 5,00 und Posten B 42,00 sowie C 9,99");

        Assert.NotNull(fields.Amount);
        Assert.Equal("42,00", fields.Amount!.Value);
    }

    [Fact]
    public void Extract_InvoiceNumber_NeedsLabel()
    {
        var labelled = _extractor.Extract("Rechnungsnummer: RG-2026-0815");
        var bare = _extractor.Extract("Vielen Dank für Ihren Einkauf, RG-2026-0815.");

        Assert.NotNull(labelled.InvoiceNumber);
        Assert.Equal("RG-2026-0815", labelled.InvoiceNumber!.Value);
        Assert.Null(bare.InvoiceNumber);
    }
}
