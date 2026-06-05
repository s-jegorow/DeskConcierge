using DeskConcierge.Infrastructure.Llm;
using Xunit;

namespace DeskConcierge.Infrastructure.Tests.Llm;

public class OllamaDocumentAnalyzerTests
{
    [Fact]
    public void Parse_FullResponse_MapsEveryField()
    {
        const string json = """
            {
              "absender": "Telekommunikation Nord AG",
              "typ": "Rechnung",
              "zusammenfassung": "Monatliche Mobilfunkrechnung.",
              "termine": [{ "datum": "18.06.2026", "anlass": "Zahlungsziel" }],
              "handlungsbedarf": true
            }
            """;

        var analysis = OllamaDocumentAnalyzer.Parse(json);

        Assert.NotNull(analysis);
        Assert.Equal("Telekommunikation Nord AG", analysis!.Sender);
        Assert.Equal("Rechnung", analysis.DocumentType);
        Assert.Equal("Monatliche Mobilfunkrechnung.", analysis.Summary);
        Assert.True(analysis.ActionRequired);
        var appointment = Assert.Single(analysis.Appointments);
        Assert.Equal("18.06.2026", appointment.Date);
        Assert.Equal("Zahlungsziel", appointment.Subject);
    }

    [Fact]
    public void Parse_NullSender_BecomesNull()
    {
        const string json = """
            { "absender": null, "typ": "Werbung", "zusammenfassung": "Prospekt.",
              "termine": [], "handlungsbedarf": false }
            """;

        var analysis = OllamaDocumentAnalyzer.Parse(json);

        Assert.NotNull(analysis);
        Assert.Null(analysis!.Sender);
        Assert.Empty(analysis.Appointments);
    }

    [Fact]
    public void Parse_EmptyType_FallsBackToSonstiges()
    {
        const string json = """
            { "absender": "Amt", "typ": "", "zusammenfassung": "",
              "termine": [], "handlungsbedarf": false }
            """;

        var analysis = OllamaDocumentAnalyzer.Parse(json);

        Assert.NotNull(analysis);
        Assert.Equal("Sonstiges", analysis!.DocumentType);
    }

    [Fact]
    public void Parse_AppointmentWithoutDate_IsDropped()
    {
        const string json = """
            { "absender": "Amt", "typ": "Behördenbrief", "zusammenfassung": "Frist.",
              "termine": [{ "datum": "", "anlass": "leer" },
                          { "datum": "30.06.2026", "anlass": "Antwort" }],
              "handlungsbedarf": true }
            """;

        var analysis = OllamaDocumentAnalyzer.Parse(json);

        Assert.NotNull(analysis);
        var appointment = Assert.Single(analysis!.Appointments);
        Assert.Equal("30.06.2026", appointment.Date);
    }
}
