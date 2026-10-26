namespace Core.Health;

/// <summary>
/// Process and database reachability.
/// </summary>
public class HealthDto
{
    /// <summary>
    /// <c>ok</c> when the database accepts a connection, otherwise <c>degraded</c>.
    /// </summary>
    public string Status { get; set; } = "degraded";

    /// <summary>
    /// True when the database connection succeeds.
    /// </summary>
    public bool Database { get; set; }
}
