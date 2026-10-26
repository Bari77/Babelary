/**
 * Signed-in account.
 */
export class Account {
    public constructor(
        public id: string,
        public name: string,
        public email: string,
        public isInstanceOwner: boolean,
    ) {}

    public static fromDto(dto: AccountDto): Account {
        return new Account(dto.id, dto.name, dto.email, dto.isInstanceOwner);
    }
}

/**
 * Raw account payload.
 */
export interface AccountDto {
    id: string;
    name: string;
    email: string;
    isInstanceOwner: boolean;
}
