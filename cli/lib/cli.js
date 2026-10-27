"use strict";

const fs = require("node:fs");
const path = require("node:path");
const { style } = require("./style");
const { unzip } = require("./zip");

const LOCALE = /^[a-z]{2}-[A-Z]{2}$/;

/**
 * Runs one invocation and returns the process exit code.
 * @param {string[]} args
 * @param {{ cwd: string, token?: string, stdout: NodeJS.WritableStream, stderr: NodeJS.WritableStream }} io
 */
async function run(args, io) {
    if (args.length === 0 || args[0] === "--help" || args[0] === "-h" || args[0] === "help") {
        writeHelp(io);
        return 0;
    }

    const token = (io.token ?? "").trim();
    if (!token) {
        fail(io, "BABELARY_TOKEN is missing. Stopped before any network call.");
        return 1;
    }

    let config;
    try {
        config = loadConfig(io.cwd);
    } catch (error) {
        fail(io, String(error.message));
        return 1;
    }

    try {
        if (args[0] === "push" && args[1] === "sources") {
            return await pushSources(config, token, args.slice(2), io);
        }

        if (args[0] === "push" && args[1] === "translations") {
            return await pushTranslations(config, token, args.slice(2), io);
        }

        if (args[0] === "pull" && args.length === 1) {
            return await pull(config, token, io);
        }

        if (args[0] === "status" && args.length === 1) {
            return await status(config, token, io);
        }
    } catch (error) {
        fail(io, redact(error.message, token));
        return 1;
    }

    fail(io, "Unknown command. babelary --help");
    return 1;
}

function writeHelp(io) {
    const out = style(io.stdout);
    const row = (command, text) => `  ${out.accent(cell(command, 28))}${out.muted(text)}\n`;
    io.stdout.write(`${out.strong("babelary")}  ${out.muted("syncs a repository with a Babelary instance")}\n\n`);
    io.stdout.write(`  ${out.muted("The token is read from BABELARY_TOKEN.")}\n`);
    io.stdout.write(`  ${out.muted("It does not belong in babelary.yml.")}\n`);
    io.stdout.write(`  ${out.muted("layout: locale stores one <locale>.json in base_path.")}\n\n`);
    io.stdout.write(`${out.strong("Commands")}\n`);
    io.stdout.write(row("push sources", "uploads the source locale JSON files"));
    io.stdout.write(row("push translations", "uploads the target locale folders"));
    io.stdout.write(row("pull", "writes the JSON files for each locale"));
    io.stdout.write(row("status", "shows what is left to translate"));
    io.stdout.write(`\n${out.strong("Options")}\n`);
    io.stdout.write(row("--delete-missing-files", "removes server files that are missing on disk"));
    io.stdout.write(row("--overwrite-empty", "an empty local value clears the translation"));
}

function loadConfig(cwd) {
    let dir = path.resolve(cwd);
    for (;;) {
        const candidate = path.join(dir, "babelary.yml");
        if (fs.existsSync(candidate)) {
            return parseConfig(fs.readFileSync(candidate, "utf8"), dir);
        }

        const parent = path.dirname(dir);
        if (parent === dir) {
            break;
        }

        dir = parent;
    }

    throw new Error("babelary.yml was not found.");
}

function parseConfig(text, directory) {
    const values = {};
    for (const raw of text.split(/\r?\n/)) {
        const line = raw.trim();
        if (line.length === 0 || line.startsWith("#")) {
            continue;
        }

        const split = line.indexOf(":");
        if (split <= 0) {
            throw new Error("babelary.yml: unreadable line.");
        }

        const key = line.slice(0, split).trim();
        let value = line.slice(split + 1).trim();
        if ((value.startsWith("\"") && value.endsWith("\"")) || (value.startsWith("'") && value.endsWith("'"))) {
            value = value.slice(1, -1);
        }

        if (key === "token" || key === "api_token" || key === "babelary_token") {
            throw new Error("Remove the token from babelary.yml. Use the BABELARY_TOKEN variable.");
        }

        values[key] = value;
    }

    for (const key of ["base_url", "project", "source_locale", "base_path"]) {
        if (!values[key]) {
            throw new Error(`babelary.yml: ${key} is missing.`);
        }
    }

    const layout = values.layout ?? "folder";
    if (layout !== "folder" && layout !== "locale") {
        throw new Error("babelary.yml: layout must be folder or locale.");
    }

    return {
        baseUrl: values.base_url.replace(/\/+$/, ""),
        project: values.project,
        sourceLocale: values.source_locale,
        basePath: path.resolve(directory, values.base_path),
        localeLayout: layout === "locale",
    };
}

