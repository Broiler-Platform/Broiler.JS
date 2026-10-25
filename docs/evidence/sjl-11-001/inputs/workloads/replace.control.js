// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// REPLACE (CONTROL) - the workload in replace.js with each replacement replaced by a type test of its
// input. The same lines, patterns, function, loops and accumulation; the difference between the two is
// the cost of the replacements.

var texts = [];
for (var i = 0; i < 6; i++) {
    var line = "";
    for (var k = 0; k < 12; k++) {
        line += "user" + i + "x" + k + "@host" + k + ".com, ";
    }
    texts.push(line);
}
var numbered = /(\w+)@(\w+)\.com/g;
var named = /(?<user>\w+)@(?<site>\w+)\.com/g;
function swap(whole, user, site) {
    return site + " at " + user;
}
var total = 0;
for (var r = 0; r < 400; r++) {
    for (var j = 0; j < texts.length; j++) {
        total += typeof texts[j] === "string" ? 1 : 0;
        total += typeof texts[j] === "string" ? 1 : 0;
        total += typeof texts[j] === "string" ? 1 : 0;
    }
}
total;
