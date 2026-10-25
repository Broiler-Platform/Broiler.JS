// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// BACKTRACK (CONTROL) - the workload in backtrack.js with each call replaced by a type test of its input.
// The same subjects, patterns, loops and accumulation; the difference between the two is the cost of the
// calls.

var runs = [];
for (var i = 4; i <= 12; i += 2) {
    runs.push("a".repeat(i) + "b");
    runs.push("a".repeat(i) + "c");
}
var choices = [];
for (var k = 2; k <= 12; k += 2) {
    choices.push("ab".repeat(k) + "c");
    choices.push("ab".repeat(k) + "x");
}
var words = "the cat sat on the the mat and the dog dog ran to the park park at noon";
var quoted = "say 'hello' and \"goodbye\" then 'it\\'s late' and \"a \\\"quote\\\" here\"";
var nested = /^(a+)+b$/;
var choice = /^(?:a|ab|abc)*c$/;
var repeated = /\b(\w+)\s+\1\b/g;
var strings = /(["'])(?:\\.|(?!\1)[^\\])*\1/g;
var palindrome = /(?<first>\w)(?<second>\w)\k<second>\k<first>/;
var total = 0;
for (var r = 0; r < 200; r++) {
    for (var j = 0; j < runs.length; j++) {
        total += typeof runs[j] === "string" ? 1 : 0;
    }
    for (var c = 0; c < choices.length; c++) {
        total += typeof choices[c] === "string" ? 1 : 0;
    }
    var pairs = words;
    total += pairs === null || typeof pairs === "string" ? 1 : 0;
    var literals = quoted;
    total += literals === null || typeof literals === "string" ? 1 : 0;
    var mirror = words;
    total += mirror === null || typeof mirror === "string" ? 1 : 0;
}
total;
