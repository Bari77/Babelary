/**
 * One key in the editor.
 */
export class TranslationEntry {
    public constructor(
        public keyId: string,
        public fileName: string,
        public path: string,
        public sourceValue: string,
        public text: string,
        public status: string,
        public proposalId: string | null,
        public proposalText: string | null,
        public proposalStatus: string | null,
        public proposalAuthor: string | null,
    ) {}

    public static fromDto(dto: TranslationEntryDto): TranslationEntry {
        return new TranslationEntry(
            dto.keyId,
            dto.fileName ?? "",
            dto.path,
            dto.sourceValue,
            dto.text,
            dto.status,
            dto.proposalId,
            dto.proposalText,
            dto.proposalStatus,
            dto.proposalAuthor,
        );
    }
}

/**
 * Raw editor row.
 */
export interface TranslationEntryDto {
    keyId: string;
    fileName?: string;
    path: string;
    sourceValue: string;
    text: string;
    status: string;
    proposalId: string | null;
    proposalText: string | null;
    proposalStatus: string | null;
    proposalAuthor: string | null;
}

/**
 * A proposal waiting for the owner.
 */
export class Proposal {
    public constructor(
        public id: string,
        public keyId: string,
        public locale: string,
        public fileName: string,
        public path: string,
        public sourceValue: string,
        public currentText: string,
        public text: string,
        public authorName: string,
    ) {}

    public static fromDto(dto: ProposalDto): Proposal {
        return new Proposal(
            dto.id,
            dto.keyId,
            dto.locale,
            dto.fileName,
            dto.path,
            dto.sourceValue,
            dto.currentText,
            dto.text,
            dto.authorName,
        );
    }
}

/**
 * Raw proposal payload.
 */
export interface ProposalDto {
    id: string;
    keyId: string;
    locale: string;
    fileName: string;
    path: string;
    sourceValue: string;
    currentText: string;
    text: string;
    authorName: string;
}

/**
 * Result of one row in a bulk decision.
 */
export class ProposalDecision {
    public constructor(
        public id: string,
        public error: string | null,
        public detail: string | null,
    ) {}

    public static fromDto(dto: ProposalDecisionDto): ProposalDecision {
        return new ProposalDecision(dto.id, dto.error, dto.detail);
    }
}

/**
 * Raw decision payload.
 */
export interface ProposalDecisionDto {
    id: string;
    error: string | null;
    detail: string | null;
}

/**
 * Bulk approval or refusal.
 */
export class ProposalBatch {
    public constructor(
        public results: ProposalDecision[],
        public pending: Proposal[],
    ) {}

    public static fromDto(dto: ProposalBatchDto): ProposalBatch {
        return new ProposalBatch(
            (dto.results ?? []).map((result) => ProposalDecision.fromDto(result)),
            (dto.pending ?? []).map((proposal) => Proposal.fromDto(proposal)),
        );
    }
}

/**
 * Raw bulk payload.
 */
export interface ProposalBatchDto {
    results: ProposalDecisionDto[];
    pending: ProposalDto[];
}

const tokenPattern = /\{\{[A-Za-z0-9_.]+\}\}/g;

/**
 * A slice of a translation string. Tokens stay separate so the template can mark them.
 */
export interface TextPart {
    text: string;
    token: boolean;
}

/**
 * Splits a value into plain text and {{token}} parts. The text is never interpreted as HTML.
 */
export function textParts(value: string): TextPart[] {
    const parts: TextPart[] = [];
    let last = 0;
    for (const match of value.matchAll(tokenPattern)) {
        const index = match.index ?? 0;
        if (index > last) {
            parts.push({ text: value.slice(last, index), token: false });
        }
        parts.push({ text: match[0], token: true });
        last = index + match[0].length;
    }
    if (last < value.length || parts.length === 0) {
        parts.push({ text: value.slice(last), token: false });
    }
    return parts;
}
