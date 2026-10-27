import { Component, computed, inject, signal } from "@angular/core";
import { MatDialog } from "@angular/material/dialog";
import { MatIcon } from "@angular/material/icon";
import { MatProgressSpinner } from "@angular/material/progress-spinner";
import { MatTooltip } from "@angular/material/tooltip";
import { ActivatedRoute } from "@angular/router";
import { SkeletonComponent } from "@core/skeleton.component";
import { DeleteFileDialog } from "@features/projects/components/delete-file.dialog";
import {
    ImportPanelComponent,
    ImportQueueItem,
    PickedFile,
    selectionItems,
} from "@features/projects/components/import-panel.component";
import { ImportFileResult } from "@features/projects/models/source-file.model";
import { ProjectsStore } from "@features/projects/stores/projects.store";
import { SessionStore } from "@features/session/stores/session.store";
import { TranslateModule } from "@ngx-translate/core";
import { firstValueFrom } from "rxjs";

/**
 * Source files of a project. Only the owner imports them.
 */
@Component({
    selector: "app-project-source",
    standalone: true,
    imports: [
        TranslateModule,
        MatIcon,
        MatTooltip,
        MatProgressSpinner,
        SkeletonComponent,
        ImportPanelComponent,
    ],
    templateUrl: "./project-source.component.html",
    styleUrl: "./project-source.component.scss",
})
export class ProjectSourceComponent {
    public readonly projectsStore = inject(ProjectsStore);
    public readonly owner = computed(() => this.sessionStore.account()?.isInstanceOwner === true);
    public readonly selected = signal<PickedFile[]>([]);
    public readonly results = signal<ImportFileResult[]>([]);
    public readonly busy = signal(false);
    public readonly deleting = signal<string | null>(null);
    public readonly failed = signal(false);
    public readonly loaded = signal(false);
    public readonly keyTotal = computed(() => this.projectsStore.sourceFiles().reduce((sum, file) => sum + file.keyCount, 0));
    public readonly items = computed<ImportQueueItem[]>(() => {
        if (this.busy()) {
            return selectionItems(this.selected(), true);
        }

        const results = this.results();
        if (results.length > 0) {
            return results.map((row) => this.toItem(row));
        }

        return selectionItems(this.selected(), false);
    });

    private readonly sessionStore = inject(SessionStore);
    private readonly route = inject(ActivatedRoute);
    private readonly dialog = inject(MatDialog);

    public constructor() {
        const slug = (this.route.parent ?? this.route).snapshot.paramMap.get("project");
        if (slug && this.sessionStore.account()?.isInstanceOwner === true) {
            void this.projectsStore.loadFiles(slug).finally(() => this.loaded.set(true));
        }
    }

    public onFiles(files: PickedFile[]): void {
        this.selected.set(files);
        this.results.set([]);
        this.failed.set(false);
    }

    public remove(name: string): void {
        this.selected.update((files) => files.filter((file) => file.name !== name));
    }

    public async confirmDelete(name: string): Promise<void> {
        const slug = (this.route.parent ?? this.route).snapshot.paramMap.get("project");
        if (!slug || this.deleting()) {
            return;
        }

        const confirmed = await firstValueFrom(this.dialog.open(DeleteFileDialog, { data: { name } }).afterClosed());
        if (confirmed !== true) {
            return;
        }

        this.deleting.set(name);
        const removed = await this.projectsStore.deleteFile(slug, name);
        this.deleting.set(null);
        if (removed) {
            this.results.update((rows) => rows.filter((row) => row.name !== name));
        }
    }

    public async import(): Promise<void> {
        const slug = (this.route.parent ?? this.route).snapshot.paramMap.get("project");
        const files = this.selected();
        if (!slug || files.length === 0) {
            return;
        }

        this.busy.set(true);
        this.failed.set(false);
        const results = await this.projectsStore.importSources(slug, files);
        this.busy.set(false);
        this.failed.set(results === null);
        if (results) {
            this.results.set(results);
            this.selected.set([]);
        }
    }

    private toItem(row: ImportFileResult): ImportQueueItem {
        if (row.error) {
            return {
                name: row.name,
                phase: "error",
                stats: [],
                notes: [{ key: "ERROR." + row.error }],
                summaryKey: null,
                summaryParams: {},
            };
        }

        return {
            name: row.name,
            phase: "done",
            stats: [
                { key: "PROJECT.STAT_CREATED", value: row.created },
                { key: "PROJECT.STAT_UPDATED", value: row.updated },
                { key: "PROJECT.STAT_DELETED", value: row.deleted },
            ],
            notes: [],
            summaryKey: "PROJECT.FILES_RESULT",
            summaryParams: { created: row.created, updated: row.updated, deleted: row.deleted },
        };
    }
}
