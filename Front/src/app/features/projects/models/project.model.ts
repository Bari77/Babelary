/**
 * A translation project.
 */
export class Project {
    public constructor(
        public slug: string,
        public name: string,
        public sourceLocale: string,
        public targets: string[],
        public progress: LocaleProgress[],
        public targetCount: number,
        public translated: number,
        public pending: number,
        public pendingOpen: number,
        public total: number,
        public sourceLocaleLocked: boolean,
    ) {}

    public covered(): number {
        return Math.min(this.total, this.translated + Math.max(0, this.pendingOpen));
    }

    public static fromDto(dto: ProjectDto): Project {
        return new Project(
            dto.slug,
            dto.name,
            dto.sourceLocale,
            dto.targets ?? [],
            (dto.progress ?? []).map((row) => LocaleProgress.fromDto(row)),
            dto.targetCount,
            dto.translated,
            dto.pending ?? 0,
            dto.pendingOpen ?? 0,
            dto.total,
            dto.sourceLocaleLocked,
        );
    }
}

/**
 * Raw project payload.
 */
export interface ProjectDto {
    slug: string;
    name: string;
    sourceLocale: string;
    targets: string[];
    progress: LocaleProgressDto[];
    targetCount: number;
    translated: number;
    pending?: number;
    pendingOpen?: number;
    total: number;
    sourceLocaleLocked: boolean;
}

/**
 * Progress of one target locale.
 */
export class LocaleProgress {
    public constructor(
        public locale: string,
        public translated: number,
        public total: number,
        public pending: number,
        public pendingOpen: number = 0,
    ) {}

    public covered(): number {
        return Math.min(this.total, this.translated + Math.max(0, this.pendingOpen));
    }

    public static fromDto(dto: LocaleProgressDto): LocaleProgress {
        return new LocaleProgress(dto.locale, dto.translated, dto.total, dto.pending ?? 0, dto.pendingOpen ?? 0);
    }
}

/**
 * Raw progress payload.
 */
export interface LocaleProgressDto {
    locale: string;
    translated: number;
    total: number;
    pending?: number;
    pendingOpen?: number;
}
