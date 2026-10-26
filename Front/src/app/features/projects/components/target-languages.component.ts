import { Component, computed, inject, signal, viewChild } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { MatIconButton } from "@angular/material/button";
import { MatDialog } from "@angular/material/dialog";
import { MatFormField, MatLabel, MatPrefix } from "@angular/material/form-field";
import { MatIcon } from "@angular/material/icon";
import { MatInput } from "@angular/material/input";
import { MatListOption, MatSelectionList } from "@angular/material/list";
import { MatTooltip } from "@angular/material/tooltip";
import { BreakpointObserver, Breakpoints } from "@angular/cdk/layout";
import { Locale } from "@features/locales/models/locale.model";
import { LocalesStore } from "@features/locales/stores/locales.store";
import { RemoveTargetsDialog } from "@features/projects/components/remove-targets.dialog";
import { ProjectsStore } from "@features/projects/stores/projects.store";
import { TranslateModule } from "@ngx-translate/core";
import { map, firstValueFrom } from "rxjs";

/**
 * Two lists of locales: the catalog on one side, the project targets on the other.
 */
@Component({
    selector: "app-target-languages",
    standalone: true,
    imports: [
        TranslateModule,
        MatFormField,
        MatLabel,
        MatPrefix,
        MatInput,
        MatSelectionList,
        MatListOption,
        MatIconButton,
        MatIcon,
        MatTooltip,
    ],
    templateUrl: "./target-languages.component.html",
    styleUrl: "./target-languages.component.scss",
})
export class TargetLanguagesComponent {
    public readonly projectsStore = inject(ProjectsStore);
    public readonly localesStore = inject(LocalesStore);
    public readonly query = signal("");
    public readonly availableSelected = signal(0);
    public readonly projectSelected = signal(0);
    public readonly handset = toSignal(
        inject(BreakpointObserver)
            .observe(Breakpoints.Handset)
            .pipe(map((state) => state.matches)),
        { initialValue: false },
    );

    public readonly lists = computed(() => {
        const project = this.projectsStore.current();
        const targets = new Set(project?.targets ?? []);
        const source = project?.sourceLocale ?? "";
        const query = this.query();
        const matching = this.localesStore.locales().filter((locale) => localeMatches(locale.label, query));
        return {
            available: matching.filter((locale) => locale.code !== source && !targets.has(locale.code)),
            chosen: matching.filter((locale) => targets.has(locale.code)),
        };
    });

    private readonly availableList = viewChild<MatSelectionList>("availableList");
    private readonly projectList = viewChild<MatSelectionList>("projectList");
    private readonly dialog = inject(MatDialog);

    public constructor() {
        void this.localesStore.load();
    }

    public onQuery(value: string): void {
        this.query.set(value);
        this.availableList()?.deselectAll();
        this.projectList()?.deselectAll();
        this.availableSelected.set(0);
        this.projectSelected.set(0);
    }

    public onAvailableSelection(list: MatSelectionList): void {
        this.availableSelected.set(this.picked(list, this.lists().available).length);
    }

    public onProjectSelection(list: MatSelectionList): void {
        this.projectSelected.set(this.picked(list, this.lists().chosen).length);
    }

    public async addSelected(): Promise<void> {
        await this.add(this.picked(this.availableList(), this.lists().available));
    }

    public async addAll(): Promise<void> {
        await this.add(this.lists().available);
    }

    public async removeSelected(): Promise<void> {
        await this.remove(this.picked(this.projectList(), this.lists().chosen));
    }

    public async removeAll(): Promise<void> {
        await this.remove(this.lists().chosen);
    }

    private async add(locales: Locale[]): Promise<void> {
        const slug = this.projectsStore.current()?.slug;
        if (!slug || locales.length === 0) {
            return;
        }

        const updated = await this.projectsStore.addTargets(slug, locales.map((locale) => locale.code));
        if (updated) {
            this.availableList()?.deselectAll();
            this.availableSelected.set(0);
        }
    }

    private async remove(locales: Locale[]): Promise<void> {
        const slug = this.projectsStore.current()?.slug;
        if (!slug || locales.length === 0) {
            return;
        }

        const confirmed = await firstValueFrom(this.dialog.open(RemoveTargetsDialog).afterClosed());
        if (confirmed !== true) {
            return;
        }

        const updated = await this.projectsStore.removeTargets(slug, locales.map((locale) => locale.code));
        if (updated) {
            this.projectList()?.deselectAll();
            this.projectSelected.set(0);
        }
    }

    private picked(list: MatSelectionList | undefined, visible: Locale[]): Locale[] {
        if (!list) {
            return [];
        }

        const selected = new Set(list.selectedOptions.selected.map((option) => option.value as string));
        return visible.filter((locale) => selected.has(locale.code));
    }
}

function localeMatches(label: string, query: string): boolean {
    const words = fold(query)
        .split(/[^\p{L}\p{N}]+/u)
        .filter((word) => word.length > 0);
    if (words.length === 0) {
        return true;
    }

    const labelWords = fold(label).split(/[^\p{L}\p{N}]+/u);
    return words.every((word) => labelWords.some((labelWord) => labelWord.startsWith(word)));
}

function fold(value: string): string {
    return value
        .normalize("NFD")
        .replace(/\p{M}/gu, "")
        .toLocaleLowerCase("fr-FR");
}
