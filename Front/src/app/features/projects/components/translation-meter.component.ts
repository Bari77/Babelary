import { Component, computed, input } from "@angular/core";
import { TranslateModule } from "@ngx-translate/core";

/**
 * Approved translations in green, waiting proposals in blue, on one bar.
 */
@Component({
    selector: "app-translation-meter",
    standalone: true,
    imports: [TranslateModule],
    template: `
        <div class="meter-row">
            <div
                class="meter"
                role="meter"
                aria-valuemin="0"
                [attr.aria-valuenow]="covered()"
                [attr.aria-valuemax]="total()"
                [attr.aria-label]="
                    'PROJECT.METER'
                        | translate: { translated: translated(), total: total(), pending: pending(), percent: percent() }
                "
            >
                <span class="ok" [style.width.%]="green()"></span>
                <span class="wait" [style.left.%]="green()" [style.width.%]="blue()"></span>
            </div>
            <span class="percent">{{ percent() }}&nbsp;%</span>
            @if (chips() && !compact() && pending() > 0) {
                <span class="proposal-count">{{ pending() }}</span>
            }
        </div>
    `,
    styles: `
        :host {
            display: block;
            width: max-content;
        }

        .meter-row {
            display: flex;
            align-items: center;
            gap: 6px;
        }

        .meter {
            position: relative;
            flex: 0 0 var(--meter-width, 8rem);
            width: var(--meter-width, 8rem);
            height: 6px;
            border-radius: 999px;
            background: color-mix(in srgb, var(--mat-sys-on-surface) 12%, transparent);
            overflow: hidden;
        }

        .ok,
        .wait {
            position: absolute;
            top: 0;
            bottom: 0;
        }

        .ok {
            left: 0;
            background: var(--status-ok);
        }

        .wait {
            background: var(--status-proposal);
        }

        .percent {
            flex: 0 0 auto;
            min-width: 3rem;
            color: var(--mat-sys-on-surface-variant);
            font: var(--mat-sys-label-medium);
            font-variant-numeric: tabular-nums;
            text-align: end;
            white-space: nowrap;
        }

        .proposal-count {
            min-width: 1.25rem;
            padding: 0 6px;
            border-radius: 999px;
            background: var(--status-proposal-bg);
            color: var(--status-proposal);
            font: var(--mat-sys-label-small);
            text-align: center;
        }
    `,
})
export class TranslationMeterComponent {
    public readonly translated = input(0);
    public readonly total = input(0);
    public readonly pending = input(0);
    public readonly pendingOpen = input(0);
    public readonly compact = input(false);
    public readonly chips = input(true);
    public readonly covered = computed(() => coveredCount(this.translated(), this.pendingOpen(), this.total()));
    public readonly blue = computed(() => {
        const total = this.total();
        const pending = Math.min(Math.max(this.pending(), 0), total);
        if (total <= 0 || pending <= 0) {
            return 0;
        }

        return Math.min(100, Math.max((pending / total) * 100, 1));
    });
    public readonly green = computed(() => {
        const total = this.total();
        const saved = Math.max(0, this.translated() - Math.max(0, this.pending() - this.pendingOpen()));
        if (total <= 0 || saved <= 0) {
            return 0;
        }

        return Math.min((saved / total) * 100, Math.max(0, 100 - this.blue()));
    });
    public readonly percent = computed(() => {
        const covered = this.covered();
        const total = this.total();
        if (total <= 0 || covered <= 0) {
            return 0;
        }

        if (covered >= total) {
            return 100;
        }

        return Math.min(99, Math.max(1, Math.round((covered / total) * 100)));
    });
}

export function coveredCount(translated: number, pendingOpen: number, total: number): number {
    return Math.min(Math.max(total, 0), translated + Math.max(0, pendingOpen));
}
