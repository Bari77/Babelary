using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Core.Accounts;
using Core.Exceptions;
using Core.Projects;
using Infrastructure.Persistence;
using Infrastructure.Projects;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Application.Tests;

/// <summary>
/// Project rules: owner creates, slug is stable, a member cannot create.
/// </summary>
public class ProjectsImplTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly BabelaryDbContext db;
    private readonly ProjectsImpl projects;
    private readonly Guid ownerId = Guid.NewGuid();
    private readonly Guid memberId = Guid.NewGuid();

    public ProjectsImplTests()
    {
        this.connection = new SqliteConnection("DataSource=:memory:");
        this.connection.Open();
        var options = new DbContextOptionsBuilder<BabelaryDbContext>().UseSqlite(this.connection).Options;
        this.db = new BabelaryDbContext(options);
        this.db.Database.EnsureCreated();
        this.db.Accounts.AddRange(
            new Account
            {
                Id = this.ownerId,
                Name = "Bari",
                Email = "bari@example.com",
                PasswordHash = "hash",
                IsInstanceOwner = true,
                CreatedAt = DateTime.UtcNow,
            },
            new Account
            {
                Id = this.memberId,
                Name = "Trad",
                Email = "trad@example.com",
                PasswordHash = "hash",
                IsInstanceOwner = false,
                CreatedAt = DateTime.UtcNow,
            });
        this.db.SaveChanges();
        this.projects = new ProjectsImpl(this.db, new PasswordHasher<Account>());
    }

    [Fact]
    public async Task Owner_creates_a_project_and_lists_it()
    {
        var created = await this.projects.CreateAsync(this.ownerId, "kert-front", " Kert ", "en-US", CancellationToken.None);

        Assert.Equal("kert-front", created.Slug);
        Assert.Equal("Kert", created.Name);
        Assert.Equal("en-US", created.SourceLocale);
        Assert.Equal(0, created.TargetCount);
        Assert.Equal(0, created.Translated);
        Assert.Equal(0, created.Total);
        Assert.False(created.SourceLocaleLocked);

        var list = await this.projects.ListAsync(this.ownerId, CancellationToken.None);
        Assert.Single(list);
        Assert.Equal("kert-front", list[0].Slug);
    }

    [Fact]
    public async Task Member_cannot_create_a_project()
    {
        var error = await Assert.ThrowsAsync<ForbiddenException>(() =>
            this.projects.CreateAsync(this.memberId, "kert-front", "Kert", "en-US", CancellationToken.None));

        Assert.Equal("ERR_FORBIDDEN", error.Message);
        Assert.Empty(await this.projects.ListAsync(this.memberId, CancellationToken.None));
    }

    [Fact]
    public async Task Invalid_slug_and_locale_are_refused()
    {
        var slug = await Assert.ThrowsAsync<BadRequestException>(() =>
            this.projects.CreateAsync(this.ownerId, "Kert Front", "Kert", "en-US", CancellationToken.None));
        var locale = await Assert.ThrowsAsync<BadRequestException>(() =>
            this.projects.CreateAsync(this.ownerId, "kert-front", "Kert", "en", CancellationToken.None));
        var unknown = await Assert.ThrowsAsync<BadRequestException>(() =>
            this.projects.CreateAsync(this.ownerId, "kert-front", "Kert", "zz-ZZ", CancellationToken.None));

        Assert.Equal("ERR_PROJECT_SLUG_INVALID", slug.Message);
        Assert.Equal("ERR_LOCALE_INVALID", locale.Message);
        Assert.Equal("ERR_LOCALE_INVALID", unknown.Message);
    }

    [Fact]
    public async Task Duplicate_slug_is_refused()
    {
        await this.projects.CreateAsync(this.ownerId, "kert-front", "Kert", "en-US", CancellationToken.None);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            this.projects.CreateAsync(this.ownerId, "kert-front", "Autre", "fr-FR", CancellationToken.None));

        Assert.Equal("ERR_PROJECT_SLUG_TAKEN", error.Message);
    }

    [Fact]
    public async Task Owner_renames_the_project_and_its_source_locale()
    {
        await this.projects.CreateAsync(this.ownerId, "kert-front", "Kert", "en-US", CancellationToken.None);

        var updated = await this.projects.UpdateAsync(this.ownerId, "kert-front", "Kert Front", "fr-FR", CancellationToken.None);

        Assert.Equal("kert-front", updated.Slug);
        Assert.Equal("Kert Front", updated.Name);
        Assert.Equal("fr-FR", updated.SourceLocale);
    }

    [Fact]
    public async Task Member_does_not_see_a_project_he_does_not_own()
    {
        await this.projects.CreateAsync(this.ownerId, "kert-front", "Kert", "en-US", CancellationToken.None);

        var error = await Assert.ThrowsAsync<NotFoundException>(() =>
            this.projects.GetAsync(this.memberId, "kert-front", CancellationToken.None));

        Assert.Equal("ERR_NOT_FOUND", error.Message);
    }

    [Fact]
    public async Task Owner_adds_and_removes_target_locales()
    {
        await this.projects.CreateAsync(this.ownerId, "kert-front", "Kert", "en-US", CancellationToken.None);

        var added = await this.projects.AddTargetsAsync(
            this.ownerId,
            "kert-front",
            ["fr-FR", "ko-KR"],
            CancellationToken.None);

        Assert.Equal(2, added.TargetCount);
        Assert.Equal(["fr-FR", "ko-KR"], added.Targets);

        var listed = await this.projects.ListAsync(this.ownerId, CancellationToken.None);
        Assert.Equal(2, listed[0].TargetCount);

        var source = await Assert.ThrowsAsync<ConflictException>(() =>
            this.projects.AddTargetsAsync(this.ownerId, "kert-front", ["en-US"], CancellationToken.None));
        var duplicate = await Assert.ThrowsAsync<ConflictException>(() =>
            this.projects.AddTargetsAsync(this.ownerId, "kert-front", ["fr-FR"], CancellationToken.None));
        var blockedSource = await Assert.ThrowsAsync<ConflictException>(() =>
            this.projects.UpdateAsync(this.ownerId, "kert-front", "Kert", "fr-FR", CancellationToken.None));

        Assert.Equal("ERR_TARGET_IS_SOURCE", source.Message);
        Assert.Equal("ERR_TARGET_EXISTS", duplicate.Message);
        Assert.Equal("ERR_TARGET_IS_SOURCE", blockedSource.Message);

        var removed = await this.projects.RemoveTargetsAsync(
            this.ownerId,
            "kert-front",
            ["ko-KR"],
            CancellationToken.None);

        Assert.Equal(["fr-FR"], removed.Targets);

        var missing = await Assert.ThrowsAsync<NotFoundException>(() =>
            this.projects.RemoveTargetsAsync(this.memberId, "kert-front", ["fr-FR"], CancellationToken.None));
        Assert.Equal("ERR_NOT_FOUND", missing.Message);
    }

    [Fact]
    public async Task Owner_imports_nested_source_files_and_keeps_translations()
    {
        await this.projects.CreateAsync(this.ownerId, "kert-front", "Kert", "en-US", CancellationToken.None);
        await this.projects.AddTargetsAsync(this.ownerId, "kert-front", ["fr-FR"], CancellationToken.None);

        var imported = await this.projects.ImportSourcesAsync(
            this.ownerId,
            "kert-front",
            [
                new SourceFileUpload
                {
                    Name = "account.json",
                    Content = """
                        {
                          "ACCOUNT": {
                            "LOGIN": "Username",
                            "PASSWORD": " Password "
                          }
                        }
                        """,
                },
                new SourceFileUpload { Name = "notes.txt", Content = "{}" },
            ],
            CancellationToken.None);

        Assert.Equal(2, imported.Files[0].Created);
        Assert.Null(imported.Files[0].Error);
        Assert.Equal("ERR_FILE_NAME_INVALID", imported.Files[1].Error);
        Assert.True(imported.Project.SourceLocaleLocked);
        Assert.Equal(2, imported.Project.Total);
        Assert.Equal(0, imported.Project.Translated);
        Assert.Equal(2, imported.Project.Progress[0].Total);

        var files = await this.projects.ListFilesAsync(this.ownerId, "kert-front", CancellationToken.None);
        Assert.Equal("account.json", files[0].Name);
        Assert.Equal(2, files[0].KeyCount);

        var locked = await Assert.ThrowsAsync<ConflictException>(() =>
            this.projects.UpdateAsync(this.ownerId, "kert-front", "Kert", "fr-FR", CancellationToken.None));
        Assert.Equal("ERR_SOURCE_LOCALE_LOCKED", locked.Message);

        var login = await this.db.ProjectValues.SingleAsync(value => value.Key.Path == "ACCOUNT.LOGIN");
        login.Text = "Identifiant";
        await this.db.SaveChangesAsync();

        var again = await this.projects.ImportSourcesAsync(
            this.ownerId,
            "kert-front",
            [
                new SourceFileUpload
                {
                    Name = "account.json",
                    Content = """
                        {
                          "ACCOUNT": { "LOGIN": "User", "TITLE": "Account" },
                          "COUNT": 1
                        }
                        """,
                },
            ],
            CancellationToken.None);

        Assert.Equal("ERR_FILE_LEAF_NOT_STRING", again.Files[0].Error);
        Assert.Equal(2, (await this.projects.ListFilesAsync(this.ownerId, "kert-front", CancellationToken.None))[0].KeyCount);

        var replaced = await this.projects.ImportSourcesAsync(
            this.ownerId,
            "kert-front",
            [
                new SourceFileUpload
                {
                    Name = "account.json",
                    Content = """{ "ACCOUNT": { "LOGIN": "User", "TITLE": "Account" } }""",
                },
            ],
            CancellationToken.None);

        Assert.Equal(1, replaced.Files[0].Created);
        Assert.Equal(1, replaced.Files[0].Updated);
        Assert.Equal(1, replaced.Files[0].Deleted);
        Assert.Equal(1, replaced.Project.Translated);

        var kept = await this.db.ProjectValues.Include(value => value.Key).SingleAsync(value => value.Key.Path == "ACCOUNT.LOGIN");
        Assert.Equal("User", kept.Key.SourceValue);
        Assert.Equal("Identifiant", kept.Text);
        Assert.Equal("Username", kept.SourceSnapshot);
        Assert.False(await this.db.ProjectKeys.AnyAsync(key => key.Path == "ACCOUNT.PASSWORD"));

        var hidden = await Assert.ThrowsAsync<NotFoundException>(() =>
            this.projects.ImportSourcesAsync(this.memberId, "kert-front", [new SourceFileUpload { Name = "account.json", Content = "{}" }], CancellationToken.None));
        Assert.Equal("ERR_NOT_FOUND", hidden.Message);
    }

    [Fact]
    public async Task Invitation_creates_a_member_who_only_sees_assigned_languages()
    {
        await this.projects.CreateAsync(this.ownerId, "kert-front", "Kert", "en-US", CancellationToken.None);
        await this.projects.AddTargetsAsync(this.ownerId, "kert-front", ["fr-FR", "ko-KR"], CancellationToken.None);
        await this.projects.ImportSourcesAsync(
            this.ownerId,
            "kert-front",
            [new SourceFileUpload { Name = "account.json", Content = """{ "ACCOUNT": { "LOGIN": "Username" } }""" }],
            CancellationToken.None);

        var created = await this.projects.InviteAsync(
            this.ownerId,
            "kert-front",
            "trad@example.com",
            ["fr-FR"],
            CancellationToken.None);
        var preview = await this.projects.PreviewInviteAsync(created.Code, CancellationToken.None);
        Assert.Equal("Kert", preview.ProjectName);
        Assert.Equal(["fr-FR"], preview.Locales);
        Assert.True(preview.AccountExists);

        var needsLogin = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            this.projects.AcceptInviteAsync(created.Code, null, null, null, CancellationToken.None));
        Assert.Equal("ERR_INVITE_LOGIN", needsLogin.Message);

        var joined = await this.projects.AcceptInviteAsync(created.Code, this.memberId, null, null, CancellationToken.None);
        Assert.Equal(this.memberId, joined.Id);

        var visible = await this.projects.ListAsync(this.memberId, CancellationToken.None);
        Assert.Equal(["fr-FR"], visible[0].Targets);
        Assert.Equal(1, visible[0].Total);

        var files = await this.projects.ListFileProgressAsync(this.memberId, "kert-front", "fr-FR", CancellationToken.None);
        Assert.Equal("account.json", files[0].Name);
        Assert.Equal(1, files[0].Total);
        Assert.Equal(0, files[0].Translated);

        var refused = await Assert.ThrowsAsync<ForbiddenException>(() =>
            this.projects.ListFileProgressAsync(this.memberId, "kert-front", "ko-KR", CancellationToken.None));
        var blocked = await Assert.ThrowsAsync<ForbiddenException>(() =>
            this.projects.ImportSourcesAsync(this.memberId, "kert-front", [new SourceFileUpload { Name = "a.json", Content = "{}" }], CancellationToken.None));
        Assert.Equal("ERR_PROJECT_FORBIDDEN", refused.Message);
        Assert.Equal("ERR_PROJECT_FORBIDDEN", blocked.Message);

        var fresh = await this.projects.InviteAsync(this.ownerId, "kert-front", "neu@example.com", ["ko-KR"], CancellationToken.None);
        var account = await this.projects.AcceptInviteAsync(fresh.Code, null, "Neuve", "phrase-secrete", CancellationToken.None);
        Assert.False(account.IsInstanceOwner);
        Assert.Equal("neu@example.com", account.Email);
        var consumed = await Assert.ThrowsAsync<NotFoundException>(() =>
            this.projects.PreviewInviteAsync(fresh.Code, CancellationToken.None));
        Assert.Equal("ERR_INVITE_INVALID", consumed.Message);

        var pending = await this.projects.InviteAsync(this.ownerId, "kert-front", "autre@example.com", ["fr-FR"], CancellationToken.None);
        await this.projects.RevokeInviteAsync(this.ownerId, "kert-front", pending.Id, CancellationToken.None);
        var revoked = await Assert.ThrowsAsync<NotFoundException>(() =>
            this.projects.PreviewInviteAsync(pending.Code, CancellationToken.None));
        Assert.Equal("ERR_INVITE_INVALID", revoked.Message);

        var others = await this.projects.ListAsync(account.Id, CancellationToken.None);
        Assert.Single(others);
        Assert.Equal(["ko-KR"], others[0].Targets);
    }

    [Fact]
    public async Task Member_proposes_and_the_owner_approves_or_refuses_in_bulk()
    {
        await this.projects.CreateAsync(this.ownerId, "kert-front", "Kert", "en-US", CancellationToken.None);
        await this.projects.AddTargetsAsync(this.ownerId, "kert-front", ["fr-FR"], CancellationToken.None);
        await this.projects.ImportSourcesAsync(
            this.ownerId,
            "kert-front",
            [new SourceFileUpload { Name = "account.json", Content = """{ "HELLO": "Hello {{name}}", "TITLE": "Home" }""" }],
            CancellationToken.None);
        var invite = await this.projects.InviteAsync(this.ownerId, "kert-front", "trad@example.com", ["fr-FR"], CancellationToken.None);
        await this.projects.AcceptInviteAsync(invite.Code, this.memberId, null, null, CancellationToken.None);

        var entries = await this.projects.ListEntriesAsync(this.memberId, "kert-front", "fr-FR", "account.json", CancellationToken.None);
        var hello = entries.Single(entry => entry.Path == "HELLO");
        var title = entries.Single(entry => entry.Path == "TITLE");
        var broken = await Assert.ThrowsAsync<BadRequestException>(() =>
            this.projects.SaveTranslationAsync(this.memberId, "kert-front", "fr-FR", hello.KeyId, "Bonjour", CancellationToken.None));
        Assert.Equal("ERR_PLACEHOLDER_MISSING", broken.Message);
        Assert.Equal("{{name}}", broken.Detail);

        var proposed = await this.projects.SaveTranslationAsync(
            this.memberId, "kert-front", "fr-FR", hello.KeyId, "Bonjour {{name}}", CancellationToken.None);
        var second = await this.projects.SaveTranslationAsync(
            this.memberId, "kert-front", "fr-FR", title.KeyId, "Accueil", CancellationToken.None);
        Assert.Equal("pending", proposed.ProposalStatus);
        Assert.Equal("", proposed.Text);
        var waiting = await this.projects.GetAsync(this.ownerId, "kert-front", CancellationToken.None);
        Assert.Equal(0, waiting.Translated);
        Assert.Equal(2, waiting.Pending);
        Assert.Equal(2, waiting.PendingOpen);
        Assert.Equal(2, waiting.Progress.Single(row => row.Locale == "fr-FR").PendingOpen);

        var files = await this.projects.ListFileProgressAsync(this.ownerId, "kert-front", "fr-FR", CancellationToken.None);
        Assert.Equal(2, files[0].Pending);
        Assert.Equal(2, files[0].PendingOpen);

        var refused = await this.projects.RefuseProposalsAsync(
            this.ownerId, "kert-front", "fr-FR", [second.ProposalId!.Value], CancellationToken.None);
        Assert.Null(refused.Results[0].Error);
        Assert.Single(refused.Pending);

        var approved = await this.projects.ApproveProposalsAsync(
            this.ownerId, "kert-front", "fr-FR", [proposed.ProposalId!.Value, Guid.NewGuid()], CancellationToken.None);
        Assert.Null(approved.Results[0].Error);
        Assert.Equal("ERR_NOT_FOUND", approved.Results[1].Error);
        Assert.Empty(approved.Pending);

        var after = await this.projects.ListEntriesAsync(this.memberId, "kert-front", "fr-FR", "account.json", CancellationToken.None);
        Assert.Equal("Bonjour {{name}}", after.Single(entry => entry.Path == "HELLO").Text);
        Assert.Equal("translated", after.Single(entry => entry.Path == "HELLO").Status);
        Assert.Equal("empty", after.Single(entry => entry.Path == "TITLE").Status);
        Assert.Equal("refused", after.Single(entry => entry.Path == "TITLE").ProposalStatus);

        var direct = await this.projects.SaveTranslationAsync(this.ownerId, "kert-front", "fr-FR", title.KeyId, "Home", CancellationToken.None);
        Assert.Equal("identical", direct.Status);
        Assert.Null(direct.ProposalStatus);
        Assert.Equal(2, (await this.projects.GetAsync(this.ownerId, "kert-front", CancellationToken.None)).Translated);

        var wrong = await Assert.ThrowsAsync<ForbiddenException>(() =>
            this.projects.ListProposalsAsync(this.memberId, "kert-front", "fr-FR", CancellationToken.None));
        Assert.Equal("ERR_PROJECT_FORBIDDEN", wrong.Message);
    }

    [Fact]
    public async Task Export_keeps_the_source_shape_and_writes_saved_translations()
    {
        await this.projects.CreateAsync(this.ownerId, "kert-front", "Kert", "en-US", CancellationToken.None);
        await this.projects.AddTargetsAsync(this.ownerId, "kert-front", ["fr-FR", "de-DE"], CancellationToken.None);
        await this.projects.ImportSourcesAsync(
            this.ownerId,
            "kert-front",
            [
                new SourceFileUpload
                {
                    Name = "account.json",
                    Content = """
                        {
                          "ACCOUNT": {
                            "LOGIN": "User",
                            "PASSWORD": "Secret"
                          }
                        }
                        """,
                },
                new SourceFileUpload
                {
                    Name = "home.json",
                    Content = """
                        { "HOME": { "TITLE": "Home" } }
                        """,
                },
            ],
            CancellationToken.None);

        var login = await this.db.ProjectValues.SingleAsync(value => value.Locale == "fr-FR" && value.Key.Path == "ACCOUNT.LOGIN");
        login.Text = " Identifiant ";
        var title = await this.db.ProjectValues.SingleAsync(value => value.Locale == "fr-FR" && value.Key.Path == "HOME.TITLE");
        title.Text = "Accueil";
        await this.db.SaveChangesAsync();

        var entries = await this.projects.ListEntriesAsync(this.ownerId, "kert-front", "fr-FR", "account.json", CancellationToken.None);
        var password = entries.Single(entry => entry.Path == "ACCOUNT.PASSWORD");
        var invite = await this.projects.InviteAsync(this.ownerId, "kert-front", "trad@example.com", ["fr-FR"], CancellationToken.None);
        await this.projects.AcceptInviteAsync(invite.Code, this.memberId, null, null, CancellationToken.None);
        await this.projects.SaveTranslationAsync(this.memberId, "kert-front", "fr-FR", password.KeyId, "Proposition", CancellationToken.None);

        var export = await this.projects.ExportLocaleAsync(this.ownerId, "kert-front", "fr-FR", CancellationToken.None);
        Assert.Equal("fr-FR.zip", export.FileName);
        using var archive = new ZipArchive(new MemoryStream(export.Content), ZipArchiveMode.Read);
        Assert.Equal(["account.json", "home.json"], archive.Entries.Select(entry => entry.FullName).ToArray());

        using var account = JsonDocument.Parse(ReadZip(archive, "account.json"));
        Assert.Equal(["ACCOUNT"], account.RootElement.EnumerateObject().Select(property => property.Name).ToArray());
        var group = account.RootElement.GetProperty("ACCOUNT");
        Assert.Equal(["LOGIN", "PASSWORD"], group.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.Equal(" Identifiant ", group.GetProperty("LOGIN").GetString());
        Assert.Equal("", group.GetProperty("PASSWORD").GetString());

        using var home = JsonDocument.Parse(ReadZip(archive, "home.json"));
        Assert.Equal("Accueil", home.RootElement.GetProperty("HOME").GetProperty("TITLE").GetString());

        var memberExport = await this.projects.ExportLocaleAsync(this.memberId, "kert-front", "fr-FR", CancellationToken.None);
        Assert.Equal("fr-FR.zip", memberExport.FileName);

        var otherLocale = await Assert.ThrowsAsync<ForbiddenException>(() =>
            this.projects.ExportLocaleAsync(this.memberId, "kert-front", "de-DE", CancellationToken.None));
        Assert.Equal("ERR_PROJECT_FORBIDDEN", otherLocale.Message);

        var source = await this.projects.ExportLocaleAsync(this.ownerId, "kert-front", "en-US", CancellationToken.None);
        Assert.Equal("en-US.zip", source.FileName);
        using var sourceArchive = new ZipArchive(new MemoryStream(source.Content), ZipArchiveMode.Read);
        using var sourceAccount = JsonDocument.Parse(ReadZip(sourceArchive, "account.json"));
        Assert.Equal("User", sourceAccount.RootElement.GetProperty("ACCOUNT").GetProperty("LOGIN").GetString());

        var stranger = await Assert.ThrowsAsync<NotFoundException>(() =>
            this.projects.ExportLocaleAsync(Guid.NewGuid(), "kert-front", "fr-FR", CancellationToken.None));
        Assert.Equal("ERR_NOT_FOUND", stranger.Message);
    }

    [Fact]
    public async Task Angular_catalog_keeps_dotted_ids_and_round_trips()
    {
        await this.projects.CreateAsync(this.ownerId, "gc-platform", "Platform", "en-US", CancellationToken.None);
        await this.projects.AddTargetsAsync(this.ownerId, "gc-platform", ["fr-FR"], CancellationToken.None);
        var imported = await this.projects.ImportSourcesAsync(
            this.ownerId,
            "gc-platform",
            [
                new SourceFileUpload
                {
                    Name = "en-US.json",
                    Content = """
                        {
                          "locale": "en-US",
                          "translations": {
                            "core.breadcrumb.moderation": "Moderation",
                            "core.breadcrumb.moderation.users": "Users {$PH}"
                          }
                        }
                        """,
                },
            ],
            CancellationToken.None);

        Assert.Equal(2, imported.Files[0].Created);
        var saved = await this.projects.ImportTranslationsAsync(
            this.ownerId,
            "gc-platform",
            "fr-FR",
            [
                new SourceFileUpload
                {
                    Name = "en-US.json",
                    Content = """
                        {
                          "locale": "fr-FR",
                          "translations": {
                            "core.breadcrumb.moderation": "Modération",
                            "core.breadcrumb.moderation.users": "Utilisateurs {$PH}"
                          }
                        }
                        """,
                },
            ],
            CancellationToken.None);
        Assert.Equal(2, saved.Files[0].Updated);

        var export = await this.projects.ExportLocaleAsync(this.ownerId, "gc-platform", "fr-FR", CancellationToken.None);
        using var archive = new ZipArchive(new MemoryStream(export.Content), ZipArchiveMode.Read);
        using var document = JsonDocument.Parse(ReadZip(archive, "en-US.json"));
        Assert.Equal("fr-FR", document.RootElement.GetProperty("locale").GetString());
        var translations = document.RootElement.GetProperty("translations");
        Assert.Equal("Modération", translations.GetProperty("core.breadcrumb.moderation").GetString());
        Assert.Equal("Utilisateurs {$PH}", translations.GetProperty("core.breadcrumb.moderation.users").GetString());
    }

    private static string ReadZip(ZipArchive archive, string name)
    {
        var entry = archive.GetEntry(name);
        Assert.NotNull(entry);
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }

    [Fact]
    public async Task Reset_clears_sources_and_keeps_languages_and_members()
    {
        await this.projects.CreateAsync(this.ownerId, "kert-front", "Kert", "en-US", CancellationToken.None);
        await this.projects.AddTargetsAsync(this.ownerId, "kert-front", ["fr-FR"], CancellationToken.None);
        await this.projects.ImportSourcesAsync(
            this.ownerId,
            "kert-front",
            [new SourceFileUpload { Name = "account.json", Content = """{ "ACCOUNT": { "LOGIN": "User" } }""" }],
            CancellationToken.None);
        var login = await this.db.ProjectValues.SingleAsync(value => value.Locale == "fr-FR");
        login.Text = "Identifiant";
        await this.db.SaveChangesAsync();
        var invite = await this.projects.InviteAsync(this.ownerId, "kert-front", "trad@example.com", ["fr-FR"], CancellationToken.None);
        await this.projects.AcceptInviteAsync(invite.Code, this.memberId, null, null, CancellationToken.None);

        var forbidden = await Assert.ThrowsAsync<ForbiddenException>(() =>
            this.projects.ResetAsync(this.memberId, "kert-front", CancellationToken.None));
        Assert.Equal("ERR_PROJECT_FORBIDDEN", forbidden.Message);

        var reset = await this.projects.ResetAsync(this.ownerId, "kert-front", CancellationToken.None);
        Assert.Equal("en-US", reset.SourceLocale);
        Assert.Equal(["fr-FR"], reset.Targets);
        Assert.Equal(0, reset.Total);
        Assert.False(reset.SourceLocaleLocked);
        Assert.Empty(await this.projects.ListFilesAsync(this.ownerId, "kert-front", CancellationToken.None));
        Assert.Single(await this.projects.ListMembersAsync(this.ownerId, "kert-front", CancellationToken.None));
        Assert.Single(await this.projects.ListAsync(this.memberId, CancellationToken.None));

        var renamed = await this.projects.UpdateAsync(this.ownerId, "kert-front", "Kert", "de-DE", CancellationToken.None);
        Assert.Equal("de-DE", renamed.SourceLocale);
    }

    [Fact]
    public async Task Delete_removes_the_project_and_keeps_the_account()
    {
        await this.projects.CreateAsync(this.ownerId, "kert-front", "Kert", "en-US", CancellationToken.None);
        await this.projects.AddTargetsAsync(this.ownerId, "kert-front", ["fr-FR"], CancellationToken.None);
        var invite = await this.projects.InviteAsync(this.ownerId, "kert-front", "trad@example.com", ["fr-FR"], CancellationToken.None);
        await this.projects.AcceptInviteAsync(invite.Code, this.memberId, null, null, CancellationToken.None);

        var forbidden = await Assert.ThrowsAsync<ForbiddenException>(() =>
            this.projects.DeleteAsync(this.memberId, "kert-front", CancellationToken.None));
        Assert.Equal("ERR_PROJECT_FORBIDDEN", forbidden.Message);

        await this.projects.DeleteAsync(this.ownerId, "kert-front", CancellationToken.None);

        var missing = await Assert.ThrowsAsync<NotFoundException>(() =>
            this.projects.GetAsync(this.ownerId, "kert-front", CancellationToken.None));
        Assert.Equal("ERR_NOT_FOUND", missing.Message);
        Assert.Empty(await this.projects.ListAsync(this.ownerId, CancellationToken.None));
        Assert.Empty(await this.projects.ListAsync(this.memberId, CancellationToken.None));
        Assert.NotNull(await this.db.Accounts.FindAsync(this.ownerId));
    }

    [Fact]
    public async Task Owner_imports_translation_files_and_keeps_empty_leaves()
    {
        await this.projects.CreateAsync(this.ownerId, "kert-front", "Kert", "en-US", CancellationToken.None);
        await this.projects.AddTargetsAsync(this.ownerId, "kert-front", ["fr-FR", "de-DE"], CancellationToken.None);
        await this.projects.ImportSourcesAsync(
            this.ownerId,
            "kert-front",
            [new SourceFileUpload
            {
                Name = "account.json",
                Content = """
                    {
                      "ACCOUNT": {
                        "LOGIN": "User {{name}}",
                        "FORM": "Form",
                        "TITLE": "Home",
                        "NOTE": "Hello {{name}}"
                      }
                    }
                    """,
            }],
            CancellationToken.None);

        var login = await this.db.ProjectValues.SingleAsync(value => value.Key.Path == "ACCOUNT.LOGIN" && value.Locale == "fr-FR");
        login.Text = "Identifiant {{name}}";
        login.SourceSnapshot = "Previous";
        var form = await this.db.ProjectValues.SingleAsync(value => value.Key.Path == "ACCOUNT.FORM" && value.Locale == "fr-FR");
        form.Text = "Formulaire";
        form.SourceSnapshot = "Old";
        var title = await this.db.ProjectKeys.SingleAsync(key => key.Path == "ACCOUNT.TITLE");
        this.db.ProjectProposals.Add(new ProjectProposal
        {
            KeyId = title.Id,
            Locale = "fr-FR",
            AuthorAccountId = this.memberId,
            Text = "Proposition",
            Status = ProjectProposal.Pending,
            UpdatedAt = DateTime.UtcNow,
        });
        await this.db.SaveChangesAsync();
        var invite = await this.projects.InviteAsync(this.ownerId, "kert-front", "trad@example.com", ["fr-FR"], CancellationToken.None);
        await this.projects.AcceptInviteAsync(invite.Code, this.memberId, null, null, CancellationToken.None);

        var proposed = await this.projects.ImportTranslationsAsync(
            this.memberId,
            "kert-front",
            "fr-FR",
            [new SourceFileUpload { Name = "account.json", Content = """{ "ACCOUNT": { "TITLE": "Proposition membre" } }""" }],
            CancellationToken.None);
        Assert.Equal(1, proposed.Files[0].Updated);
        Assert.Equal(
            "Proposition membre",
            (await this.db.ProjectProposals.SingleAsync(proposal => proposal.Locale == "fr-FR" && proposal.Key.Path == "ACCOUNT.TITLE")).Text);
        Assert.Equal("", (await this.db.ProjectValues.SingleAsync(value => value.Locale == "fr-FR" && value.Key.Path == "ACCOUNT.TITLE")).Text);

        var otherLocale = await Assert.ThrowsAsync<ForbiddenException>(() =>
            this.projects.ImportTranslationsAsync(
                this.memberId,
                "kert-front",
                "de-DE",
                [new SourceFileUpload { Name = "account.json", Content = "{}" }],
                CancellationToken.None));
        Assert.Equal("ERR_PROJECT_FORBIDDEN", otherLocale.Message);

        var source = await Assert.ThrowsAsync<ForbiddenException>(() =>
            this.projects.ImportTranslationsAsync(
                this.ownerId,
                "kert-front",
                "en-US",
                [new SourceFileUpload { Name = "account.json", Content = "{}" }],
                CancellationToken.None));
        Assert.Equal("ERR_PROJECT_FORBIDDEN", source.Message);

        var imported = await this.projects.ImportTranslationsAsync(
            this.ownerId,
            "kert-front",
            "fr-FR",
            [
                new SourceFileUpload
                {
                    Name = "account.json",
                    Content = """
                        {
                          "ACCOUNT": {
                            "LOGIN": "Identifiant {{name}}",
                            "FORM": "",
                            "TITLE": "Accueil",
                            "NOTE": "Salut"
                          },
                          "EXTRA": "Nope"
                        }
                        """,
                },
                new SourceFileUpload { Name = "missing.json", Content = "{}" },
            ],
            CancellationToken.None);

        var account = imported.Files[0];
        Assert.Equal(1, account.Updated);
        Assert.Equal(1, account.Unchanged);
        Assert.Equal(1, account.Skipped);
        Assert.Equal(1, account.Ignored);
        Assert.Equal(0, account.Missing);
        Assert.Equal("ACCOUNT.NOTE", account.Issues[0].Path);
        Assert.Equal("ERR_PLACEHOLDER_MISSING", account.Issues[0].Error);
        Assert.Equal("{{name}}", account.Issues[0].Detail);
        Assert.Equal("ERR_FILE_MISSING", imported.Files[1].Error);
        Assert.Equal(3, imported.Project.Translated);

        login = await this.db.ProjectValues.SingleAsync(value => value.Key.Path == "ACCOUNT.LOGIN" && value.Locale == "fr-FR");
        Assert.Equal("Previous", login.SourceSnapshot);
        form = await this.db.ProjectValues.SingleAsync(value => value.Key.Path == "ACCOUNT.FORM" && value.Locale == "fr-FR");
        Assert.Equal("Formulaire", form.Text);
        Assert.Equal("Old", form.SourceSnapshot);
        var savedTitle = await this.db.ProjectValues.SingleAsync(value => value.Key.Path == "ACCOUNT.TITLE" && value.Locale == "fr-FR");
        Assert.Equal("Accueil", savedTitle.Text);
        Assert.Equal("Home", savedTitle.SourceSnapshot);
        Assert.Equal("", (await this.db.ProjectValues.SingleAsync(value => value.Key.Path == "ACCOUNT.NOTE" && value.Locale == "fr-FR")).Text);
        Assert.Empty(await this.db.ProjectProposals.Where(proposal => proposal.Locale == "fr-FR").ToListAsync());
        Assert.Equal("", (await this.db.ProjectValues.SingleAsync(value => value.Key.Path == "ACCOUNT.TITLE" && value.Locale == "de-DE")).Text);

        var replay = await this.projects.ImportTranslationsAsync(
            this.ownerId,
            "kert-front",
            "fr-FR",
            [new SourceFileUpload
            {
                Name = "account.json",
                Content = """
                    {
                      "ACCOUNT": {
                        "LOGIN": "Identifiant {{name}}",
                        "FORM": "",
                        "TITLE": "Accueil",
                        "NOTE": "Salut"
                      },
                      "EXTRA": "Nope"
                    }
                    """,
            }],
            CancellationToken.None);
        Assert.Equal(0, replay.Files[0].Updated);
        Assert.Equal(2, replay.Files[0].Unchanged);
        Assert.Equal(0, replay.Files[0].Missing);

        var flat = await this.projects.ImportTranslationsAsync(
            this.ownerId,
            "kert-front",
            "fr-FR",
            [new SourceFileUpload { Name = "account.json", Content = """{ "ACCOUNT.LOGIN": "Connexion {{name}}" }""" }],
            CancellationToken.None);
        Assert.Equal(1, flat.Files[0].Updated);
        Assert.Equal(3, flat.Files[0].Missing);
        Assert.Equal(
            "Connexion {{name}}",
            (await this.db.ProjectValues.SingleAsync(value => value.Key.Path == "ACCOUNT.LOGIN" && value.Locale == "fr-FR")).Text);
    }

    [Fact]
    public async Task Owner_lists_and_approves_proposals_of_every_language()
    {
        await this.projects.CreateAsync(this.ownerId, "kert-front", "Kert", "en-US", CancellationToken.None);
        await this.projects.AddTargetsAsync(this.ownerId, "kert-front", ["de-DE", "fr-FR"], CancellationToken.None);
        await this.projects.ImportSourcesAsync(
            this.ownerId,
            "kert-front",
            [new SourceFileUpload { Name = "home.json", Content = """{ "HOME": { "TITLE": "Home" } }""" }],
            CancellationToken.None);
        var invite = await this.projects.InviteAsync(this.ownerId, "kert-front", "trad@example.com", ["de-DE", "fr-FR"], CancellationToken.None);
        await this.projects.AcceptInviteAsync(invite.Code, this.memberId, null, null, CancellationToken.None);
        var german = await this.projects.ListEntriesAsync(this.memberId, "kert-front", "de-DE", "home.json", CancellationToken.None);
        var french = await this.projects.ListEntriesAsync(this.memberId, "kert-front", "fr-FR", "home.json", CancellationToken.None);
        await this.projects.SaveTranslationAsync(this.memberId, "kert-front", "de-DE", german[0].KeyId, "Titel", CancellationToken.None);
        await this.projects.SaveTranslationAsync(this.memberId, "kert-front", "fr-FR", french[0].KeyId, "Accueil", CancellationToken.None);

        var all = await this.projects.ListProposalsAsync(this.ownerId, "kert-front", null, CancellationToken.None);
        Assert.Equal(["de-DE", "fr-FR"], all.Select(proposal => proposal.Locale));
        Assert.All(all, proposal => Assert.Equal("home.json", proposal.FileName));

        var frenchOnly = await this.projects.ListProposalsAsync(this.ownerId, "kert-front", "fr-FR", CancellationToken.None);
        Assert.Equal(["fr-FR"], frenchOnly.Select(proposal => proposal.Locale));

        var approved = await this.projects.ApproveProposalsAsync(
            this.ownerId, "kert-front", null, all.Select(proposal => proposal.Id).ToArray(), CancellationToken.None);
        Assert.All(approved.Results, result => Assert.Null(result.Error));
        Assert.Empty(approved.Pending);
        Assert.Equal("Titel", (await this.db.ProjectValues.SingleAsync(value => value.Locale == "de-DE")).Text);
        Assert.Equal("Accueil", (await this.db.ProjectValues.SingleAsync(value => value.Locale == "fr-FR")).Text);
    }

    [Fact]
    public async Task Push_keeps_a_missing_file_until_delete_is_asked()
    {
        await this.projects.CreateAsync(this.ownerId, "kert-front", "Kert", "en-US", CancellationToken.None);
        await this.projects.ImportSourcesAsync(
            this.ownerId,
            "kert-front",
            [
                new SourceFileUpload { Name = "home.json", Content = """{ "HOME": { "TITLE": "Home" } }""" },
                new SourceFileUpload { Name = "pet.json", Content = """{ "PET": { "NAME": "Dog" } }""" },
            ],
            CancellationToken.None);

        var kept = await this.projects.ImportSourcesAsync(
            this.ownerId,
            "kert-front",
            [new SourceFileUpload { Name = "home.json", Content = """{ "HOME": { "TITLE": "Home" } }""" }],
            CancellationToken.None);
        Assert.Equal(0, kept.DeletedFiles);
        Assert.Equal(2, await this.db.ProjectFiles.CountAsync());

        var removed = await this.projects.ImportSourcesAsync(
            this.ownerId,
            "kert-front",
            [new SourceFileUpload { Name = "home.json", Content = """{ "HOME": { "TITLE": "Home" } }""" }],
            CancellationToken.None,
            deleteMissingFiles: true);
        Assert.Equal(1, removed.DeletedFiles);
        Assert.Equal("home.json", Assert.Single(await this.db.ProjectFiles.Select(file => file.Name).ToListAsync()));
    }

    [Fact]
    public async Task Deleting_a_source_file_removes_its_keys_and_translations()
    {
        await this.projects.CreateAsync(this.ownerId, "kert-front", "Kert", "en-US", CancellationToken.None);
        await this.projects.AddTargetsAsync(this.ownerId, "kert-front", ["fr-FR"], CancellationToken.None);
        await this.projects.ImportSourcesAsync(
            this.ownerId,
            "kert-front",
            [
                new SourceFileUpload { Name = "home.json", Content = """{ "HOME": { "TITLE": "Home", "LEAD": "Hello" } }""" },
                new SourceFileUpload { Name = "pet.json", Content = """{ "PET": { "NAME": "Dog" } }""" },
            ],
            CancellationToken.None);
        var home = await this.projects.ListEntriesAsync(this.ownerId, "kert-front", "fr-FR", "home.json", CancellationToken.None);
        await this.projects.SaveTranslationAsync(this.ownerId, "kert-front", "fr-FR", home[0].KeyId, "Accueil", CancellationToken.None);
        var invite = await this.projects.InviteAsync(this.ownerId, "kert-front", "trad@example.com", ["fr-FR"], CancellationToken.None);
        await this.projects.AcceptInviteAsync(invite.Code, this.memberId, null, null, CancellationToken.None);
        await this.projects.SaveTranslationAsync(this.memberId, "kert-front", "fr-FR", home[1].KeyId, "Bonjour", CancellationToken.None);

        var forbidden = await Assert.ThrowsAsync<ForbiddenException>(() =>
            this.projects.DeleteFileAsync(this.memberId, "kert-front", "home.json", CancellationToken.None));
        Assert.Equal("ERR_PROJECT_FORBIDDEN", forbidden.Message);
        Assert.Equal(2, await this.db.ProjectFiles.CountAsync());

        var deleted = await this.projects.DeleteFileAsync(this.ownerId, "kert-front", "home.json", CancellationToken.None);
        Assert.Equal(1, deleted.Total);
        Assert.True(deleted.SourceLocaleLocked);
        Assert.Equal("pet.json", Assert.Single(await this.db.ProjectFiles.Select(file => file.Name).ToListAsync()));
        Assert.Equal("PET.NAME", Assert.Single(await this.db.ProjectKeys.Select(key => key.Path).ToListAsync()));
        Assert.Empty(await this.db.ProjectProposals.ToListAsync());
        Assert.DoesNotContain(await this.db.ProjectValues.Select(value => value.Text).ToListAsync(), text => text == "Accueil");

        var cleared = await this.projects.DeleteFileAsync(this.ownerId, "kert-front", "pet.json", CancellationToken.None);
        Assert.Equal(0, cleared.Total);
        Assert.False(cleared.SourceLocaleLocked);

        var missing = await Assert.ThrowsAsync<NotFoundException>(() =>
            this.projects.DeleteFileAsync(this.ownerId, "kert-front", "home.json", CancellationToken.None));
        Assert.Equal("ERR_FILE_MISSING", missing.Message);
    }

    [Fact]
    public async Task Empty_translation_stays_until_overwrite_is_asked()
    {
        await this.projects.CreateAsync(this.ownerId, "kert-front", "Kert", "en-US", CancellationToken.None);
        await this.projects.AddTargetsAsync(this.ownerId, "kert-front", ["fr-FR"], CancellationToken.None);
        await this.projects.ImportSourcesAsync(
            this.ownerId,
            "kert-front",
            [new SourceFileUpload { Name = "home.json", Content = """{ "HOME": { "TITLE": "Home" } }""" }],
            CancellationToken.None);
        await this.projects.ImportTranslationsAsync(
            this.ownerId,
            "kert-front",
            "fr-FR",
            [new SourceFileUpload { Name = "home.json", Content = """{ "HOME": { "TITLE": "Accueil" } }""" }],
            CancellationToken.None);

        var skipped = await this.projects.ImportTranslationsAsync(
            this.ownerId,
            "kert-front",
            "fr-FR",
            [new SourceFileUpload { Name = "home.json", Content = """{ "HOME": { "TITLE": "" } }""" }],
            CancellationToken.None);
        Assert.Equal(1, skipped.Files[0].Skipped);
        Assert.Equal("Accueil", (await this.db.ProjectValues.SingleAsync(value => value.Locale == "fr-FR")).Text);

        var cleared = await this.projects.ImportTranslationsAsync(
            this.ownerId,
            "kert-front",
            "fr-FR",
            [new SourceFileUpload { Name = "home.json", Content = """{ "HOME": { "TITLE": "" } }""" }],
            CancellationToken.None,
            overwriteEmpty: true);
        Assert.Equal(1, cleared.Files[0].Updated);
        Assert.Equal("", (await this.db.ProjectValues.SingleAsync(value => value.Locale == "fr-FR")).Text);
    }

    [Fact]
    public async Task Status_lists_empty_keys_and_counts_review()
    {
        await this.projects.CreateAsync(this.ownerId, "kert-front", "Kert", "en-US", CancellationToken.None);
        await this.projects.AddTargetsAsync(this.ownerId, "kert-front", ["fr-FR"], CancellationToken.None);
        await this.projects.ImportSourcesAsync(
            this.ownerId,
            "kert-front",
            [new SourceFileUpload
            {
                Name = "home.json",
                Content = """{ "HOME": { "TITLE": "Home", "NOTE": "Note" } }""",
            }],
            CancellationToken.None);
        await this.projects.ImportTranslationsAsync(
            this.ownerId,
            "kert-front",
            "fr-FR",
            [new SourceFileUpload { Name = "home.json", Content = """{ "HOME": { "TITLE": "Accueil" } }""" }],
            CancellationToken.None);
        var title = await this.db.ProjectValues.SingleAsync(value => value.Key.Path == "HOME.TITLE");
        title.SourceSnapshot = "Previous";
        await this.db.SaveChangesAsync();

        var status = await this.projects.StatusAsync(this.ownerId, "kert-front", CancellationToken.None);
        var french = Assert.Single(status.Locales);
        Assert.Equal(2, french.Total);
        Assert.Equal(0, french.Translated);
        Assert.Equal(1, french.Review);
        var empty = Assert.Single(french.Empty);
        Assert.Equal("home.json", empty.File);
        Assert.Equal("HOME.NOTE", empty.Path);
    }

    [Fact]
    public async Task List_entries_without_a_file_returns_every_file()
    {
        await this.projects.CreateAsync(this.ownerId, "kert-front", "Kert", "en-US", CancellationToken.None);
        await this.projects.AddTargetsAsync(this.ownerId, "kert-front", ["fr-FR"], CancellationToken.None);
        await this.projects.ImportSourcesAsync(
            this.ownerId,
            "kert-front",
            [
                new SourceFileUpload { Name = "account.json", Content = """{ "ACCOUNT": { "LOGIN": "User" } }""" },
                new SourceFileUpload { Name = "home.json", Content = """{ "HOME": { "TITLE": "Home" } }""" },
            ],
            CancellationToken.None);

        var all = await this.projects.ListEntriesAsync(this.ownerId, "kert-front", "fr-FR", null, CancellationToken.None);
        Assert.Equal(["account.json", "home.json"], all.Select(entry => entry.FileName).ToArray());
        Assert.Equal(["ACCOUNT.LOGIN", "HOME.TITLE"], all.Select(entry => entry.Path).ToArray());

        var home = await this.projects.ListEntriesAsync(this.ownerId, "kert-front", "fr-FR", "home.json", CancellationToken.None);
        Assert.Equal("home.json", Assert.Single(home).FileName);
    }

    public void Dispose()
    {
        this.db.Dispose();
        this.connection.Dispose();
    }
}
