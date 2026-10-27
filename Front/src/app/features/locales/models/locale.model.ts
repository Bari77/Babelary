const languageNames = new Intl.DisplayNames(["fr-FR"], { type: "language" });
const regionNames = new Intl.DisplayNames(["fr-FR"], { type: "region" });
const regionalBase = 0x1f1e6;

/**
 * Flag emoji for the region of an `xx-XX` locale. Empty when the region is not a country code.
 */
export function localeFlag(code: string): string {
    const region = /^[a-z]{2}-([A-Z]{2})$/.exec(code)?.[1];
    if (!region) {
        return "";
    }

    return String.fromCodePoint(regionalBase + region.charCodeAt(0) - 65, regionalBase + region.charCodeAt(1) - 65);
}

/**
 * French name of a locale, such as « Français (France) ». Empty when the name cannot be formed.
 */
export function localeLabel(code: string): string {
    const separator = code.indexOf("-");
    if (separator <= 0) {
        return "";
    }

    const language = languageNames.of(code.slice(0, separator));
    const region = regionNames.of(code.slice(separator + 1));
    if (!language || !region || language === code.slice(0, separator) || region === code.slice(separator + 1)) {
        return "";
    }

    const text = `${language} (${region})`;
    return text.charAt(0).toLocaleUpperCase("fr-FR") + text.slice(1);
}

/**
 * True when every word of the query is a prefix of a word in the label, ignoring accents.
 */
export function matchesPrefix(label: string, query: string): boolean {
    const words = fold(query)
        .split(/[^\p{L}\p{N}]+/u)
        .filter((word) => word.length > 0);
    if (words.length === 0) {
        return true;
    }

    const labelWords = fold(label).split(/[^\p{L}\p{N}]+/u);
    return words.every((word) => labelWords.some((labelWord) => labelWord.startsWith(word)));
}

function fold(value: string): string {
    return value
        .normalize("NFD")
        .replace(/\p{M}/gu, "")
        .toLocaleLowerCase("fr-FR");
}

/**
 * A locale the interface can offer. The code stays internal.
 */
export class Locale {
    public constructor(
        public code: string,
        public label: string,
    ) {}

    public static fromDto(dto: LocaleDto): Locale {
        return new Locale(dto.code, localeLabel(dto.code));
    }
}

/**
 * Raw locale payload.
 */
export interface LocaleDto {
    code: string;
}
