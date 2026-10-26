import { HttpErrorResponse } from "@angular/common/http";
import { inject, signal } from "@angular/core";
import { apiErrorMessage } from "@core/http/api-error";
import { CreatedInvite, FileProgress, InvitePreview, ProjectInvite, ProjectMember } from "@features/projects/models/membership.model";
import { Proposal, ProposalBatch, TranslationEntry } from "@features/projects/models/translation.model";
import { Project } from "@features/projects/models/project.model";
import { ImportFileResult, ImportTranslationFileResult, ProjectFile } from "@features/projects/models/source-file.model";
import { ProjectsService } from "@features/projects/services/projects.service";
import { Account } from "@features/session/models/account.model";
import { SessionStore } from "@features/session/stores/session.store";
import { TranslateService } from "@ngx-translate/core";
import { firstValueFrom } from "rxjs";

/**
 * Visible projects and the one open for editing.
 */
export class ProjectsStore {
    public readonly projects = signal<Project[]>([]);
    public readonly current = signal<Project | null>(null);
    public readonly loaded = signal(false);
    public readonly pending = signal(false);
    public readonly error = signal<string | null>(null);
    public readonly sourceFiles = signal<ProjectFile[]>([]);
    public readonly fileProgress = signal<FileProgress[]>([]);
    public readonly members = signal<ProjectMember[]>([]);
    public readonly invites = signal<ProjectInvite[]>([]);
    public readonly joinError = signal<string | null>(null);
    public readonly entries = signal<TranslationEntry[]>([]);
    public readonly proposals = signal<Proposal[]>([]);

    private readonly projectsService = inject(ProjectsService);
    private readonly sessionStore = inject(SessionStore);
    private readonly translate = inject(TranslateService);

    public select(project: Project): void {
        this.current.set(project);
    }

    public async load(): Promise<void> {
        this.error.set(null);
        try {
            this.projects.set(await firstValueFrom(this.projectsService.list()));
        } catch (err) {
            this.error.set(this.failure(err));
        } finally {
            this.loaded.set(true);
        }
    }

    public async open(slug: string): Promise<void> {
        this.error.set(null);
        this.pending.set(true);
        try {
            const project = await firstValueFrom(this.projectsService.get(slug));
            this.current.set(project);
        } catch (err) {
            this.current.set(null);
            this.error.set(this.failure(err));
        } finally {
            this.pending.set(false);
        }
    }

    public async create(body: { slug: string; name: string; sourceLocale: string }): Promise<Project | null> {
        this.pending.set(true);
        this.error.set(null);
        try {
            const created = await firstValueFrom(this.projectsService.create(body));
            this.current.set(created);
            this.projects.set(await firstValueFrom(this.projectsService.list()));
            return created;
        } catch (err) {
            this.error.set(this.failure(err));
            return null;
        } finally {
            this.pending.set(false);
        }
    }

    public async update(slug: string, body: { name: string; sourceLocale: string }): Promise<Project | null> {
        this.pending.set(true);
        this.error.set(null);
        try {
            const updated = await firstValueFrom(this.projectsService.update(slug, body));
            this.current.set(updated);
            this.projects.set(await firstValueFrom(this.projectsService.list()));
            return updated;
        } catch (err) {
            this.error.set(this.failure(err));
            return null;
        } finally {
            this.pending.set(false);
        }
    }

    public async reset(slug: string): Promise<Project | null> {
        this.pending.set(true);
        this.error.set(null);
        try {
            const updated = await firstValueFrom(this.projectsService.reset(slug));
            this.remember(updated);
            this.projects.set(await firstValueFrom(this.projectsService.list()));
            this.sourceFiles.set([]);
            this.fileProgress.set([]);
            this.entries.set([]);
            this.proposals.set([]);
            return updated;
        } catch (err) {
            this.error.set(this.failure(err));
            return null;
        } finally {
            this.pending.set(false);
        }
    }

    public async remove(slug: string): Promise<boolean> {
        this.pending.set(true);
        this.error.set(null);
        try {
            await firstValueFrom(this.projectsService.delete(slug));
            this.projects.update((projects) => projects.filter((project) => project.slug !== slug));
            if (this.current()?.slug === slug) {
                this.current.set(null);
            }

            this.sourceFiles.set([]);
            this.fileProgress.set([]);
            this.entries.set([]);
            this.proposals.set([]);
            this.members.set([]);
            this.invites.set([]);
            return true;
        } catch (err) {
            this.error.set(this.failure(err));
            return false;
        } finally {
            this.pending.set(false);
        }
    }

