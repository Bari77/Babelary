"use strict";

/**
 * ANSI colors for a terminal. Plain text when the stream is not a terminal
 * or when NO_COLOR is set. FORCE_COLOR turns them back on.
 * @param {NodeJS.WritableStream} stream
 */
function style(stream) {
    const noColor = process.env.NO_COLOR !== undefined;
    const forced = process.env.FORCE_COLOR !== undefined && process.env.FORCE_COLOR !== "0";
    const on = !noColor && (forced || Boolean(stream && stream.isTTY));
    const paint = (open) => (text) => (on ? `\x1b[${open}m${text}\x1b[0m` : String(text));

    return {
        strong: paint("1"),
        muted: paint("2"),
        ok: paint("32"),
        warn: paint("33"),
        bad: paint("31"),
        accent: paint("36"),
    };
}

module.exports = { style };
