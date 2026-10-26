using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Core.Accounts;
using Core.Exceptions;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Accounts;

/// <summary>
/// Accounts stored in PostgreSQL. Passwords and CLI secrets are never written in clear text.
/// </summary>
public class AccountsImpl(BabelaryDbContext db, IPasswordHasher<Account> passwords) : IAccounts
{
    /// <inheritdoc />
    public async Task<SetupStatusDto> GetSetupStatusAsync(CancellationToken token)
    {
        return new SetupStatusDto
        {
            Installed = await db.Accounts.AnyAsync(token),
        };
    }

    /// <inheritdoc />
    public async Task<AccountDto> CreateOwnerAsync(string? name, string? email, string? password, CancellationToken token)
    {
        var displayName = NormalizeName(name);
        var normalizedEmail = NormalizeEmail(email);
        EnsurePassword(password);

        if (await db.Accounts.AnyAsync(account => account.Email == normalizedEmail, token))
        {
            throw new ConflictException("ERR_EMAIL_TAKEN");
        }

        if (await db.Accounts.AnyAsync(token))
        {
            throw new ConflictException("ERR_ALREADY_INSTALLED");
        }

        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = displayName,
            Email = normalizedEmail,
            IsInstanceOwner = true,
            CreatedAt = DateTime.UtcNow,
        };
        account.PasswordHash = passwords.HashPassword(account, password!);
        db.Accounts.Add(account);

        try
        {
            await db.SaveChangesAsync(token);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("ERR_EMAIL_TAKEN");
        }

