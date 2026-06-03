using DeskConcierge.Core.Domain;
using Xunit;

namespace DeskConcierge.Core.Tests.Domain;

public class DocumentTests
{
    [Fact]
    public void Constructor_SetsProvidedValues_AndGeneratesIdentity()
    {
        var before = DateTimeOffset.UtcNow;

        var doc = new Document("/inbox/scan-001.pdf", "abc123");

        Assert.Equal("/inbox/scan-001.pdf", doc.OriginalPath);
        Assert.Equal("abc123", doc.ContentHash);
        Assert.NotEqual(Guid.Empty, doc.Id);
        Assert.InRange(doc.CreatedAt, before, DateTimeOffset.UtcNow);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_RejectsEmptyPath(string? path)
    {
        Assert.Throws<ArgumentException>(() => new Document(path!, "abc123"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_RejectsEmptyHash(string? hash)
    {
        Assert.Throws<ArgumentException>(() => new Document("/inbox/scan-001.pdf", hash!));
    }
}
