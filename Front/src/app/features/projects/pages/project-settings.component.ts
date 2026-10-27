import { Component, DestroyRef, computed, effect, inject, signal, viewChild } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from "@angular/forms";
import { MatButton } from "@angular/material/button";
import { MatIcon } from "@angular/material/icon";
import { MatTooltip } from "@angular/material/tooltip";
import { MatCard, MatCardContent, MatCardHeader, MatCardTitle } from "@angular/material/card";
import { MatDialog } from "@angular/material/dialog";
import { MatOption } from "@angular/material/core";
import { MatError, MatFormField, MatHint, MatLabel } from "@angular/material/form-field";
import { MatInput } from "@angular/material/input";
import { MatProgressSpinner } from "@angular/material/progress-spinner";
import { MatSelect, MatSelectTrigger } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { ActivatedRoute, Router } from "@angular/router";
import { firstValueFrom } from "rxjs";
import { SkeletonComponent } from "@core/skeleton.component";
import { LocaleFlagComponent } from "@features/locales/components/locale-flag.component";
import { LocalesStore } from "@features/locales/stores/locales.store";
import { DeleteProjectDialog } from "@features/projects/components/delete-project.dialog";
import { ResetProjectDialog } from "@features/projects/components/reset-project.dialog";
import { TargetLanguagesComponent } from "@features/projects/components/target-languages.component";
import { ProjectsStore } from "@features/projects/stores/projects.store";
import { SessionStore } from "@features/session/stores/session.store";
import { TranslateModule, TranslateService } from "@ngx-translate/core";

/**
 * Project settings: name, source locale, and target languages.
 */
@Component({
    selector: "app-project-settings",
    standalone: true,
    imports: [
        ReactiveFormsModule,
        TranslateModule,
        MatCard,
        MatCardHeader,
        MatCardTitle,
        MatCardContent,
        MatFormField,
        MatLabel,
        MatHint,
        MatError,
        MatInput,
        MatSelect,
        MatSelectTrigger,
        MatOption,
        MatButton,
        MatIcon,
        MatTooltip,
        MatProgressSpinner,
        TargetLanguagesComponent,
        LocaleFlagComponent,
        SkeletonComponent,
    ],
    templateUrl: "./project-settings.component.html",
    styleUrl: "./project-settings.component.scss",
})
export class ProjectSettingsComponent {
    public readonly projectsStore = inject(ProjectsStore);
    public readonly localesStore = inject(LocalesStore);
    public readonly owner = computed(() => this.sessionStore.account()?.isInstanceOwner === true);
    public readonly creating = signal(true);
    public readonly localeQuery = signal("");
    public readonly localePanelOpen = signal(false);
    public readonly sourceLimit = signal(localePage);
    public readonly form = new FormGroup({
        slug: new FormControl("", {
            nonNullable: true,
            validators: [Validators.required, Validators.pattern(/^[a-z0-9]+(?:-[a-z0-9]+)*$/), Validators.maxLength(80)],
        }),
        name: new FormControl("", {
            nonNullable: true,
            validators: [Validators.required, Validators.maxLength(120)],
        }),
        sourceLocale: new FormControl("", {
            nonNullable: true,
            validators: [Validators.required, (control) => this.knownLocale(control)],
        }),
    });

    private readonly sessionStore = inject(SessionStore);
    private readonly localeSelect = viewChild(MatSelect);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly snackBar = inject(MatSnackBar);
    private readonly dialog = inject(MatDialog);
    private readonly translate = inject(TranslateService);
    public readonly routeSlug = signal<string | null>(null);
    private appliedSlug: string | null = null;
    private detachSourceScroll: (() => void) | null = null;

    public constructor() {
        void this.localesStore.load().then(() => this.form.controls.sourceLocale.updateValueAndValidity());
        const projectRoute = this.route.parent ?? this.route;
        projectRoute.paramMap.pipe(takeUntilDestroyed(inject(DestroyRef))).subscribe((params) => {
            const slug = params.get("project");
            this.routeSlug.set(slug);
            if (!slug) {
                this.creating.set(true);
                this.appliedSlug = null;
                this.form.reset({ slug: "", name: "", sourceLocale: "" });
                this.form.controls.slug.enable();
                this.form.controls.sourceLocale.enable();
                return;
            }

            this.creating.set(false);
            this.form.controls.slug.disable();
        });

        effect(() => {
            const slug = this.routeSlug();
            const project = this.projectsStore.current();
            if (!slug || !project || project.slug !== slug || this.appliedSlug === slug) {
                return;
            }

            this.appliedSlug = slug;
            this.form.patchValue({
                slug: project.slug,
                name: project.name,
                sourceLocale: project.sourceLocale,
            });
            if (project.sourceLocaleLocked) {
                this.form.controls.sourceLocale.disable();
            } else {
                this.form.controls.sourceLocale.enable();
            }
            this.form.markAsPristine();
        });

        effect(() => {
            const slug = this.routeSlug();
            const project = this.projectsStore.current();
            if (!slug || this.creating() || !project || project.slug !== slug) {
                return;
            }

            if (project.sourceLocaleLocked) {
                this.form.controls.sourceLocale.disable();
            } else {
                this.form.controls.sourceLocale.enable();
            }
        });
    }