        return ToDto(account);
    }

    /// <inheritdoc />
    public async Task<AccountDto> LoginAsync(string? email, string? password, CancellationToken token)
    {
        var normalizedEmail = EmailAddress.Normalize(email);
        if (normalizedEmail is null || string.IsNullOrEmpty(password))
        {
            throw new UnauthorizedException("ERR_INVALID_CREDENTIALS");
        }

        var account = await db.Accounts.FirstOrDefaultAsync(item => item.Email == normalizedEmail, token);
        if (account is null || !PasswordMatches(account, password))
        {
            throw new UnauthorizedException("ERR_INVALID_CREDENTIALS");
        }

        if (db.Entry(account).Property(item => item.PasswordHash).IsModified)
        {
            await db.SaveChangesAsync(token);
        }

        return ToDto(account);
    }

    /// <inheritdoc />
    public async Task<Guid> OpenSessionAsync(Guid accountId, CancellationToken token)
    {
        var session = new Session
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
        };
        db.Sessions.Add(session);
        await db.SaveChangesAsync(token);
        return session.Id;
    }

    /// <inheritdoc />
    public async Task CloseSessionAsync(Guid sessionId, CancellationToken token)
    {
        var session = await db.Sessions.FirstOrDefaultAsync(item => item.Id == sessionId, token);
        if (session is null)
        {
            return;
        }

        db.Sessions.Remove(session);
        await db.SaveChangesAsync(token);
    }

    /// <inheritdoc />
    public async Task<bool> TouchSessionAsync(Guid sessionId, CancellationToken token)
    {
        var session = await db.Sessions.FirstOrDefaultAsync(item => item.Id == sessionId, token);
        if (session is null || session.ExpiresAt <= DateTime.UtcNow)
        {
            if (session is not null)
            {
                db.Sessions.Remove(session);
                await db.SaveChangesAsync(token);
            }

            return false;
        }

        if (session.ExpiresAt < DateTime.UtcNow.AddDays(29))
        {
            session.ExpiresAt = DateTime.UtcNow.AddDays(30);
            await db.SaveChangesAsync(token);
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<AccountDto> GetAsync(Guid accountId, CancellationToken token)
    {
        var account = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(item => item.Id == accountId, token);
        if (account is null)
        {
            throw new UnauthorizedException("ERR_UNAUTHORIZED");
        }

        return ToDto(account);
    }

    /// <inheritdoc />
    public async Task<AccountDto> UpdateNameAsync(Guid accountId, string? name, CancellationToken token)
    {
        var account = await RequireAccountAsync(accountId, token);
        account.Name = NormalizeName(name);
        await db.SaveChangesAsync(token);
        return ToDto(account);
    }

    /// <inheritdoc />
    public async Task ChangePasswordAsync(Guid accountId, string? currentPassword, string? password, CancellationToken token)
    {
        var account = await RequireAccountAsync(accountId, token);
        if (string.IsNullOrEmpty(currentPassword) || !PasswordMatches(account, currentPassword))
        {
            throw new UnauthorizedException("ERR_INVALID_CREDENTIALS");
        }

        EnsurePassword(password);
        account.PasswordHash = passwords.HashPassword(account, password!);
        await db.SaveChangesAsync(token);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TokenDto>> ListTokensAsync(Guid accountId, CancellationToken token)
    {
        return await db.CliTokens
            .AsNoTracking()
            .Where(item => item.AccountId == accountId && item.RevokedAt == null)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new TokenDto
            {
                Id = item.Id,
                Name = item.Name,
                CreatedAt = item.CreatedAt,
            })
            .ToListAsync(token);
    }

    /// <inheritdoc />
    public async Task<CreatedTokenDto> CreateTokenAsync(Guid accountId, string? name, CancellationToken token)
    {
        _ = await RequireAccountAsync(accountId, token);
        var secret = NewSecret();
        var created = new CliToken
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Name = NormalizeTokenName(name),
            Hash = HashSecret(secret),
            CreatedAt = DateTime.UtcNow,
        };
        db.CliTokens.Add(created);
        await db.SaveChangesAsync(token);

        return new CreatedTokenDto
        {
            Id = created.Id,
            Name = created.Name,
            Secret = secret,
            CreatedAt = created.CreatedAt,
        };
    }

    /// <inheritdoc />
    public async Task RevokeTokenAsync(Guid accountId, Guid tokenId, CancellationToken token)
    {
        var row = await db.CliTokens.FirstOrDefaultAsync(
            item => item.Id == tokenId && item.AccountId == accountId && item.RevokedAt == null,
            token);
        if (row is null)
        {
            throw new NotFoundException("ERR_NOT_FOUND");
        }

        row.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(token);
    }

    /// <inheritdoc />
    public async Task<AccountDto?> AuthenticateCliTokenAsync(string? secret, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            return null;
        }

        var hash = HashSecret(secret);
        var row = await db.CliTokens
            .AsNoTracking()
            .Include(item => item.Account)
            .FirstOrDefaultAsync(item => item.Hash == hash && item.RevokedAt == null, token);

        return row?.Account is null ? null : ToDto(row.Account);
    }

    private async Task<Account> RequireAccountAsync(Guid accountId, CancellationToken token)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(item => item.Id == accountId, token);
        if (account is null)
        {
            throw new UnauthorizedException("ERR_UNAUTHORIZED");
        }

        return account;
    }

    private bool PasswordMatches(Account account, string password)
    {
        var result = passwords.VerifyHashedPassword(account, account.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            return false;
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            account.PasswordHash = passwords.HashPassword(account, password);
        }

        return true;
    }

    private static AccountDto ToDto(Account account)
    {
        return new AccountDto
        {
            Id = account.Id,
            Name = account.Name,
            Email = account.Email,
            IsInstanceOwner = account.IsInstanceOwner,
        };
    }

    private static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BadRequestException("ERR_NAME_REQUIRED");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > 120)
        {
            throw new BadRequestException("ERR_NAME_TOO_LONG");
        }

        return trimmed;
    }

    private static string NormalizeEmail(string? email)
    {
        return EmailAddress.Normalize(email) ?? throw new BadRequestException("ERR_EMAIL_INVALID");
    }

    private static void EnsurePassword(string? password)
    {
        if (!PasswordRules.IsValid(password))
        {
            throw new BadRequestException("ERR_PASSWORD_TOO_SHORT");
        }
    }

    private static string NormalizeTokenName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BadRequestException("ERR_TOKEN_NAME_REQUIRED");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > 80)
        {
            throw new BadRequestException("ERR_TOKEN_NAME_TOO_LONG");
        }

        return trimmed;
    }

    private static string NewSecret()
    {
        return "bly_" + Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
    }

    private static string HashSecret(string secret)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
    }
}
