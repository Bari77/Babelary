namespace Core.Accounts;

/// <summary>
/// Account returned to the signed-in user.
/// </summary>
public class AccountDto
{
    /// <summary>Identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Login.</summary>
    public string Email { get; set; } = "";

    /// <summary>True when this account created the instance.</summary>
    public bool IsInstanceOwner { get; set; }
}
