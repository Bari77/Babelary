import { DatePipe } from "@angular/common";
import { Component, computed, inject, signal } from "@angular/core";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { MatButton, MatIconButton } from "@angular/material/button";
import { MatCard, MatCardContent, MatCardHeader, MatCardTitle } from "@angular/material/card";
import { MatCheckbox } from "@angular/material/checkbox";
import { MatDialog } from "@angular/material/dialog";
import { MatError, MatFormField, MatLabel } from "@angular/material/form-field";
import { MatIcon } from "@angular/material/icon";
import { MatInput } from "@angular/material/input";
import { ActivatedRoute } from "@angular/router";
import { SkeletonComponent } from "@core/skeleton.component";
import { LocaleFlagComponent } from "@features/locales/components/locale-flag.component";
import { LocalesStore } from "@features/locales/stores/locales.store";
import { RemoveMemberDialog } from "@features/projects/components/remove-member.dialog";
import { ProjectMember } from "@features/projects/models/membership.model";
import { ProjectsStore } from "@features/projects/stores/projects.store";
import { SessionStore } from "@features/session/stores/session.store";
import { TranslateModule } from "@ngx-translate/core";
import { firstValueFrom } from "rxjs";

/**
 * Project members and the links that let a translator join without an e-mail.
 */
@Component({
    selector: "app-project-members",
    standalone: true,
    imports: [
        DatePipe,
        ReactiveFormsModule,
        TranslateModule,
        MatCard,
        MatCardHeader,
        MatCardTitle,
        MatCardContent,
        MatFormField,
        MatLabel,
        MatError,
        MatInput,
        MatButton,
        MatIconButton,
        MatIcon,
        MatCheckbox,
        LocaleFlagComponent,
        SkeletonComponent,
    ],
    templateUrl: "./project-members.component.html",
    styleUrl: "./project-members.component.scss",
})
export class ProjectMembersComponent {
    public readonly projectsStore = inject(ProjectsStore);
    public readonly localesStore = inject(LocalesStore);
    public readonly owner = computed(() => this.sessionStore.account()?.isInstanceOwner === true);
    public readonly project = computed(() => this.projectsStore.current());
    public readonly link = signal<string | null>(null);
    public readonly linkInviteId = signal<string | null>(null);
    public readonly copied = signal(false);
    public readonly editing = signal<string | null>(null);
    public readonly loaded = signal(false);
    public readonly inviteLocales = signal<string[]>([]);
    public readonly editLocales = signal<string[]>([]);
    public readonly form = new FormGroup({
        email: new FormControl("", { nonNullable: true, validators: [Validators.required, Validators.email] }),
    });

    private readonly sessionStore = inject(SessionStore);
    private readonly dialog = inject(MatDialog);
    private readonly slug = inject(ActivatedRoute).parent?.snapshot.paramMap.get("project") ?? "";

    public constructor() {
        void this.localesStore.load();
        if (this.owner() && this.slug) {
            void this.projectsStore.loadMembers(this.slug).finally(() => this.loaded.set(true));
        }
    }

    public names(locales: string[]): string {
        return locales.map((locale) => this.localesStore.label(locale) || locale).join(", ");
    }

    public toggleInvite(code: string, checked: boolean): void {
        this.inviteLocales.set(this.withLocale(this.inviteLocales(), code, checked));
    }

    public toggleEdit(code: string, checked: boolean): void {
        this.editLocales.set(this.withLocale(this.editLocales(), code, checked));
    }

    public startEdit(member: ProjectMember): void {
        this.editing.set(member.accountId);
        this.editLocales.set([...member.locales]);
    }

    public async saveEdit(): Promise<void> {
        const accountId = this.editing();
        if (!accountId || this.editLocales().length === 0) {
            return;
        }

        const ok = await this.projectsStore.updateMember(this.slug, accountId, this.editLocales());
        if (ok) {
            this.editing.set(null);
        }
    }

    public async invite(): Promise<void> {
        if (this.form.invalid || this.inviteLocales().length === 0) {
            this.form.markAllAsTouched();
            return;
        }

        const created = await this.projectsStore.invite(this.slug, this.form.controls.email.getRawValue(), this.inviteLocales());
        if (!created) {
            return;
        }

        this.link.set(`${window.location.origin}/rejoindre?code=${encodeURIComponent(created.code)}`);
        this.linkInviteId.set(created.id);
        this.copied.set(false);
        this.form.reset();
        this.inviteLocales.set([]);
    }

    public async copy(): Promise<void> {
        const link = this.link();
        if (!link) {
            return;
        }

        try {
            await navigator.clipboard.writeText(link);
            this.copied.set(true);
        } catch {
            this.copied.set(false);
        }
    }

    public async revoke(inviteId: string): Promise<void> {
        const removed = await this.projectsStore.revokeInvite(this.slug, inviteId);
        if (removed && this.linkInviteId() === inviteId) {
            this.link.set(null);
            this.linkInviteId.set(null);
        }
    }

    public async remove(member: ProjectMember): Promise<void> {
        const confirmed = await firstValueFrom(this.dialog.open(RemoveMemberDialog).afterClosed());
        if (confirmed === true) {
            await this.projectsStore.removeMember(this.slug, member.accountId);
        }
    }

    private withLocale(locales: string[], code: string, checked: boolean): string[] {
        const current = new Set(locales);
        if (checked) {
            current.add(code);
        } else {
            current.delete(code);
        }
        return [...current];
    }
}
