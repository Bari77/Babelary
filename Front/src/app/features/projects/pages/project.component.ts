import { BreakpointObserver, Breakpoints } from "@angular/cdk/layout";
import { Component, DestroyRef, computed, inject } from "@angular/core";
import { takeUntilDestroyed, toSignal } from "@angular/core/rxjs-interop";
import { MatIcon } from "@angular/material/icon";
import { MatTabLink, MatTabNav, MatTabNavPanel } from "@angular/material/tabs";
import { MatTooltip } from "@angular/material/tooltip";
import { ActivatedRoute, Router, RouterLink, RouterLinkActive, RouterOutlet } from "@angular/router";
import { ProjectsStore } from "@features/projects/stores/projects.store";
import { SessionStore } from "@features/session/stores/session.store";
import { TranslateModule } from "@ngx-translate/core";
import { map } from "rxjs";

/**
 * Project frame: Traduction and Paramètres, with room for later tabs.
 */
@Component({
    selector: "app-project",
    standalone: true,
    imports: [
        RouterOutlet,
        RouterLink,
        RouterLinkActive,
        TranslateModule,
        MatTabNav,
        MatTabLink,
        MatTabNavPanel,
        MatIcon,
        MatTooltip,
    ],
    templateUrl: "./project.component.html",
    styleUrl: "./project.component.scss",
    host: { class: "project-frame" },
})
export class ProjectComponent {
    public readonly owner = computed(() => this.sessionStore.account()?.isInstanceOwner === true);
    public readonly proposalCount = computed(() => this.projectsStore.current()?.pending ?? 0);
    public readonly handset = toSignal(
        inject(BreakpointObserver)
            .observe(Breakpoints.Handset)
            .pipe(map((state) => state.matches)),
        { initialValue: false },
    );

    private readonly projectsStore = inject(ProjectsStore);
    private readonly sessionStore = inject(SessionStore);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);

    public constructor() {
        this.route.paramMap.pipe(takeUntilDestroyed(inject(DestroyRef))).subscribe((params) => {
            const slug = params.get("project");
            if (slug) {
                void this.projectsStore.open(slug);
            }
        });
    }

    public slug(): string | null {
        return this.route.snapshot.paramMap.get("project");
    }

    public translationActive(): boolean {
        const slug = this.slug();
        if (!slug) {
            return false;
        }

        const path = this.router.url.split(/[?#]/)[0];
        return path === `/projet/${slug}` || path.startsWith(`/projet/${slug}/editeur/`);
    }

    public proposalsActive(): boolean {
        const slug = this.slug();
        if (!slug) {
            return false;
        }

        const path = this.router.url.split(/[?#]/)[0];
        return path === `/projet/${slug}/propositions` || path.startsWith(`/projet/${slug}/propositions/`);
    }
}
