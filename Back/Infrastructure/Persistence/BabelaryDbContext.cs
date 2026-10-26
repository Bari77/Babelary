using Core.Accounts;
using Core.Projects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

/// <summary>
/// Babelary database.
/// </summary>
public class BabelaryDbContext(DbContextOptions<BabelaryDbContext> options) : DbContext(options)
{
    /// <summary>Accounts.</summary>
    public DbSet<Account> Accounts => Set<Account>();

    /// <summary>Web sessions.</summary>
    public DbSet<Session> Sessions => Set<Session>();

    /// <summary>CLI tokens.</summary>
    public DbSet<CliToken> CliTokens => Set<CliToken>();

    /// <summary>Translation projects.</summary>
    public DbSet<Project> Projects => Set<Project>();

    /// <summary>Target locales of a project.</summary>
    public DbSet<ProjectTarget> ProjectTargets => Set<ProjectTarget>();

    /// <summary>Source files of a project.</summary>
    public DbSet<ProjectFile> ProjectFiles => Set<ProjectFile>();

    /// <summary>Translatable keys.</summary>
    public DbSet<ProjectKey> ProjectKeys => Set<ProjectKey>();

    /// <summary>Translated values.</summary>
    public DbSet<ProjectValue> ProjectValues => Set<ProjectValue>();

    /// <summary>Translation proposals waiting for the owner.</summary>
    public DbSet<ProjectProposal> ProjectProposals => Set<ProjectProposal>();

    /// <summary>Project memberships.</summary>
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    /// <summary>Locales assigned to a member.</summary>
    public DbSet<ProjectMemberLocale> ProjectMemberLocales => Set<ProjectMemberLocale>();

    /// <summary>Invitations.</summary>
    public DbSet<ProjectInvite> ProjectInvites => Set<ProjectInvite>();

    /// <summary>Locales carried by an invitation.</summary>
    public DbSet<ProjectInviteLocale> ProjectInviteLocales => Set<ProjectInviteLocale>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.Property(account => account.Name).HasMaxLength(120).IsRequired();
            entity.Property(account => account.Email).HasMaxLength(320).IsRequired();
            entity.Property(account => account.PasswordHash).IsRequired();
            entity.HasIndex(account => account.Email).IsUnique();
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasOne(session => session.Account)
                .WithMany(account => account.Sessions)
                .HasForeignKey(session => session.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CliToken>(entity =>
        {
            entity.Property(token => token.Name).HasMaxLength(80).IsRequired();
            entity.Property(token => token.Hash).HasMaxLength(64).IsRequired();
            entity.HasIndex(token => token.Hash).IsUnique();
            entity.HasOne(token => token.Account)
                .WithMany(account => account.Tokens)
                .HasForeignKey(token => token.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.Property(project => project.Slug).HasMaxLength(80).IsRequired();
            entity.Property(project => project.Name).HasMaxLength(120).IsRequired();
            entity.Property(project => project.SourceLocale).HasMaxLength(5).IsRequired();
            entity.HasIndex(project => project.Slug).IsUnique();
            entity.HasOne(project => project.Owner)
                .WithMany()
                .HasForeignKey(project => project.OwnerAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProjectTarget>(entity =>
        {
            entity.Property(target => target.Locale).HasMaxLength(5).IsRequired();
            entity.HasIndex(target => new { target.ProjectId, target.Locale }).IsUnique();
            entity.HasOne(target => target.Project)
                .WithMany(project => project.Targets)
                .HasForeignKey(target => target.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectFile>(entity =>
        {
            entity.Property(file => file.Name).HasMaxLength(80).IsRequired();
            entity.HasIndex(file => new { file.ProjectId, file.Name }).IsUnique();
            entity.HasOne(file => file.Project)
                .WithMany(project => project.Files)
                .HasForeignKey(file => file.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectKey>(entity =>
        {
            entity.Property(key => key.Path).HasMaxLength(SourceDocument.MaxPathLength).IsRequired();
            entity.Property(key => key.SourceValue).IsRequired();
            entity.HasIndex(key => new { key.FileId, key.Path }).IsUnique();
            entity.HasOne(key => key.File)
                .WithMany(file => file.Keys)
                .HasForeignKey(key => key.FileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectValue>(entity =>
        {
            entity.Property(value => value.Locale).HasMaxLength(5).IsRequired();
            entity.Property(value => value.Text).IsRequired();
            entity.Property(value => value.SourceSnapshot).IsRequired();
            entity.HasIndex(value => new { value.KeyId, value.Locale }).IsUnique();
            entity.HasOne(value => value.Key)
                .WithMany(key => key.Values)
                .HasForeignKey(value => value.KeyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectProposal>(entity =>
        {
            entity.Property(proposal => proposal.Locale).HasMaxLength(5).IsRequired();
            entity.Property(proposal => proposal.Text).IsRequired();
            entity.Property(proposal => proposal.Status).HasMaxLength(16).IsRequired();
            entity.HasIndex(proposal => new { proposal.KeyId, proposal.Locale }).IsUnique();
            entity.HasOne(proposal => proposal.Key)
                .WithMany()
                .HasForeignKey(proposal => proposal.KeyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(proposal => proposal.Author)
                .WithMany()
                .HasForeignKey(proposal => proposal.AuthorAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.HasIndex(member => new { member.ProjectId, member.AccountId }).IsUnique();
            entity.HasOne(member => member.Project)
                .WithMany(project => project.Members)
                .HasForeignKey(member => member.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(member => member.Account)
                .WithMany()
                .HasForeignKey(member => member.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectMemberLocale>(entity =>
        {
            entity.Property(locale => locale.Locale).HasMaxLength(5).IsRequired();
            entity.HasIndex(locale => new { locale.MemberId, locale.Locale }).IsUnique();
            entity.HasOne(locale => locale.Member)
                .WithMany(member => member.Locales)
                .HasForeignKey(locale => locale.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectInvite>(entity =>
        {
            entity.Property(invite => invite.Email).HasMaxLength(320).IsRequired();
            entity.Property(invite => invite.TokenHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(invite => invite.TokenHash).IsUnique();
            entity.HasOne(invite => invite.Project)
                .WithMany(project => project.Invites)
                .HasForeignKey(invite => invite.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectInviteLocale>(entity =>
        {
            entity.Property(locale => locale.Locale).HasMaxLength(5).IsRequired();
            entity.HasIndex(locale => new { locale.InviteId, locale.Locale }).IsUnique();
            entity.HasOne(locale => locale.Invite)
                .WithMany(invite => invite.Locales)
                .HasForeignKey(locale => locale.InviteId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
