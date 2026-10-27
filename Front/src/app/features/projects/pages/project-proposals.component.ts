import { Component, computed, inject, signal } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { MatButton } from "@angular/material/button";
import { MatCheckbox } from "@angular/material/checkbox";
import { MatDialog } from "@angular/material/dialog";
import { MatFormField, MatLabel, MatPrefix } from "@angular/material/form-field";
import { MatIcon } from "@angular/material/icon";
import { MatInput } from "@angular/material/input";
import { MatProgressBar } from "@angular/material/progress-bar";
import { MatTooltip } from "@angular/material/tooltip";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { SkeletonComponent } from "@core/skeleton.component";
import { localeLabel } from "@features/locales/models/locale.model";
import { ChoiceMenuComponent, ChoiceOption } from "@features/projects/components/choice-menu.component";
import { ListPagerComponent, pageWindow } from "@features/projects/components/list-pager.component";
import { RefuseProposalsDialog } from "@features/projects/components/refuse-proposals.dialog";
import { TranslationMeterComponent } from "@features/projects/components/translation-meter.component";
import { TranslationStatusComponent } from "@features/projects/components/translation-status.component";
import { LocaleProgress } from "@features/projects/models/project.model";
import { Proposal, textParts } from "@features/projects/models/translation.model";
import { ProjectsStore } from "@features/projects/stores/projects.store";
import { TranslateModule, TranslateService } from "@ngx-translate/core";
import { firstValueFrom } from "rxjs";

/**
 * Pending proposals. The owner reviews one language or every language, and approves or refuses a selection.
 */
@Component({
    selector: "app-project-proposals",
    standalone: true,
    imports: [
        RouterLink,
        TranslateModule,
        MatButton,
        MatIcon,
        MatTooltip,
        MatCheckbox,
        MatFormField,
        MatLabel,
        MatPrefix,
        MatInput,
        MatProgressBar,
        ChoiceMenuComponent,
        SkeletonComponent,
        ListPagerComponent,
        TranslationMeterComponent,
        TranslationStatusComponent,
    ],
    templateUrl: "./project-proposals.component.html",
    styleUrl: "./project-proposals.component.scss",
})
export class ProjectProposalsComponent {
    public readonly projectsStore = inject(ProjectsStore);
    public readonly query = signal("");
    public readonly file = signal("");
    public readonly pageSize = signal(50);
    public readonly pageIndex = signal(0);
    public readonly selected = signal<ReadonlySet<string>>(new Set());
    public readonly rowError = signal<Record<string, string>>({});
    public readonly busy = signal(false);
    public readonly ready = signal(false);
    public readonly slug = signal("");
    public readonly locale = signal("");
    public readonly language = computed(() =>
        this.locale() ? localeLabel(this.locale()) || this.locale() : this.translate.instant("PROJECT.LOCALE_ALL"),
    );
    public readonly targets = computed(() => this.projectsStore.current()?.targets ?? []);
    public readonly localeProgress = computed(() => {
        const progress = this.projectsStore.current()?.progress ?? [];
        const code = this.locale();
        if (code) {
            return progress.find((row) => row.locale === code);
        }

        return new LocaleProgress(
            "",
            progress.reduce((sum, row) => sum + row.translated, 0),
            progress.reduce((sum, row) => sum + row.total, 0),
            progress.reduce((sum, row) => sum + row.pending, 0),
            progress.reduce((sum, row) => sum + row.pendingOpen, 0),
        );
    });
    public readonly localeOptions = computed((): ChoiceOption[] => {
        const options = this.targets().map((code) => {
            const row = this.projectsStore.current()?.progress.find((item) => item.locale === code);
            return {
                id: code,
                name: localeLabel(code) || code,
                detail: row ? `${row.covered()}/${row.total}` : "",
                pending: row?.pending ?? 0,
            };
        });
        return [
            {
                id: "",
                name: this.translate.instant("PROJECT.LOCALE_ALL"),
                detail: "",
                pending: options.reduce((sum, option) => sum + option.pending, 0),
            },
            ...options,
        ];
    });
    public readonly files = computed(() => [...new Set(this.projectsStore.proposals().map((proposal) => proposal.fileName))]);
    public readonly fileLabel = computed(() => this.file() || this.translate.instant("PROJECT.FILE_ALL"));
    public readonly fileOptions = computed((): ChoiceOption[] => [
        { id: "", name: this.translate.instant("PROJECT.FILE_ALL"), detail: "", pending: 0 },
        ...this.files().map((name) => ({ id: name, name, detail: "", pending: 0 })),
    ]);
    public readonly filtered = computed(() => {
        const query = this.query().trim().toLocaleLowerCase("fr");
        const file = this.file();
        return this.projectsStore.proposals().filter((proposal) => {
            if (file && proposal.fileName !== file) {
                return false;
            }
            if (query.length === 0) {
                return true;
            }
            const language = localeLabel(proposal.locale) || proposal.locale;
            const haystack =
                `${language} ${proposal.fileName} ${proposal.path} ${proposal.sourceValue} ${proposal.currentText} ${proposal.text} ${proposal.authorName}`
                    .toLocaleLowerCase("fr");
            return haystack.includes(query);
        });
    });
    public readonly paging = computed(() => pageWindow(this.filtered().length, this.pageIndex(), this.pageSize()));
    public readonly pageRows = computed(() => {
        const paging = this.paging();
        return this.filtered().slice(paging.start, paging.end);
    });
    public readonly chosen = computed(() => this.filtered().filter((proposal) => this.selected().has(proposal.id)));
    public readonly allSelected = computed(() => this.filtered().length > 0 && this.chosen().length === this.filtered().length);
    public readonly someSelected = computed(() => this.chosen().length > 0 && !this.allSelected());

    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly dialog = inject(MatDialog);
    private readonly translate = inject(TranslateService);
    private anchor = 0;
    private request = 0;

