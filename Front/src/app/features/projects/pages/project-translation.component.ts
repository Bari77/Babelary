import { Component, computed, inject, signal, viewChild } from "@angular/core";
import { MatIcon } from "@angular/material/icon";
import { MatTooltip } from "@angular/material/tooltip";
import { RouterLink } from "@angular/router";
import { SkeletonComponent } from "@core/skeleton.component";
import { LocaleFlagComponent } from "@features/locales/components/locale-flag.component";
import { LocalesStore } from "@features/locales/stores/locales.store";
import { TranslationImportComponent } from "@features/projects/components/translation-import.component";
import { TranslationMeterComponent } from "@features/projects/components/translation-meter.component";
import { LocaleProgress, Project } from "@features/projects/models/project.model";
import { ProjectsStore } from "@features/projects/stores/projects.store";
import { TranslateModule } from "@ngx-translate/core";

/**
 * Current translation state, with one progress row per language.
 */
@Component({
    selector: "app-project-translation",
    standalone: true,
    imports: [
        TranslateModule,
        MatIcon,
        MatTooltip,
        RouterLink,
        TranslationMeterComponent,
        LocaleFlagComponent,
        TranslationImportComponent,
        SkeletonComponent,
    ],
    templateUrl: "./project-translation.component.html",
    styleUrl: "./project-translation.component.scss",
})
export class ProjectTranslationComponent {
    public readonly projectsStore = inject(ProjectsStore);
    public readonly localesStore = inject(LocalesStore);
    public readonly openLocale = signal<string | null>(null);
    public readonly filesLoading = signal(false);
    public readonly exporting = signal<string | null>(null);
    public readonly project = computed(() => this.projectsStore.current());
    public readonly importFailed = computed(() => this.importer()?.failed() === true);

    private readonly importer = viewChild(TranslationImportComponent);

    public constructor() {
        void this.localesStore.load();
    }

    public rows(project: Project): LocaleProgress[] {
        if (project.progress.length > 0) {
            return project.progress;
        }

        return project.targets.map((locale) => new LocaleProgress(locale, 0, 0, 0));
    }

    public async download(slug: string, locale: string): Promise<void> {
        if (this.exporting()) {
            return;
        }

        this.exporting.set(locale);
        try {
            const blob = await this.projectsStore.exportLocale(slug, locale);
            if (!blob) {
                return;
            }

            const url = URL.createObjectURL(blob);
            const anchor = document.createElement("a");
            anchor.href = url;
            anchor.download = `${locale}.zip`;
            anchor.click();
            URL.revokeObjectURL(url);
        } finally {
            this.exporting.set(null);
        }
    }

    public async toggle(slug: string, locale: string): Promise<void> {
        if (this.openLocale() === locale) {
            this.openLocale.set(null);
            return;
        }

        this.openLocale.set(locale);
        this.filesLoading.set(true);
        await this.projectsStore.loadFileProgress(slug, locale, true);
        this.filesLoading.set(false);
    }
}
