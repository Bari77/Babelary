"use strict";

const zlib = require("node:zlib");

/**
 * Reads the file entries of a zip buffer. Babelary exports deflate or store.
 * @param {Buffer} buffer
 * @returns {{ name: string, content: Buffer }[]}
 */
function unzip(buffer) {
    const eocd = findEocd(buffer);
    const count = buffer.readUInt16LE(eocd + 10);
    let offset = buffer.readUInt32LE(eocd + 16);
    const files = [];
    for (let index = 0; index < count; index++) {
        if (buffer.readUInt32LE(offset) !== 0x02014b50) {
            throw new Error("Unreadable archive.");
        }

        const method = buffer.readUInt16LE(offset + 10);
        const compressedSize = buffer.readUInt32LE(offset + 20);
        const nameLength = buffer.readUInt16LE(offset + 28);
        const extraLength = buffer.readUInt16LE(offset + 30);
        const commentLength = buffer.readUInt16LE(offset + 32);
        const localOffset = buffer.readUInt32LE(offset + 42);
        const name = buffer.toString("utf8", offset + 46, offset + 46 + nameLength);
        const localNameLength = buffer.readUInt16LE(localOffset + 26);
        const localExtraLength = buffer.readUInt16LE(localOffset + 28);
        const dataStart = localOffset + 30 + localNameLength + localExtraLength;
        const compressed = buffer.subarray(dataStart, dataStart + compressedSize);
        const content = method === 0 ? compressed : method === 8 ? zlib.inflateRawSync(compressed) : null;
        if (content === null) {
            throw new Error("Unreadable archive.");
        }

        files.push({ name, content });
        offset += 46 + nameLength + extraLength + commentLength;
    }

    return files;
}

function findEocd(buffer) {
    const min = Math.max(0, buffer.length - 22 - 0xffff);
    for (let offset = buffer.length - 22; offset >= min; offset--) {
        if (buffer.readUInt32LE(offset) === 0x06054b50) {
            return offset;
        }
    }

    throw new Error("Unreadable archive.");
}

module.exports = { unzip };
