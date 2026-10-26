namespace Core.Health;

/// <summary>
/// Reads process health.
/// </summary>
public interface IHealth
{
    /// <summary>
    /// Checks the database connection.
    /// </summary>
    Task<HealthDto> GetAsync(CancellationToken token);
}
