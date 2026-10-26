import { Injectable } from "@angular/core";
import { BaseService } from "@core/http/base.service";
import { Account } from "@features/session/models/account.model";
import { Observable } from "rxjs";

/**
 * Web session.
 */
@Injectable()
export class SessionService extends BaseService {
    protected override baseUrlService = "Session";

    public login(body: { email: string; password: string }): Observable<Account> {
        return this.httpPost(Account, "login", body);
    }

    public logout(): Observable<void> {
        return this.httpPostEmpty("logout", {});
    }

    public me(): Observable<Account | null> {
        return this.httpGet(Account, "me");
    }
}
