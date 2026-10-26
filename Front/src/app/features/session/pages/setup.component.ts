import { Component, inject, signal } from "@angular/core";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { MatButton, MatIconButton } from "@angular/material/button";
import { MatCard, MatCardContent, MatCardHeader, MatCardTitle } from "@angular/material/card";
import { MatError, MatFormField, MatHint, MatLabel, MatSuffix } from "@angular/material/form-field";
import { MatIcon } from "@angular/material/icon";
import { MatInput } from "@angular/material/input";
import { MatProgressSpinner } from "@angular/material/progress-spinner";
import { Router } from "@angular/router";
import { passwordsMatch } from "@features/session/passwords-match";
import { SessionStore } from "@features/session/stores/session.store";
import { TranslateModule } from "@ngx-translate/core";

/**
 * Creates the instance owner. This page disappears once an account exists.
 */
@Component({
    selector: "app-setup",
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
        MatError,
        MatHint,
        MatSuffix,
        MatInput,
        MatButton,
        MatIconButton,
        MatIcon,
        MatProgressSpinner,
    ],
    templateUrl: "./setup.component.html",
})
export class SetupComponent {
    public readonly sessionStore = inject(SessionStore);
    public readonly hidePassword = signal(true);
    public readonly form = new FormGroup(
        {
            name: new FormControl("", {
                nonNullable: true,
                validators: [Validators.required, Validators.maxLength(120)],
            }),
            email: new FormControl("", {
                nonNullable: true,
                validators: [Validators.required, Validators.email],
            }),
            password: new FormControl("", {
                nonNullable: true,
                validators: [Validators.required, Validators.minLength(12)],
            }),
            confirm: new FormControl("", { nonNullable: true, validators: [Validators.required] }),
        },
        { validators: passwordsMatch },
    );

    private readonly router = inject(Router);

    public async submit(): Promise<void> {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        const value = this.form.getRawValue();
        const ok = await this.sessionStore.createOwner({
            name: value.name,
            email: value.email,
            password: value.password,
        });
        if (ok) {
            await this.router.navigateByUrl("/");
        }
    }
}