async function pushSources(config, token, args, io) {
    if (!only(args, "--delete-missing-files")) {
        fail(io, "Unknown option. babelary --help");
        return 1;
    }

    const files = [];
    let invalid = false;
    if (config.localeLayout) {
        const loaded = readLocaleDocument(config.basePath, config.sourceLocale);
        if (loaded.error) {
            fail(io, loaded.error);
            return 1;
        }

        files.push({ name: `${config.sourceLocale}.json`, content: loaded.content });
    } else {
        const directory = path.join(config.basePath, config.sourceLocale);
        const names = jsonFiles(directory);
        if (names === null) {
            fail(io, `Source folder not found: ${directory}`);
            return 1;
        }

        if (names.length === 0) {
            fail(io, "No source files.");
            return 1;
        }

        for (const name of names) {
            const content = fs.readFileSync(path.join(directory, name), "utf8");
            const problem = documentProblem(content);
            if (problem) {
                invalid = true;
                fail(io, `${name}: ${problem}, file not sent.`);
                continue;
            }

            files.push({ name, content });
        }
    }

    if (files.length === 0) {
        return 1;
    }

    const deleteMissing = args.includes("--delete-missing-files") && !invalid;
    if (args.includes("--delete-missing-files") && invalid) {
        fail(io, "Some files are invalid: files missing on disk were not deleted.");
    }

    const result = await call(
        config,
        token,
        "POST",
        `/api/v1/Projects/importSources?project=${encodeURIComponent(config.project)}&deleteMissingFiles=${deleteMissing}`,
        { files },
    );
    const out = style(io.stdout);
    const rows = result.files ?? [];
    const width = columnWidth(rows.map((file) => file.name));
    let failed = false;
    let created = 0;
    let updated = 0;
    let deleted = 0;
    heading(io, "Sources", `${config.sourceLocale}  ->  ${config.project}`);
    for (const file of rows) {
        if (file.error) {
            failed = true;
            io.stdout.write(`  ${out.bad(cell(file.name, width))}${out.bad(labelError(file.error))}\n`);
            continue;
        }

        created += file.created;
        updated += file.updated;
        deleted += file.deleted;
        io.stdout.write(`  ${out.accent(cell(file.name, width))}${sourceBits(out, file)}\n`);
    }

    const summary = [`${count(rows.length, "file", "files")}`];
    if (created > 0) {
        summary.push(out.ok(count(created, "created", "created")));
    }

    if (updated > 0) {
        summary.push(out.warn(count(updated, "updated", "updated")));
    }

    if (deleted > 0) {
        summary.push(out.bad(count(deleted, "deleted", "deleted")));
    }

    if (result.deletedFiles > 0) {
        summary.push(out.bad(count(result.deletedFiles, "file removed from the server", "files removed from the server")));
    }

    io.stdout.write(`\n  ${summary.join(out.muted("  ·  "))}\n`);
    return failed ? 1 : 0;
}

