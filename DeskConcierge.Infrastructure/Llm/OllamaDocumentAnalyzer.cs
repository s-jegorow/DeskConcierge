using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeskConcierge.Core.Abstractions;
using DeskConcierge.Core.Domain;
using Microsoft.Extensions.Logging;

namespace DeskConcierge.Infrastructure.Llm;

public sealed class OllamaDocumentAnalyzer : IDocumentAnalyzer
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly ILogger<OllamaDocumentAnalyzer> _logger;

    public OllamaDocumentAnalyzer(HttpClient http, OllamaOptions options, ILogger<OllamaDocumentAnalyzer> logger)
    {
        _http = http;
        _model = options.Model;
        _http.BaseAddress ??= new Uri(options.Endpoint);
        _logger = logger;
    }

    public async Task<DocumentAnalysis?> AnalyzeAsync(string ocrText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ocrText))
            return null;

        _logger.LogDebug("sending {Chars} chars to {Model}", ocrText.Length, _model);

        var request = new
        {
            model = _model,
            stream = false,
            format = ResponseSchema,
            options = new { temperature = 0 },
            messages = new[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = $"OCR-Text:\n\"\"\"\n{ocrText}\n\"\"\"" }
            }
        };

        using var response = await _http.PostAsJsonAsync("/api/chat", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken);
        var content = envelope?.Message?.Content;
        var result = content is null ? null : Parse(content);
        _logger.LogDebug("analysis done: sender {Sender}, type {Type}", result?.Sender ?? "unknown", result?.DocumentType ?? "unknown");
        return result;
    }

    // parsing split out so it's testable without a running model
    public static DocumentAnalysis? Parse(string json)
    {
        var dto = JsonSerializer.Deserialize<AnalysisDto>(json);
        if (dto is null)
            return null;

        var appointments = (dto.Termine ?? [])
            .Where(t => !string.IsNullOrWhiteSpace(t.Datum))
            .Select(t => new Appointment(t.Datum!.Trim(), (t.Anlass ?? string.Empty).Trim()))
            .ToList();

        var sender = string.IsNullOrWhiteSpace(dto.Absender) ? null : dto.Absender.Trim();
        var type = string.IsNullOrWhiteSpace(dto.Typ) ? "Sonstiges" : dto.Typ.Trim();

        return new DocumentAnalysis(sender, type, (dto.Zusammenfassung ?? string.Empty).Trim(), appointments, dto.Handlungsbedarf);
    }

    private const string SystemPrompt = """
        Du analysierst eingescannte deutsche Haushaltspost. Eingabe ist der OCR-Text
        eines Dokuments; er kann Lese- und Trennfehler enthalten.

        Gib ausschließlich JSON nach dem Schema zurück. Trage nur ein, was im Text
        belegbar steht. Ist ein Feld nicht erkennbar, nutze null bzw. eine leere Liste.
        Rate nicht, erfinde nichts.

        Felder:
        - absender: Firma, Behörde oder Person, die das Dokument schickt (meist im
          Briefkopf). null, wenn unklar.
        - typ: Dokumentart in 1–2 Wörtern (z. B. Rechnung, Mahnung, Behördenbrief,
          Versicherung, Vertrag, Werbung, Privatbrief). "Sonstiges", wenn unklar.
        - zusammenfassung: 1–2 sachliche Sätze auf Deutsch, worum es geht.
        - termine: alle Datumsangaben mit Bedeutung (Frist, Zahlungsziel, Termin) als
          { "datum": "TT.MM.JJJJ", "anlass": "kurz" }. Leere Liste, wenn keine.
        - handlungsbedarf: true, wenn der Empfänger handeln muss (zahlen, antworten,
          Frist wahren), sonst false.
        """;

    // json schema handed to ollama's "format" so the reply is always valid
    private static readonly object ResponseSchema = new
    {
        type = "object",
        properties = new
        {
            absender = new { type = new[] { "string", "null" } },
            typ = new { type = "string" },
            zusammenfassung = new { type = "string" },
            termine = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        datum = new { type = "string" },
                        anlass = new { type = "string" }
                    },
                    required = new[] { "datum", "anlass" }
                }
            },
            handlungsbedarf = new { type = "boolean" }
        },
        required = new[] { "absender", "typ", "zusammenfassung", "termine", "handlungsbedarf" }
    };

    private sealed class OllamaChatResponse
    {
        [JsonPropertyName("message")] public OllamaMessage? Message { get; set; }
    }

    private sealed class OllamaMessage
    {
        [JsonPropertyName("content")] public string? Content { get; set; }
    }

    private sealed class AnalysisDto
    {
        [JsonPropertyName("absender")] public string? Absender { get; set; }
        [JsonPropertyName("typ")] public string? Typ { get; set; }
        [JsonPropertyName("zusammenfassung")] public string? Zusammenfassung { get; set; }
        [JsonPropertyName("termine")] public List<TerminDto>? Termine { get; set; }
        [JsonPropertyName("handlungsbedarf")] public bool Handlungsbedarf { get; set; }
    }

    private sealed class TerminDto
    {
        [JsonPropertyName("datum")] public string? Datum { get; set; }
        [JsonPropertyName("anlass")] public string? Anlass { get; set; }
    }
}
