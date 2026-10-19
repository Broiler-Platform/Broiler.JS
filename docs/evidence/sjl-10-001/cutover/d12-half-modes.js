// SJL-10.5: decisions D11's and D12's operations in the four rounding modes d12-ext.js and d12z.js leave out (halfCeil,
// halfFloor, halfTrunc and halfEven), with the lines forward from b as controls. Run in this engine's shell
// (--script-host) at the base 1c411b68 and after 6dbff805, the reference polyfill and Broiler.VM; TZ=UTC, no LANG.
var out = [];
function t(label, f) { var r; try { r = String(f()); } catch (e) { r = (e && e.constructor && e.constructor.name) + ": " + String(e && e.message).split("\n")[0]; } out.push(label + " => " + r); }
var a = Temporal.ZonedDateTime.from("2011-12-31T16:15+14:00[Pacific/Apia]");
var b = Temporal.ZonedDateTime.from("2011-12-31T00:00+14:00[Pacific/Apia]");
var d = Temporal.Duration.from("-PT16H15M");
var modes = ["halfCeil", "halfFloor", "halfTrunc", "halfEven"];
modes.forEach(function (m) {
  // Decision D11: the day window back from a has no length.
  t("D11 a.since(b) day/day " + m, function () { return a.since(b, {largestUnit: "day", smallestUnit: "day", roundingMode: m}); });
  t("D11 a.until(b) day/day " + m, function () { return a.until(b, {largestUnit: "day", smallestUnit: "day", roundingMode: m}); });
  t("D11 a.since(b) month/day " + m, function () { return a.since(b, {largestUnit: "month", smallestUnit: "day", roundingMode: m}); });
  t("D11 -PT16H15M round day relativeTo a " + m, function () { return d.round({smallestUnit: "day", roundingMode: m, relativeTo: a}); });
  // Decision D12: NudgeToZonedTime's day span back from a is zero.
  t("D12 a.since(b) day/hour " + m, function () { return a.since(b, {largestUnit: "day", smallestUnit: "hour", roundingMode: m}); });
  t("D12 a.until(b) day/hour " + m, function () { return a.until(b, {largestUnit: "day", smallestUnit: "hour", roundingMode: m}); });
  t("D12 -PT16H15M round largest day smallest hour " + m, function () { return d.round({largestUnit: "day", smallestUnit: "hour", roundingMode: m, relativeTo: a}); });
  // Controls: forward from b.
  t("b.until(a) day/day " + m, function () { return b.until(a, {largestUnit: "day", smallestUnit: "day", roundingMode: m}); });
  t("b.until(a) day/hour " + m, function () { return b.until(a, {largestUnit: "day", smallestUnit: "hour", roundingMode: m}); });
});
print(out.join("\n"));
