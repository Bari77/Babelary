import { Component, computed, inject, signal } from "@angular/core";
import { MatButton } from "@angular/material/button";
import { MatCard, MatCardContent, MatCardHeader, MatCardTitle } from "@angular/material/card";
import { MatProgressSpinner } from "@angular/material/progress-spinner";
import { ActivatedRoute } from "@angular/router";
import { LocalesStore } from "@features/locales/stores/locales.store";
import { ChoiceMenuComponent, ChoiceOption } from "@features/projects/components/choice-menu.component";
import { ImportFileResult, ImportTranslationFileResult } from "@features/projects/models/source-file.model";
import { ProjectsStore } from "@features/projects/stores/projects.store";
import { SessionStore } from "@features/session/stores/session.store";
import { TranslateModule } from "@ngx-translate/core";

/**
 * Source files of a project. Only the owner imports them.
 */
@Component({
    selector: "app-project-source",
    standalone: true,
    imports: [
        TranslateModule,
        MatCard,
        MatCardHeader,
        MatCardTitle,
        MatCardContent,
        MatButton,
        MatProgressSpinner,
        ChoiceMenuComponent,
    ],
    templateUrl: "./project-source.component.html",
    styleUrl: "./project-source.component.scss",
})
export class ProjectSourceComponent {
    public readonly projectsStore = inject(ProjectsStore);
    public readonly localesStore = inject(LocalesStore);
    public readonly owner = computed(() => this.sessionStore.account()?.isInstanceOwner === true);
    public readonly selected = signal<{ name: string; content: string }[]>([]);
    public readonly results = signal<ImportFileResult[]>([]);
    public readonly translationLocale = signal("");
    public readonly translationFiles = signal<{ name: string; content: string }[]>([]);
    public readonly translationResults = signal<ImportTranslationFileResult[]>([]);
    public readonly busy = signal<"sources" | "translations" | null>(null);
    public readonly failureOn = signal<"sources" | "translations" | null>(null);
    public readonly localeOptions = computed<ChoiceOption[]>(() =>
        (this.projectsStore.current()?.targets ?? []).map((code) => ({
            id: code,
            name: this.localesStore.label(code) || code,
            detail: "",
            pending: 0,
        })),
    );

    private readonly sessionStore = inject(SessionStore);
    private readonly route = inject(ActivatedRoute);

    public constructor() {
        void this.localesStore.load();
        const slug = (this.route.parent ?? this.route).snapshot.paramMap.get("project");
        if (slug && this.sessionStore.account()?.isInstanceOwner === true) {
            void this.projectsStore.loadFiles(slug);
        }
    }

    public async onFiles(event: Event): Promise<void> {
        const files = await readFiles(event);
        if (files) {
            this.selected.set(files);
        }
    }

    public async import(): Promise<void> {
        const slug = (this.route.parent ?? this.route).snapshot.paramMap.get("project");
        const files = this.selected();
        if (!slug || files.length === 0) {
            return;
        }

        this.busy.set("sources");
        const results = await this.projectsStore.importSources(slug, files);
        this.busy.set(null);
        this.failureOn.set(results ? null : "sources");
        if (results) {
            this.results.set(results);
            this.selected.set([]);
        }
    }

    public async onTranslationFiles(event: Event): Promise<void> {
        const files = await readFiles(event);
        if (files) {
            this.translationFiles.set(files);
        }
    }

    public async importTranslations(): Promise<void> {
        const slug = (this.route.parent ?? this.route).snapshot.paramMap.get("project");
        const locale = this.translationLocale();
        const files = this.translationFiles();
        if (!slug || locale === "" || files.length === 0) {
            return;
        }

        this.busy.set("translations");
        const results = await this.projectsStore.importTranslations(slug, locale, files);
        this.busy.set(null);
        this.failureOn.set(results ? null : "translations");
        if (results) {
            this.translationResults.set(results);
            this.translationFiles.set([]);
        }
    }
}

async function readFiles(event: Event): Promise<{ name: string; content: string }[] | null> {
    const input = event.target;
    if (!(input instanceof HTMLInputElement) || !input.files) {
        return null;
    }

    const files = await Promise.all([...input.files].map(async (file) => ({ name: file.name, content: await file.text() })));
    input.value = "";
    return files;
}
