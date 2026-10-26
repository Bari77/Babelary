import { Component } from "@angular/core";
import { MatButton } from "@angular/material/button";
import { MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle } from "@angular/material/dialog";
import { TranslateModule } from "@ngx-translate/core";

/**
 * Confirms that resetting a project deletes its source files and translations.
 */
@Component({
    selector: "app-reset-project-dialog",
    standalone: true,
    imports: [MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatButton, TranslateModule],
    template: `
        <h2 mat-dialog-title>{{ "PROJECT.RESET_TITLE" | translate }}</h2>
        <mat-dialog-content>{{ "PROJECT.RESET_BODY" | translate }}</mat-dialog-content>
        <mat-dialog-actions align="end">
            <button mat-button type="button" mat-dialog-close>{{ "PROJECT.CANCEL" | translate }}</button>
            <button mat-flat-button class="danger" type="button" [mat-dialog-close]="true">
                {{ "PROJECT.RESET_CONFIRM" | translate }}
            </button>
        </mat-dialog-actions>
    `,
})
export class ResetProjectDialog {}
