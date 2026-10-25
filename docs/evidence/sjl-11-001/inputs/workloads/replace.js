// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// REPLACE - String.prototype.replace and replaceAll with a global pattern: a template with numbered
// references, a template with named references, and a replacement function, over lines of addresses.
//
// The answer counts the replacements that completed, not their results: SJL-11 corrects results in both
// engines, and a measurement whose answer changed with a correction would refuse the very change it
// measures, as SJL-3's did. The control beside this file is the same loops with each replacement
// replaced by a type test of its input. README.md beside this file says which rule judges it.

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
        total += typeof texts[j].replace(numbered, "$2 at $1") === "string" ? 1 : 0;
        total += typeof texts[j].replaceAll(named, "$<site> at $<user>") === "string" ? 1 : 0;
        total += typeof texts[j].replace(numbered, swap) === "string" ? 1 : 0;
    }
}
total;
