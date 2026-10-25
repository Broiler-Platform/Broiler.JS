// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// SCAN - a global search for a literal across a long ASCII text: String.prototype.match with a global
// literal pattern, which reads the whole text on every call, and RegExp.prototype.test with a literal
// that first occurs at the text's end.
//
// The answer counts the searches that completed, not what they found: SJL-11 corrects results in both
// engines, and a measurement whose answer changed with a correction would refuse the very change it
// measures, as SJL-3's did. The control beside this file is the same loops with each search replaced by
// a type test of its input. README.md beside this file says which rule judges it.

var words = ["alpha", "beta", "gamma", "delta", "epsilon", "zeta", "eta", "theta", "iota", "kappa"];
var parts = [];
for (var i = 0; i < 2000; i++) {
    parts.push(words[i % words.length]);
    if (i % 97 === 0) {
        parts.push("needle");
    }
}
parts.push("haystack-end");
var text = parts.join(" ");
var everywhere = /needle/g;
var atTheEnd = /haystack-end/;
var total = 0;
for (var r = 0; r < 200; r++) {
    var found = text.match(everywhere);
    total += found === null || typeof found === "object" ? 1 : 0;
    total += typeof atTheEnd.test(text) === "boolean" ? 1 : 0;
}
total;
