import { Project, ProjectDto } from "@features/projects/models/project.model";

/**
 * A source file already stored on the project.
 */
export class ProjectFile {
    public constructor(
        public name: string,
        public keyCount: number,
    ) {}

    public static fromDto(dto: ProjectFileDto): ProjectFile {
        return new ProjectFile(dto.name, dto.keyCount);
    }
}

/**
 * Raw source file payload.
 */
export interface ProjectFileDto {
    name: string;
    keyCount: number;
}

/**
 * What the import did to one file.
 */
export class ImportFileResult {
    public constructor(
        public name: string,
        public created: number,
        public updated: number,
        public deleted: number,
        public error: string | null,
    ) {}

    public static fromDto(dto: ImportFileResultDto): ImportFileResult {
        return new ImportFileResult(dto.name, dto.created, dto.updated, dto.deleted, dto.error);
    }
}

/**
 * Raw import row.
 */
export interface ImportFileResultDto {
    name: string;
    created: number;
    updated: number;
    deleted: number;
    error: string | null;
}

/**
 * Project state after an import, plus one row per uploaded file.
 */
export class ImportSourcesResult {
    public constructor(
        public project: Project,
        public files: ImportFileResult[],
    ) {}

    public static fromDto(dto: ImportSourcesResultDto): ImportSourcesResult {
        return new ImportSourcesResult(
            Project.fromDto(dto.project),
            (dto.files ?? []).map((file) => ImportFileResult.fromDto(file)),
        );
    }
}

/**
 * Raw import payload.
 */
export interface ImportSourcesResultDto {
    project: ProjectDto;
    files: ImportFileResultDto[];
}

/**
 * One key refused while importing a translation file.
 */
export class ImportTranslationIssue {
    public constructor(
        public path: string,
        public error: string,
        public detail: string | null,
    ) {}

    public static fromDto(dto: ImportTranslationIssueDto): ImportTranslationIssue {
        return new ImportTranslationIssue(dto.path, dto.error, dto.detail);
    }
}

/**
 * Raw key issue.
 */
export interface ImportTranslationIssueDto {
    path: string;
    error: string;
    detail: string | null;
}

/**
 * What a translation import did to one file.
 */
export class ImportTranslationFileResult {
    public constructor(
        public name: string,
        public updated: number,
        public unchanged: number,
        public skipped: number,
        public ignored: number,
        public missing: number,
        public issues: ImportTranslationIssue[],
        public error: string | null,
    ) {}

    public static fromDto(dto: ImportTranslationFileResultDto): ImportTranslationFileResult {
        return new ImportTranslationFileResult(
            dto.name,
            dto.updated,
            dto.unchanged,
            dto.skipped,
            dto.ignored,
            dto.missing ?? 0,
            (dto.issues ?? []).map((issue) => ImportTranslationIssue.fromDto(issue)),
            dto.error,
        );
    }
}

/**
 * Raw translation import row.
 */
export interface ImportTranslationFileResultDto {
    name: string;
    updated: number;
    unchanged: number;
    skipped: number;
    ignored: number;
    missing: number;
    issues: ImportTranslationIssueDto[];
    error: string | null;
}

/**
 * Project state after importing translations for one locale.
 */
export class ImportTranslationsResult {
    public constructor(
        public project: Project,
        public files: ImportTranslationFileResult[],
    ) {}

    public static fromDto(dto: ImportTranslationsResultDto): ImportTranslationsResult {
        return new ImportTranslationsResult(
            Project.fromDto(dto.project),
            (dto.files ?? []).map((file) => ImportTranslationFileResult.fromDto(file)),
        );
    }
}

/**
 * Raw translation import payload.
 */
export interface ImportTranslationsResultDto {
    project: ProjectDto;
    files: ImportTranslationFileResultDto[];
}