async function pushTranslations(config, token, args, io) {
    if (!only(args, "--overwrite-empty")) {
        fail(io, "Unknown option. babelary --help");
        return 1;
    }

    const project = await call(
        config,
        token,
        "GET",
        `/api/v1/Projects/get?project=${encodeURIComponent(config.project)}`,
    );
    const targets = new Set(project.targets ?? []);
    const overwrite = args.includes("--overwrite-empty");
    const folders = config.localeLayout ? localeJsonNames(config.basePath) : localeFolders(config.basePath);
    if (folders === null) {
        fail(io, `Folder not found: ${config.basePath}`);
        return 1;
    }

    const out = style(io.stdout);
    heading(io, "Translations", config.project);
    let failed = false;
    const serverFile = `${config.sourceLocale}.json`;
    for (const locale of folders) {
        if (locale === config.sourceLocale || locale === project.sourceLocale) {
            continue;
        }

        const name = out.accent(cell(locale, 8));
        if (!LOCALE.test(locale)) {
            io.stdout.write(`  ${name}${out.warn("folder ignored")}\n`);
            continue;
        }

        if (!targets.has(locale)) {
            io.stdout.write(`  ${name}${out.warn("ignored, locale not assigned")}\n`);
            continue;
        }

        const names = config.localeLayout ? [serverFile] : (jsonFiles(path.join(config.basePath, locale)) ?? []);
        const files = [];
        for (const fileName of names) {
            const relative = config.localeLayout ? `${locale}.json` : `${locale}/${fileName}`;
            const content = fs.readFileSync(
                config.localeLayout ? path.join(config.basePath, `${locale}.json`) : path.join(config.basePath, locale, fileName),
                "utf8",
            );
            const problem = documentProblem(content);
            if (problem) {
                failed = true;
                fail(io, `${relative}: ${problem}, file not sent.`);
                continue;
            }

            files.push({ name: config.localeLayout ? serverFile : fileName, content });
        }

        if (files.length === 0) {
            continue;
        }

        const result = await call(
            config,
            token,
            "POST",
            `/api/v1/Projects/importTranslations?project=${encodeURIComponent(config.project)}&locale=${encodeURIComponent(locale)}&overwriteEmpty=${overwrite}`,
            { files },
        );
        const tally = tallyTranslations(result.files ?? []);
        if (tally.failed) {
            failed = true;
        }

        io.stdout.write(`  ${name}${translationBits(out, tally).join(out.muted("  ·  "))}\n`);
        for (const issue of tally.issues) {
            const detail = issue.detail ? ` ${issue.detail}` : "";
            io.stdout.write(`      ${out.muted(cell(issue.file, 24))}${out.muted(cell(issue.path, 24))}${out.bad(labelError(issue.error) + detail)}\n`);
        }
    }

    return failed ? 1 : 0;
}

async function pull(config, token, io) {
    const project = await call(
        config,
        token,
        "GET",
        `/api/v1/Projects/get?project=${encodeURIComponent(config.project)}`,
    );
    const locales = [project.sourceLocale, ...(project.targets ?? [])];
    const out = style(io.stdout);
    heading(io, "Pull", config.project);
    let failed = false;
    for (const locale of locales) {
        const archive = await download(
            config,
            token,
            `/api/v1/Projects/exportLocale?project=${encodeURIComponent(config.project)}&locale=${encodeURIComponent(locale)}`,
        );
        const files = unzip(archive).filter((file) => !file.name.includes("..") && !file.name.includes("/") && !file.name.includes("\\"));
        if (config.localeLayout) {
            if (files.length !== 1) {
                failed = true;
                fail(io, `${locale}: locale layout expects one JSON file, got ${files.length}.`);
                continue;
            }

            fs.writeFileSync(path.join(config.basePath, `${locale}.json`), files[0].content);
            io.stdout.write(`  ${out.accent(cell(locale, 8))}${out.ok(`${locale}.json`)}\n`);
            continue;
        }

        const directory = path.join(config.basePath, locale);
        fs.mkdirSync(directory, { recursive: true });
        for (const file of files) {
            fs.writeFileSync(path.join(directory, file.name), file.content);
        }

        io.stdout.write(`  ${out.accent(cell(locale, 8))}${out.ok(count(files.length, "file", "files"))}\n`);
    }

    return failed ? 1 : 0;
}

