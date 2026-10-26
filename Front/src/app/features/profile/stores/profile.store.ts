import { inject, signal, WritableSignal } from "@angular/core";
import { apiErrorMessage } from "@core/http/api-error";
import { CliToken, CreatedCliToken } from "@features/profile/models/cli-token.model";
import { ProfileService } from "@features/profile/services/profile.service";
import { TokensService } from "@features/profile/services/tokens.service";
import { SessionStore } from "@features/session/stores/session.store";
import { TranslateService } from "@ngx-translate/core";
import { firstValueFrom } from "rxjs";

/**
 * Profile edits and CLI tokens.
 */
export class ProfileStore {
    public readonly tokens = signal<CliToken[]>([]);
    public readonly revealed = signal<CreatedCliToken | null>(null);
    public readonly nameError = signal<string | null>(null);
    public readonly passwordError = signal<string | null>(null);
    public readonly tokenError = signal<string | null>(null);
    public readonly pending = signal(false);

    private readonly profileService = inject(ProfileService);
    private readonly tokensService = inject(TokensService);
    private readonly sessionStore = inject(SessionStore);
    private readonly translate = inject(TranslateService);

    public async load(): Promise<void> {
        try {
            this.tokens.set(await firstValueFrom(this.tokensService.list()));
        } catch (err) {
            this.tokenError.set(this.failure(err));
        }
    }

    public updateName(name: string): Promise<boolean> {
        return this.run(this.nameError, async () => {
            const account = await firstValueFrom(this.profileService.updateName({ name }));
            this.sessionStore.replaceAccount(account);
        });
    }

    public changePassword(currentPassword: string, password: string): Promise<boolean> {
        return this.run(this.passwordError, async () => {
            await firstValueFrom(this.profileService.changePassword({ currentPassword, password }));
        });
    }

    public createToken(name: string): Promise<boolean> {
        return this.run(this.tokenError, async () => {
            const created = await firstValueFrom(this.tokensService.create({ name }));
            this.revealed.set(created);
            this.tokens.set(await firstValueFrom(this.tokensService.list()));
        });
    }

    public revoke(id: string): Promise<boolean> {
        return this.run(this.tokenError, async () => {
            await firstValueFrom(this.tokensService.revoke(id));
            if (this.revealed()?.id === id) {
                this.revealed.set(null);
            }

            this.tokens.set(await firstValueFrom(this.tokensService.list()));
        });
    }

    public dismissRevealed(): void {
        this.revealed.set(null);
    }

    private async run(error: WritableSignal<string | null>, action: () => Promise<void>): Promise<boolean> {
        this.pending.set(true);
        error.set(null);
        try {
            await action();
            return true;
        } catch (err) {
            error.set(this.failure(err));
            return false;
        } finally {
            this.pending.set(false);
        }
    }

    private failure(err: unknown): string {
        return apiErrorMessage(err) || this.translate.instant("ERROR.ERR_INTERNAL");
    }
}
