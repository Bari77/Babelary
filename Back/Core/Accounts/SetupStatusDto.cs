namespace Core.Accounts;

/// <summary>
/// Whether the instance already has its owner.
/// </summary>
public class SetupStatusDto
{
    /// <summary>True once the first account exists.</summary>
    public bool Installed { get; set; }
}