async function status(config, token, io) {
    const report = await call(
        config,
        token,
        "GET",
        `/api/v1/Projects/status?project=${encodeURIComponent(config.project)}`,
    );
    const out = style(io.stdout);
    const locales = report.locales ?? [];
    heading(io, "Status", config.project);
    let missing = false;
    for (const locale of locales) {
        const empty = locale.empty ?? [];
        const ratio = `${locale.translated} / ${locale.total}`;
        const share = `${percent(locale.translated, locale.total)} %`;
        const complete = empty.length === 0 && locale.review === 0;
        const notes = [];
        if (locale.review > 0) {
            notes.push(out.warn(count(locale.review, "to review", "to review")));
        }

        if (empty.length > 0) {
            missing = true;
            notes.push(out.bad(count(empty.length, "untranslated", "untranslated")));
        } else if (complete) {
            notes.push(out.ok("up to date"));
        }

        const figures = out.muted(cell(ratio, 16)) + (complete ? out.ok(cell(share, 8)) : out.warn(cell(share, 8)));
        io.stdout.write(`  ${out.accent(cell(locale.locale, 8))}${figures}${notes.join("  ")}\n`);
        for (const line of emptyLines(empty)) {
            io.stdout.write(`      ${out.muted(line)}\n`);
        }
    }

    return missing ? 1 : 0;
}

function jsonFiles(directory) {
    if (!fs.existsSync(directory)) {
        return null;
    }

    return fs.readdirSync(directory, { withFileTypes: true })
        .filter((entry) => entry.isFile() && entry.name.endsWith(".json"))
        .map((entry) => entry.name)
        .sort((left, right) => left.localeCompare(right));
}

function readLocaleDocument(directory, locale) {
    const name = `${locale}.json`;
    const filePath = path.join(directory, name);
    if (!fs.existsSync(filePath) || !fs.statSync(filePath).isFile()) {
        return { error: `Source file not found: ${filePath}` };
    }

    const content = fs.readFileSync(filePath, "utf8");
    const problem = documentProblem(content);
    if (problem) {
        return { error: `${name}: ${problem}, file not sent.` };
    }

    return { content };
}

function localeJsonNames(directory) {
    if (!fs.existsSync(directory)) {
        return null;
    }

    return fs.readdirSync(directory, { withFileTypes: true })
        .filter((entry) => entry.isFile() && entry.name.endsWith(".json") && LOCALE.test(entry.name.slice(0, -".json".length)))
        .map((entry) => entry.name.slice(0, -".json".length))
        .sort((left, right) => left.localeCompare(right));
}

function localeFolders(directory) {
    if (!fs.existsSync(directory)) {
        return null;
    }

    return fs.readdirSync(directory, { withFileTypes: true })
        .filter((entry) => entry.isDirectory())
        .map((entry) => entry.name)
        .sort((left, right) => left.localeCompare(right));
}

function documentProblem(text) {
    let data;
    try {
        data = JSON.parse(text);
    } catch {
        return "invalid JSON";
    }

    if (data === null || typeof data !== "object" || Array.isArray(data)) {
        return "invalid JSON";
    }

    return leafProblem(data);
}

function leafProblem(value) {
    if (typeof value === "string") {
        return null;
    }

    if (value === null || typeof value !== "object" || Array.isArray(value)) {
        return "a leaf is not a string";
    }

    for (const child of Object.values(value)) {
        const problem = leafProblem(child);
        if (problem) {
            return problem;
        }
    }

    return null;
}

function only(args, allowed) {
    return args.every((arg) => arg === allowed);
}

function fail(io, message) {
    const err = style(io.stderr);
    io.stderr.write(err.bad(message) + "\n");
}

function heading(io, title, detail) {
    const out = style(io.stdout);
    io.stdout.write(`${out.strong(title)}  ${out.muted(detail)}\n\n`);
}

function cell(text, width) {
    const value = String(text ?? "");
    return value + " ".repeat(Math.max(1, width - value.length));
}

function columnWidth(names) {
    return Math.max(12, ...names.map((name) => String(name ?? "").length)) + 2;
}

function sourceBits(out, file) {
    const bits = [];
    if (file.created > 0) {
        bits.push(out.ok(count(file.created, "created", "created")));
    }

    if (file.updated > 0) {
        bits.push(out.warn(count(file.updated, "updated", "updated")));
    }

    if (file.deleted > 0) {
        bits.push(out.bad(count(file.deleted, "deleted", "deleted")));
    }

    return bits.length === 0 ? out.muted("unchanged") : bits.join(out.muted("  ·  "));
}

