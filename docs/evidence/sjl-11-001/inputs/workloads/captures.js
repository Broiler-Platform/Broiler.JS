// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// CAPTURES - RegExp.prototype.exec with named groups and the d flag: a pattern of five named groups
// over each line of a small log, and a global pattern with one named group, run again from the start of
// each line, so that every call builds its groups object and its indices.
//
// The answer counts the calls that completed, not what they found: SJL-11 corrects results in both
// engines, and a measurement whose answer changed with a correction would refuse the very change it
// measures, as SJL-3's did. The control beside this file is the same loops with each call replaced by a
// type test of its input. README.md beside this file says which rule judges it.

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
        var first = stamp.exec(samples[j]);
        total += first === null || first.indices !== undefined ? 1 : 0;
        reference.lastIndex = 0;
        var second = reference.exec(samples[j]);
        total += second === null || second.groups !== undefined ? 1 : 0;
    }
}
total;
