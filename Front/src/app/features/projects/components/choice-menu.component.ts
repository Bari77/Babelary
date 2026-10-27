import { Component, ElementRef, computed, input, output, signal, viewChild } from "@angular/core";
import { MatFormField, MatLabel } from "@angular/material/form-field";
import { MatIcon } from "@angular/material/icon";
import { MatInput } from "@angular/material/input";
import { MatMenu, MatMenuItem, MatMenuTrigger } from "@angular/material/menu";
import { MatTooltip } from "@angular/material/tooltip";
import { LocaleFlagComponent } from "@features/locales/components/locale-flag.component";
import { matchesPrefix } from "@features/locales/models/locale.model";

/**
 * One row of a language or file menu.
 */
export interface ChoiceOption {
    id: string;
    name: string;
    detail: string;
    pending: number;
}

/**
 * Compact trigger that opens a searchable menu. Built for a dozen languages or a few dozen files.
 */
@Component({
    selector: "app-choice-menu",
    standalone: true,
    imports: [MatMenu, MatMenuItem, MatMenuTrigger, MatIcon, MatTooltip, MatFormField, MatLabel, MatInput, LocaleFlagComponent],
    template: `
        <button
            type="button"
            class="trigger"
            [matMenuTriggerFor]="panel"
            [matTooltip]="value()"
            [attr.aria-label]="label() + ' ' + value()"
            (menuOpened)="onOpened()"
            (menuClosed)="query.set('')"
        >
            <span class="kind">{{ label() }}</span>
            <app-locale-flag [code]="current()" />
            <span class="current">{{ value() }}</span>
            <mat-icon>expand_more</mat-icon>
        </button>
        <mat-menu #panel="matMenu" class="choice-menu">
            <div class="finder" (click)="$event.stopPropagation()" (mousedown)="$event.stopPropagation()" (keydown)="onSearchKey($event)">
                <mat-form-field subscriptSizing="dynamic">
                    <mat-label>{{ searchLabel() }}</mat-label>
                    <input #search matInput autocomplete="off" [value]="query()" (input)="onQuery($event)" />
                </mat-form-field>
            </div>
            @for (option of visible(); track option.id) {
                <button mat-menu-item type="button" (click)="picked.emit(option.id)">
                    @if (option.id === current()) {
                        <mat-icon>check</mat-icon>
                    }
                    <span class="choice">
                        <app-locale-flag [code]="option.id" />
                        <span class="choice-name">{{ option.name }}</span>
                        @if (option.detail) {
                            <span class="choice-count">{{ option.detail }}</span>
                        }
                        @if (option.pending > 0) {
                            <span class="proposal-count">{{ option.pending }}</span>
                        }
                    </span>
                </button>
            } @empty {
                <button mat-menu-item type="button" disabled>{{ emptyLabel() }}</button>
            }
        </mat-menu>
    `,
    styles: `
        :host {
            display: inline-flex;
            max-width: 16rem;
            min-width: 0;
        }

        .trigger {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            width: 100%;
            height: 32px;
            padding: 0 6px 0 10px;
            border: 1px solid var(--mat-sys-outline-variant);
            border-radius: 999px;
            background: transparent;
            color: inherit;
            cursor: pointer;
            font: var(--mat-sys-body-medium);
        }

        .kind {
            color: var(--mat-sys-on-surface-variant);
            font: var(--mat-sys-label-medium);
            white-space: nowrap;
        }

        .current {
            min-width: 0;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
        }

        .trigger mat-icon {
            flex-shrink: 0;
            width: 18px;
            height: 18px;
            color: var(--mat-sys-on-surface-variant);
            font-size: 18px;
        }

        .finder {
            position: sticky;
            top: 0;
            z-index: 1;
            padding: 8px 8px 4px;
            background: var(--mat-sys-surface-container);
        }

        .choice {
            display: flex;
            align-items: center;
            gap: 8px;
            width: 100%;
            min-width: 0;
        }

        .choice-name {
            flex: 1;
            min-width: 0;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
        }

        .choice-count {
            color: var(--mat-sys-on-surface-variant);
            font: var(--mat-sys-label-medium);
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
export class ChoiceMenuComponent {
    public readonly label = input.required<string>();
    public readonly value = input.required<string>();
    public readonly current = input.required<string>();
    public readonly searchLabel = input.required<string>();
    public readonly emptyLabel = input.required<string>();
    public readonly options = input.required<readonly ChoiceOption[]>();
    public readonly picked = output<string>();
    public readonly query = signal("");
    public readonly visible = computed(() =>
        this.options().filter((option) => matchesPrefix(`${option.name} ${option.id}`, this.query())),
    );

    private readonly searchInput = viewChild<ElementRef<HTMLInputElement>>("search");

    public onOpened(): void {
        this.query.set("");
        setTimeout(() => this.searchInput()?.nativeElement.focus());
    }

    public onQuery(event: Event): void {
        const input = event.target;
        if (input instanceof HTMLInputElement) {
            this.query.set(input.value);
        }
    }

    public onSearchKey(event: KeyboardEvent): void {
        if (event.key !== "Escape" && event.key !== "Tab") {
            event.stopPropagation();
        }
    }
}
