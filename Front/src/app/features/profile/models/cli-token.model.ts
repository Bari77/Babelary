/**
 * CLI token listed in the profile. The secret is absent.
 */
export class CliToken {
    public constructor(
        public id: string,
        public name: string,
        public createdAt: string,
    ) {}

    public static fromDto(dto: CliTokenDto): CliToken {
        return new CliToken(dto.id, dto.name, dto.createdAt);
    }
}

/**
 * Raw token payload.
 */
export interface CliTokenDto {
    id: string;
    name: string;
    createdAt: string;
}

/**
 * Token just created. The secret is shown once.
 */
export class CreatedCliToken {
    public constructor(
        public id: string,
        public name: string,
        public secret: string,
        public createdAt: string,
    ) {}

    public static fromDto(dto: CreatedCliTokenDto): CreatedCliToken {
        return new CreatedCliToken(dto.id, dto.name, dto.secret, dto.createdAt);
    }
}

/**
 * Raw created-token payload.
 */
export interface CreatedCliTokenDto {
    id: string;
    name: string;
    secret: string;
    createdAt: string;
}
