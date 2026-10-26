import { BreakpointObserver, Breakpoints } from "@angular/cdk/layout";
import { afterNextRender, Component, computed, inject, Injector, signal, viewChild } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { MatIconButton } from "@angular/material/button";
import { MatIcon } from "@angular/material/icon";
import { MatListItem, MatListItemIcon, MatListItemTitle, MatNavList } from "@angular/material/list";
import { MatSidenav, MatSidenavContainer, MatSidenavContent } from "@angular/material/sidenav";
import { MatToolbar } from "@angular/material/toolbar";
import { MatTooltip } from "@angular/material/tooltip";
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from "@angular/router";
import { localeLabel } from "@features/locales/models/locale.model";
import { ProjectsStore } from "@features/projects/stores/projects.store";
import { SessionStore } from "@features/session/stores/session.store";
import { TranslateModule } from "@ngx-translate/core";
import { filter, map, startWith } from "rxjs";

interface ShellCrumb {
    path: string;
    key?: string;
    text?: string;
}

/**
 * Signed-in frame: navigation, account name, logout.
 */
@Component({
    selector: "app-shell",
    standalone: true,
    imports: [
        RouterOutlet,
        RouterLink,
        RouterLinkActive,
        TranslateModule,
        MatSidenavContainer,
        MatSidenav,
        MatSidenavContent,
        MatToolbar,
        MatNavList,
        MatListItem,
        MatListItemIcon,
        MatListItemTitle,
        MatIcon,
        MatIconButton,
        MatTooltip,
    ],
    templateUrl: "./shell.component.html",
    styleUrl: "./shell.component.scss",
})
export class ShellComponent {
    private static readonly navKey = "babelary.nav.collapsed";

    public readonly sessionStore = inject(SessionStore);
    public readonly handset = toSignal(
        inject(BreakpointObserver)
            .observe(Breakpoints.Handset)
            .pipe(map((state) => state.matches)),
        { initialValue: false },
    );

    /** Full page only: labels stay available in the handset drawer. */
    public readonly collapsed = signal(this.readCollapsed());
    public readonly iconOnly = computed(() => this.collapsed() && !this.handset());
    public readonly crumbs = computed(() => this.crumbsFor(this.path(), this.projectsStore.current()));

    private readonly router = inject(Router);
    private readonly projectsStore = inject(ProjectsStore);
    private readonly injector = inject(Injector);
    private readonly path = toSignal(
        this.router.events.pipe(
            filter((event): event is NavigationEnd => event instanceof NavigationEnd),
            map(() => this.router.url),
            startWith(this.router.url),
        ),
        { initialValue: this.router.url },
    );
    private readonly container = viewChild(MatSidenavContainer);

    public toggleCollapsed(): void {
        const next = !this.collapsed();
        this.collapsed.set(next);
        localStorage.setItem(ShellComponent.navKey, next ? "1" : "0");
        afterNextRender(() => this.container()?.updateContentMargins(), { injector: this.injector });
    }

    public closeIfHandset(nav: MatSidenav): void {
        if (this.handset()) {
            void nav.close();
        }
    }

    public async logout(): Promise<void> {
        await this.sessionStore.logout();
        await this.router.navigateByUrl("/connexion");
    }

    private readCollapsed(): boolean {
        try {
            return localStorage.getItem(ShellComponent.navKey) === "1";
        } catch {
            return false;
        }
    }

    private crumbsFor(url: string, current: { slug: string; name: string } | null): ShellCrumb[] {
        const path = url.split(/[?#]/)[0];
        if (path === "/profil") {
            return [{ key: "GLOBAL.PROFILE", path: "/profil" }];
        }

        if (path === "/projet/nouveau") {
            return [
                { key: "GLOBAL.PROJECTS", path: "/" },
                { key: "PROJECT.CREATE", path: "/projet/nouveau" },
            ];
        }

        const match = /^\/projet\/([^/]+)(?:\/([^/]+)(?:\/([^/]+)(?:\/([^/]+))?)?)?$/.exec(path);
        if (match) {
            const slug = decodeURIComponent(match[1]);
            const text = current?.slug === slug ? current.name : slug;
            const projectPath = `/projet/${match[1]}`;
            if (match[2] === "editeur" && match[3] && match[4]) {
                const code = decodeURIComponent(match[3]);
                const label = localeLabel(code) || code;
                return [
                    { key: "GLOBAL.PROJECTS", path: "/" },
                    { text, path: projectPath },
                    { text: label, path: projectPath },
                    { text: decodeURIComponent(match[4]), path },
                ];
            }

            if (match[2] === "propositions") {
                const crumbs: ShellCrumb[] = [
                    { key: "GLOBAL.PROJECTS", path: "/" },
                    { text, path: projectPath },
                    { key: "PROJECT.PROPOSALS", path: `${projectPath}/propositions` },
                ];
                if (match[3]) {
                    const code = decodeURIComponent(match[3]);
                    crumbs.push({ text: localeLabel(code) || code, path });
                }
                return crumbs;
            }

            if (match[2] === "parametres" || match[2] === "source" || match[2] === "membres") {
                const tab =
                    match[2] === "source" ? "PROJECT.TAB_SOURCE" : match[2] === "membres" ? "PROJECT.TAB_MEMBERS" : "PROJECT.TAB_SETTINGS";
                return [
                    { key: "GLOBAL.PROJECTS", path: "/" },
                    { text, path: projectPath },
                    { key: tab, path: `${projectPath}/${match[2]}` },
                ];
            }

            return [
                { key: "GLOBAL.PROJECTS", path: "/" },
                { text, path: projectPath },
            ];
        }

        return [{ key: "GLOBAL.PROJECTS", path: "/" }];
    }
}
