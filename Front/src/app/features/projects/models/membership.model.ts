/**
 * Progress of one source file in one target locale.
 */
export class FileProgress {
    public constructor(
        public name: string,
        public translated: number,
        public total: number,
        public pending: number,
        public pendingOpen: number = 0,
    ) {}

    public covered(): number {
        return Math.min(this.total, this.translated + Math.max(0, this.pendingOpen));
    }

    public static fromDto(dto: FileProgressDto): FileProgress {
        return new FileProgress(dto.name, dto.translated, dto.total, dto.pending ?? 0, dto.pendingOpen ?? 0);
    }
}

/**
 * Raw file progress payload.
 */
export interface FileProgressDto {
    name: string;
    translated: number;
    total: number;
    pending?: number;
    pendingOpen?: number;
}

/**
 * A translator of a project.
 */
export class ProjectMember {
    public constructor(
        public accountId: string,
        public name: string,
        public email: string,
        public locales: string[],
    ) {}

    public static fromDto(dto: ProjectMemberDto): ProjectMember {
        return new ProjectMember(dto.accountId, dto.name, dto.email, dto.locales ?? []);
    }
}

/**
 * Raw member payload.
 */
export interface ProjectMemberDto {
    accountId: string;
    name: string;
    email: string;
    locales: string[];
}

/**
 * An invitation that can still be opened.
 */
export class ProjectInvite {
    public constructor(
        public id: string,
        public email: string,
        public locales: string[],
        public expiresAt: string,
    ) {}

    public static fromDto(dto: ProjectInviteDto): ProjectInvite {
        return new ProjectInvite(dto.id, dto.email, dto.locales ?? [], dto.expiresAt);
    }
}

/**
 * Raw invitation payload.
 */
export interface ProjectInviteDto {
    id: string;
    email: string;
    locales: string[];
    expiresAt: string;
}

/**
 * Invitation just created. The code is shown once.
 */
export class CreatedInvite extends ProjectInvite {
    public constructor(
        id: string,
        email: string,
        locales: string[],
        expiresAt: string,
        public code: string,
    ) {
        super(id, email, locales, expiresAt);
    }

    public static override fromDto(dto: CreatedInviteDto): CreatedInvite {
        return new CreatedInvite(dto.id, dto.email, dto.locales ?? [], dto.expiresAt, dto.code);
    }
}

/**
 * Raw created invitation payload.
 */
export interface CreatedInviteDto extends ProjectInviteDto {
    code: string;
}

/**
 * What an invitation link shows before a password is chosen.
 */
export class InvitePreview {
    public constructor(
        public projectName: string,
        public projectSlug: string,
        public email: string,
        public locales: string[],
        public accountExists: boolean,
    ) {}

    public static fromDto(dto: InvitePreviewDto): InvitePreview {
        return new InvitePreview(dto.projectName, dto.projectSlug, dto.email, dto.locales ?? [], dto.accountExists);
    }
}

/**
 * Raw invitation preview payload.
 */
export interface InvitePreviewDto {
    projectName: string;
    projectSlug: string;
    email: string;
    locales: string[];
    accountExists: boolean;
}
