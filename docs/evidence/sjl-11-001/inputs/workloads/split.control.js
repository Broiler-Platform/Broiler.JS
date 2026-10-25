// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// SPLIT (CONTROL) - the workload in split.js with each split replaced by a type test of its input. The
// same rows, patterns, loops and accumulation; the difference between the two is the cost of the splits.

var rows = [];
for (var i = 0; i < 20; i++) {
    var row = "";
    for (var k = 0; k < 15; k++) {
        row += (k === 0 ? "" : (k % 3 === 0 ? " ; " : ",")) + "field" + i + "-" + k;
    }
    rows.push(row);
}
var separators = /\s*[,;]\s*/;
var digits = /(\d+)/;
var comma = /,/;
var total = 0;
for (var r = 0; r < 200; r++) {
    for (var j = 0; j < rows.length; j++) {
        total += typeof rows[j] === "string" ? 1 : 0;
        total += typeof rows[j] === "string" ? 1 : 0;
        total += typeof rows[j] === "string" ? 1 : 0;
    }
}
total;
