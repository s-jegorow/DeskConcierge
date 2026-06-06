using System.Text.Json;
using DeskConcierge.Core.Abstractions;
using DeskConcierge.Core.Application;
using DeskConcierge.Core.Domain;
using DeskConcierge.Core.Pipeline;
using DeskConcierge.Infrastructure.Llm;
using DeskConcierge.Infrastructure.Ocr;
using DeskConcierge.Infrastructure.Persistence;
using DeskConcierge.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DeskConcierge")
    ?? "Data Source=deskconcierge.db";

builder.Services.AddDbContext<DeskConciergeDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IDocumentStorage, InboxDocumentStorage>();
builder.Services.AddScoped<IDocumentArchive, FileSystemDocumentArchive>();
builder.Services.AddScoped<IOcrEngine, TesseractOcrEngine>();
builder.Services.AddScoped<FieldExtractor>();
builder.Services.AddScoped<DocumentIntakeService>();
builder.Services.AddScoped<DocumentReprocessor>();

var ollamaOptions = builder.Configuration.GetSection("Llm").Get<OllamaOptions>() ?? new OllamaOptions();
builder.Services.AddSingleton(ollamaOptions);
builder.Services.AddHttpClient<IDocumentAnalyzer, OllamaDocumentAnalyzer>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DeskConciergeDbContext>();
    db.Database.Migrate(); // fine for dev, revisit before production
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", () => new { status = "ok" });

app.MapPost("/api/documents", async (IFormFile file, DocumentIntakeService intake, CancellationToken ct) =>
{
    await using var stream = file.OpenReadStream();
    var result = await intake.IngestAsync(stream, file.FileName, ct);

    var dto = new
    {
        result.Document.Id,
        result.Document.OriginalPath,
        result.Document.CreatedAt,
        result.Document.ContentHash
    };

    // open question: 409 Conflict or 200 with the existing doc? going with 409 for now
    return result.Outcome == IngestOutcome.Created
        ? Results.Created($"/api/documents/{result.Document.Id}", dto)
        : Results.Conflict(dto);
}).DisableAntiforgery();

app.MapGet("/api/documents", async (IDocumentRepository repository, CancellationToken ct) =>
{
    var documents = await repository.GetAllAsync(ct);
    var summaries = documents.Select(d => new
    {
        d.Id,
        d.OriginalPath,
        d.CreatedAt,
        d.ContentHash,
        d.OcrConfidence,
        d.Iban,
        d.Date,
        d.Amount,
        d.InvoiceNumber,
        d.Sender,
        d.DocumentType,
        d.Summary,
        d.ActionRequired,
        Appointments = ParseAppointments(d.AppointmentsJson),
        // full text so the frontend can search across it; move to a server-side query/FTS once the archive grows
        d.OcrText
    });
    return Results.Ok(summaries);
});

// re-run the pipeline on leftovers still sitting in the inbox (predate the archive stage)
app.MapPost("/api/documents/reprocess", async (DocumentReprocessor reprocessor, CancellationToken ct) =>
{
    var count = await reprocessor.ReprocessInboxAsync(ct);
    return Results.Ok(new { reprocessed = count });
});

app.MapGet("/api/documents/{id:guid}", async (Guid id, IDocumentRepository repository, CancellationToken ct) =>
{
    var document = await repository.GetByIdAsync(id, ct);
    return document is null
        ? Results.NotFound()
        : Results.Ok(new
        {
            document.Id,
            document.OriginalPath,
            document.CreatedAt,
            document.ContentHash,
            document.OcrConfidence,
            document.OcrText,
            document.Iban,
            document.IbanConfidence,
            document.Date,
            document.DateConfidence,
            document.Amount,
            document.AmountConfidence,
            document.InvoiceNumber,
            document.InvoiceNumberConfidence,
            document.Sender,
            document.DocumentType,
            document.Summary,
            document.ActionRequired,
            Appointments = ParseAppointments(document.AppointmentsJson)
        });
});

// serve the stored original so the frontend can show it; path comes from the db, not the caller
app.MapGet("/api/documents/{id:guid}/original", async (Guid id, IDocumentRepository repository, CancellationToken ct) =>
{
    var document = await repository.GetByIdAsync(id, ct);
    if (document is null || !File.Exists(document.OriginalPath))
        return Results.NotFound();

    var stream = File.OpenRead(document.OriginalPath);
    return Results.File(stream, ContentTypeFor(document.OriginalPath), Path.GetFileName(document.OriginalPath), enableRangeProcessing: true);
});

static Appointment[] ParseAppointments(string? json)
    => string.IsNullOrEmpty(json) ? [] : JsonSerializer.Deserialize<Appointment[]>(json) ?? [];

static string ContentTypeFor(string path) => Path.GetExtension(path).ToLowerInvariant() switch
{
    ".pdf" => "application/pdf",
    ".png" => "image/png",
    ".jpg" or ".jpeg" => "image/jpeg",
    ".tif" or ".tiff" => "image/tiff",
    ".webp" => "image/webp",
    _ => "application/octet-stream"
};

// smoke test for the llm stage — paste ocr text, see what the model makes of it
app.MapPost("/api/analyze", async (AnalyzeRequest body, IDocumentAnalyzer analyzer, CancellationToken ct) =>
{
    var analysis = await analyzer.AnalyzeAsync(body.Text, ct);
    return analysis is null ? Results.NoContent() : Results.Ok(analysis);
});

app.Run();

record AnalyzeRequest(string Text);
