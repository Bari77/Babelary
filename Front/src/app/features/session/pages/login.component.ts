import { Component, inject, signal } from "@angular/core";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { MatButton, MatIconButton } from "@angular/material/button";
import { MatCard, MatCardContent, MatCardHeader, MatCardTitle } from "@angular/material/card";
import { MatError, MatFormField, MatLabel, MatSuffix } from "@angular/material/form-field";
import { MatIcon } from "@angular/material/icon";
import { MatInput } from "@angular/material/input";
import { MatProgressSpinner } from "@angular/material/progress-spinner";
import { ActivatedRoute, Router } from "@angular/router";
import { SessionStore } from "@features/session/stores/session.store";
import { TranslateModule } from "@ngx-translate/core";

/**
 * Email and password sign-in.
 */
@Component({
    selector: "app-login",
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
        MatSuffix,
        MatInput,
        MatButton,
        MatIconButton,
        MatIcon,
        MatProgressSpinner,
    ],
    templateUrl: "./login.component.html",
})
export class LoginComponent {
    public readonly sessionStore = inject(SessionStore);
    public readonly hidePassword = signal(true);
    public readonly form = new FormGroup({
        email: new FormControl("", { nonNullable: true, validators: [Validators.required, Validators.email] }),
        password: new FormControl("", { nonNullable: true, validators: [Validators.required] }),
    });

    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);

    public async submit(): Promise<void> {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        const value = this.form.getRawValue();
        const ok = await this.sessionStore.login({ email: value.email, password: value.password });
        if (ok) {
            const retour = this.route.snapshot.queryParamMap.get("retour");
            await this.router.navigateByUrl(retour?.startsWith("/rejoindre") ? retour : "/");
        }
    }
}