    public filteredLocales() {
        const query = fold(this.localeQuery());
        const locales = this.localesStore.locales();
        if (!query) {
            return locales;
        }

        const words = query.split(/[^\p{L}\p{N}]+/u).filter((word) => word.length > 0);
        return locales.filter((locale) => {
            const labelWords = fold(locale.label).split(/[^\p{L}\p{N}]+/u);
            return words.every((word) => labelWords.some((labelWord) => labelWord.startsWith(word)));
        });
    }

    public visibleSourceLocales() {
        const shown = this.filteredLocales().slice(0, this.sourceLimit());
        const selected = this.form.controls.sourceLocale.value;
        if (!selected || shown.some((locale) => locale.code === selected)) {
            return shown;
        }

        const current = this.localesStore.locales().find((locale) => locale.code === selected);
        return current ? [current, ...shown] : shown;
    }

    public onLocalePanel(open: boolean, search: HTMLInputElement): void {
        this.detachSourceScroll?.();
        this.detachSourceScroll = null;
        this.localePanelOpen.set(open);
        this.sourceLimit.set(localePage);
        if (!open) {
            this.localeQuery.set("");
            search.value = "";
            return;
        }

        setTimeout(() => {
            if (!this.localePanelOpen()) {
                return;
            }

            search.focus();
            const panel = this.localeSelect()?.panel?.nativeElement;
            if (!panel) {
                return;
            }

            const onScroll = () => this.growSourceLocales(panel);
            panel.addEventListener("scroll", onScroll, { passive: true });
            this.detachSourceScroll = () => panel.removeEventListener("scroll", onScroll);
        });
    }

    public onSearch(event: Event): void {
        const input = event.target;
        if (!(input instanceof HTMLInputElement)) {
            return;
        }

        this.localeQuery.set(input.value);
        this.sourceLimit.set(localePage);
        const panel = this.localeSelect()?.panel?.nativeElement;
        if (panel) {
            panel.scrollTop = 0;
        }
    }

    public onSearchKey(event: KeyboardEvent): void {
        event.stopPropagation();
        if (event.key !== "Enter") {
            return;
        }

        event.preventDefault();
        const first = this.filteredLocales()[0];
        if (!first) {
            return;
        }

        this.form.controls.sourceLocale.setValue(first.code);
        this.form.controls.sourceLocale.markAsDirty();
        this.localeSelect()?.close();
    }

    public async save(): Promise<void> {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        const value = this.form.getRawValue();
        if (this.creating()) {
            const created = await this.projectsStore.create({
                slug: value.slug,
                name: value.name,
                sourceLocale: value.sourceLocale,
            });
            if (created) {
                await this.router.navigate(["/projet", created.slug, "parametres"]);
                this.saved();
            }
            return;
        }

        const updated = await this.projectsStore.update(value.slug, {
            name: value.name,
            sourceLocale: value.sourceLocale,
        });
        if (updated) {
            this.saved();
        }
    }

    public async reset(): Promise<void> {
        const confirmed = await firstValueFrom(this.dialog.open(ResetProjectDialog).afterClosed());
        if (confirmed !== true) {
            return;
        }

        const updated = await this.projectsStore.reset(this.form.getRawValue().slug);
        if (updated) {
            this.snackBar.open(this.translate.instant("PROJECT.RESET_DONE"), undefined, { duration: 2500 });
        }
    }

    public async remove(): Promise<void> {
        const confirmed = await firstValueFrom(this.dialog.open(DeleteProjectDialog).afterClosed());
        if (confirmed !== true) {
            return;
        }

        const removed = await this.projectsStore.remove(this.form.getRawValue().slug);
        if (removed) {
            this.snackBar.open(this.translate.instant("PROJECT.DELETE_DONE"), undefined, { duration: 2500 });
            await this.router.navigate(["/"]);
        }
    }

    private growSourceLocales(panel: HTMLElement): void {
        const total = this.filteredLocales().length;
        if (this.sourceLimit() >= total || panel.scrollTop + panel.clientHeight < panel.scrollHeight - 80) {
            return;
        }

        this.sourceLimit.update((count) => Math.min(total, count + localePage));
    }

    private saved(): void {
        this.snackBar.open(this.translate.instant("PROJECT.SAVED"), undefined, { duration: 2500 });
    }

    private knownLocale(control: AbstractControl<string>): ValidationErrors | null {
        const value = control.value;
        if (!value || !this.localesStore.loaded() || this.localesStore.locales().length === 0) {
            return null;
        }

        return this.localesStore.has(value) ? null : { locale: true };
    }
}

const localePage = 40;

function fold(value: string): string {
    return value
        .normalize("NFD")
        .replace(/\p{M}/gu, "")
        .toLocaleLowerCase("fr-FR");
}
