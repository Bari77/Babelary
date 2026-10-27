import { Component, computed, inject, signal } from "@angular/core";
import { MatButton } from "@angular/material/button";
import { MatIcon } from "@angular/material/icon";
import { MatProgressSpinner } from "@angular/material/progress-spinner";
import { HealthProbe, HealthService } from "@features/home/services/health.service";
import { TranslateModule } from "@ngx-translate/core";
import { firstValueFrom } from "rxjs";

type Tone = "ok" | "down" | "unknown" | "loading";

interface ServiceRow {
    id: string;
    icon: string;
    name: string;
    detail: string;
    state: string;
    tone: Tone;
}

/**
 * Reachability of the site, the API, and the database.
 */
@Component({
    selector: "app-health",
    standalone: true,
    imports: [TranslateModule, MatButton, MatIcon, MatProgressSpinner],
    templateUrl: "./health.component.html",
    styleUrl: "./health.component.scss",
})
export class HealthComponent {
    public readonly pending = signal(true);
    public readonly rows = signal<ServiceRow[]>(loadingRows());
    public readonly summary = computed(() => summarize(this.rows()));

    private readonly healthService = inject(HealthService);

    public constructor() {
        void this.refresh();
    }

    public async refresh(): Promise<void> {
        this.pending.set(true);
        try {
            this.rows.set(rowsFor(await firstValueFrom(this.healthService.probe())));
        } finally {
            this.pending.set(false);
        }
    }
}

function loadingRows(): ServiceRow[] {
    return [
        row("web", "public", "HEALTH.WEB", "HEALTH.WEB_OK", "HEALTH.STATE_OK", "ok"),
        row("api", "cloud", "HEALTH.API", "HEALTH.API_LOADING", "HEALTH.STATE_LOADING", "loading"),
        row("database", "storage", "HEALTH.DATABASE", "HEALTH.DATABASE_LOADING", "HEALTH.STATE_LOADING", "loading"),
    ];
}

function rowsFor(probe: HealthProbe): ServiceRow[] {
    if (!probe.reached) {
        return [
            row("web", "public", "HEALTH.WEB", "HEALTH.WEB_OK", "HEALTH.STATE_OK", "ok"),
            row("api", "cloud", "HEALTH.API", "HEALTH.API_DOWN", "HEALTH.STATE_DOWN", "down"),
            row("database", "storage", "HEALTH.DATABASE", "HEALTH.DATABASE_UNKNOWN", "HEALTH.STATE_UNKNOWN", "unknown"),
        ];
    }

    const database = probe.health.database;
    return [
        row("web", "public", "HEALTH.WEB", "HEALTH.WEB_OK", "HEALTH.STATE_OK", "ok"),
        row("api", "cloud", "HEALTH.API", "HEALTH.API_OK", "HEALTH.STATE_OK", "ok"),
        row(
            "database",
            "storage",
            "HEALTH.DATABASE",
            database ? "HEALTH.DATABASE_OK" : "HEALTH.DATABASE_DOWN",
            database ? "HEALTH.STATE_OK" : "HEALTH.STATE_DOWN",
            database ? "ok" : "down",
        ),
    ];
}

function row(id: string, icon: string, name: string, detail: string, state: string, tone: Tone): ServiceRow {
    return { id, icon, name, detail, state, tone };
}

function summarize(rows: ServiceRow[]): { tone: Tone; key: string } {
    if (rows.some((item) => item.tone === "loading")) {
        return { tone: "loading", key: "HEALTH.CHECKING" };
    }

    if (rows.some((item) => item.tone === "down")) {
        return { tone: "down", key: "HEALTH.DEGRADED" };
    }

    return { tone: "ok", key: "HEALTH.ALL_OK" };
}
