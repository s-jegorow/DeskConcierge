using System.Globalization;
using System.Text.RegularExpressions;

namespace DeskConcierge.Core.Pipeline;

// builds the human-readable archive path: {year}/{sender}/{date}_{sender-slug}_{ref}.ext
public static class ArchivePathBuilder
{
    private static readonly char[] IllegalFolderChars = "/\\:*?\"<>|".ToCharArray();

    public static string BuildRelativePath(
        DateTimeOffset uploadedAt, string? sender, string? invoiceNumber, string contentHash, string extension)
    {
        var year = uploadedAt.ToString("yyyy", CultureInfo.InvariantCulture);
        var folder = FolderName(sender);
        var date = uploadedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var senderSlug = Slug(sender);
        if (senderSlug.Length == 0)
            senderSlug = "unbekannt";

        var reference = Slug(invoiceNumber);
        if (reference.Length == 0)
            reference = ShortHash(contentHash);

        var fileName = $"{date}_{senderSlug}_{reference}{NormalizeExtension(extension)}";

        // forward slashes are fine on macOS/Linux/Windows; keeps the path deterministic for tests
        return $"{year}/{folder}/{fileName}";
    }

    // folder keeps spaces and case for finder readability, only path-breaking chars go
    private static string FolderName(string? sender)
    {
        if (string.IsNullOrWhiteSpace(sender))
            return "Unbekannt";

        var cleaned = new string(sender
            .Where(c => !IllegalFolderChars.Contains(c) && !char.IsControl(c))
            .ToArray());
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

        return cleaned.Length == 0 ? "Unbekannt" : cleaned;
    }

    // lowercase, umlauts transliterated, everything else collapsed to single hyphens
    private static string Slug(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var lowered = text.ToLowerInvariant()
            .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss");
        return Regex.Replace(lowered, "[^a-z0-9]+", "-").Trim('-');
    }

    private static string ShortHash(string contentHash)
        => contentHash.Length >= 8 ? contentHash[..8] : contentHash;

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return string.Empty;

        var ext = extension.Trim().ToLowerInvariant();
        return ext.StartsWith('.') ? ext : "." + ext;
    }
}
