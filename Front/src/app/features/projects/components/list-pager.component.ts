import { Component, computed, input, output } from "@angular/core";
import { MatIcon } from "@angular/material/icon";
import { TranslateModule } from "@ngx-translate/core";

/**
 * Page window over a list. `start` is the first index, `end` is exclusive.
 */
export function pageWindow(length: number, pageIndex: number, pageSize: number): { page: number; start: number; end: number } {
    const size = pageSize > 0 ? pageSize : 50;
    const pages = Math.max(1, Math.ceil(length / size));
    const page = Math.min(Math.max(pageIndex, 0), pages - 1);
    const start = length === 0 ? 0 : page * size;
    return { page, start, end: Math.min(length, start + size) };
}

/**
 * Pages a long list by 50 or 100 rows. Hidden while every row already fits on one page.
 */
@Component({
    selector: "app-list-pager",
    standalone: true,
    imports: [TranslateModule, MatIcon],
    template: `
        @if (length() > 50) {
            <nav class="pager" [attr.aria-label]="'PROJECT.PAGE_NAV' | translate">
                <span class="range">
                    {{ "PROJECT.PAGE_RANGE" | translate: { start: start(), end: end(), total: length() } }}
                </span>
                <div class="sizes" role="group" [attr.aria-label]="'PROJECT.PAGE_SIZE' | translate">
                    @for (size of sizes; track size) {
                        <button
                            type="button"
                            [class.active]="pageSize() === size"
                            [attr.aria-pressed]="pageSize() === size"
                            [attr.aria-label]="'PROJECT.PAGE_SIZE_OPTION' | translate: { count: size }"
                            (click)="chooseSize(size)"
                        >
                            {{ size }}
                        </button>
                    }
                </div>
                <button
                    type="button"
                    class="step"
                    [disabled]="paging().page === 0"
                    [attr.aria-label]="'PROJECT.PAGE_PREVIOUS' | translate"
                    (click)="pageChange.emit(paging().page - 1)"
                >
                    <mat-icon>chevron_left</mat-icon>
                </button>
                <button
                    type="button"
                    class="step"
                    [disabled]="paging().end >= length()"
                    [attr.aria-label]="'PROJECT.PAGE_NEXT' | translate"
                    (click)="pageChange.emit(paging().page + 1)"
                >
                    <mat-icon>chevron_right</mat-icon>
                </button>
            </nav>
        }
    `,
    styles: `
        .pager {
            display: flex;
            flex-wrap: wrap;
            align-items: center;
            gap: 8px;
        }

        .range {
            color: var(--mat-sys-on-surface-variant);
            font: var(--mat-sys-label-medium);
        }

        .sizes {
            display: inline-flex;
            gap: 6px;
        }

        button {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            height: 32px;
            border: 1px solid var(--mat-sys-outline);
            border-radius: 999px;
            background: transparent;
            color: inherit;
            cursor: pointer;
            font: var(--mat-sys-label-medium);
        }

        .sizes button {
            padding: 0 10px;
        }

        .sizes button.active {
            border-color: transparent;
            background: var(--mat-sys-secondary-container);
            color: var(--mat-sys-on-secondary-container);
        }

        .step {
            width: 32px;
            padding: 0;
        }

        .step mat-icon {
            width: 18px;
            height: 18px;
            font-size: 18px;
        }

        button:disabled {
            opacity: 0.38;
            cursor: default;
        }
    `,
})
export class ListPagerComponent {
    public readonly sizes = [50, 100];
    public readonly length = input(0);
    public readonly pageIndex = input(0);
    public readonly pageSize = input(50);
    public readonly pageChange = output<number>();
    public readonly sizeChange = output<number>();
    public readonly paging = computed(() => pageWindow(this.length(), this.pageIndex(), this.pageSize()));
    public readonly start = computed(() => (this.length() === 0 ? 0 : this.paging().start + 1));
    public readonly end = computed(() => this.paging().end);

    public chooseSize(size: number): void {
        if (size !== this.pageSize()) {
            this.sizeChange.emit(size);
        }
    }
}
