using Core.Accounts;

namespace Core.Projects;

/// <summary>
/// Projects visible to an account.
/// </summary>
public interface IProjects
{
    /// <summary>Lists the projects this account owns.</summary>
    Task<IReadOnlyList<ProjectDto>> ListAsync(Guid accountId, CancellationToken token);

    /// <summary>Reads one project owned by the account.</summary>
    Task<ProjectDto> GetAsync(Guid accountId, string? project, CancellationToken token);

    /// <summary>Creates a project. Only the instance owner may call this.</summary>
    Task<ProjectDto> CreateAsync(Guid accountId, string? slug, string? name, string? sourceLocale, CancellationToken token);

    /// <summary>Changes the display name and, while no key exists, the source locale.</summary>
    Task<ProjectDto> UpdateAsync(Guid accountId, string? project, string? name, string? sourceLocale, CancellationToken token);

    /// <summary>Deletes source files, keys, and translations. Locales and members stay.</summary>
    Task<ProjectDto> ResetAsync(Guid accountId, string? project, CancellationToken token);

    /// <summary>Deletes the project and everything that belongs to it.</summary>
    Task DeleteAsync(Guid accountId, string? project, CancellationToken token);

    /// <summary>Adds target locales. They cannot repeat the source locale.</summary>
    Task<ProjectDto> AddTargetsAsync(Guid accountId, string? project, IReadOnlyList<string?>? locales, CancellationToken token);

    /// <summary>Removes target locales and the values that belong to them.</summary>
    Task<ProjectDto> RemoveTargetsAsync(Guid accountId, string? project, IReadOnlyList<string?>? locales, CancellationToken token);

    /// <summary>Lists the source files of a project the account owns.</summary>
    Task<IReadOnlyList<ProjectFileDto>> ListFilesAsync(Guid accountId, string? project, CancellationToken token);

    /// <summary>
    /// Imports source JSON files. A file that cannot be read is reported and left unchanged.
    /// When <paramref name="deleteMissingFiles"/> is set, source files absent from this import are removed.
    /// </summary>
    Task<ImportSourcesDto> ImportSourcesAsync(
        Guid accountId,
        string? project,
        IReadOnlyList<SourceFileUpload>? files,
        CancellationToken token,
        bool deleteMissingFiles = false);

    /// <summary>
    /// Writes translations from JSON files that match the source file names.
    /// The owner stores the saved value. A member stores a proposal for an assigned locale.
    /// An empty leaf does not erase a saved value unless <paramref name="overwriteEmpty"/> is set.
    /// An unknown key is ignored.
    /// </summary>
    Task<ImportTranslationsDto> ImportTranslationsAsync(
        Guid accountId,
        string? project,
        string? locale,
        IReadOnlyList<SourceFileUpload>? files,
        CancellationToken token,
        bool overwriteEmpty = false);

    /// <summary>
    /// Builds a zip of one locale. The source locale exports the source texts.
    /// A target locale keeps the source file names and key shape, with saved translations in the leaves.
    /// An empty translation stays an empty string.
    /// </summary>
    Task<LocaleExport> ExportLocaleAsync(Guid accountId, string? project, string? locale, CancellationToken token);

    /// <summary>
    /// Per target locale: translated values, values to review, and the keys that are still empty.
    /// </summary>
    Task<SyncStatusDto> StatusAsync(Guid accountId, string? project, CancellationToken token);

    /// <summary>Progress of each source file for one target locale the account may see.</summary>
    Task<IReadOnlyList<FileProgressDto>> ListFileProgressAsync(Guid accountId, string? project, string? locale, CancellationToken token);

    /// <summary>Keys of one file in one target locale, with the saved value and any proposal.</summary>
    Task<IReadOnlyList<TranslationEntryDto>> ListEntriesAsync(Guid accountId, string? project, string? locale, string? file, CancellationToken token);

    /// <summary>Owner writes the saved value. A member stores a proposal instead.</summary>
    Task<TranslationEntryDto> SaveTranslationAsync(Guid accountId, string? project, string? locale, Guid keyId, string? text, CancellationToken token);

    /// <summary>Pending proposals. An empty locale returns every target language. Reserved to the owner.</summary>
    Task<IReadOnlyList<ProposalDto>> ListProposalsAsync(Guid accountId, string? project, string? locale, CancellationToken token);

    /// <summary>Applies the selected proposals. An empty locale accepts every target language. A broken token stays pending.</summary>
    Task<ProposalBatchDto> ApproveProposalsAsync(Guid accountId, string? project, string? locale, IReadOnlyList<Guid>? ids, CancellationToken token);

    /// <summary>Marks the selected proposals as refused. An empty locale accepts every target language. The saved values stay.</summary>
    Task<ProposalBatchDto> RefuseProposalsAsync(Guid accountId, string? project, string? locale, IReadOnlyList<Guid>? ids, CancellationToken token);

    /// <summary>Members of a project. Reserved to the owner.</summary>
    Task<IReadOnlyList<ProjectMemberDto>> ListMembersAsync(Guid accountId, string? project, CancellationToken token);

    /// <summary>Invitations that are still open. Reserved to the owner.</summary>
    Task<IReadOnlyList<ProjectInviteDto>> ListInvitesAsync(Guid accountId, string? project, CancellationToken token);

    /// <summary>Creates a single-use link. Nothing is sent by e-mail.</summary>
    Task<CreatedInviteDto> InviteAsync(Guid accountId, string? project, string? email, IReadOnlyList<string?>? locales, CancellationToken token);

    /// <summary>Cancels an open invitation.</summary>
    Task RevokeInviteAsync(Guid accountId, string? project, Guid inviteId, CancellationToken token);

    /// <summary>Replaces the locales assigned to a member.</summary>
    Task<ProjectMemberDto> UpdateMemberAsync(Guid accountId, string? project, Guid memberAccountId, IReadOnlyList<string?>? locales, CancellationToken token);

    /// <summary>Removes a member. The account stays.</summary>
    Task RemoveMemberAsync(Guid accountId, string? project, Guid memberAccountId, CancellationToken token);

    /// <summary>Reads an invitation link without consuming it.</summary>
    Task<InvitePreviewDto> PreviewInviteAsync(string? code, CancellationToken token);

    /// <summary>
    /// Consumes an invitation. A new e-mail creates the account. An existing e-mail must be the signed-in account.
    /// </summary>
    Task<AccountDto> AcceptInviteAsync(string? code, Guid? signedInAccountId, string? name, string? password, CancellationToken token);
}
