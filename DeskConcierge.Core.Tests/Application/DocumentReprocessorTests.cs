using DeskConcierge.Core.Abstractions;
using DeskConcierge.Core.Application;
using DeskConcierge.Core.Domain;
using DeskConcierge.Core.Pipeline;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DeskConcierge.Core.Tests.Application;

public class DocumentReprocessorTests
{
    [Fact]
    public async Task ReprocessInboxAsync_InboxLeftover_ArchivesAndUpdates()
    {
        var inboxFile = CreateTempFile("inbox", "letter.png");
        var document = new Document(inboxFile, "hash-1");
        var repository = new FakeRepository(document);
        var archive = new FakeArchive();
        var reprocessor = new DocumentReprocessor(repository, new FakeOcrEngine(), new FieldExtractor(), new FakeAnalyzer(), archive, NullLogger<DocumentReprocessor>.Instance);

        var count = await reprocessor.ReprocessInboxAsync();

        Assert.Equal(1, count);
        Assert.Equal(1, archive.FileCount);
        Assert.StartsWith("/store/archived/", document.OriginalPath);
        Assert.Contains(document, repository.Updated);
    }

    [Fact]
    public async Task ReprocessInboxAsync_AlreadyArchived_IsSkipped()
    {
        var document = new Document("/store/2026/Acme/file.png", "hash-2");
        var repository = new FakeRepository(document);
        var archive = new FakeArchive();
        var reprocessor = new DocumentReprocessor(repository, new FakeOcrEngine(), new FieldExtractor(), new FakeAnalyzer(), archive, NullLogger<DocumentReprocessor>.Instance);

        var count = await reprocessor.ReprocessInboxAsync();

        Assert.Equal(0, count);
        Assert.Equal(0, archive.FileCount);
        Assert.Empty(repository.Updated);
    }

    [Fact]
    public async Task ReprocessInboxAsync_InboxRecordButFileGone_IsSkipped()
    {
        var document = new Document("/inbox/missing.png", "hash-3");
        var repository = new FakeRepository(document);
        var archive = new FakeArchive();
        var reprocessor = new DocumentReprocessor(repository, new FakeOcrEngine(), new FieldExtractor(), new FakeAnalyzer(), archive, NullLogger<DocumentReprocessor>.Instance);

        var count = await reprocessor.ReprocessInboxAsync();

        Assert.Equal(0, count);
        Assert.Equal(0, archive.FileCount);
    }

    private static string CreateTempFile(string subfolder, string fileName)
    {
        var dir = Path.Combine(Path.GetTempPath(), "deskconcierge-tests", Guid.NewGuid().ToString("N"), subfolder);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, fileName);
        File.WriteAllText(path, "x");
        return path;
    }

    private sealed class FakeArchive : IDocumentArchive
    {
        public int FileCount { get; private set; }

        public Task<string> FileAsync(Document document, CancellationToken cancellationToken = default)
        {
            FileCount++;
            return Task.FromResult($"/store/archived/{document.Id}{Path.GetExtension(document.OriginalPath)}");
        }
    }

    private sealed class FakeRepository : IDocumentRepository
    {
        private readonly List<Document> _documents;

        public FakeRepository(params Document[] documents) => _documents = documents.ToList();

        public List<Document> Updated { get; } = new();

        public Task AddAsync(Document document, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateAsync(Document document, CancellationToken cancellationToken = default)
        {
            Updated.Add(document);
            return Task.CompletedTask;
        }

        public Task<Document?> FindByHashAsync(string contentHash, CancellationToken cancellationToken = default)
            => Task.FromResult<Document?>(null);

        public Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Document>>(_documents);

        public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_documents.FirstOrDefault(d => d.Id == id));
    }

    private sealed class FakeOcrEngine : IOcrEngine
    {
        public Task<OcrResult> ReadAsync(string filePath, CancellationToken cancellationToken = default)
            => Task.FromResult(new OcrResult("hello world", 90f));
    }

    private sealed class FakeAnalyzer : IDocumentAnalyzer
    {
        public Task<DocumentAnalysis?> AnalyzeAsync(string ocrText, CancellationToken cancellationToken = default)
            => Task.FromResult<DocumentAnalysis?>(null);
    }
}
