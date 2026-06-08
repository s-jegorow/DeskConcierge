using System.Diagnostics;
using System.Globalization;
using DeskConcierge.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace DeskConcierge.Infrastructure.Ocr;

public sealed class TesseractOcrEngine : IOcrEngine
{
    private readonly ILogger<TesseractOcrEngine> _logger;

    public TesseractOcrEngine(ILogger<TesseractOcrEngine> logger)
    {
        _logger = logger;
    }

    // shelling out to the tesseract CLI — most reliable on macOS; could swap for an in-process engine behind IOcrEngine
    public async Task<OcrResult> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ocr start: {FilePath}", filePath);
        var result = Path.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase)
            ? await ReadPdfAsync(filePath, cancellationToken)
            : await ReadImageAsync(filePath, cancellationToken);
        _logger.LogDebug("ocr done: {FilePath} — {Words} words, confidence {Confidence:F1}%",
            filePath, result.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length, result.MeanConfidence);
        return result;
    }

    private static async Task<OcrResult> ReadImageAsync(string filePath, CancellationToken cancellationToken)
    {
        var (tsv, exitCode, error) = await RunAsync("tesseract",
            new[] { filePath, "stdout", "-l", "deu+eng", "tsv" }, cancellationToken);

        if (exitCode != 0)
            throw new InvalidOperationException($"tesseract failed (exit {exitCode}): {error}");

        return ParseTsv(tsv);
    }

    // tesseract can't read pdfs, so rasterize the pages with pdftoppm (poppler) and ocr each one
    private static async Task<OcrResult> ReadPdfAsync(string filePath, CancellationToken cancellationToken)
    {
        var workDir = Path.Combine(Path.GetTempPath(), "deskconcierge-pdf", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        try
        {
            var prefix = Path.Combine(workDir, "page");
            var (_, exitCode, error) = await RunAsync("pdftoppm",
                new[] { "-png", "-r", "300", filePath, prefix }, cancellationToken);

            if (exitCode != 0)
                throw new InvalidOperationException($"pdftoppm failed (exit {exitCode}): {error}");

            var pages = Directory.GetFiles(workDir, "page*.png").OrderBy(p => p, StringComparer.Ordinal).ToList();

            var texts = new List<string>();
            var weightedConfidence = 0f;
            var totalWords = 0;
            foreach (var page in pages)
            {
                var result = await ReadImageAsync(page, cancellationToken);
                if (result.Text.Length == 0)
                    continue;
                texts.Add(result.Text);
                // weight each page's mean by its word count so a sparse page doesn't skew the whole
                var wordCount = result.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                weightedConfidence += result.MeanConfidence * wordCount;
                totalWords += wordCount;
            }

            var meanConfidence = totalWords > 0 ? weightedConfidence / totalWords : 0f;
            return new OcrResult(string.Join("\n\n", texts), meanConfidence);
        }
        finally
        {
            try { Directory.Delete(workDir, recursive: true); } catch { /* temp cleanup, best effort */ }
        }
    }

    private static async Task<(string StdOut, int ExitCode, string StdErr)> RunAsync(string fileName, string[] arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start the {fileName} process.");

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        return (await stdoutTask, process.ExitCode, await stderrTask);
    }

    private static OcrResult ParseTsv(string tsv)
    {
        var words = new List<string>();
        var confidences = new List<float>();

        // tesseract tsv: tab-separated, header row first, word rows carry the text in the last column
        foreach (var line in tsv.Split('\n').Skip(1))
        {
            var columns = line.Split('\t');
            if (columns.Length < 12)
                continue;

            var text = columns[11].Trim();
            if (text.Length == 0)
                continue;

            words.Add(text);
            if (float.TryParse(columns[10], NumberStyles.Float, CultureInfo.InvariantCulture, out var confidence) && confidence >= 0)
                confidences.Add(confidence);
        }

        var meanConfidence = confidences.Count > 0 ? confidences.Average() : 0f;
        return new OcrResult(string.Join(' ', words), meanConfidence);
    }
}
