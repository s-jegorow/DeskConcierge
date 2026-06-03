using DeskConcierge.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace DeskConcierge.Infrastructure.Persistence;

public sealed class DeskConciergeDbContext : DbContext
{
    public DeskConciergeDbContext(DbContextOptions<DeskConciergeDbContext> options)
        : base(options)
    {
    }

    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DeskConciergeDbContext).Assembly);
    }
}
