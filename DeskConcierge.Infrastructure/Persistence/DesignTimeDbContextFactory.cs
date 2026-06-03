using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DeskConcierge.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DeskConciergeDbContext>
{
    public DeskConciergeDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DeskConciergeDbContext>()
            .UseSqlite("Data Source=deskconcierge.db")
            .Options;

        return new DeskConciergeDbContext(options);
    }
}
