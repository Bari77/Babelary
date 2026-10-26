/**
 * Whether the instance already has an owner.
 */
export class SetupStatus {
    public constructor(public installed: boolean) {}

    public static fromDto(dto: SetupStatusDto): SetupStatus {
        return new SetupStatus(dto.installed);
    }
}

/**
 * Raw setup payload.
 */
export interface SetupStatusDto {
    installed: boolean;
}
