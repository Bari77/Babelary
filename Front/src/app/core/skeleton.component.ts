import { Component, computed, input } from "@angular/core";
import { TranslateModule } from "@ngx-translate/core";

/**
 * Placeholder rows shown while a list is still loading.
 */
@Component({
    selector: "app-skeleton",
    standalone: true,
    imports: [TranslateModule],
    host: {
        "[attr.data-shape]": "shape()",
    },
    template: `
        <div class="bones" role="status" [attr.aria-label]="'GLOBAL.LOADING' | translate">
            @for (row of rows(); track row) {
                <div class="bone">
                    @for (width of bars(row); track $index) {
                        <span class="bar" [style.width.%]="width"></span>
                    }
                </div>
            }
        </div>
    `,
    styles: `
        :host {
            display: grid;
            gap: 0;
        }

        :host([data-shape="cards"]) {
            grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
            gap: 10px;
        }

        .bones {
            display: contents;
        }

        .bone {
            display: flex;
            align-items: center;
            gap: 16px;
            min-height: 44px;
            padding: 8px 0;
            border-bottom: 1px solid var(--mat-sys-outline-variant);
        }

        :host([data-shape="cards"]) .bone {
            display: grid;
            gap: 10px;
            align-content: center;
            min-height: 72px;
            padding: 14px 16px;
            border: 0;
            border-radius: 12px;
            background: var(--mat-sys-surface-container);
        }

        :host([data-shape="keys"]) .bone {
            display: grid;
            gap: 10px;
            align-items: start;
            padding: 16px 0;
        }

        .bar {
            display: block;
            height: 12px;
            border-radius: 999px;
            background: linear-gradient(
                100deg,
                var(--mat-sys-surface-container-high) 25%,
                var(--mat-sys-surface-container-highest) 45%,
                var(--mat-sys-surface-container-high) 65%
            );
            background-size: 220% 100%;
            animation: skeleton 1.3s ease-in-out infinite;
        }

        :host([data-shape="keys"]) .bar:not(:first-child) {
            height: 40px;
            border-radius: 10px;
        }

        @keyframes skeleton {
            from {
                background-position: 100% 0;
            }

            to {
                background-position: -100% 0;
            }
        }

        @media (prefers-reduced-motion: reduce) {
            .bar {
                animation: none;
            }
        }
    `,
})
export class SkeletonComponent {
    public readonly count = input(6);
    public readonly shape = input<"rows" | "cards" | "keys">("rows");
    public readonly rows = computed(() => Array.from({ length: Math.max(1, this.count()) }, (_, index) => index));

    public bars(row: number): number[] {
        if (this.shape() === "cards") {
            return [58 + (row % 3) * 12, 36];
        }

        if (this.shape() === "keys") {
            return [24 + (row % 4) * 8, 100, 88];
        }

        return [30 + (row % 4) * 8, 14, 34];
    }
}
