import { Component, computed, inject, signal } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormControl, ReactiveFormsModule } from "@angular/forms";
import { MatIconButton } from "@angular/material/button";
import { MatFormField, MatLabel, MatPrefix, MatSuffix } from "@angular/material/form-field";
import { MatIcon } from "@angular/material/icon";
import { MatInput } from "@angular/material/input";
import { MatTooltip } from "@angular/material/tooltip";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { SkeletonComponent } from "@core/skeleton.component";
import { localeLabel } from "@features/locales/models/locale.model";
import { ChoiceMenuComponent, ChoiceOption } from "@features/projects/components/choice-menu.component";
import { ListPagerComponent, pageWindow } from "@features/projects/components/list-pager.component";
import { TranslationMeterComponent } from "@features/projects/components/translation-meter.component";
import { TranslationStatusComponent } from "@features/projects/components/translation-status.component";
import { FileProgress } from "@features/projects/models/membership.model";
import { TextPart, TranslationEntry, textParts } from "@features/projects/models/translation.model";
import { ProjectsStore } from "@features/projects/stores/projects.store";
import { SessionStore } from "@features/session/stores/session.store";
import { TranslateModule, TranslateService } from "@ngx-translate/core";

/**
 * Editor for one target locale. A file can be selected, or every file at once.
 * The owner writes the saved value. A member submits a proposal.
 */
@Component({
    selector: "app-project-editor",
    standalone: true,
    imports: [
        ReactiveFormsModule,
        RouterLink,
        TranslateModule,
        MatIconButton,
        MatIcon,
        MatTooltip,
        MatFormField,
        MatLabel,
        MatPrefix,
        MatSuffix,
        MatInput,
        ChoiceMenuComponent,
        SkeletonComponent,
        ListPagerComponent,
        TranslationMeterComponent,
        TranslationStatusComponent,
    ],
    templateUrl: "./project-editor.component.html",
    styleUrl: "./project-editor.component.scss",
})
export class ProjectEditorComponent {
    public readonly projectsStore = inject(ProjectsStore);
    public readonly owner = computed(() => this.sessionStore.account()?.isInstanceOwner === true);
    public readonly query = signal("");
    public readonly filter = signal<"all" | "empty" | "identical" | "review">("all");
    public readonly pageSize = signal(50);
    public readonly pageIndex = signal(0);
    public readonly saving = signal<string | null>(null);
    public readonly rowError = signal<Record<string, string>>({});
    public readonly slug = signal("");
    public readonly locale = signal("");
    public readonly file = signal("");
    public readonly ready = signal(false);
    public readonly exporting = signal(false);
    public readonly language = computed(() => localeLabel(this.locale()) || this.locale());
    public readonly fileLabel = computed(() => this.file() || this.translate.instant("PROJECT.FILE_ALL"));
    public readonly targets = computed(() => this.projectsStore.current()?.targets ?? []);
    public readonly files = computed(() => this.projectsStore.fileProgress());
    public readonly localeOptions = computed((): ChoiceOption[] =>
        this.targets().map((code) => {
            const row = this.projectsStore.current()?.progress.find((item) => item.locale === code);
            return {
                id: code,
                name: localeLabel(code) || code,
                detail: row ? `${row.covered()}/${row.total}` : "",
                pending: 0,
            };
        }),
    );
    public readonly fileOptions = computed((): ChoiceOption[] => {
        const files = this.files();
        const covered = files.reduce((sum, item) => sum + item.covered(), 0);
        const total = files.reduce((sum, item) => sum + item.total, 0);
        return [
            {
                id: "",
                name: this.translate.instant("PROJECT.FILE_ALL"),
                detail: total > 0 ? `${covered}/${total}` : "",
                pending: 0,
            },
            ...files.map((item) => ({
                id: item.name,
                name: item.name,
                detail: `${item.covered()}/${item.total}`,
                pending: 0,
            })),
        ];
    });
    public readonly meter = computed(() => {
        const current = this.files().find((item) => item.name === this.file());
        if (current) {
            return current;
        }

        const files = this.files();
        return new FileProgress(
            "",
            files.reduce((sum, item) => sum + item.translated, 0),
            files.reduce((sum, item) => sum + item.total, 0),
            files.reduce((sum, item) => sum + item.pending, 0),
            files.reduce((sum, item) => sum + item.pendingOpen, 0),
        );
    });
    public readonly visible = computed(() => {
        const query = this.query().trim().toLocaleLowerCase("fr");
        return this.projectsStore.entries().filter((entry) => {
            if (this.filter() === "empty" && entry.status !== "empty") {
                return false;
            }
            if (this.filter() === "identical" && entry.status !== "identical") {
                return false;
            }
            if (this.filter() === "review" && entry.status !== "review") {
                return false;
            }
            if (query.length === 0) {
                return true;
            }
            const proposal = entry.proposalText ?? "";
            return [entry.fileName, entry.path, entry.sourceValue, entry.text, proposal].some((value) =>
                value.toLocaleLowerCase("fr").includes(query),
            );
        });
    });
    public readonly paging = computed(() => pageWindow(this.visible().length, this.pageIndex(), this.pageSize()));
    public readonly pageEntries = computed(() => {
        const paging = this.paging();
        return this.visible().slice(paging.start, paging.end);
    });

