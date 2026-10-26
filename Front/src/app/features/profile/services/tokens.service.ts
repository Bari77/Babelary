import { HttpParams } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { BaseService } from "@core/http/base.service";
import { CliToken, CreatedCliToken } from "@features/profile/models/cli-token.model";
import { Observable } from "rxjs";

/**
 * Personal CLI tokens.
 */
@Injectable()
export class TokensService extends BaseService {
    protected override baseUrlService = "Tokens";

    public list(): Observable<CliToken[]> {
        return this.httpList(CliToken, "list");
    }

    public create(body: { name: string }): Observable<CreatedCliToken> {
        return this.httpPost(CreatedCliToken, "create", body);
    }

    public revoke(id: string): Observable<void> {
        return this.httpDelete("revoke", new HttpParams().set("id", id));
    }
}
