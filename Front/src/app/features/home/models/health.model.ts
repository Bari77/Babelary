/**
 * API reachability.
 */
export class Health {
    public constructor(
        public status: string,
        public database: boolean,
    ) {}

    public static fromDto(dto: HealthDto): Health {
        return new Health(dto.status, dto.database);
    }
}

/**
 * Raw health payload.
 */
export interface HealthDto {
    status: string;
    database: boolean;
}
