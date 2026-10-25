// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// SCAN (CONTROL) - the workload in scan.js with each search replaced by a type test of its input. The
// same text, patterns, loops and accumulation; the difference between the two is the cost of the
// searches.

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
    var found = text;
    total += found === null || typeof found === "string" ? 1 : 0;
    total += typeof text === "string" ? 1 : 0;
}
total;
