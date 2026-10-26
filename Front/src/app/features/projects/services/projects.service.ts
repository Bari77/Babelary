import { HttpParams } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { BaseService } from "@core/http/base.service";
import { CreatedInvite, FileProgress, InvitePreview, ProjectInvite, ProjectMember } from "@features/projects/models/membership.model";
import { Proposal, ProposalBatch, TranslationEntry } from "@features/projects/models/translation.model";
import { Project } from "@features/projects/models/project.model";
import { ImportSourcesResult, ImportTranslationsResult, ProjectFile } from "@features/projects/models/source-file.model";
import { Account } from "@features/session/models/account.model";
import { Observable } from "rxjs";

/**
 * Project list, creation, and update.
 */
@Injectable()
export class ProjectsService extends BaseService {
    protected override baseUrlService = "Projects";

    public list(): Observable<Project[]> {
        return this.httpList(Project, "list");
    }

    public get(project: string): Observable<Project | null> {
        return this.httpGet(Project, "get", new HttpParams().set("project", project));
    }

    public create(body: { slug: string; name: string; sourceLocale: string }): Observable<Project> {
        return this.httpPost(Project, "create", body);
    }

    public update(project: string, body: { name: string; sourceLocale: string }): Observable<Project> {
        return this.httpPost(Project, "update", body, new HttpParams().set("project", project));
    }

    public reset(project: string): Observable<Project> {
        return this.httpPost(Project, "reset", {}, new HttpParams().set("project", project));
    }

    public delete(project: string): Observable<void> {
        return this.http.post<void>(this.getURL("delete"), {}, { params: new HttpParams().set("project", project) });
    }

    public addTargets(project: string, locales: string[]): Observable<Project> {
        return this.httpPost(Project, "addTargets", { locales }, new HttpParams().set("project", project));
    }

    public removeTargets(project: string, locales: string[]): Observable<Project> {
        return this.httpPost(Project, "removeTargets", { locales }, new HttpParams().set("project", project));
    }

    public listFiles(project: string): Observable<ProjectFile[]> {
        return this.httpList(ProjectFile, "listFiles", new HttpParams().set("project", project));
    }

    public importSources(project: string, files: { name: string; content: string }[]): Observable<ImportSourcesResult> {
        return this.httpPost(ImportSourcesResult, "importSources", { files }, new HttpParams().set("project", project));
    }

    public importTranslations(
        project: string,
        locale: string,
        files: { name: string; content: string }[],
    ): Observable<ImportTranslationsResult> {
        return this.httpPost(
            ImportTranslationsResult,
            "importTranslations",
            { files },
            new HttpParams().set("project", project).set("locale", locale),
        );
    }

    public exportLocale(project: string, locale: string): Observable<Blob> {
        return this.http.get(this.getURL("exportLocale"), {
            params: new HttpParams().set("project", project).set("locale", locale),
            responseType: "blob",
        });
    }

    public listFileProgress(project: string, locale: string): Observable<FileProgress[]> {
        return this.httpList(FileProgress, "listFileProgress", new HttpParams().set("project", project).set("locale", locale));
    }

    public listMembers(project: string): Observable<ProjectMember[]> {
        return this.httpList(ProjectMember, "listMembers", new HttpParams().set("project", project));
    }

    public listInvites(project: string): Observable<ProjectInvite[]> {
        return this.httpList(ProjectInvite, "listInvites", new HttpParams().set("project", project));
    }

    public invite(project: string, body: { email: string; locales: string[] }): Observable<CreatedInvite> {
        return this.httpPost(CreatedInvite, "invite", body, new HttpParams().set("project", project));
    }

    public revokeInvite(project: string, invite: string): Observable<void> {
        return this.http.post<void>(this.getURL("revokeInvite"), {}, {
            params: new HttpParams().set("project", project).set("invite", invite),
        });
    }

    public updateMember(project: string, account: string, locales: string[]): Observable<ProjectMember> {
        return this.httpPost(ProjectMember, "updateMember", { locales }, new HttpParams().set("project", project).set("account", account));
    }

    public removeMember(project: string, account: string): Observable<void> {
        return this.http.post<void>(this.getURL("removeMember"), {}, {
            params: new HttpParams().set("project", project).set("account", account),
        });
    }

    public previewInvite(code: string): Observable<InvitePreview | null> {
        return this.httpGet(InvitePreview, "previewInvite", new HttpParams().set("code", code));
    }

    public acceptInvite(body: { code: string; name?: string; password?: string }): Observable<Account> {
        return this.httpPost(Account, "acceptInvite", body);
    }

    public listEntries(project: string, locale: string, file: string): Observable<TranslationEntry[]> {
        return this.httpList(
            TranslationEntry,
            "listEntries",
            new HttpParams().set("project", project).set("locale", locale).set("file", file),
        );
    }

    public saveTranslation(project: string, locale: string, body: { keyId: string; text: string }): Observable<TranslationEntry> {
        return this.httpPost(TranslationEntry, "saveTranslation", body, new HttpParams().set("project", project).set("locale", locale));
    }

    public listProposals(project: string, locale: string): Observable<Proposal[]> {
        return this.httpList(Proposal, "listProposals", this.projectParams(project, locale));
    }

    public approveProposals(project: string, locale: string, ids: string[]): Observable<ProposalBatch> {
        return this.httpPost(ProposalBatch, "approveProposals", { ids }, this.projectParams(project, locale));
    }

    public refuseProposals(project: string, locale: string, ids: string[]): Observable<ProposalBatch> {
        return this.httpPost(ProposalBatch, "refuseProposals", { ids }, this.projectParams(project, locale));
    }

    private projectParams(project: string, locale: string): HttpParams {
        const params = new HttpParams().set("project", project);
        return locale ? params.set("locale", locale) : params;
    }
}
