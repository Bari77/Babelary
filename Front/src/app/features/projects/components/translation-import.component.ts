import { Component, computed, effect, inject, signal } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { LocalesStore } from "@features/locales/stores/locales.store";
import { ChoiceMenuComponent, ChoiceOption } from "@features/projects/components/choice-menu.component";
import {
    ImportNote,
    ImportPanelComponent,
    ImportQueueItem,
    PickedFile,
    selectionItems,
} from "@features/projects/components/import-panel.component";
import { ImportTranslationFileResult } from "@features/projects/models/source-file.model";
import { ProjectsStore } from "@features/projects/stores/projects.store";
import { SessionStore } from "@features/session/stores/session.store";
import { TranslateModule } from "@ngx-translate/core";

/**
 * Translation import for the owner, shown above the language list.
 */
@Component({
    selector: "app-translation-import",
    standalone: true,
    imports: [TranslateModule, ChoiceMenuComponent, ImportPanelComponent],
    templateUrl: "./translation-import.component.html",
    styleUrl: "./translation-import.component.scss",
})
export class TranslationImportComponent {
    public readonly projectsStore = inject(ProjectsStore);
    public readonly localesStore = inject(LocalesStore);
    public readonly owner = computed(() => this.sessionStore.account()?.isInstanceOwner === true);
    public readonly locale = signal("");
    public readonly selected = signal<PickedFile[]>([]);
    public readonly results = signal<ImportTranslationFileResult[]>([]);
    public readonly busy = signal(false);
    public readonly failed = signal(false);
    public readonly localeOptions = computed<ChoiceOption[]>(() =>
        (this.projectsStore.current()?.targets ?? []).map((code) => ({
            id: code,
            name: this.localesStore.label(code) || code,
            detail: "",
            pending: 0,
        })),
    );
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

    public constructor() {
        void this.localesStore.load();
        effect(() => {
            const targets = this.projectsStore.current()?.targets ?? [];
            if (targets.length === 1 && this.locale() === "") {
                this.locale.set(targets[0]);
            }
        });
    }

    public onFiles(files: PickedFile[]): void {
        this.selected.set(files);
        this.results.set([]);
        this.failed.set(false);
    }

    public remove(name: string): void {
        this.selected.update((files) => files.filter((file) => file.name !== name));
    }

    public async import(): Promise<void> {
        const slug = (this.route.parent ?? this.route).snapshot.paramMap.get("project");
        const locale = this.locale();
        const files = this.selected();
        if (!slug || locale === "" || files.length === 0) {
            return;
        }

        this.busy.set(true);
        this.failed.set(false);
        const results = await this.projectsStore.importTranslations(slug, locale, files);
        this.busy.set(false);
        this.failed.set(results === null);
        if (results) {
            this.results.set(results);
            this.selected.set([]);
        }
    }

    private toItem(row: ImportTranslationFileResult): ImportQueueItem {
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

        const notes: ImportNote[] = [];
        if (row.missing === 1) {
            notes.push({ key: "PROJECT.TRANSLATIONS_MISSING_ONE" });
        } else if (row.missing > 1) {
            notes.push({ key: "PROJECT.TRANSLATIONS_MISSING_MANY", params: { count: row.missing } });
        }

        for (const issue of row.issues) {
            notes.push({
                key: "ERROR." + issue.error,
                params: { token: issue.detail ?? "" },
                prefix: issue.path,
            });
        }

        return {
            name: row.name,
            phase: notes.length > 0 ? "warning" : "done",
            stats: [
                { key: "PROJECT.STAT_SAVED", value: row.updated },
                { key: "PROJECT.STAT_UNCHANGED", value: row.unchanged },
                { key: "PROJECT.STAT_SKIPPED", value: row.skipped },
                { key: "PROJECT.STAT_IGNORED", value: row.ignored },
            ],
            notes,
            summaryKey: "PROJECT.TRANSLATIONS_RESULT",
            summaryParams: {
                updated: row.updated,
                unchanged: row.unchanged,
                skipped: row.skipped,
                ignored: row.ignored,
            },
        };
    }
}