    private readonly sessionStore = inject(SessionStore);
    private readonly translate = inject(TranslateService);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly controls = new Map<string, FormControl<string>>();
    private request = 0;

    public constructor() {
        this.route.paramMap.pipe(takeUntilDestroyed()).subscribe((params) => {
            const slug = this.route.parent?.snapshot.paramMap.get("project") ?? "";
            const locale = params.get("locale") ?? "";
            const file = params.get("file") ?? "";
            const request = ++this.request;
            this.slug.set(slug);
            this.locale.set(locale);
            this.file.set(file);
            this.ready.set(false);
            this.pageIndex.set(0);
            this.controls.clear();
            this.rowError.set({});
            void this.openEntries(slug, locale, file, request);
            void this.projectsStore.loadFileProgress(slug, locale);
        });
    }

    private async openEntries(slug: string, locale: string, file: string, request: number): Promise<void> {
        await this.projectsStore.loadEntries(slug, locale, file);
        if (request === this.request) {
            this.ready.set(true);
        }
    }

    public async openLocale(code: string): Promise<void> {
        if (!code || code === this.locale()) {
            return;
        }

        await this.router.navigate(this.editorCommands(code, this.file()));
    }

    public async download(): Promise<void> {
        if (this.exporting()) {
            return;
        }

        this.exporting.set(true);
        try {
            const blob = await this.projectsStore.exportLocale(this.slug(), this.locale());
            if (!blob) {
                return;
            }

            const url = URL.createObjectURL(blob);
            const anchor = document.createElement("a");
            anchor.href = url;
            anchor.download = `${this.locale()}.zip`;
            anchor.click();
            URL.revokeObjectURL(url);
        } finally {
            this.exporting.set(false);
        }
    }

    public async openFile(name: string): Promise<void> {
        if (name === this.file()) {
            return;
        }

        await this.router.navigate(this.editorCommands(this.locale(), name));
    }

    private editorCommands(locale: string, file: string): string[] {
        const commands = ["/projet", this.slug(), "editeur", locale];
        if (file) {
            commands.push(file);
        }

        return commands;
    }

    public parts(value: string): TextPart[] {
        return textParts(value);
    }

    public onQuery(value: string): void {
        this.query.set(value);
        this.pageIndex.set(0);
    }

    public setFilter(value: string): void {
        if ((value === "all" || value === "empty" || value === "identical" || value === "review") && value !== this.filter()) {
            this.filter.set(value);
            this.pageIndex.set(0);
        }
    }

    public setPageSize(size: number): void {
        if (size === 50 || size === 100) {
            this.pageSize.set(size);
            this.pageIndex.set(0);
        }
    }

    public control(entry: TranslationEntry): FormControl<string> {
        let control = this.controls.get(entry.keyId);
        if (!control) {
            control = new FormControl(this.baseline(entry), { nonNullable: true });
            this.controls.set(entry.keyId, control);
        }
        return control;
    }

    public baseline(entry: TranslationEntry): string {
        if (!this.owner() && entry.proposalStatus) {
            return entry.proposalText ?? "";
        }
        return entry.text;
    }

    public dirty(): boolean {
        return this.projectsStore.entries().some((entry) => this.controls.get(entry.keyId)?.value !== this.baseline(entry));
    }

    public copySource(entry: TranslationEntry): void {
        this.control(entry).setValue(entry.sourceValue);
    }

    public async clear(entry: TranslationEntry): Promise<void> {
        const control = this.control(entry);
        control.setValue("");
        if (this.baseline(entry) === "") {
            return;
        }

        await this.save(entry);
    }

    public async save(entry: TranslationEntry): Promise<void> {
        const text = this.control(entry).value;
        this.saving.set(entry.keyId);
        const error = await this.projectsStore.saveTranslation(this.slug(), this.locale(), entry.keyId, text);
        this.saving.set(null);
        this.rowError.update((current) => ({ ...current, [entry.keyId]: error ?? "" }));
        const updated = this.projectsStore.entries().find((item) => item.keyId === entry.keyId);
        if (!error && updated) {
            this.control(updated).setValue(this.baseline(updated));
        }

        await this.projectsStore.loadFileProgress(this.slug(), this.locale());
    }
}
