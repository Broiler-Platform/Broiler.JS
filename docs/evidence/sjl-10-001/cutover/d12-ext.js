var out = [];
function t(label, f) { var r; try { r = String(f()); } catch (e) { r = (e && e.constructor && e.constructor.name) + ": " + String(e && e.message).split("\n")[0]; } out.push(label + " => " + r); }
var a = Temporal.ZonedDateTime.from("2011-12-31T16:15+14:00[Pacific/Apia]");
var b = Temporal.ZonedDateTime.from("2011-12-31T00:00+14:00[Pacific/Apia]");
var c = Temporal.ZonedDateTime.from("2011-12-29T12:00-10:00[Pacific/Apia]");
var modes = ["trunc", "floor", "ceil", "expand", "halfExpand"];
// The brief's two lines
t("a.since(b) day/day", function () { return a.since(b, {largestUnit: "day", smallestUnit: "day"}); });
t("b.until(a) day/day", function () { return b.until(a, {largestUnit: "day", smallestUnit: "day"}); });
// JSD-0079 section 4's cases
t("a.since(b) hour", function () { return a.since(b, {largestUnit: "hour"}); });
t("Duration -PT16H15M round day relativeTo a", function () { return Temporal.Duration.from("-PT16H15M").round({smallestUnit: "day", relativeTo: a}); });
t("Duration -PT16H15M total day relativeTo a", function () { return Temporal.Duration.from("-PT16H15M").total({unit: "day", relativeTo: a}); });
t("b.until(a) day floor", function () { return b.until(a, {largestUnit: "day", smallestUnit: "day", roundingMode: "floor"}); });
// Every direction and mode, day and week windows
modes.forEach(function (m) {
  t("a.since(b) day " + m, function () { return a.since(b, {largestUnit: "day", smallestUnit: "day", roundingMode: m}); });
  t("a.until(b) day " + m, function () { return a.until(b, {largestUnit: "day", smallestUnit: "day", roundingMode: m}); });
  t("b.since(a) day " + m, function () { return b.since(a, {largestUnit: "day", smallestUnit: "day", roundingMode: m}); });
  t("b.until(a) day " + m, function () { return b.until(a, {largestUnit: "day", smallestUnit: "day", roundingMode: m}); });
  t("a.since(b) week " + m, function () { return a.since(b, {largestUnit: "week", smallestUnit: "week", roundingMode: m}); });
  t("a.since(b) month/day " + m, function () { return a.since(b, {largestUnit: "month", smallestUnit: "day", roundingMode: m}); });
  t("a.since(c) day " + m, function () { return a.since(c, {largestUnit: "day", smallestUnit: "day", roundingMode: m}); });
  t("c.until(a) day " + m, function () { return c.until(a, {largestUnit: "day", smallestUnit: "day", roundingMode: m}); });
  t("round -PT16H15M day " + m, function () { return Temporal.Duration.from("-PT16H15M").round({smallestUnit: "day", roundingMode: m, relativeTo: a}); });
  t("round PT16H15M day " + m + " rel b", function () { return Temporal.Duration.from("PT16H15M").round({smallestUnit: "day", roundingMode: m, relativeTo: b}); });
  t("round -PT16H15M week " + m, function () { return Temporal.Duration.from("-PT16H15M").round({smallestUnit: "week", roundingMode: m, relativeTo: a}); });
  t("round -PT16H15M largest day smallest hour " + m, function () { return Temporal.Duration.from("-PT16H15M").round({largestUnit: "day", smallestUnit: "hour", roundingMode: m, relativeTo: a}); });
});
modes.forEach(function (m) {
  t("a.since(b) day/hour " + m, function () { return a.since(b, {largestUnit: "day", smallestUnit: "hour", roundingMode: m}); });
  t("a.until(b) day/hour " + m, function () { return a.until(b, {largestUnit: "day", smallestUnit: "hour", roundingMode: m}); });
  t("b.until(a) day/hour " + m, function () { return b.until(a, {largestUnit: "day", smallestUnit: "hour", roundingMode: m}); });
  t("a.since(b) day/minute x30 " + m, function () { return a.since(b, {largestUnit: "day", smallestUnit: "minute", roundingIncrement: 30, roundingMode: m}); });
  t("round -PT16H15M largest day smallest minute x30 " + m, function () { return Temporal.Duration.from("-PT16H15M").round({largestUnit: "day", smallestUnit: "minute", roundingIncrement: 30, roundingMode: m, relativeTo: a}); });
  t("round -PT16H15M largest month smallest hour " + m, function () { return Temporal.Duration.from("-PT16H15M").round({largestUnit: "month", smallestUnit: "hour", roundingMode: m, relativeTo: a}); });
});
t("round -PT16H15M largest day (no smallest)", function () { return Temporal.Duration.from("-PT16H15M").round({largestUnit: "day", relativeTo: a}); });
t("a.subtract P1D", function () { return a.subtract("P1D"); });
t("b.subtract P1D", function () { return b.subtract("P1D"); });
t("total -PT16H15M week", function () { return Temporal.Duration.from("-PT16H15M").total({unit: "week", relativeTo: a}); });
t("total -PT16H15M hour", function () { return Temporal.Duration.from("-PT16H15M").total({unit: "hour", relativeTo: a}); });
t("total PT16H15M day rel b", function () { return Temporal.Duration.from("PT16H15M").total({unit: "day", relativeTo: b}); });
t("compare -PT16H15M -P1D rel a", function () { return Temporal.Duration.compare("-PT16H15M", "-P1D", {relativeTo: a}); });
t("a.round day", function () { return a.round("day"); });
t("a.hoursInDay", function () { return a.hoursInDay; });
t("c.hoursInDay", function () { return c.hoursInDay; });
print(out.join("\n"));
