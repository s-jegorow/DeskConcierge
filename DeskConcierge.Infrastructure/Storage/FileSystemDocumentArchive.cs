using System.Text.Encodings.Web;
using System.Text.Json;
using DeskConcierge.Core.Abstractions;
using DeskConcierge.Core.Domain;
using DeskConcierge.Core.Pipeline;

namespace DeskConcierge.Infrastructure.Storage;

public sealed class FileSystemDocumentArchive : IDocumentArchive
{
    // TODO: store root hardcoded for now, move to config alongside the inbox path
    private const string StorePath = "store";

    // relaxed encoder so umlauts stay readable in the sidecar instead of ü escapes
    private static readonly JsonSerializerOptions SidecarJson = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<string> FileAsync(Document document, CancellationToken cancellationToken = default)
    {
        var source = document.OriginalPath;
        var relative = ArchivePathBuilder.BuildRelativePath(
            document.CreatedAt, document.Sender, document.InvoiceNumber, document.ContentHash, Path.GetExtension(source));

        var target = Path.Combine(StorePath, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);

        // content hash already dedups identical uploads; guard anyway against a name clash
        if (File.Exists(target))
            target = MakeUnique(target, document.ContentHash);

        File.Move(source, target);
        await WriteSidecarAsync(document, target, cancellationToken);

        return Path.GetFullPath(target);
    }

    private static string MakeUnique(string target, string contentHash)
    {
        var dir = Path.GetDirectoryName(target)!;
        var name = Path.GetFileNameWithoutExtension(target);
        var ext = Path.GetExtension(target);
        var suffix = contentHash.Length >= 8 ? contentHash[..8] : contentHash;
        return Path.Combine(dir, $"{name}_{suffix}{ext}");
    }

    // open format next to the original: readable without the app, lets the db be rebuilt
    private static async Task WriteSidecarAsync(Document document, string target, CancellationToken cancellationToken)
    {
        Appointment[] appointments = string.IsNullOrEmpty(document.AppointmentsJson)
            ? []
            : JsonSerializer.Deserialize<Appointment[]>(document.AppointmentsJson) ?? [];

        var sidecar = new
        {
            id = document.Id,
            contentHash = document.ContentHash,
            uploadedAt = document.CreatedAt,
            ocr = new { confidence = document.OcrConfidence, text = document.OcrText },
            fields = new
            {
                iban = document.Iban,
                date = document.Date,
                amount = document.Amount,
                invoiceNumber = document.InvoiceNumber
            },
            analysis = new
            {
                sender = document.Sender,
                type = document.DocumentType,
                summary = document.Summary,
                actionRequired = document.ActionRequired,
                appointments
            }
        };

        var json = JsonSerializer.Serialize(sidecar, SidecarJson);
        await File.WriteAllTextAsync(target + ".json", json, cancellationToken);
    }
}
