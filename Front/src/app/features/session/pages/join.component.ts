import { Component, computed, inject, signal } from "@angular/core";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { MatButton, MatIconButton } from "@angular/material/button";
import { MatCard, MatCardContent, MatCardHeader, MatCardTitle } from "@angular/material/card";
import { MatError, MatFormField, MatHint, MatLabel, MatSuffix } from "@angular/material/form-field";
import { MatIcon } from "@angular/material/icon";
import { MatInput } from "@angular/material/input";
import { MatProgressSpinner } from "@angular/material/progress-spinner";
import { ActivatedRoute, Router } from "@angular/router";
import { localeLabel } from "@features/locales/models/locale.model";
import { InvitePreview } from "@features/projects/models/membership.model";
import { ProjectsStore } from "@features/projects/stores/projects.store";
import { SessionStore } from "@features/session/stores/session.store";
import { TranslateModule } from "@ngx-translate/core";

/**
 * Invitation link. Sign-in is the default. An account is created only when this e-mail has none.
 */
@Component({
    selector: "app-join",
    standalone: true,
    imports: [
        ReactiveFormsModule,
        TranslateModule,
        MatCard,
        MatCardHeader,
        MatCardTitle,
        MatCardContent,
        MatFormField,
        MatLabel,
        MatHint,
        MatError,
        MatSuffix,
        MatInput,
        MatButton,
        MatIconButton,
        MatIcon,
        MatProgressSpinner,
    ],
    templateUrl: "./join.component.html",
    styleUrl: "./join.component.scss",
})
export class JoinComponent {
    public readonly projectsStore = inject(ProjectsStore);
    public readonly sessionStore = inject(SessionStore);
    public readonly preview = signal<InvitePreview | null>(null);
    public readonly creating = signal(false);
    public readonly hidePassword = signal(true);
    public readonly loginForm = new FormGroup({
        password: new FormControl("", { nonNullable: true, validators: [Validators.required] }),
    });
    public readonly createForm = new FormGroup({
        name: new FormControl("", { nonNullable: true, validators: [Validators.required, Validators.maxLength(80)] }),
        password: new FormControl("", { nonNullable: true, validators: [Validators.required, Validators.minLength(12)] }),
    });

    public readonly signedInEmail = computed(() => this.sessionStore.account()?.email ?? "");

    public readonly sameAccount = computed(() => {
        const preview = this.preview();
        const email = this.signedInEmail();
        return !!preview?.accountExists && email.toLowerCase() === preview.email.toLowerCase();
    });

    private readonly router = inject(Router);
    private readonly code = inject(ActivatedRoute).snapshot.queryParamMap.get("code") ?? "";

    public constructor() {
        void this.load();
    }

    public names(): string {
        return (this.preview()?.locales ?? []).map((locale) => localeLabel(locale) || locale).join(", ");
    }

    public showCreate(): void {
        this.creating.set(true);
        this.sessionStore.error.set(null);
        this.projectsStore.joinError.set(null);
    }

    public showLogin(): void {
        this.creating.set(false);
        this.sessionStore.error.set(null);
        this.projectsStore.joinError.set(null);
    }

    public async login(): Promise<void> {
        const preview = this.preview();
        if (!preview || this.loginForm.invalid) {
            this.loginForm.markAllAsTouched();
            return;
        }

        const ok = await this.sessionStore.login({
            email: preview.email,
            password: this.loginForm.controls.password.getRawValue(),
        });
        if (!ok) {
            return;
        }

        await this.enter(preview);
    }

    public async create(): Promise<void> {
        const preview = this.preview();
        if (!preview || preview.accountExists || this.createForm.invalid) {
            this.createForm.markAllAsTouched();
            return;
        }

        const value = this.createForm.getRawValue();
        const account = await this.projectsStore.acceptInvite({
            code: this.code,
            name: value.name,
            password: value.password,
        });
        if (account) {
            await this.router.navigateByUrl(`/projet/${preview.projectSlug}`);
        }
    }

    public async enter(preview = this.preview()): Promise<void> {
        if (!preview) {
            return;
        }

        const account = await this.projectsStore.acceptInvite({ code: this.code });
        if (account) {
            await this.router.navigateByUrl(`/projet/${preview.projectSlug}`);
        }
    }

    private async load(): Promise<void> {
        const preview = await this.projectsStore.previewInvite(this.code);
        this.preview.set(preview);
    }
}
