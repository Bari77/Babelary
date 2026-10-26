using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence;

/// <summary>
/// Design-time factory so EF Core can add migrations without the web host.
/// </summary>
public class BabelaryDbContextFactory : IDesignTimeDbContextFactory<BabelaryDbContext>
{
    /// <inheritdoc />
    public BabelaryDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BabelaryDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=babelary;Username=babelary;Password=babelary")
            .Options;
        return new BabelaryDbContext(options);
    }
}
