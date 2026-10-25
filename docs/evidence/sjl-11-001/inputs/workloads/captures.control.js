// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// CAPTURES (CONTROL) - the workload in captures.js with each exec call replaced by a type test of its
// input. The same lines, patterns, loops, lastIndex writes and accumulation; the difference between the
// two is the cost of the calls.

var samples = [];
for (var i = 0; i < 40; i++) {
    var month = 1 + (i % 12);
    var day = 1 + (i % 28);
    samples.push("entry " + i + " dated " + (1990 + i) + "-" + (month < 10 ? "0" + month : month) + "-"
        + (day < 10 ? "0" + day : day) + " at 12:" + (10 + i) + ", ref #" + (1000 + i * 7));
}
var stamp = /(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2}) at (?<hour>\d{2}):(?<minute>\d{2})/d;
var reference = /#(?<ref>\d+)/dg;
var total = 0;
for (var r = 0; r < 300; r++) {
    for (var j = 0; j < samples.length; j++) {
        var first = samples[j];
        total += first === null || typeof first === "string" ? 1 : 0;
        reference.lastIndex = 0;
        var second = samples[j];
        total += second === null || typeof second === "string" ? 1 : 0;
    }
}
total;