function tallyTranslations(files) {
    const tally = { updated: 0, unchanged: 0, skipped: 0, ignored: 0, issues: [], failed: false, files: files.length };
    for (const file of files) {
        if (file.error) {
            tally.failed = true;
            tally.issues.push({ file: file.name, path: "", error: file.error, detail: "" });
            continue;
        }

        tally.updated += file.updated;
        tally.unchanged += file.unchanged;
        tally.skipped += file.skipped;
        tally.ignored += file.ignored;
        for (const issue of file.issues ?? []) {
            tally.failed = true;
            tally.issues.push({ file: file.name, path: issue.path, error: issue.error, detail: issue.detail });
        }
    }

    return tally;
}

function translationBits(out, tally) {
    const bits = [];
    if (tally.updated > 0) {
        bits.push(out.ok(count(tally.updated, "saved", "saved")));
    }

    if (tally.unchanged > 0) {
        bits.push(out.muted(count(tally.unchanged, "unchanged", "unchanged")));
    }

    if (tally.skipped > 0) {
        bits.push(out.muted(count(tally.skipped, "empty value kept", "empty values kept")));
    }

    if (tally.ignored > 0) {
        bits.push(out.warn(count(tally.ignored, "unknown key", "unknown keys")));
    }

    if (tally.issues.length > 0) {
        bits.push(out.bad(count(tally.issues.length, "rejected", "rejected")));
    }

    if (bits.length === 0) {
        bits.push(out.muted("nothing to send"));
    }

    return bits;
}

function labelError(code) {
    const labels = {
        ERR_PLACEHOLDER_MISSING: "missing placeholder",
        ERR_PLACEHOLDER_UNKNOWN: "unknown placeholder",
        ERR_FILE_MISSING: "file not in the project",
        ERR_FILE_JSON_INVALID: "invalid JSON",
        ERR_FILE_NAME_INVALID: "invalid file name",
    };
    return labels[code] || code || "error";
}

function percent(part, total) {
    if (!total || part <= 0) {
        return 0;
    }

    return Math.floor((part / total) * 100);
}

function emptyLines(empty) {
    if (empty.length <= 8) {
        return empty.map((key) => `${cell(key.file, 24)}${key.path}`);
    }

    const byFile = new Map();
    for (const key of empty) {
        byFile.set(key.file, (byFile.get(key.file) ?? 0) + 1);
    }

    return [...byFile.entries()].map(([file, amount]) => `${cell(file, 24)}${count(amount, "key", "keys")}`);
}

function count(value, one, many) {
    const number = Number(value) || 0;
    return `${number} ${number === 1 ? one : many}`;
}

function redact(message, token) {
    const text = message || "Unexpected error.";
    return token ? text.split(token).join("[token]") : text;
}

async function call(config, token, method, urlPath, body) {
    const payload = await request(config, token, method, urlPath, body);
    return payload ?? {};
}

async function download(config, token, urlPath) {
    const response = await request(config, token, "GET", urlPath);
    return response;
}

async function request(config, token, method, urlPath, body) {
    let response;
    try {
        response = await fetch(config.baseUrl + urlPath, {
            method,
            headers: {
                Authorization: `Bearer ${token}`,
                Accept: body === undefined && method === "GET" ? "*/*" : "application/json",
                ...(body === undefined ? {} : { "Content-Type": "application/json" }),
            },
            body: body === undefined ? undefined : JSON.stringify(body),
        });
    } catch {
        throw new Error("The Babelary instance is not responding.");
    }

    if (response.status === 401) {
        throw new Error("Token rejected.");
    }

    if (method === "GET" && urlPath.includes("exportLocale")) {
        if (!response.ok) {
            throw new Error(await errorMessage(response, config));
        }

        return Buffer.from(await response.arrayBuffer());
    }

    if (!response.ok) {
        throw new Error(await errorMessage(response, config));
    }

    const text = await response.text();
    if (text.length === 0) {
        return {};
    }

    return JSON.parse(text);
}

async function errorMessage(response, config) {
    const text = await response.text();
    let code = "ERR_INTERNAL";
    try {
        code = JSON.parse(text).Message || "ERR_INTERNAL";
    } catch {
        return "ERR_INTERNAL";
    }

    if (code === "ERR_NOT_FOUND") {
        return `Project not found: ${config.project}. Check the project value in babelary.yml.`;
    }

    return code;
}

module.exports = { run, loadConfig };
