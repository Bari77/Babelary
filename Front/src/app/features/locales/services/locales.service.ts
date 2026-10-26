import { Injectable } from "@angular/core";
import { BaseService } from "@core/http/base.service";
import { Locale } from "@features/locales/models/locale.model";
import { Observable } from "rxjs";

/**
 * Accepted locales.
 */
@Injectable()
export class LocalesService extends BaseService {
    protected override baseUrlService = "Locales";

    public list(): Observable<Locale[]> {
        return this.httpList(Locale, "list");
    }
}
