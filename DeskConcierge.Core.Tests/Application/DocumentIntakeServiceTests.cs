using System.Text;
using DeskConcierge.Core.Abstractions;
using DeskConcierge.Core.Application;
using DeskConcierge.Core.Domain;
using DeskConcierge.Core.Pipeline;
using Xunit;

namespace DeskConcierge.Core.Tests.Application;

public class DocumentIntakeServiceTests
{
    private const string AbcHash = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";

    [Fact]
    public async Task IngestAsync_NewContent_StoresPersistsAndAppliesOcr()
    {
        var repository = new FakeRepository();
        var storage = new FakeStorage();
        var ocr = new FakeOcrEngine();
        var service = new DocumentIntakeService(repository, storage, ocr, new FieldExtractor());
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("abc"));

        var result = await service.IngestAsync(content, "scan.pdf");

        Assert.Equal(IngestOutcome.Created, result.Outcome);
        Assert.Equal(AbcHash, result.Document.ContentHash);
        Assert.Single(repository.Saved);
        Assert.Equal(1, storage.SaveCount);
        Assert.Equal(1, ocr.ReadCount);
        Assert.Equal("hello world", result.Document.OcrText);
        Assert.Equal(95f, result.Document.OcrConfidence);
    }

    [Fact]
    public async Task IngestAsync_DuplicateContent_SkipsStorageOcrAndReturnsExisting()
    {
        var existing = new Document("/inbox/old.pdf", AbcHash);
        var repository = new FakeRepository(existing);
        var storage = new FakeStorage();
        var ocr = new FakeOcrEngine();
        var service = new DocumentIntakeService(repository, storage, ocr, new FieldExtractor());
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("abc"));

        var result = await service.IngestAsync(content, "scan.pdf");

        Assert.Equal(IngestOutcome.Duplicate, result.Outcome);
        Assert.Same(existing, result.Document);
        Assert.Empty(repository.Saved);
        Assert.Equal(0, storage.SaveCount);
        Assert.Equal(0, ocr.ReadCount);
    }

    [Fact]
    public async Task IngestAsync_RunsFieldExtractionOnOcrText()
    {
        var repository = new FakeRepository();
        var storage = new FakeStorage();
        var ocr = new FakeOcrEngine("Bitte zahlen auf DE89 3704 0044 0532 0130 00");
        var service = new DocumentIntakeService(repository, storage, ocr, new FieldExtractor());
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("abc"));

        var result = await service.IngestAsync(content, "scan.pdf");

        Assert.Equal("DE89370400440532013000", result.Document.Iban);
    }

    private sealed class FakeRepository : IDocumentRepository
    {
        private readonly Document? _existing;

        public FakeRepository(Document? existing = null) => _existing = existing;

        public List<Document> Saved { get; } = new();

        public Task AddAsync(Document document, CancellationToken cancellationToken = default)
        {
            Saved.Add(document);
            return Task.CompletedTask;
        }

        public Task<Document?> FindByHashAsync(string contentHash, CancellationToken cancellationToken = default)
            => Task.FromResult(_existing is not null && _existing.ContentHash == contentHash ? _existing : null);

        public Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Document>>(Saved);

        public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Saved.FirstOrDefault(d => d.Id == id));
    }

    private sealed class FakeStorage : IDocumentStorage
    {
        public int SaveCount { get; private set; }

        public Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult($"/inbox/{fileName}");
        }
    }

    private sealed class FakeOcrEngine : IOcrEngine
    {
        private readonly string _text;

        public FakeOcrEngine(string text = "hello world") => _text = text;

        public int ReadCount { get; private set; }

        public Task<OcrResult> ReadAsync(string filePath, CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return Task.FromResult(new OcrResult(_text, 95f));
        }
    }
}
