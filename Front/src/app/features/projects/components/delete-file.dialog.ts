import { Component, inject } from "@angular/core";
import { MatButton } from "@angular/material/button";
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle } from "@angular/material/dialog";
import { TranslateModule } from "@ngx-translate/core";

/**
 * Confirms that deleting a source file removes its keys and translations.
 */
@Component({
    selector: "app-delete-file-dialog",
    standalone: true,
    imports: [MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatButton, TranslateModule],
    template: `
        <h2 mat-dialog-title>{{ "PROJECT.DELETE_FILE_TITLE" | translate }}</h2>
        <mat-dialog-content>{{ "PROJECT.DELETE_FILE_BODY" | translate: { name: data.name } }}</mat-dialog-content>
        <mat-dialog-actions align="end">
            <button mat-button type="button" mat-dialog-close>{{ "PROJECT.CANCEL" | translate }}</button>
            <button mat-flat-button class="danger" type="button" [mat-dialog-close]="true">
                {{ "PROJECT.DELETE_CONFIRM" | translate }}
            </button>
        </mat-dialog-actions>
    `,
})
export class DeleteFileDialog {
    public readonly data = inject<{ name: string }>(MAT_DIALOG_DATA);
}
