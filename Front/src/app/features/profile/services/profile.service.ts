import { Injectable } from "@angular/core";
import { BaseService } from "@core/http/base.service";
import { Account } from "@features/session/models/account.model";
import { Observable } from "rxjs";

/**
 * Name and password of the signed-in account.
 */
@Injectable()
export class ProfileService extends BaseService {
    protected override baseUrlService = "Profile";

    public updateName(body: { name: string }): Observable<Account> {
        return this.httpPost(Account, "update", body);
    }

    public changePassword(body: { currentPassword: string; password: string }): Observable<void> {
        return this.httpPostEmpty("password", body);
    }
}
