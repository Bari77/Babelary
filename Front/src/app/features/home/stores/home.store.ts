import { inject, resource } from "@angular/core";
import { HealthService } from "@features/home/services/health.service";
import { firstValueFrom } from "rxjs";

export class HomeStore {
    public readonly health = resource({
        loader: () => firstValueFrom(this.healthService.get()),
    });

    private readonly healthService = inject(HealthService);

    public reload(): void {
        this.health.reload();
    }
}
