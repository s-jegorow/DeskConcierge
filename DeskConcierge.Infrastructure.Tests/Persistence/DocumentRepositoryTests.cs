using DeskConcierge.Core.Domain;
using DeskConcierge.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DeskConcierge.Infrastructure.Tests.Persistence;

public class DocumentRepositoryTests
{
    private static DeskConciergeDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<DeskConciergeDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new DeskConciergeDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task AddAsync_PersistsDocument_AndFindByHashReturnsIt()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        await using var context = CreateContext(connection);
        var repository = new DocumentRepository(context);
        var document = new Document("/inbox/scan-001.pdf", "abc123");

        await repository.AddAsync(document);
        var found = await repository.FindByHashAsync("abc123");

        Assert.NotNull(found);
        Assert.Equal(document.Id, found!.Id);
        Assert.Equal("/inbox/scan-001.pdf", found.OriginalPath);
    }

    [Fact]
    public async Task FindByHashAsync_ReturnsNull_WhenUnknown()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        await using var context = CreateContext(connection);
        var repository = new DocumentRepository(context);

        var found = await repository.FindByHashAsync("does-not-exist");

        Assert.Null(found);
    }

    [Fact]
    public async Task AddAsync_RejectsDuplicateHash_ViaUniqueIndex()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        await using var context = CreateContext(connection);
        var repository = new DocumentRepository(context);
        await repository.AddAsync(new Document("/inbox/a.pdf", "same-hash"));

        await Assert.ThrowsAnyAsync<DbUpdateException>(
            () => repository.AddAsync(new Document("/inbox/b.pdf", "same-hash")));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsSavedDocuments()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        await using var context = CreateContext(connection);
        var repository = new DocumentRepository(context);
        await repository.AddAsync(new Document("/inbox/a.pdf", "hash-a"));
        await repository.AddAsync(new Document("/inbox/b.pdf", "hash-b"));

        var all = await repository.GetAllAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMatchingDocument()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        await using var context = CreateContext(connection);
        var repository = new DocumentRepository(context);
        var document = new Document("/inbox/a.pdf", "hash-a");
        await repository.AddAsync(document);

        var found = await repository.GetByIdAsync(document.Id);

        Assert.NotNull(found);
        Assert.Equal(document.Id, found!.Id);
    }
}
