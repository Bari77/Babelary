import { Component } from "@angular/core";
import { MatButton } from "@angular/material/button";
import { MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle } from "@angular/material/dialog";
import { TranslateModule } from "@ngx-translate/core";

/**
 * Confirms that removing target locales deletes their translations.
 */
@Component({
    selector: "app-remove-targets-dialog",
    standalone: true,
    imports: [MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatButton, TranslateModule],
    template: `
        <h2 mat-dialog-title>{{ "PROJECT.REMOVE_TARGETS_TITLE" | translate }}</h2>
        <mat-dialog-content>{{ "PROJECT.REMOVE_TARGETS_BODY" | translate }}</mat-dialog-content>
        <mat-dialog-actions align="end">
            <button mat-button type="button" mat-dialog-close>{{ "PROJECT.CANCEL" | translate }}</button>
            <button mat-flat-button color="warn" type="button" [mat-dialog-close]="true">
                {{ "PROJECT.REMOVE_TARGETS_CONFIRM" | translate }}
            </button>
        </mat-dialog-actions>
    `,
})
export class RemoveTargetsDialog {}
