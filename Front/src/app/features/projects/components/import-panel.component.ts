import { Component, ElementRef, computed, input, output, signal, viewChild } from "@angular/core";
import { MatButton, MatIconButton } from "@angular/material/button";
import { MatIcon } from "@angular/material/icon";
import { MatProgressBar } from "@angular/material/progress-bar";
import { MatProgressSpinner } from "@angular/material/progress-spinner";
import { TranslateModule } from "@ngx-translate/core";

/**
 * One chosen file, still waiting to be sent.
 */
export interface PickedFile {
    name: string;
    content: string;
    size: number;
}

/**
 * A line under a finished import: a missing key, or a key the server refused.
 */
export interface ImportNote {
    key: string;
    params?: Record<string, string | number>;
    prefix?: string;
}

/**
 * Visual state of one file in the import queue.
 */
export type ImportPhase = "ready" | "running" | "done" | "warning" | "error";

/**
 * What the queue draws for one file.
 */
export interface ImportQueueItem {
    name: string;
    phase: ImportPhase;
    size?: number;
    stats: { key: string; value: number }[];
    notes: ImportNote[];
    summaryKey: string | null;
    summaryParams: Record<string, number>;
}

/**
 * Files chosen and not yet sent, or the same files while the request is in flight.
 */
export function selectionItems(files: { name: string; size?: number }[], running: boolean): ImportQueueItem[] {
    return files.map((file) => ({
        name: file.name,
        phase: running ? "running" : "ready",
        size: file.size,
        stats: [],
        notes: [],
        summaryKey: null,
        summaryParams: {},
    }));
}

/**
 * Drop zone and per-file import states. The parent decides when to send and what the result means.
 */
@Component({
    selector: "app-import-panel",
    standalone: true,
    imports: [TranslateModule, MatButton, MatIconButton, MatIcon, MatProgressBar, MatProgressSpinner],
    templateUrl: "./import-panel.component.html",
    styleUrl: "./import-panel.component.scss",
})
export class ImportPanelComponent {
    public readonly items = input<ImportQueueItem[]>([]);
    public readonly busy = input(false);
    public readonly canPick = input(true);
    public readonly canImport = input(false);
    public readonly error = input<string | null>(null);
    public readonly picked = output<PickedFile[]>();
    public readonly removed = output<string>();
    public readonly confirmed = output<void>();

    public readonly over = signal(false);
    public readonly actionable = computed(() => this.items().some((item) => item.phase === "ready" || item.phase === "running"));
    public readonly phaseKey: Record<ImportPhase, string> = {
        ready: "PROJECT.IMPORT_READY",
        running: "PROJECT.IMPORT_RUNNING",
        done: "PROJECT.IMPORT_DONE",
        warning: "PROJECT.IMPORT_WARNING",
        error: "PROJECT.IMPORT_FAILED",
    };

    private readonly picker = viewChild<ElementRef<HTMLInputElement>>("picker");
    private depth = 0;

    public openPicker(event?: Event): void {
        event?.stopPropagation();
        if (this.busy() || !this.canPick()) {
            return;
        }

        this.picker()?.nativeElement.click();
    }

    public async onInput(event: Event): Promise<void> {
        const input = event.target;
        if (!this.canPick() || !(input instanceof HTMLInputElement) || !input.files || input.files.length === 0) {
            return;
        }

        const files = await readFiles(input.files);
        input.value = "";
        if (files.length > 0) {
            this.picked.emit(files);
        }
    }

    public onDragEnter(event: DragEvent): void {
        event.preventDefault();
        if (this.busy() || !this.canPick()) {
            return;
        }

        this.depth += 1;
        this.over.set(true);
    }

    public onDragOver(event: DragEvent): void {
        event.preventDefault();
    }

    public onDragLeave(): void {
        this.depth = Math.max(0, this.depth - 1);
        if (this.depth === 0) {
            this.over.set(false);
        }
    }

    public async onDrop(event: DragEvent): Promise<void> {
        event.preventDefault();
        this.depth = 0;
        this.over.set(false);
        if (this.busy() || !this.canPick() || !event.dataTransfer?.files.length) {
            return;
        }

        const files = await readFiles(event.dataTransfer.files);
        if (files.length > 0) {
            this.picked.emit(files);
        }
    }

    public formatSize(bytes: number): string {
        if (bytes < 1024) {
            return `${bytes} o`;
        }

        if (bytes < 1024 * 1024) {
            return `${Math.max(1, Math.round(bytes / 1024))} Ko`;
        }

        return `${(bytes / (1024 * 1024)).toFixed(1)} Mo`;
    }
}

async function readFiles(list: FileList): Promise<PickedFile[]> {
    return Promise.all(
        [...list].map(async (file) => ({
            name: file.name,
            content: await file.text(),
            size: file.size,
        })),
    );
}
