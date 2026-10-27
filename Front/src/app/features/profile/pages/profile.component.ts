import { DatePipe } from "@angular/common";
import { Component, inject, OnDestroy, signal } from "@angular/core";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { MatButton, MatIconButton } from "@angular/material/button";
import { MatCard, MatCardContent, MatCardHeader, MatCardTitle } from "@angular/material/card";
import { MatChip, MatChipSet } from "@angular/material/chips";
import { MatDialog } from "@angular/material/dialog";
import { MatError, MatFormField, MatHint, MatLabel, MatSuffix } from "@angular/material/form-field";
import { MatIcon } from "@angular/material/icon";
import { MatInput } from "@angular/material/input";
import {
    MatList,
    MatListItem,
    MatListItemIcon,
    MatListItemLine,
    MatListItemTitle,
} from "@angular/material/list";
import { MatProgressSpinner } from "@angular/material/progress-spinner";
import { MatSnackBar } from "@angular/material/snack-bar";
import { SkeletonComponent } from "@core/skeleton.component";
import { CliToken } from "@features/profile/models/cli-token.model";
import { RevokeTokenDialog } from "@features/profile/components/revoke-token.dialog";
import { ProfileStore } from "@features/profile/stores/profile.store";
import { passwordsMatch } from "@features/session/passwords-match";
import { SessionStore } from "@features/session/stores/session.store";
import { TranslateModule, TranslateService } from "@ngx-translate/core";
import { firstValueFrom } from "rxjs";

/**
 * Name, password, and CLI tokens.
 */
@Component({
    selector: "app-profile",
    standalone: true,
    imports: [
        ReactiveFormsModule,
        TranslateModule,
        DatePipe,
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
        MatChipSet,
        MatChip,
        MatList,
        MatListItem,
        MatListItemIcon,
        MatListItemTitle,
        MatListItemLine,
        MatProgressSpinner,
        SkeletonComponent,
    ],
    templateUrl: "./profile.component.html",
    styleUrl: "./profile.component.scss",
})
export class ProfileComponent implements OnDestroy {
    public readonly sessionStore = inject(SessionStore);
    public readonly profileStore = inject(ProfileStore);
    public readonly hidePassword = signal(true);
    public readonly nameForm = new FormGroup({
        name: new FormControl(this.sessionStore.account()?.name ?? "", {
            nonNullable: true,
            validators: [Validators.required, Validators.maxLength(120)],
        }),
    });
    public readonly passwordForm = new FormGroup(
        {
            currentPassword: new FormControl("", { nonNullable: true, validators: [Validators.required] }),
            password: new FormControl("", {
                nonNullable: true,
                validators: [Validators.required, Validators.minLength(12)],
            }),
            confirm: new FormControl("", { nonNullable: true, validators: [Validators.required] }),
        },
        { validators: passwordsMatch },
    );
    public readonly tokenForm = new FormGroup({
        name: new FormControl("", {
            nonNullable: true,
            validators: [Validators.required, Validators.maxLength(80)],
        }),
    });

    private readonly dialog = inject(MatDialog);
    private readonly snackBar = inject(MatSnackBar);
    private readonly translate = inject(TranslateService);

    public constructor() {
        void this.profileStore.load();
    }

    public ngOnDestroy(): void {
        this.profileStore.dismissRevealed();
    }

    public async saveName(): Promise<void> {
        if (this.nameForm.invalid) {
            this.nameForm.markAllAsTouched();
            return;
        }

        const ok = await this.profileStore.updateName(this.nameForm.getRawValue().name);
        if (ok) {
            this.notify("PROFILE.SAVED");
        }
    }

    public async savePassword(): Promise<void> {
        if (this.passwordForm.invalid) {
            this.passwordForm.markAllAsTouched();
            return;
        }

        const value = this.passwordForm.getRawValue();
        const ok = await this.profileStore.changePassword(value.currentPassword, value.password);
        if (ok) {
            this.passwordForm.reset();
            this.notify("PROFILE.SAVED");
        }
    }

    public async createToken(): Promise<void> {
        if (this.tokenForm.invalid) {
            this.tokenForm.markAllAsTouched();
            return;
        }

        const ok = await this.profileStore.createToken(this.tokenForm.getRawValue().name);
        if (ok) {
            this.tokenForm.reset();
        }
    }

    public async revoke(token: CliToken): Promise<void> {
        const confirmed = await firstValueFrom(this.dialog.open(RevokeTokenDialog).afterClosed());
        if (confirmed !== true) {
            return;
        }

        const ok = await this.profileStore.revoke(token.id);
        if (ok) {
            this.notify("PROFILE.REVOKED");
        }
    }

    public async copy(secret: string): Promise<void> {
        await navigator.clipboard.writeText(secret);
        this.notify("GLOBAL.COPIED");
    }

    private notify(key: string): void {
        this.snackBar.open(this.translate.instant(key), undefined, { duration: 2500 });
    }
}
