// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// SPLIT - String.prototype.split by a pattern: separators with optional white space around them, a
// capturing pattern whose captures are spliced into the result, and a split with a limit, over rows of
// delimited fields.
//
// The answer counts the splits that completed, not their results: SJL-11 corrects results in both
// engines, and a measurement whose answer changed with a correction would refuse the very change it
// measures, as SJL-3's did. The control beside this file is the same loops with each split replaced by
// a type test of its input. README.md beside this file says which rule judges it.

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
        total += Array.isArray(rows[j].split(separators)) ? 1 : 0;
        total += Array.isArray(rows[j].split(digits)) ? 1 : 0;
        total += Array.isArray(rows[j].split(comma, 4)) ? 1 : 0;
    }
}
total;
