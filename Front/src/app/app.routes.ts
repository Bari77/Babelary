import { Routes } from "@angular/router";
import { leaveEditorGuard } from "@features/projects/pages/project-editor.guard";
import { sessionGuard } from "@features/session/session.guard";

export const routes: Routes = [
    {
        path: "installation",
        canActivate: [sessionGuard],
        loadComponent: () => import("@features/session/pages/setup.component").then((m) => m.SetupComponent),
    },
    {
        path: "connexion",
        canActivate: [sessionGuard],
        loadComponent: () => import("@features/session/pages/login.component").then((m) => m.LoginComponent),
    },
    {
        path: "rejoindre",
        canActivate: [sessionGuard],
        loadComponent: () => import("@features/session/pages/join.component").then((m) => m.JoinComponent),
    },
    {
        path: "",
        canActivate: [sessionGuard],
        loadComponent: () => import("@features/shell/pages/shell.component").then((m) => m.ShellComponent),
        children: [
            {
                path: "",
                loadComponent: () => import("@features/home/pages/home.component").then((m) => m.HomeComponent),
            },
            {
                path: "profil",
                loadComponent: () => import("@features/profile/pages/profile.component").then((m) => m.ProfileComponent),
            },
            {
                path: "projet/nouveau",
                loadComponent: () => import("@features/projects/pages/project.component").then((m) => m.ProjectComponent),
                children: [
                    {
                        path: "",
                        loadComponent: () =>
                            import("@features/projects/pages/project-settings.component").then((m) => m.ProjectSettingsComponent),
                    },
                ],
            },
            {
                path: "projet/:project",
                loadComponent: () => import("@features/projects/pages/project.component").then((m) => m.ProjectComponent),
                children: [
                    {
                        path: "",
                        loadComponent: () =>
                            import("@features/projects/pages/project-translation.component").then((m) => m.ProjectTranslationComponent),
                    },
                    {
                        path: "editeur/:locale/:file",
                        pathMatch: "full",
                        canDeactivate: [leaveEditorGuard],
                        loadComponent: () =>
                            import("@features/projects/pages/project-editor.component").then((m) => m.ProjectEditorComponent),
                    },
                    {
                        path: "editeur/:locale",
                        pathMatch: "full",
                        canDeactivate: [leaveEditorGuard],
                        loadComponent: () =>
                            import("@features/projects/pages/project-editor.component").then((m) => m.ProjectEditorComponent),
                    },
                    {
                        path: "propositions",
                        loadComponent: () =>
                            import("@features/projects/pages/project-proposals.component").then((m) => m.ProjectProposalsComponent),
                    },
                    {
                        path: "propositions/:locale",
                        loadComponent: () =>
                            import("@features/projects/pages/project-proposals.component").then((m) => m.ProjectProposalsComponent),
                    },
                    {
                        path: "source",
                        loadComponent: () =>
                            import("@features/projects/pages/project-source.component").then((m) => m.ProjectSourceComponent),
                    },
                    {
                        path: "membres",
                        loadComponent: () =>
                            import("@features/projects/pages/project-members.component").then((m) => m.ProjectMembersComponent),
                    },
                    {
                        path: "parametres",
                        loadComponent: () =>
                            import("@features/projects/pages/project-settings.component").then((m) => m.ProjectSettingsComponent),
                    },
                ],
            },
        ],
    },
    { path: "**", redirectTo: "" },
];
