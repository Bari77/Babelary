import { computed, inject, signal } from "@angular/core";
import { apiErrorMessage } from "@core/http/api-error";
import { Locale } from "@features/locales/models/locale.model";
import { LocalesService } from "@features/locales/services/locales.service";
import { TranslateService } from "@ngx-translate/core";
import { firstValueFrom } from "rxjs";

/**
 * Locales offered when choosing a project language.
 */
export class LocalesStore {
    public readonly locales = signal<Locale[]>([]);
    public readonly loaded = signal(false);
    public readonly error = signal<string | null>(null);
    public readonly labelByCode = computed(() => new Map(this.locales().map((locale) => [locale.code, locale.label])));

    private readonly localesService = inject(LocalesService);
    private readonly translate = inject(TranslateService);
    private loading: Promise<void> | null = null;

    public label(code: string): string {
        return this.labelByCode().get(code) ?? "";
    }

    public has(code: string): boolean {
        return this.labelByCode().has(code);
    }

    public load(): Promise<void> {
        if (this.loaded()) {
            return Promise.resolve();
        }

        this.loading ??= this.fetch();
        return this.loading;
    }

    private async fetch(): Promise<void> {
        this.error.set(null);
        try {
            const locales = (await firstValueFrom(this.localesService.list()))
                .filter((locale) => locale.label.length > 0)
                .sort((left, right) => left.label.localeCompare(right.label, "fr"));
            this.locales.set(locales);
            this.loaded.set(true);
        } catch (err) {
            this.loading = null;
            this.error.set(apiErrorMessage(err) || this.translate.instant("ERROR.ERR_INTERNAL"));
        }
    }
}
