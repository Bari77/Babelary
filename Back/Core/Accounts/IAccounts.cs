namespace Core.Accounts;

/// <summary>
/// Accounts, web sessions, and CLI tokens.
/// </summary>
public interface IAccounts
{
    /// <summary>Reports whether the instance owner exists.</summary>
    Task<SetupStatusDto> GetSetupStatusAsync(CancellationToken token);

    /// <summary>Creates the instance owner and refuses any later call.</summary>
    Task<AccountDto> CreateOwnerAsync(string? name, string? email, string? password, CancellationToken token);

    /// <summary>Checks email and password.</summary>
    Task<AccountDto> LoginAsync(string? email, string? password, CancellationToken token);

    /// <summary>Opens a web session and returns its id.</summary>
    Task<Guid> OpenSessionAsync(Guid accountId, CancellationToken token);

    /// <summary>Deletes a web session.</summary>
    Task CloseSessionAsync(Guid sessionId, CancellationToken token);

    /// <summary>Returns false when the session is missing or expired, and slides the deadline.</summary>
    Task<bool> TouchSessionAsync(Guid sessionId, CancellationToken token);

    /// <summary>Reads one account.</summary>
    Task<AccountDto> GetAsync(Guid accountId, CancellationToken token);

    /// <summary>Changes the display name.</summary>
    Task<AccountDto> UpdateNameAsync(Guid accountId, string? name, CancellationToken token);

    /// <summary>Changes the password after checking the current one.</summary>
    Task ChangePasswordAsync(Guid accountId, string? currentPassword, string? password, CancellationToken token);

    /// <summary>Lists active CLI tokens, without their secrets.</summary>
    Task<IReadOnlyList<TokenDto>> ListTokensAsync(Guid accountId, CancellationToken token);

    /// <summary>Creates a CLI token and returns the secret once.</summary>
    Task<CreatedTokenDto> CreateTokenAsync(Guid accountId, string? name, CancellationToken token);

    /// <summary>Revokes a CLI token owned by the account.</summary>
    Task RevokeTokenAsync(Guid accountId, Guid tokenId, CancellationToken token);

    /// <summary>Resolves a bearer secret to its account, or null when it is unknown or revoked.</summary>
    Task<AccountDto?> AuthenticateCliTokenAsync(string? secret, CancellationToken token);
}
