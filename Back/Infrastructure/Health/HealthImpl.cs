using Core.Health;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Health;

/// <summary>
/// Health probe backed by the database.
/// </summary>
public class HealthImpl(BabelaryDbContext db) : IHealth
{
    /// <inheritdoc />
    public async Task<HealthDto> GetAsync(CancellationToken token)
    {
        var database = await db.Database.CanConnectAsync(token);
        return new HealthDto
        {
            Status = database ? "ok" : "degraded",
            Database = database,
        };
    }
}
