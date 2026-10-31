"use strict";

const assert = require("node:assert/strict");
const test = require("node:test");
const { exportPath, fromLayout, match, normalize } = require("./pattern");

test("folder pattern keeps the file name and adds the locale folder", () => {
    assert.equal(match("{locale}/{name}.json", "en-US", "en-US", "en-US/home.json"), "home.json");
    assert.equal(exportPath("{locale}/{name}.json", "fr-FR", "en-US", "home.json"), "fr-FR/home.json");
});

test("locale file is renamed to the exported language", () => {
    assert.equal(match("{locale}.json", "en-US", "en-US", "en-US.json"), "en-US.json");
    assert.equal(exportPath("{locale}.json", "fr-FR", "en-US", "en-US.json"), "fr-FR.json");
});

test("name can sit beside the locale", () => {
    assert.equal(match("{name}.{locale}.json", "fr-FR", "en-US", "home.fr-FR.json"), "home.json");
    assert.equal(exportPath("{name}.{locale}.json", "en-US", "en-US", "home.json"), "home.en-US.json");
});

test("a single file pattern leaves another stored name alone", () => {
    assert.equal(exportPath("{locale}.json", "fr-FR", "en-US", "home.json"), "home.json");
});

test("star is a file wildcard", () => {
    assert.equal(normalize("{locale}/*.json"), "{locale}/{name}.json");
    assert.equal(normalize("{locale}/*"), "{locale}/{file}");
});

test("layout is a shortcut and files wins", () => {
    assert.equal(fromLayout("locale", undefined), "{locale}.json");
    assert.equal(fromLayout("folder", "{name}.{locale}.json"), "{name}.{locale}.json");
});

test("invalid patterns are refused", () => {
    for (const pattern of ["{name}.json", "{locale}/{locale}.json", "../{locale}.json", "{locale}/{name}.txt"]) {
        assert.throws(() => normalize(pattern), /invalid/i);
    }
});
