var out = [];
function t(label, f) { var r; try { r = String(f()); } catch (e) { r = (e && e.constructor && e.constructor.name) + ": " + String(e && e.message).split("\n")[0]; } out.push(label + " => " + r); }
var a = Temporal.ZonedDateTime.from("2011-12-31T16:15+14:00[Pacific/Apia]");
var b = Temporal.ZonedDateTime.from("2011-12-31T00:00+14:00[Pacific/Apia]");
var modes = ["trunc", "floor", "ceil", "expand", "halfExpand"];
var d = Temporal.Duration.from("-PT16H15M");
// Decision D11's case, for context: the day window back from 2011-12-31 (NudgeToCalendarUnit).
t("D11 a.since(b) day/day halfExpand", function () { return a.since(b, {largestUnit: "day", smallestUnit: "day"}); });
// Decision D12's case: NudgeToZonedTime back from 2011-12-31, its day span zero.
modes.forEach(function (m) {
  t("a.since(b) day/hour " + m, function () { return a.since(b, {largestUnit: "day", smallestUnit: "hour", roundingMode: m}); });
  t("a.until(b) day/hour " + m, function () { return a.until(b, {largestUnit: "day", smallestUnit: "hour", roundingMode: m}); });
  t("a.since(b) month/hour " + m, function () { return a.since(b, {largestUnit: "month", smallestUnit: "hour", roundingMode: m}); });
  t("a.since(b) day/minute x30 " + m, function () { return a.since(b, {largestUnit: "day", smallestUnit: "minute", roundingIncrement: 30, roundingMode: m}); });
  t("-PT16H15M round largest day smallest hour " + m, function () { return d.round({largestUnit: "day", smallestUnit: "hour", roundingMode: m, relativeTo: a}); });
  t("-PT16H15M round largest month smallest hour " + m, function () { return d.round({largestUnit: "month", smallestUnit: "hour", roundingMode: m, relativeTo: a}); });
  t("-PT16H15M round largest day smallest minute x30 " + m, function () { return d.round({largestUnit: "day", smallestUnit: "minute", roundingIncrement: 30, roundingMode: m, relativeTo: a}); });
  // Forward from the earlier instant, the day span is the next day, of 24 hours: unchanged.
  t("b.until(a) day/hour " + m, function () { return b.until(a, {largestUnit: "day", smallestUnit: "hour", roundingMode: m}); });
});
t("a.since(b) day/nanosecond x2", function () { return a.since(b, {largestUnit: "day", smallestUnit: "nanosecond", roundingIncrement: 2}); });
// No nudge: the smallest unit a nanosecond with increment 1, the largest a time unit, or a total in a time unit.
t("a.since(b) day (no smallest)", function () { return a.since(b, {largestUnit: "day"}); });
t("a.since(b) hour", function () { return a.since(b, {largestUnit: "hour"}); });
t("-PT16H15M round largest day (no smallest)", function () { return d.round({largestUnit: "day", relativeTo: a}); });
t("-PT16H15M total hour", function () { return d.total({unit: "hour", relativeTo: a}); });
print(out.join("\n"));
