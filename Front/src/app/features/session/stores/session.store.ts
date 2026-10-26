import { HttpErrorResponse } from "@angular/common/http";
import { inject, signal } from "@angular/core";
import { apiErrorMessage } from "@core/http/api-error";
import { Account } from "@features/session/models/account.model";
import { SessionService } from "@features/session/services/session.service";
import { SetupService } from "@features/session/services/setup.service";
import { TranslateService } from "@ngx-translate/core";
import { firstValueFrom } from "rxjs";

/**
 * Installation state and the signed-in account.
 */
export class SessionStore {
    public readonly installed = signal<boolean | undefined>(undefined);
    public readonly account = signal<Account | null | undefined>(undefined);
    public readonly pending = signal(false);
    public readonly error = signal<string | null>(null);

    private readonly sessionService = inject(SessionService);
    private readonly setupService = inject(SetupService);
    private readonly translate = inject(TranslateService);
    private ready: Promise<void> | null = null;

    public ensureReady(): Promise<void> {
        this.ready ??= this.load().catch((err: unknown) => {
            this.ready = null;
            throw err;
        });
        return this.ready;
    }

    public async createOwner(payload: { name: string; email: string; password: string }): Promise<boolean> {
        return this.run(async () => {
            const account = await firstValueFrom(this.setupService.create(payload));
            this.installed.set(true);
            this.account.set(account);
        });
    }

    public async login(payload: { email: string; password: string }): Promise<boolean> {
        return this.run(async () => {
            const account = await firstValueFrom(this.sessionService.login(payload));
            this.installed.set(true);
            this.account.set(account);
        });
    }

    public async logout(): Promise<void> {
        await firstValueFrom(this.sessionService.logout());
        this.account.set(null);
    }

    public replaceAccount(account: Account): void {
        this.account.set(account);
    }

    private async load(): Promise<void> {
        const status = await firstValueFrom(this.setupService.status());
        this.installed.set(status?.installed ?? false);
        if (!status?.installed) {
            this.account.set(null);
            return;
        }

        try {
            this.account.set(await firstValueFrom(this.sessionService.me()));
        } catch (err) {
            if (err instanceof HttpErrorResponse && err.status === 401) {
                this.account.set(null);
                return;
            }

            throw err;
        }
    }

    private async run(action: () => Promise<void>): Promise<boolean> {
        this.pending.set(true);
        this.error.set(null);
        try {
            await action();
            return true;
        } catch (err) {
            this.error.set(apiErrorMessage(err) || this.translate.instant("ERROR.ERR_INTERNAL"));
            return false;
        } finally {
            this.pending.set(false);
        }
    }
}
