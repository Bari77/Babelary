import { Injectable } from "@angular/core";
import { BaseService } from "@core/http/base.service";
import { Account } from "@features/session/models/account.model";
import { SetupStatus } from "@features/session/models/setup-status.model";
import { Observable } from "rxjs";

/**
 * Installation of the instance owner.
 */
@Injectable()
export class SetupService extends BaseService {
    protected override baseUrlService = "Setup";

    public status(): Observable<SetupStatus | null> {
        return this.httpGet(SetupStatus, "status");
    }

    public create(body: { name: string; email: string; password: string }): Observable<Account> {
        return this.httpPost(Account, "create", body);
    }
}
