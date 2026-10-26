import { Component, inject } from "@angular/core";
import { MatButton } from "@angular/material/button";
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle } from "@angular/material/dialog";
import { TranslateModule } from "@ngx-translate/core";

/**
 * Confirms refusal of one or more translation proposals.
 */
@Component({
    selector: "app-refuse-proposals-dialog",
    standalone: true,
    imports: [MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatButton, TranslateModule],
    template: `
        <h2 mat-dialog-title>{{ "PROJECT.REFUSE_TITLE" | translate }}</h2>
        <mat-dialog-content>{{ "PROJECT.REFUSE_BODY" | translate: { count: data.count } }}</mat-dialog-content>
        <mat-dialog-actions align="end">
            <button mat-button type="button" mat-dialog-close>{{ "PROJECT.CANCEL" | translate }}</button>
            <button mat-flat-button color="warn" type="button" [mat-dialog-close]="true">
                {{ "PROJECT.REFUSE" | translate }}
            </button>
        </mat-dialog-actions>
    `,
})
export class RefuseProposalsDialog {
    public readonly data = inject<{ count: number }>(MAT_DIALOG_DATA);
}
