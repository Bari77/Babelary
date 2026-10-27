#!/usr/bin/env node
"use strict";

const { run } = require("../lib/cli");

run(process.argv.slice(2), {
    cwd: process.cwd(),
    token: process.env.BABELARY_TOKEN,
    stdout: process.stdout,
    stderr: process.stderr,
}).then((code) => {
    process.exit(code);
});
