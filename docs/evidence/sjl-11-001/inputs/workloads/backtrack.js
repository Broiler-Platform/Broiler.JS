// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// BACKTRACK - bounded backtracking: a nested quantifier anchored at both ends, over runs that end in the
// letter it wants and runs that do not; a starred alternation of overlapping choices; a repeated word
// found through a back-reference; quoted strings closed by a back-reference behind a negative look-ahead;
// and a four-letter palindrome through named back-references. Every subject is short enough that the
// work of each call is bounded.
//
// The answer counts the calls that completed, not what they found: SJL-11 corrects results in both
// engines, and a measurement whose answer changed with a correction would refuse the very change it
// measures, as SJL-3's did. The control beside this file is the same loops with each call replaced by a
// type test of its input. README.md beside this file says which rule judges it.

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
        total += typeof nested.test(runs[j]) === "boolean" ? 1 : 0;
    }
    for (var c = 0; c < choices.length; c++) {
        total += typeof choice.test(choices[c]) === "boolean" ? 1 : 0;
    }
    var pairs = words.match(repeated);
    total += pairs === null || typeof pairs === "object" ? 1 : 0;
    var literals = quoted.match(strings);
    total += literals === null || typeof literals === "object" ? 1 : 0;
    var mirror = palindrome.exec(words);
    total += mirror === null || typeof mirror === "object" ? 1 : 0;
}
total;
