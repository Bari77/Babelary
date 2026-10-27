using System.Security.Claims;
using Api.Auth;
using Core.Accounts;
using Core.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Projects of the signed-in account.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class ProjectsController(IProjects projects, IAccounts accounts) : ControllerBase
{
    /// <summary>Lists the projects owned by the signed-in account.</summary>
    [HttpGet("list")]
    public Task<IReadOnlyList<ProjectDto>> List(CancellationToken token)
        => projects.ListAsync(AccountSignIn.RequireAccountId(User), token);

    /// <summary>Reads one project by its stable identifier.</summary>
    [HttpGet("get")]
    public Task<ProjectDto> Get(string? project, CancellationToken token)
        => projects.GetAsync(AccountSignIn.RequireAccountId(User), project, token);

    /// <summary>Creates a project. Reserved to the instance owner.</summary>
    [HttpPost("create")]
    public Task<ProjectDto> Create(CreateProjectRequest request, CancellationToken token)
        => projects.CreateAsync(
            AccountSignIn.RequireAccountId(User),
            request.Slug,
            request.Name,
            request.SourceLocale,
            token);

    /// <summary>Changes the display name and the source locale.</summary>
    [HttpPost("update")]
    public Task<ProjectDto> Update(string? project, UpdateProjectRequest request, CancellationToken token)
        => projects.UpdateAsync(
            AccountSignIn.RequireAccountId(User),
            project,
            request.Name,
            request.SourceLocale,
            token);

    /// <summary>Deletes source files and translations. Locales and members stay.</summary>
    [HttpPost("reset")]
    public Task<ProjectDto> Reset(string? project, CancellationToken token)
        => projects.ResetAsync(AccountSignIn.RequireAccountId(User), project, token);

    /// <summary>Deletes the project.</summary>
    [HttpPost("delete")]
    public Task Delete(string? project, CancellationToken token)
        => projects.DeleteAsync(AccountSignIn.RequireAccountId(User), project, token);

    /// <summary>Adds target locales to a project.</summary>
    [HttpPost("addTargets")]
    public Task<ProjectDto> AddTargets(string? project, TargetLocalesRequest request, CancellationToken token)
        => projects.AddTargetsAsync(AccountSignIn.RequireAccountId(User), project, request.Locales, token);

    /// <summary>Removes target locales from a project.</summary>
    [HttpPost("removeTargets")]
    public Task<ProjectDto> RemoveTargets(string? project, TargetLocalesRequest request, CancellationToken token)
        => projects.RemoveTargetsAsync(AccountSignIn.RequireAccountId(User), project, request.Locales, token);

    /// <summary>Lists the source files of a project.</summary>
    [HttpGet("listFiles")]
    public Task<IReadOnlyList<ProjectFileDto>> ListFiles(string? project, CancellationToken token)
        => projects.ListFilesAsync(AccountSignIn.RequireAccountId(User), project, token);

    /// <summary>Deletes one source file and the keys that belong to it.</summary>
    [HttpPost("deleteFile")]
    public Task<ProjectDto> DeleteFile(string? project, string? file, CancellationToken token)
        => projects.DeleteFileAsync(AccountSignIn.RequireAccountId(User), project, file, token);

    /// <summary>Imports source JSON files for the project owner.</summary>
    [HttpPost("importSources")]
    public Task<ImportSourcesDto> ImportSources(
        string? project,
        bool deleteMissingFiles,
        ImportSourcesRequest request,
        CancellationToken token)
        => projects.ImportSourcesAsync(
            AccountSignIn.RequireAccountId(User),
            project,
            request.Files,
            token,
            deleteMissingFiles);

    /// <summary>Imports translation JSON files for one target locale. A member is limited to assigned locales.</summary>
    [HttpPost("importTranslations")]
    public Task<ImportTranslationsDto> ImportTranslations(
        string? project,
        string? locale,
        bool overwriteEmpty,
        ImportSourcesRequest request,
        CancellationToken token)
        => projects.ImportTranslationsAsync(
            AccountSignIn.RequireAccountId(User),
            project,
            locale,
            request.Files,
            token,
            overwriteEmpty);

    /// <summary>Translation progress for the CLI. Lists the keys that are still empty.</summary>
    [HttpGet("status")]
    public Task<SyncStatusDto> Status(string? project, CancellationToken token)
        => projects.StatusAsync(AccountSignIn.RequireAccountId(User), project, token);

    /// <summary>Downloads one target locale as a zip named by its code.</summary>
    [HttpGet("exportLocale")]
    public async Task<FileContentResult> ExportLocale(string? project, string? locale, CancellationToken token)
    {
        var export = await projects.ExportLocaleAsync(AccountSignIn.RequireAccountId(User), project, locale, token);
        return File(export.Content, "application/zip", export.FileName);
    }

    /// <summary>Progress of each file in one target locale.</summary>
    [HttpGet("listFileProgress")]
    public Task<IReadOnlyList<FileProgressDto>> ListFileProgress(string? project, string? locale, CancellationToken token)
        => projects.ListFileProgressAsync(AccountSignIn.RequireAccountId(User), project, locale, token);

    /// <summary>Keys of one target locale. An empty file returns every source file.</summary>
    [HttpGet("listEntries")]
    public Task<IReadOnlyList<TranslationEntryDto>> ListEntries(string? project, string? locale, string? file, CancellationToken token)
        => projects.ListEntriesAsync(AccountSignIn.RequireAccountId(User), project, locale, file, token);

    /// <summary>Saves a translation. A member's text becomes a proposal.</summary>
    [HttpPost("saveTranslation")]
    public Task<TranslationEntryDto> SaveTranslation(string? project, string? locale, SaveTranslationRequest request, CancellationToken token)
        => projects.SaveTranslationAsync(AccountSignIn.RequireAccountId(User), project, locale, request.KeyId, request.Text, token);

    /// <summary>Pending proposals. An empty locale returns every target language.</summary>
    [HttpGet("listProposals")]
    public Task<IReadOnlyList<ProposalDto>> ListProposals(string? project, string? locale, CancellationToken token)
        => projects.ListProposalsAsync(AccountSignIn.RequireAccountId(User), project, locale, token);

    /// <summary>Approves the selected proposals.</summary>
    [HttpPost("approveProposals")]
    public Task<ProposalBatchDto> ApproveProposals(string? project, string? locale, ProposalIdsRequest request, CancellationToken token)
        => projects.ApproveProposalsAsync(AccountSignIn.RequireAccountId(User), project, locale, request.Ids, token);

    /// <summary>Refuses the selected proposals.</summary>
    [HttpPost("refuseProposals")]
    public Task<ProposalBatchDto> RefuseProposals(string? project, string? locale, ProposalIdsRequest request, CancellationToken token)
        => projects.RefuseProposalsAsync(AccountSignIn.RequireAccountId(User), project, locale, request.Ids, token);

    /// <summary>Lists the translators of a project.</summary>
    [HttpGet("listMembers")]
    public Task<IReadOnlyList<ProjectMemberDto>> ListMembers(string? project, CancellationToken token)
        => projects.ListMembersAsync(AccountSignIn.RequireAccountId(User), project, token);

    /// <summary>Lists invitations that are still open.</summary>
    [HttpGet("listInvites")]
    public Task<IReadOnlyList<ProjectInviteDto>> ListInvites(string? project, CancellationToken token)
        => projects.ListInvitesAsync(AccountSignIn.RequireAccountId(User), project, token);

    /// <summary>Creates a shareable invitation. No e-mail is sent.</summary>
    [HttpPost("invite")]
    public Task<CreatedInviteDto> Invite(string? project, InviteRequest request, CancellationToken token)
        => projects.InviteAsync(AccountSignIn.RequireAccountId(User), project, request.Email, request.Locales, token);

    /// <summary>Cancels an open invitation.</summary>
    [HttpPost("revokeInvite")]
    public Task RevokeInvite(string? project, Guid invite, CancellationToken token)
        => projects.RevokeInviteAsync(AccountSignIn.RequireAccountId(User), project, invite, token);

    /// <summary>Changes the locales assigned to a member.</summary>
    [HttpPost("updateMember")]
    public Task<ProjectMemberDto> UpdateMember(string? project, Guid account, TargetLocalesRequest request, CancellationToken token)
        => projects.UpdateMemberAsync(AccountSignIn.RequireAccountId(User), project, account, request.Locales, token);

    /// <summary>Removes a member from the project.</summary>
    [HttpPost("removeMember")]
    public Task RemoveMember(string? project, Guid account, CancellationToken token)
        => projects.RemoveMemberAsync(AccountSignIn.RequireAccountId(User), project, account, token);

    /// <summary>Shows the project and languages of an invitation link.</summary>
    [AllowAnonymous]
    [HttpGet("previewInvite")]
    public Task<InvitePreviewDto> PreviewInvite(string? code, CancellationToken token)
        => projects.PreviewInviteAsync(code, token);

    /// <summary>Consumes an invitation and opens a session.</summary>
    [AllowAnonymous]
    [HttpPost("acceptInvite")]
    public async Task<AccountDto> AcceptInvite(AcceptInviteRequest request, CancellationToken token)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? signedIn = Guid.TryParse(raw, out var id) ? id : null;
        var account = await projects.AcceptInviteAsync(request.Code, signedIn, request.Name, request.Password, token);
        var sessionId = await accounts.OpenSessionAsync(account.Id, token);
        await AccountSignIn.SignInAsync(HttpContext, account, sessionId);
        return account;
    }

    /// <summary>New project.</summary>
    public class CreateProjectRequest
    {
        /// <summary>Stable identifier.</summary>
        public string? Slug { get; set; }

        /// <summary>Display name.</summary>
        public string? Name { get; set; }

        /// <summary>Source locale, <c>xx-XX</c>.</summary>
        public string? SourceLocale { get; set; }
    }

    /// <summary>Editable project fields.</summary>
    public class UpdateProjectRequest
    {
        /// <summary>Display name.</summary>
        public string? Name { get; set; }

        /// <summary>Source locale, <c>xx-XX</c>.</summary>
        public string? SourceLocale { get; set; }
    }

    /// <summary>Locales to add or remove.</summary>
    public class TargetLocalesRequest
    {
        /// <summary>Locale codes, <c>xx-XX</c>.</summary>
        public string[]? Locales { get; set; }
    }

    /// <summary>Source files to import.</summary>
    public class ImportSourcesRequest
    {
        /// <summary>JSON files. A file that cannot be read is reported and skipped.</summary>
        public SourceFileUpload[]? Files { get; set; }
    }

    /// <summary>Invitation to create.</summary>
    public class InviteRequest
    {
        /// <summary>Login of the translator.</summary>
        public string? Email { get; set; }

        /// <summary>Target locales to assign.</summary>
        public string[]? Locales { get; set; }
    }

    /// <summary>Invitation acceptance.</summary>
    public class AcceptInviteRequest
    {
        /// <summary>Secret from the shared link.</summary>
        public string? Code { get; set; }

        /// <summary>Display name, required when the e-mail has no account yet.</summary>
        public string? Name { get; set; }

        /// <summary>Password, required when the e-mail has no account yet.</summary>
        public string? Password { get; set; }
    }

    /// <summary>Translation to save or propose.</summary>
    public class SaveTranslationRequest
    {
        /// <summary>Key identifier.</summary>
        public Guid KeyId { get; set; }

        /// <summary>Text. Spaces are kept. Empty clears the value or proposes to clear it.</summary>
        public string? Text { get; set; }
    }

    /// <summary>Proposals selected for a bulk decision.</summary>
    public class ProposalIdsRequest
    {
        /// <summary>Proposal identifiers.</summary>
        public Guid[]? Ids { get; set; }
    }
}
