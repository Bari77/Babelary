import { HttpErrorResponse } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { BaseService } from "@core/http/base.service";
import { Health, HealthDto } from "@features/home/models/health.model";
import { catchError, map, Observable, of } from "rxjs";

/**
 * Result of one health probe. A 503 still counts as a reached API.
 */
export type HealthProbe = { reached: true; health: Health } | { reached: false };

/**
 * Health endpoint.
 */
@Injectable()
export class HealthService extends BaseService {
    protected override baseUrlService = "Health";

    public probe(): Observable<HealthProbe> {
        return this.http.get<HealthDto>(this.getURL("get")).pipe(
            map((dto) => ({ reached: true as const, health: Health.fromDto(dto) })),
            catchError((err: HttpErrorResponse) => {
                const body: unknown = err.error;
                if (isHealthDto(body)) {
                    return of({ reached: true as const, health: Health.fromDto(body) });
                }

                return of({ reached: false as const });
            }),
        );
    }
}

function isHealthDto(body: unknown): body is HealthDto {
    if (body == null || typeof body !== "object") {
        return false;
    }

    const dto = body as HealthDto;
    return typeof dto.status === "string" && typeof dto.database === "boolean";
}
