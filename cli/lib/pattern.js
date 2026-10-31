"use strict";

const FOLDER = "{locale}/{name}.json";
const LOCALE_FILE = "{locale}.json";
const SEGMENT = /^(?:[A-Za-z0-9._-]|\{(?:locale|source|name|file)\})+$/;

/**
 * Accepts a pattern, or the folder layout when it is empty.
 * `*` is a `{file}` segment and `*.json` is `{name}.json`.
 * @param {string | undefined} pattern
 */
function normalize(pattern) {
    let value = String(pattern ?? "").trim().replaceAll("\\", "/");
    if (value.length === 0) {
        return FOLDER;
    }

    if (value.length > 200 || value.startsWith("/") || value.endsWith("/") || value.includes("..")) {
        throw new Error("File pattern is invalid.");
    }

    const segments = value.split("/");
    if (segments.some((segment) => segment.length === 0)) {
        throw new Error("File pattern is invalid.");
    }

    value = segments
        .map((segment) => (segment === "*" ? "{file}" : segment === "*.json" ? "{name}.json" : segment))
        .join("/");
    const localeCount = count(value, "{locale}");
    const variable = count(value, "{name}") + count(value, "{file}");
    const known = value.split("/").every((segment) => SEGMENT.test(segment));
    if (!known || localeCount !== 1 || variable > 1 || (!value.endsWith(".json") && !value.includes("{file}"))) {
        throw new Error("File pattern is invalid.");
    }

    return value;
}

/**
 * @param {string} pattern
 * @param {string} locale
 * @param {string} sourceLocale
 * @param {string} relativePath
 * @returns {string | null} stored file name
 */
function match(pattern, locale, sourceLocale, relativePath) {
    const normalized = normalize(pattern);
    const found = matcher(normalized, locale, sourceLocale).exec(relativePath.replaceAll("\\", "/"));
    if (!found) {
        return null;
    }

    if (found.groups?.name) {
        return `${found.groups.name}.json`;
    }

    if (found.groups?.file) {
        return found.groups.file;
    }

    return render(normalized, sourceLocale, sourceLocale, "").split("/").pop() ?? null;
}

/**
 * @param {string} pattern
 * @param {string} locale
 * @param {string} sourceLocale
 * @param {string} storedName
 */
function exportPath(pattern, locale, sourceLocale, storedName) {
    const normalized = normalize(pattern);
    if (count(normalized, "{name}") + count(normalized, "{file}") === 0) {
        const sourcePath = render(normalized, sourceLocale, sourceLocale, storedName);
        const sourceFile = sourcePath.split("/").pop();
        if (sourceFile !== storedName) {
            return storedName;
        }
    }

    return render(normalized, locale, sourceLocale, storedName);
}

/**
 * @param {string} layout
 * @param {string | undefined} files
 */
function fromLayout(layout, files) {
    if (files) {
        return normalize(files);
    }

    if (layout === "locale") {
        return LOCALE_FILE;
    }

    if (layout !== "folder") {
        throw new Error("babelary.yml: layout must be folder or locale.");
    }

    return FOLDER;
}

function render(pattern, locale, sourceLocale, storedName) {
    const stem = storedName.endsWith(".json") ? storedName.slice(0, -".json".length) : storedName;
    return pattern
        .replaceAll("{locale}", locale)
        .replaceAll("{source}", sourceLocale)
        .replaceAll("{name}", stem)
        .replaceAll("{file}", storedName);
}

function matcher(pattern, locale, sourceLocale) {
    let expression = "^";
    for (let index = 0; index < pattern.length;) {
        const token = ["{locale}", "{source}", "{name}", "{file}"].find((candidate) => pattern.startsWith(candidate, index));
        if (token === "{locale}") {
            expression += escapeRegExp(locale);
            index += token.length;
        } else if (token === "{source}") {
            expression += escapeRegExp(sourceLocale);
            index += token.length;
        } else if (token === "{name}") {
            expression += "(?<name>[A-Za-z0-9][A-Za-z0-9._-]*)";
            index += token.length;
        } else if (token === "{file}") {
            expression += "(?<file>[A-Za-z0-9][A-Za-z0-9._-]*\\.json)";
            index += token.length;
        } else {
            expression += escapeRegExp(pattern[index]);
            index += 1;
        }
    }

    return new RegExp(`${expression}$`);
}

function count(value, token) {
    return value.split(token).length - 1;
}

function escapeRegExp(value) {
    return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

module.exports = {
    FOLDER,
    LOCALE_FILE,
    normalize,
    match,
    exportPath,
    fromLayout,
};
