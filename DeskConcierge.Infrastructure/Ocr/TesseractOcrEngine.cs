using System.Diagnostics;
using System.Globalization;
using DeskConcierge.Core.Abstractions;

namespace DeskConcierge.Infrastructure.Ocr;

public sealed class TesseractOcrEngine : IOcrEngine
{
    // shelling out to the tesseract CLI — most reliable on macOS; could swap for an in-process engine behind IOcrEngine
    public async Task<OcrResult> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "tesseract",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(filePath);
        startInfo.ArgumentList.Add("stdout");
        startInfo.ArgumentList.Add("-l");
        startInfo.ArgumentList.Add("deu+eng");
        startInfo.ArgumentList.Add("tsv");

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start the tesseract process.");

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        var tsv = await stdoutTask;
        var error = await stderrTask;

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"tesseract failed (exit {process.ExitCode}): {error}");

        return ParseTsv(tsv);
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
