using Core.Accounts;
using Core.Exceptions;
using Infrastructure.Accounts;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Application.Tests;

/// <summary>
/// Account rules from the session spec: one owner, password length, token shown once.
/// </summary>
public class AccountsImplTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly BabelaryDbContext db;
    private readonly AccountsImpl accounts;

    public AccountsImplTests()
    {
        this.connection = new SqliteConnection("DataSource=:memory:");
        this.connection.Open();
        var options = new DbContextOptionsBuilder<BabelaryDbContext>().UseSqlite(this.connection).Options;
        this.db = new BabelaryDbContext(options);
        this.db.Database.EnsureCreated();
        this.accounts = new AccountsImpl(this.db, new PasswordHasher<Account>());
    }

    [Fact]
    public async Task Empty_instance_accepts_one_owner()
    {
        var status = await this.accounts.GetSetupStatusAsync(CancellationToken.None);
        Assert.False(status.Installed);

        var owner = await this.accounts.CreateOwnerAsync("Bari", "Bari@Example.com", "phrase-secrete", CancellationToken.None);

        Assert.Equal("Bari", owner.Name);
        Assert.Equal("bari@example.com", owner.Email);
        Assert.True(owner.IsInstanceOwner);
        Assert.True((await this.accounts.GetSetupStatusAsync(CancellationToken.None)).Installed);
    }

    [Fact]
    public async Task Short_password_is_refused()
    {
        var error = await Assert.ThrowsAsync<BadRequestException>(() =>
            this.accounts.CreateOwnerAsync("Bari", "bari@example.com", "tropcourt", CancellationToken.None));

        Assert.Equal("ERR_PASSWORD_TOO_SHORT", error.Message);
    }

    [Fact]
    public async Task Second_owner_is_refused()
    {
        await this.accounts.CreateOwnerAsync("Bari", "bari@example.com", "phrase-secrete", CancellationToken.None);

        var sameEmail = await Assert.ThrowsAsync<ConflictException>(() =>
            this.accounts.CreateOwnerAsync("Autre", "bari@example.com", "phrase-secrete", CancellationToken.None));
        var otherEmail = await Assert.ThrowsAsync<ConflictException>(() =>
            this.accounts.CreateOwnerAsync("Autre", "autre@example.com", "phrase-secrete", CancellationToken.None));

        Assert.Equal("ERR_EMAIL_TAKEN", sameEmail.Message);
        Assert.Equal("ERR_ALREADY_INSTALLED", otherEmail.Message);
    }

    [Fact]
    public async Task Login_rejects_a_wrong_password_and_accepts_the_right_one()
    {
        await this.accounts.CreateOwnerAsync("Bari", "bari@example.com", "phrase-secrete", CancellationToken.None);

        var wrong = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            this.accounts.LoginAsync("bari@example.com", "pas-la-bonne", CancellationToken.None));
        var account = await this.accounts.LoginAsync("BARI@example.com", "phrase-secrete", CancellationToken.None);

        Assert.Equal("ERR_INVALID_CREDENTIALS", wrong.Message);
        Assert.Equal("bari@example.com", account.Email);
    }

    [Fact]
    public async Task Logout_deletes_the_session()
    {
        var owner = await this.accounts.CreateOwnerAsync("Bari", "bari@example.com", "phrase-secrete", CancellationToken.None);
        var sessionId = await this.accounts.OpenSessionAsync(owner.Id, CancellationToken.None);

        Assert.True(await this.accounts.TouchSessionAsync(sessionId, CancellationToken.None));

        await this.accounts.CloseSessionAsync(sessionId, CancellationToken.None);

        Assert.False(await this.accounts.TouchSessionAsync(sessionId, CancellationToken.None));
    }

    [Fact]
    public async Task Created_token_is_not_stored_in_clear_and_dies_when_revoked()
    {
        var owner = await this.accounts.CreateOwnerAsync("Bari", "bari@example.com", "phrase-secrete", CancellationToken.None);
        var created = await this.accounts.CreateTokenAsync(owner.Id, "poste", CancellationToken.None);
        var listed = await this.accounts.ListTokensAsync(owner.Id, CancellationToken.None);
        var stored = Assert.Single(this.db.CliTokens);

        Assert.Equal("poste", Assert.Single(listed).Name);
        Assert.NotEqual(created.Secret, stored.Hash);
        Assert.DoesNotContain(created.Secret, stored.Hash);
        Assert.Equal(owner.Id, (await this.accounts.AuthenticateCliTokenAsync(created.Secret, CancellationToken.None))!.Id);

        await this.accounts.RevokeTokenAsync(owner.Id, created.Id, CancellationToken.None);

        Assert.Empty(await this.accounts.ListTokensAsync(owner.Id, CancellationToken.None));
        Assert.Null(await this.accounts.AuthenticateCliTokenAsync(created.Secret, CancellationToken.None));
    }

    [Fact]
    public async Task Password_change_replaces_the_previous_one()
    {
        var owner = await this.accounts.CreateOwnerAsync("Bari", "bari@example.com", "phrase-secrete", CancellationToken.None);

        await this.accounts.ChangePasswordAsync(owner.Id, "phrase-secrete", "nouvelle-phrase", CancellationToken.None);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            this.accounts.LoginAsync("bari@example.com", "phrase-secrete", CancellationToken.None));
        var account = await this.accounts.LoginAsync("bari@example.com", "nouvelle-phrase", CancellationToken.None);
        Assert.Equal(owner.Id, account.Id);
    }

    public void Dispose()
    {
        this.db.Dispose();
        this.connection.Dispose();
    }
}
