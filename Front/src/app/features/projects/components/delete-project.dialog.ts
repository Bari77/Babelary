import { Component } from "@angular/core";
import { MatButton } from "@angular/material/button";
import { MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle } from "@angular/material/dialog";
import { TranslateModule } from "@ngx-translate/core";

/**
 * Confirms that deleting a project removes it completely.
 */
@Component({
    selector: "app-delete-project-dialog",
    standalone: true,
    imports: [MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatButton, TranslateModule],
    template: `
        <h2 mat-dialog-title>{{ "PROJECT.DELETE_TITLE" | translate }}</h2>
        <mat-dialog-content>{{ "PROJECT.DELETE_BODY" | translate }}</mat-dialog-content>
        <mat-dialog-actions align="end">
            <button mat-button type="button" mat-dialog-close>{{ "PROJECT.CANCEL" | translate }}</button>
            <button mat-flat-button class="danger" type="button" [mat-dialog-close]="true">
                {{ "PROJECT.DELETE_CONFIRM" | translate }}
            </button>
        </mat-dialog-actions>
    `,
})
export class DeleteProjectDialog {}
