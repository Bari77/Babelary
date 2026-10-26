import { Component, computed, input } from "@angular/core";
import { MatIcon } from "@angular/material/icon";
import { MatTooltip } from "@angular/material/tooltip";
import { TranslateModule } from "@ngx-translate/core";

/**
 * Colored pill for a translation or proposal state.
 */
@Component({
    selector: "app-translation-status",
    standalone: true,
    imports: [TranslateModule, MatIcon, MatTooltip],
    template: `
        <span class="pill" [class]="kind()" [matTooltip]="label() | translate">
            <mat-icon>{{ icon() }}</mat-icon>
            <span>{{ label() | translate }}</span>
        </span>
    `,
    styles: `
        .pill {
            display: inline-flex;
            align-items: center;
            gap: 4px;
            height: 22px;
            padding: 0 8px;
            border-radius: 999px;
            font: var(--mat-sys-label-small);
            white-space: nowrap;
        }

        mat-icon {
            width: 14px;
            height: 14px;
            font-size: 14px;
        }

        .ok {
            background: var(--status-ok-bg);
            color: var(--status-ok);
        }

        .warn {
            background: var(--status-warn-bg);
            color: var(--status-warn);
        }

        .proposal {
            background: var(--status-proposal-bg);
            color: var(--status-proposal);
        }

        .bad {
            background: var(--status-bad-bg);
            color: var(--status-bad);
        }
    `,
})
export class TranslationStatusComponent {
    public readonly status = input("");
    public readonly kind = computed(() => kinds[this.status()]?.kind ?? "warn");
    public readonly icon = computed(() => kinds[this.status()]?.icon ?? "help");
    public readonly label = computed(() => kinds[this.status()]?.label ?? "PROJECT.STATUS_EMPTY");
}

const kinds: Record<string, { kind: string; icon: string; label: string }> = {
    empty: { kind: "warn", icon: "hourglass_empty", label: "PROJECT.STATUS_EMPTY" },
    translated: { kind: "ok", icon: "check_circle", label: "PROJECT.STATUS_TRANSLATED" },
    identical: { kind: "ok", icon: "done_all", label: "PROJECT.STATUS_IDENTICAL" },
    review: { kind: "warn", icon: "priority_high", label: "PROJECT.STATUS_REVIEW" },
    pending: { kind: "proposal", icon: "rate_review", label: "PROJECT.STATUS_PENDING" },
    refused: { kind: "bad", icon: "cancel", label: "PROJECT.STATUS_REFUSED" },
};
