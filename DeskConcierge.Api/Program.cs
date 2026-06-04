using DeskConcierge.Core.Abstractions;
using DeskConcierge.Core.Application;
using DeskConcierge.Core.Pipeline;
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
builder.Services.AddScoped<IOcrEngine, TesseractOcrEngine>();
builder.Services.AddScoped<FieldExtractor>();
builder.Services.AddScoped<DocumentIntakeService>();

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
        OcrPreview = d.OcrText is { Length: > 160 } text ? text[..160] + "…" : d.OcrText
    });
    return Results.Ok(summaries);
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
            document.InvoiceNumberConfidence
        });
});

app.Run();
