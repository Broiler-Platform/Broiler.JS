// The class-L probe of bundle sjl-9-001's rule as revised on 2026-11-09 (decision-rule-revised-2026-11-09.md,
// item 2): the String of the hanidec two-digit time the file's line 56 asks for, and of the same format with
// {second: "numeric"} alone, which its line 57 asks to include "六" and which line 56's failure masks; each with
// its code points, and whether each answers as class L requires.
console.log((function () {
  var date = new Date(2024, 0, 1, 2, 35, 6, 789);
  var two = new Intl.DateTimeFormat("en-US-u-nu-hanidec", { hour: "2-digit", minute: "2-digit", second: "2-digit" }).format(date);
  var one = new Intl.DateTimeFormat("en-US-u-nu-hanidec", { second: "numeric" }).format(date);
  function points(s) {
    var out = [];
    for (var i = 0; i < s.length; i++) {
      out.push("U+" + ("000" + s.charCodeAt(i).toString(16).toUpperCase()).slice(-4));
    }
    return out.join(" ");
  }
  var expected = "\u3007\u4e8c:\u4e09\u4e94:\u3007\u516d\u202fAM";
  return [
    "2-digit: " + JSON.stringify(two),
    "2-digit code points: " + points(two),
    "2-digit is exactly \"\u3007\u4e8c:\u4e09\u4e94:\u3007\u516d\" U+202F \"AM\": " + (two === expected),
    "2-digit with U+0020 for its U+202F, the file's expected String: " + JSON.stringify(two.replace("\u202f", " ")),
    "second numeric: " + JSON.stringify(one),
    "second numeric code points: " + points(one),
    "second numeric includes \"\u516d\": " + (one.indexOf("\u516d") !== -1),
    "resolved: " + JSON.stringify(new Intl.DateTimeFormat("en-US-u-nu-hanidec", { hour: "2-digit", minute: "2-digit", second: "2-digit" }).resolvedOptions()),
    "Intl.DateTimeFormat().resolvedOptions().timeZone: " + new Intl.DateTimeFormat().resolvedOptions().timeZone,
    "new Date(2024, 0, 1, 2, 35, 6, 789).toISOString(): " + date.toISOString()
  ].join("\n");
})());
