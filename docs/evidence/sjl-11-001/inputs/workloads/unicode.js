// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// UNICODE - property classes under u and v, and case-insensitive matching: General_Category and Script
// properties under u, a class difference, a class intersection and a property of strings under v, and
// case-insensitive literals under iu, iv and i, over short texts in Latin with diacritics, Greek,
// Cyrillic, Han, ASCII and emoji. Every character outside ASCII is written as an escape.
//
// The answer counts the searches that completed, not what they found: SJL-11 corrects results in both
// engines, and a measurement whose answer changed with a correction would refuse the very change it
// measures, as SJL-3's did. The control beside this file is the same loops with each search replaced by
// a type test of its input. README.md beside this file says which rule judges it.

var samples = [
    "\u00e9t\u00e9 caf\u00e9 cr\u00e8me br\u00fbl\u00e9e",
    "\u0391\u03b8\u03ae\u03bd\u03b1 \u03ba\u03b1\u03b9 \u03a3\u03bf\u03c6\u03af\u03b1",
    "\u041c\u043e\u0441\u043a\u0432\u0430 \u0438 \u041f\u0435\u0442\u0435\u0440\u0431\u0443\u0440\u0433",
    "\u6771\u4eac \u5927\u962a 2026",
    "plain ascii text 0123",
    "emoji \ud83d\ude00 \ud83d\udc4d\ud83c\udffd and a flag \ud83c\uddeb\ud83c\uddf7",
    "Stra\u00dfe STRASSE strasse",
    "\u03c3\u03bf\u03c6\u03af\u03b1 \u03a3\u039f\u03a6\u0399\u0391 \u03c3\u03bf\u03c6\u03b9\u03b1"
];
var patterns = [
    /\p{L}+/gu,
    /\p{Lu}\p{Ll}*/gu,
    /[\p{Script=Greek}\p{Script=Cyrillic}]+/gu,
    /\P{L}+/gu,
    /[\p{L}--\p{Lu}]+/gv,
    /[\p{Nd}&&\p{ASCII}]+/gv,
    /\p{RGI_Emoji}/gv,
    /\u043c\u043e\u0441\u043a\u0432\u0430/giu,
    /[\u03b1-\u03c9]+/giv,
    /caf\u00e9|stra\u00dfe/gi
];
var total = 0;
for (var r = 0; r < 150; r++) {
    for (var j = 0; j < samples.length; j++) {
        for (var p = 0; p < patterns.length; p++) {
            var found = samples[j].match(patterns[p]);
            total += found === null || typeof found === "object" ? 1 : 0;
        }
    }
}
total;