    public async addTargets(slug: string, locales: string[]): Promise<Project | null> {
        return this.changeTargets(slug, locales, (project, codes) => this.projectsService.addTargets(project, codes));
    }

    public async removeTargets(slug: string, locales: string[]): Promise<Project | null> {
        return this.changeTargets(slug, locales, (project, codes) => this.projectsService.removeTargets(project, codes));
    }

    private async changeTargets(
        slug: string,
        locales: string[],
        call: (project: string, codes: string[]) => ReturnType<ProjectsService["addTargets"]>,
    ): Promise<Project | null> {
        this.pending.set(true);
        this.error.set(null);
        try {
            const updated = await firstValueFrom(call(slug, locales));
            this.current.set(updated);
            this.projects.update((projects) => projects.map((project) => (project.slug === updated.slug ? updated : project)));
            return updated;
        } catch (err) {
            this.error.set(this.failure(err));
            return null;
        } finally {
            this.pending.set(false);
        }
    }

    public async loadFiles(slug: string): Promise<void> {
        this.error.set(null);
        try {
            this.sourceFiles.set(await firstValueFrom(this.projectsService.listFiles(slug)));
        } catch (err) {
            this.sourceFiles.set([]);
            this.error.set(this.failure(err));
        }
    }

    public async importSources(slug: string, files: { name: string; content: string }[]): Promise<ImportFileResult[] | null> {
        this.pending.set(true);
        this.error.set(null);
        try {
            const result = await firstValueFrom(this.projectsService.importSources(slug, files));
            this.remember(result.project);
            this.sourceFiles.set(await firstValueFrom(this.projectsService.listFiles(slug)));
            return result.files;
        } catch (err) {
            this.error.set(this.failure(err));
            return null;
        } finally {
            this.pending.set(false);
        }
    }

    public async importTranslations(
        slug: string,
        locale: string,
        files: { name: string; content: string }[],
    ): Promise<ImportTranslationFileResult[] | null> {
        this.pending.set(true);
        this.error.set(null);
        try {
            const result = await firstValueFrom(this.projectsService.importTranslations(slug, locale, files));
            this.remember(result.project);
            this.fileProgress.set([]);
            this.entries.set([]);
            this.proposals.set([]);
            return result.files;
        } catch (err) {
            this.error.set(this.failure(err));
            return null;
        } finally {
            this.pending.set(false);
        }
    }

    public async loadFileProgress(slug: string, locale: string, reset = false): Promise<void> {
        this.error.set(null);
        if (reset) {
            this.fileProgress.set([]);
        }

        try {
            this.fileProgress.set(await firstValueFrom(this.projectsService.listFileProgress(slug, locale)));
        } catch (err) {
            this.fileProgress.set([]);
            this.error.set(this.failure(err));
        }
    }

    public async loadEntries(slug: string, locale: string, file: string): Promise<void> {
        this.error.set(null);
        try {
            this.entries.set(await firstValueFrom(this.projectsService.listEntries(slug, locale, file)));
        } catch (err) {
            this.entries.set([]);
            this.error.set(this.failure(err));
        }
    }

    public async saveTranslation(slug: string, locale: string, keyId: string, text: string): Promise<string | null> {
        this.error.set(null);
        try {
            const entry = await firstValueFrom(this.projectsService.saveTranslation(slug, locale, { keyId, text }));
            this.entries.update((entries) => entries.map((item) => (item.keyId === entry.keyId ? entry : item)));
            await this.refresh(slug);
            return null;
        } catch (err) {
            return this.failure(err);
        }
    }

    public async loadProposals(slug: string, locale: string): Promise<void> {
        this.error.set(null);
        try {
            this.proposals.set(await firstValueFrom(this.projectsService.listProposals(slug, locale)));
        } catch (err) {
            this.proposals.set([]);
            this.error.set(this.failure(err));
        }
    }

    public async decideProposals(slug: string, locale: string, ids: string[], approve: boolean): Promise<ProposalBatch | null> {
        this.error.set(null);
        try {
            const batch = await firstValueFrom(
                approve
                    ? this.projectsService.approveProposals(slug, locale, ids)
                    : this.projectsService.refuseProposals(slug, locale, ids),
            );
            this.proposals.set(batch.pending);
            await this.refresh(slug);
            return batch;
        } catch (err) {
            this.error.set(this.failure(err));
            return null;
        }
    }

