import { Component, computed, inject, signal } from "@angular/core";
import { MatIcon } from "@angular/material/icon";
import { MatTooltip } from "@angular/material/tooltip";
import { RouterLink } from "@angular/router";
import { LocalesStore } from "@features/locales/stores/locales.store";
import { TranslationMeterComponent } from "@features/projects/components/translation-meter.component";
import { TranslationStatusComponent } from "@features/projects/components/translation-status.component";
import { LocaleProgress, Project } from "@features/projects/models/project.model";
import { ProjectsStore } from "@features/projects/stores/projects.store";
import { TranslateModule } from "@ngx-translate/core";

/**
 * Current translation state, with one progress row per language.
 */
@Component({
    selector: "app-project-translation",
    standalone: true,
    imports: [TranslateModule, MatIcon, MatTooltip, RouterLink, TranslationMeterComponent, TranslationStatusComponent],
    templateUrl: "./project-translation.component.html",
    styleUrl: "./project-translation.component.scss",
})
export class ProjectTranslationComponent {
    public readonly projectsStore = inject(ProjectsStore);
    public readonly localesStore = inject(LocalesStore);
    public readonly openLocale = signal<string | null>(null);
    public readonly exporting = signal<string | null>(null);
    public readonly project = computed(() => this.projectsStore.current());

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
        await this.projectsStore.loadFileProgress(slug, locale, true);
    }
}
