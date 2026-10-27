import { Component, inject } from "@angular/core";
import { MatButton } from "@angular/material/button";
import { MatCard, MatCardAvatar, MatCardContent, MatCardHeader, MatCardTitle } from "@angular/material/card";
import { MatIcon } from "@angular/material/icon";
import {
    MatCell,
    MatCellDef,
    MatColumnDef,
    MatHeaderCell,
    MatHeaderCellDef,
    MatHeaderRow,
    MatHeaderRowDef,
    MatRow,
    MatRowDef,
    MatTable,
} from "@angular/material/table";
import { RouterLink } from "@angular/router";
import { SkeletonComponent } from "@core/skeleton.component";
import { TranslationMeterComponent } from "@features/projects/components/translation-meter.component";
import { LocaleFlagComponent } from "@features/locales/components/locale-flag.component";
import { LocalesStore } from "@features/locales/stores/locales.store";
import { ProjectsStore } from "@features/projects/stores/projects.store";
import { SessionStore } from "@features/session/stores/session.store";
import { TranslateModule } from "@ngx-translate/core";

@Component({
    selector: "app-home",
    standalone: true,
    imports: [
        TranslateModule,
        MatCard,
        MatCardHeader,
        MatCardTitle,
        MatCardContent,
        MatCardAvatar,
        MatIcon,
        MatButton,
        RouterLink,
        MatTable,
        MatColumnDef,
        MatHeaderCell,
        MatHeaderCellDef,
        MatCell,
        MatCellDef,
        MatHeaderRow,
        MatHeaderRowDef,
        MatRow,
        MatRowDef,
        TranslationMeterComponent,
        LocaleFlagComponent,
        SkeletonComponent,
    ],
    templateUrl: "./home.component.html",
    styleUrl: "./home.component.scss",
})
export class HomeComponent {
    public readonly columns = ["name", "sourceLocale", "targetCount", "progress"];
    public readonly projectsStore = inject(ProjectsStore);
    public readonly localesStore = inject(LocalesStore);
    public readonly sessionStore = inject(SessionStore);

    public constructor() {
        void this.projectsStore.load();
        void this.localesStore.load();
    }
}
