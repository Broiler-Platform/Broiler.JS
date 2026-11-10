(function () {
  var checks = [
    ["T12 hanidec two-digit time, U+202F", function () { return new Intl.DateTimeFormat("en-US-u-nu-hanidec", { hour: "2-digit", minute: "2-digit", second: "2-digit", timeZone: "UTC" }).format(Date.UTC(2024, 0, 1, 2, 35, 6)) === "\u3007\u4e8c:\u4e09\u4e94:\u3007\u516d\u202fAM"; }],
    ["L19 fr-CA hour cycles", function () { return new Intl.Locale("fr-CA").getHourCycles().join(",") === "h23,h12"; }],
    ["DisplayNames.supportedLocalesOf (D25)", function () { return typeof Intl.DisplayNames.supportedLocalesOf === "function"; }],
    ["T16 fa's default calendar", function () { return new Intl.DateTimeFormat("fa").resolvedOptions().calendar === "persian"; }],
    ["N/plural from the shared tables: de ordinal of 2", function () { return new Intl.PluralRules("de", { type: "ordinal" }).select(2) === "other"; }],
    ["L44 the default locale is available", function () { var d = new Intl.NumberFormat().resolvedOptions().locale; return Intl.NumberFormat.supportedLocalesOf([d]).length === 1; }]
  ];
  var failed = [];
  for (var i = 0; i < checks.length; i++) { var r; try { r = checks[i][1](); } catch (e) { r = e.name + ": " + e.message; } if (r !== true) failed.push(checks[i][0] + " (" + r + ")"); }
  return failed.length === 0 ? "ok " + checks.length : "failed " + failed.join("; ");
})()