    public constructor() {
        this.route.paramMap.pipe(takeUntilDestroyed()).subscribe((params) => {
            const slug = this.route.parent?.snapshot.paramMap.get("project") ?? "";
            const locale = params.get("locale") ?? "";
            const request = ++this.request;
            this.slug.set(slug);
            this.locale.set(locale);
            this.file.set("");
            this.pageIndex.set(0);
            this.selected.set(new Set());
            this.rowError.set({});
            this.ready.set(false);
            void this.openList(slug, locale, request);
        });
    }

    private async openList(slug: string, locale: string, request: number): Promise<void> {
        await this.projectsStore.loadProposals(slug, locale);
        if (request === this.request) {
            this.ready.set(true);
        }
    }

    public async openLocale(code: string): Promise<void> {
        if (code === this.locale()) {
            return;
        }

        const commands = code ? ["/projet", this.slug(), "propositions", code] : ["/projet", this.slug(), "propositions"];
        await this.router.navigate(commands);
    }

    public heading(proposal: Proposal): string {
        const file = `${proposal.fileName} · ${proposal.path}`;
        if (this.locale()) {
            return file;
        }

        const language = localeLabel(proposal.locale) || proposal.locale;
        return `${language} · ${file}`;
    }

    public onQuery(value: string): void {
        this.query.set(value);
        this.pageIndex.set(0);
    }

    public pickFile(name: string): void {
        this.file.set(name ?? "");
        this.pageIndex.set(0);
    }

    public setPageSize(size: number): void {
        if (size === 50 || size === 100) {
            this.pageSize.set(size);
            this.pageIndex.set(0);
        }
    }

    public parts(value: string) {
        return textParts(value);
    }

    public toggleAll(event: MouseEvent): void {
        event.preventDefault();
        if (this.busy()) {
            return;
        }
        const next = new Set(this.selected());
        if (this.allSelected()) {
            for (const proposal of this.filtered()) {
                next.delete(proposal.id);
            }
        } else {
            for (const proposal of this.filtered()) {
                next.add(proposal.id);
            }
        }
        this.selected.set(next);
    }

    public toggleRow(event: MouseEvent, index: number, id: string): void {
        event.preventDefault();
        if (this.busy()) {
            return;
        }
        const rows = this.filtered();
        const next = new Set(this.selected());
        const enable = !next.has(id);
        const from = event.shiftKey ? Math.min(this.anchor, index) : index;
        const to = event.shiftKey ? Math.max(this.anchor, index) : index;
        for (let cursor = from; cursor <= to; cursor++) {
            const rowId = rows[cursor]?.id;
            if (!rowId) {
                continue;
            }
            if (enable) {
                next.add(rowId);
            } else {
                next.delete(rowId);
            }
        }
        if (!event.shiftKey) {
            this.anchor = index;
        }
        this.selected.set(next);
    }

    public async approve(): Promise<void> {
        await this.decide(true);
    }

    public async refuse(): Promise<void> {
        const count = this.chosen().length;
        if (count === 0) {
            return;
        }
        const confirmed = await firstValueFrom(this.dialog.open(RefuseProposalsDialog, { data: { count } }).afterClosed());
        if (confirmed === true) {
            await this.decide(false);
        }
    }

    private async decide(approve: boolean): Promise<void> {
        const ids = this.chosen().map((proposal) => proposal.id);
        if (ids.length === 0) {
            return;
        }
        this.busy.set(true);
        try {
            const batch = await this.projectsStore.decideProposals(this.slug(), this.locale(), ids, approve);
            if (!batch) {
                return;
            }
            const errors: Record<string, string> = {};
            const failed = new Set<string>();
            for (const result of batch.results) {
                if (result.error) {
                    failed.add(result.id);
                    errors[result.id] = this.translate.instant("ERROR." + result.error, { token: result.detail ?? "" });
                }
            }
            this.rowError.set(errors);
            this.selected.set(failed);
        } finally {
            this.busy.set(false);
        }
    }
}
