import { Injectable } from "@angular/core";
import { BaseService } from "@core/http/base.service";
import { Observable } from "rxjs";
import { Health } from "@features/home/models/health.model";

/**
 * Health endpoint.
 */
@Injectable()
export class HealthService extends BaseService {
    protected override baseUrlService = "Health";

    public get(): Observable<Health | null> {
        return this.httpGet(Health, "get");
    }
}