    public async loadMembers(slug: string): Promise<void> {
        this.error.set(null);
        try {
            const [members, invites] = await Promise.all([
                firstValueFrom(this.projectsService.listMembers(slug)),
                firstValueFrom(this.projectsService.listInvites(slug)),
            ]);
            this.members.set(members);
            this.invites.set(invites);
        } catch (err) {
            this.members.set([]);
            this.invites.set([]);
            this.error.set(this.failure(err));
        }
    }

    public async invite(slug: string, email: string, locales: string[]): Promise<CreatedInvite | null> {
        this.pending.set(true);
        this.error.set(null);
        try {
            const created = await firstValueFrom(this.projectsService.invite(slug, { email, locales }));
            this.invites.update((invites) => [...invites, created].sort((left, right) => left.email.localeCompare(right.email, "fr")));
            return created;
        } catch (err) {
            this.error.set(this.failure(err));
            return null;
        } finally {
            this.pending.set(false);
        }
    }

    public async revokeInvite(slug: string, inviteId: string): Promise<boolean> {
        this.error.set(null);
        try {
            await firstValueFrom(this.projectsService.revokeInvite(slug, inviteId));
            this.invites.update((invites) => invites.filter((invite) => invite.id !== inviteId));
            return true;
        } catch (err) {
            this.error.set(this.failure(err));
            return false;
        }
    }

    public async updateMember(slug: string, accountId: string, locales: string[]): Promise<boolean> {
        this.pending.set(true);
        this.error.set(null);
        try {
            const updated = await firstValueFrom(this.projectsService.updateMember(slug, accountId, locales));
            this.members.update((members) => members.map((member) => (member.accountId === updated.accountId ? updated : member)));
            return true;
        } catch (err) {
            this.error.set(this.failure(err));
            return false;
        } finally {
            this.pending.set(false);
        }
    }

    public async removeMember(slug: string, accountId: string): Promise<boolean> {
        this.error.set(null);
        try {
            await firstValueFrom(this.projectsService.removeMember(slug, accountId));
            this.members.update((members) => members.filter((member) => member.accountId !== accountId));
            return true;
        } catch (err) {
            this.error.set(this.failure(err));
            return false;
        }
    }

    public async exportLocale(project: string, locale: string): Promise<Blob | null> {
        this.error.set(null);
        try {
            return await firstValueFrom(this.projectsService.exportLocale(project, locale));
        } catch (err) {
            this.error.set(await this.blobFailure(err));
            return null;
        }
    }

    public async previewInvite(code: string): Promise<InvitePreview | null> {
        this.joinError.set(null);
        try {
            return await firstValueFrom(this.projectsService.previewInvite(code));
        } catch (err) {
            this.joinError.set(this.failure(err));
            return null;
        }
    }

    public async acceptInvite(body: { code: string; name?: string; password?: string }): Promise<Account | null> {
        this.pending.set(true);
        this.joinError.set(null);
        try {
            const account = await firstValueFrom(this.projectsService.acceptInvite(body));
            this.sessionStore.account.set(account);
            return account;
        } catch (err) {
            this.joinError.set(this.failure(err));
            return null;
        } finally {
            this.pending.set(false);
        }
    }

    private async refresh(slug: string): Promise<void> {
        const project = await firstValueFrom(this.projectsService.get(slug));
        if (project) {
            this.remember(project);
        }
    }

    private remember(project: Project): void {
        this.current.set(project);
        this.projects.update((projects) => projects.map((item) => (item.slug === project.slug ? project : item)));
    }

    private failure(err: unknown): string {
        return apiErrorMessage(err) || this.translate.instant("ERROR.ERR_INTERNAL");
    }

    private async blobFailure(err: unknown): Promise<string> {
        if (err instanceof HttpErrorResponse && err.error instanceof Blob) {
            try {
                const body = JSON.parse(await err.error.text()) as { Message?: string; Detail?: string };
                if (typeof body.Message === "string" && body.Message.startsWith("ERR_")) {
                    return this.translate.instant("ERROR." + body.Message, { token: body.Detail ?? "" });
                }
            } catch {
                return this.translate.instant("ERROR.ERR_INTERNAL");
            }
        }

        return this.failure(err);
    }
}
