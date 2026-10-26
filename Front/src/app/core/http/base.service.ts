import { HttpClient, HttpParams } from "@angular/common/http";
import { inject } from "@angular/core";
import { environment } from "@environments/environment";
import { Observable } from "rxjs";
import { map } from "rxjs/operators";

/**
 * Class able to build itself from an API DTO.
 */
export interface DtoConvertibleClass<Tdto, Tmodel> {
    fromDto(dto: Tdto): Tmodel;
}

/**
 * HTTP facade. URLs are `{apiUrl}/{Controller}/{action}` with query parameters.
 */
export abstract class BaseService {
    protected baseUrlService = "";
    protected readonly http: HttpClient = inject(HttpClient);

    protected getURL(action: string): string {
        return environment.apiUrl + "/" + this.baseUrlService + "/" + action;
    }

    protected httpGet<Tdto, Tmodel>(
        modelClass: DtoConvertibleClass<Tdto, Tmodel>,
        action: string,
        params?: HttpParams,
    ): Observable<Tmodel | null> {
        return this.http
            .get<Tdto>(this.getURL(action), { params })
            .pipe(map((dto) => (dto == null ? null : modelClass.fromDto(dto))));
    }

    protected httpList<Tdto, Tmodel>(
        modelClass: DtoConvertibleClass<Tdto, Tmodel>,
        action: string,
        params?: HttpParams,
    ): Observable<Tmodel[]> {
        return this.http
            .get<Tdto[]>(this.getURL(action), { params })
            .pipe(map((dtos) => dtos.map((dto) => modelClass.fromDto(dto))));
    }

    protected httpPost<Tdto, Tmodel>(
        modelClass: DtoConvertibleClass<Tdto, Tmodel>,
        action: string,
        body: unknown,
        params?: HttpParams,
    ): Observable<Tmodel> {
        return this.http
            .post<Tdto>(this.getURL(action), body, { params })
            .pipe(map((dto) => modelClass.fromDto(dto)));
    }

    protected httpPut<Tdto, Tmodel>(
        modelClass: DtoConvertibleClass<Tdto, Tmodel>,
        action: string,
        body: unknown,
    ): Observable<Tmodel> {
        return this.http
            .put<Tdto>(this.getURL(action), body)
            .pipe(map((dto) => modelClass.fromDto(dto)));
    }

    protected httpPostEmpty(action: string, body: unknown): Observable<void> {
        return this.http.post<void>(this.getURL(action), body);
    }

    protected httpDelete(action: string, params?: HttpParams): Observable<void> {
        return this.http.delete<void>(this.getURL(action), { params });
    }
}
