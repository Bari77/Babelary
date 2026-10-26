import { inject } from "@angular/core";
import { CanDeactivateFn } from "@angular/router";
import { TranslateService } from "@ngx-translate/core";

/**
 * An editor that knows whether a row was typed and not saved.
 */
export interface DirtyEditor {
    dirty(): boolean;
}

/**
 * Asks before leaving an editor row that was typed but not saved.
 */
export const leaveEditorGuard: CanDeactivateFn<DirtyEditor> = (component) => {
    if (!component.dirty()) {
        return true;
    }

    return window.confirm(inject(TranslateService).instant("PROJECT.LEAVE_DIRTY"));
};
