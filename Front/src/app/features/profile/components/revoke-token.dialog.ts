import { Component } from "@angular/core";
import { MatButton } from "@angular/material/button";
import {
    MatDialogActions,
    MatDialogClose,
    MatDialogContent,
    MatDialogTitle,
} from "@angular/material/dialog";
import { TranslateModule } from "@ngx-translate/core";

/**
 * Confirms that a CLI token will stop working.
 */
@Component({
    selector: "app-revoke-token-dialog",
    standalone: true,
    imports: [MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatButton, TranslateModule],
    template: `
        <h2 mat-dialog-title>{{ "PROFILE.REVOKE_TITLE" | translate }}</h2>
        <mat-dialog-content>{{ "PROFILE.REVOKE_BODY" | translate }}</mat-dialog-content>
        <mat-dialog-actions align="end">
            <button mat-button type="button" mat-dialog-close>{{ "PROFILE.CANCEL" | translate }}</button>
            <button mat-flat-button color="warn" type="button" [mat-dialog-close]="true">
                {{ "PROFILE.REVOKE" | translate }}
            </button>
        </mat-dialog-actions>
    `,
})
export class RevokeTokenDialog {}
