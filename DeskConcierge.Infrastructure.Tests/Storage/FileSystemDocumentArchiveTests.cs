using DeskConcierge.Core.Domain;
using DeskConcierge.Infrastructure.Storage;
using Xunit;

namespace DeskConcierge.Infrastructure.Tests.Storage;

public class FileSystemDocumentArchiveTests
{
    [Fact]
    public async Task FileAsync_MovesOriginalIntoArchiveAndWritesSidecar()
    {
        var inbox = Directory.CreateTempSubdirectory();
        var source = Path.Combine(inbox.FullName, "scan.png");
        await File.WriteAllTextAsync(source, "fake image bytes");

        var document = new Document(source, "a1b2c3d4e5f6aaaa0000000000000000000000000000000000000000000000aa");
        document.ApplyOcr("Bürgeramt Musterstadt Terminbestätigung", 95f);
        document.ApplyAnalysis(new DocumentAnalysis("Bürgeramt Musterstadt", "Behördenbrief", "Terminbestätigung Reisepass.",
            new[] { new Appointment("12.06.2026", "Termin") }, true));

        var archive = new FileSystemDocumentArchive();
        var archivedPath = await archive.FileAsync(document);

        try
        {
            Assert.True(File.Exists(archivedPath));
            Assert.False(File.Exists(source));
            Assert.True(File.Exists(archivedPath + ".json"));
            Assert.Contains(Path.Combine("2026", "Bürgeramt Musterstadt"), archivedPath);

            var sidecar = await File.ReadAllTextAsync(archivedPath + ".json");
            Assert.Contains("Behördenbrief", sidecar);
            Assert.Contains("12.06.2026", sidecar);
        }
        finally
        {
            inbox.Delete(true);
            if (Directory.Exists("store"))
                Directory.Delete("store", recursive: true);
        }
    }
}
