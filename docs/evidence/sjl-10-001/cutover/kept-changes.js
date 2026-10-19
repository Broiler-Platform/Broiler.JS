// SJL-10.5: the behaviour changes the cut-over keeps beyond the correspondence's rows (Broiler.JS/CHANGELOG.md,
// "Corrections the correspondence did not find"). One line per case: "<id> <label> => <answer>[ | <reads>]". Run in
// this engine's shell (--script-host) at the base 1c411b68 and after 6dbff805, the reference polyfill and Broiler.VM;
// TZ=UTC, no LANG.
var out = [];
var LOG = [];
function show(r) { return r === undefined ? "undefined" : String(r); }
function t(label, f) {
  var r; LOG = [];
  try { r = show(f()); } catch (e) { r = "THROWS " + (e && e.constructor && e.constructor.name); }
  out.push(label + " => " + r + (LOG.length ? " | " + LOG.join(", ") : ""));
}
// An object whose own data properties are read through getters, each Get logged.
function bag(name, fields) {
  var o = {};
  Object.keys(fields).forEach(function (k) {
    Object.defineProperty(o, k, { get: function () { LOG.push("get " + name + "." + k); return fields[k]; }, enumerable: true });
  });
  return o;
}
// A value whose toString and valueOf are logged.
function prim(name, v) {
  return {
    toString: function () { LOG.push("toString " + name); return String(v); },
    valueOf: function () { LOG.push("valueOf " + name); return v; }
  };
}
var T = Temporal, D = T.Duration;

// Part A
t("A1 P1M.round(day) relativeTo {2020-01-01, offset +25:00, timeZone 5}", function () { return D.from("P1M").round({ smallestUnit: "day", relativeTo: { year: 2020, month: 1, day: 1, offset: "+25:00", timeZone: 5 } }); });
t("A1 P1M.round(day) relativeTo {2020-01-01, offset +25:00, timeZone UTC}", function () { return D.from("P1M").round({ smallestUnit: "day", relativeTo: { year: 2020, month: 1, day: 1, offset: "+25:00", timeZone: "UTC" } }); });
t("A2 ZDT.toString({offset: bad}) and its conversions", function () { var z = T.ZonedDateTime.from("2020-01-01T00:00+00:00[UTC]"); try { return z.toString({ offset: prim("offset", "bad") }); } finally { } });

// Part B
t("B1 PlainDate.from(hebrew bag, options) reads", function () { return T.PlainDate.from(bag("b", { calendar: "hebrew", year: 5780, month: 5, day: 1 }), bag("o", { overflow: "constrain" })); });
t("B1 PlainDate(gregory).with(bag) reads", function () { return T.PlainDate.from({ calendar: "gregory", year: 2020, month: 1, day: 1 }).with(bag("b", { era: "ce", eraYear: 2021, year: 2021 })); });
t("B1 PlainMonthDay(gregory).toPlainDate(bag) reads", function () { return T.PlainMonthDay.from({ calendar: "gregory", monthCode: "M02", day: 1 }).toPlainDate(bag("b", { era: "ce", eraYear: 2021, year: 2021 })); });
t("B2 PlainDate.from({year 2020, month 1}, {overflow: bad})", function () { return T.PlainDate.from({ year: 2020, month: 1 }, { overflow: "bad" }); });
t("B3 PlainYearMonth(2020-01).toPlainDate({day: 0})", function () { return T.PlainYearMonth.from("2020-01").toPlainDate({ day: 0 }); });
t("B3 PlainMonthDay.from({month: 0, day: 1}, options) reads", function () { return T.PlainMonthDay.from(bag("b", { day: 1, month: 0, monthCode: "M01", year: 2020 }), bag("o", { overflow: "constrain" })); });
t("B4 new PlainMonthDay(2, 1, iso8601, 1900).with({day: 30})", function () { return new T.PlainMonthDay(2, 1, "iso8601", 1900).with({ day: 30 }); });
t("B5 PlainMonthDay.from({roc, year 109, month 2, day 29}, reject)", function () { return T.PlainMonthDay.from({ calendar: "roc", year: 109, month: 2, day: 29 }, { overflow: "reject" }).toString({ calendarName: "always" }); });
t("B5 PlainMonthDay.from({buddhist, year 2563, month 2, day 29}, reject)", function () { return T.PlainMonthDay.from({ calendar: "buddhist", year: 2563, month: 2, day: 29 }, { overflow: "reject" }).toString({ calendarName: "always" }); });
t("B6 new PlainDate(-271821, 4, 19, coptic).toPlainYearMonth()", function () { return new T.PlainDate(-271821, 4, 19, "coptic").toPlainYearMonth(); });
t("B6 new PlainYearMonth(-271821, 4, coptic, 19).add(P1M)", function () { return new T.PlainYearMonth(-271821, 4, "coptic", 19).add("P1M"); });
t("B6 new PlainYearMonth(-271821, 4, coptic, 19).until(new PlainYearMonth(-271821, 6, coptic, 1))", function () { return new T.PlainYearMonth(-271821, 4, "coptic", 19).until(new T.PlainYearMonth(-271821, 6, "coptic", 1)); });
t("B7 PlainDate(gregory 2020-01-01).with({year 2021, era ce, eraYear 2019})", function () { return T.PlainDate.from({ calendar: "gregory", year: 2020, month: 1, day: 1 }).with({ year: 2021, era: "ce", eraYear: 2019 }); });

// Part C
t("C1 P1M.round(day) relativeTo {calendar gregorian, 2020-01-01} reads", function () { return D.from("P1M").round({ smallestUnit: "day", relativeTo: bag("r", { calendar: "gregorian", year: 2020, month: 1, day: 1 }) }); });
t("C1 P1M.round(day) relativeTo {calendar hebrew, 5780-05-01, timeZone UTC}", function () { return D.from("P1M").round({ smallestUnit: "day", relativeTo: { calendar: "hebrew", year: 5780, month: 5, day: 1, timeZone: "UTC" } }); });
t("C2 P1M.round(day) relativeTo {2020-01-01, offset garbage}", function () { return D.from("P1M").round({ smallestUnit: "day", relativeTo: { year: 2020, month: 1, day: 1, offset: "garbage" } }); });
t("C2 P1M.total(day) relativeTo {2020-01-01, offset garbage}", function () { return D.from("P1M").total({ unit: "day", relativeTo: { year: 2020, month: 1, day: 1, offset: "garbage" } }); });
t("C2 compare(P1M, P31D) relativeTo {2020-01-01, offset garbage}", function () { return D.compare("P1M", "P31D", { relativeTo: { year: 2020, month: 1, day: 1, offset: "garbage" } }); });
t("C3 P1M.round(day) relativeTo {year Infinity, 1, 1, timeZone 5}", function () { return D.from("P1M").round({ smallestUnit: "day", relativeTo: { year: Infinity, month: 1, day: 1, timeZone: 5 } }); });
t("C4 P1M.round(day) relativeTo {2020, monthCode X1, day 1, timeZone 5}", function () { return D.from("P1M").round({ smallestUnit: "day", relativeTo: { year: 2020, monthCode: "X1", day: 1, timeZone: 5 } }); });
t("C4 P1M.round(day) relativeTo {2020, 1, day 0, timeZone 5}", function () { return D.from("P1M").round({ smallestUnit: "day", relativeTo: { year: 2020, month: 1, day: 0, timeZone: 5 } }); });
t("C5 PT1H.subtract(PT1H, 5)", function () { return D.from("PT1H").subtract("PT1H", 5); });
t("C5 PT1H.add(PT1H, bag) reads", function () { return D.from("PT1H").add("PT1H", bag("o", { relativeTo: "2020-01-01" })); });
t("C6 new PYM(-271821, 4, iso8601, 19).until(new PYM(-271821, 4, iso8601, 20))", function () { return new T.PlainYearMonth(-271821, 4, "iso8601", 19).until(new T.PlainYearMonth(-271821, 4, "iso8601", 20)); });
t("C6 new PYM(275760, 9, iso8601, 1).until(new PYM(275760, 9, iso8601, 13), year)", function () { return new T.PlainYearMonth(275760, 9, "iso8601", 1).until(new T.PlainYearMonth(275760, 9, "iso8601", 13), { smallestUnit: "year" }); });
t("C7 P1D.total(hours) relativeTo {-271821-04-19T12:00, offset -12:00, timeZone -12:00}", function () { return D.from("P1D").total({ unit: "hours", relativeTo: { year: -271821, month: 4, day: 19, hour: 12, offset: "-12:00", timeZone: "-12:00" } }); });
t("C7 P1D.total(hours) relativeTo {-271821-04-19T12:00, timeZone -12:00} (no offset)", function () { return D.from("P1D").total({ unit: "hours", relativeTo: { year: -271821, month: 4, day: 19, hour: 12, timeZone: "-12:00" } }); });

// Found by part D: the rounding of points (SJL-4.5's "Not corrected here")
t("P1 PlainTime 01:10 round {minute, 20, halfEven}", function () { return T.PlainTime.from("01:10").round({ smallestUnit: "minute", roundingIncrement: 20, roundingMode: "halfEven" }); });
t("P1 PlainDateTime 2020-01-01T01:10 round {minute, 20, halfEven}", function () { return T.PlainDateTime.from("2020-01-01T01:10").round({ smallestUnit: "minute", roundingIncrement: 20, roundingMode: "halfEven" }); });
t("P1 ZonedDateTime 2020-01-01T01:10[UTC] round {minute, 20, halfEven}", function () { return T.ZonedDateTime.from("2020-01-01T01:10+00:00[UTC]").round({ smallestUnit: "minute", roundingIncrement: 20, roundingMode: "halfEven" }); });
t("P1 PlainTime 00:01:10 round {second, 20, halfEven}", function () { return T.PlainTime.from("00:01:10").round({ smallestUnit: "second", roundingIncrement: 20, roundingMode: "halfEven" }); });
t("P1 PlainTime 01:10 round {minute, 20, halfExpand} (control)", function () { return T.PlainTime.from("01:10").round({ smallestUnit: "minute", roundingIncrement: 20, roundingMode: "halfExpand" }); });

// The review's U1 to U8
t("U1 PT100H.round({week, increment 1e9, relativeTo 2020-01-01})", function () { return D.from("PT100H").round({ smallestUnit: "week", roundingIncrement: 1e9, relativeTo: "2020-01-01" }); });
t("U1 P1W.round({day, relativeTo +275760-09-01})", function () { return D.from("P1W").round({ smallestUnit: "day", relativeTo: "+275760-09-01" }); });
t("U1 P1W.total({week, relativeTo +275760-09-01})", function () { return D.from("P1W").total({ unit: "week", relativeTo: "+275760-09-01" }); });
t("U1 -P1W.round({day, relativeTo -271821-05-01})", function () { return D.from("-P1W").round({ smallestUnit: "day", relativeTo: "-271821-05-01" }); });
t("U2 PDT -271821-04-19T00:00:00.000000001 until +275760-09-13 {year, hour, ceil}", function () { return T.PlainDateTime.from("-271821-04-19T00:00:00.000000001").until("+275760-09-13T00:00", { largestUnit: "year", smallestUnit: "hour", roundingMode: "ceil" }); });
t("U2 PDT 2020-01-31T00:00:00.000000001 until 2020-02-29 {month, hour, ceil}", function () { return T.PlainDateTime.from("2020-01-31T00:00:00.000000001").until("2020-02-29T00:00", { largestUnit: "month", smallestUnit: "hour", roundingMode: "ceil" }); });
t("U3 ZDT +275760-09-12[UTC] until +275760-09-13[UTC] {day}", function () { return T.ZonedDateTime.from("+275760-09-12T00:00+00:00[UTC]").until("+275760-09-13T00:00+00:00[UTC]", { smallestUnit: "day" }); });
t("U3 ZDT -271821-04-21[UTC] since -271821-04-20[UTC] {day}", function () { return T.ZonedDateTime.from("-271821-04-21T00:00+00:00[UTC]").since("-271821-04-20T00:00+00:00[UTC]", { smallestUnit: "day" }); });
// An era whose toString and valueOf are accessors, each read and call logged: the review's form, prim("era", "ce").
function prim2(name, v) {
  return {
    get valueOf() { LOG.push("get " + name + ".valueOf"); return function () { LOG.push("call " + name + ".valueOf"); return v; }; },
    get toString() { LOG.push("get " + name + ".toString"); return function () { LOG.push("call " + name + ".toString"); return String(v); }; }
  };
}
function eraObject() { return prim2("era", "ce"); }
t("U4 PMD.from({gregory, era <object>, eraYear 2020, M02, 29})", function () { return T.PlainMonthDay.from({ calendar: "gregory", era: eraObject(), eraYear: 2020, monthCode: "M02", day: 29 }).toString({ calendarName: "always" }); });
t("U4 PMD(gregory M02-29).toPlainDate({era ce, eraYear 2021})", function () { return T.PlainMonthDay.from({ calendar: "gregory", monthCode: "M02", day: 29 }).toPlainDate({ era: "ce", eraYear: 2021 }); });
t("U4 P1M.total(day) relativeTo {gregory, era <object>, eraYear 2020, 1, 31}", function () { return D.from("P1M").total({ unit: "day", relativeTo: { calendar: "gregory", era: eraObject(), eraYear: 2020, month: 1, day: 31 } }); });
t("U5 PMD.from({gregory, monthCode M05, month 5, day 1})", function () { return T.PlainMonthDay.from({ calendar: "gregory", monthCode: "M05", month: 5, day: 1 }); });
t("U5 PMD(hebrew M05-01).with({monthCode M06, month 6})", function () { return T.PlainMonthDay.from({ calendar: "hebrew", monthCode: "M05", day: 1 }).with({ monthCode: "M06", month: 6 }); });
t("U6 PMD(gregory M02-01).with({era ce, eraYear 5})", function () { return T.PlainMonthDay.from({ calendar: "gregory", monthCode: "M02", day: 1 }).with({ era: "ce", eraYear: 5 }).toString({ calendarName: "always" }); });
t("U6 PMD.from({gregory, era xx, eraYear 1, M01, 1}, 5)", function () { return T.PlainMonthDay.from({ calendar: "gregory", era: "xx", eraYear: 1, monthCode: "M01", day: 1 }, 5); });
t("U7 PYM(hebrew 5780-M03).with({year 5781, era xx, eraYear 1})", function () { return T.PlainYearMonth.from({ calendar: "hebrew", year: 5780, monthCode: "M03" }).with({ year: 5781, era: "xx", eraYear: 1 }); });
t("U7 PYM(coptic 1736-M03).with({year 1737, era am, eraYear 1})", function () { return T.PlainYearMonth.from({ calendar: "coptic", year: 1736, monthCode: "M03" }).with({ year: 1737, era: "am", eraYear: 1 }); });
t("U8 PYM(chinese 2021-M03).with({monthCode M02L})", function () { var y = T.PlainYearMonth.from({ calendar: "chinese", year: 2021, monthCode: "M03" }).with({ monthCode: "M02L" }); return y.monthCode + " " + y.toString(); });
t("U8 PYM(hebrew 5783-M04).with({monthCode M05L})", function () { var y = T.PlainYearMonth.from({ calendar: "hebrew", year: 5783, monthCode: "M04" }).with({ monthCode: "M05L" }); return y.monthCode + " " + y.toString(); });

// The review's code-1 and code-2, and InterpretISODateTimeOffset's step 7 (DUP2)
var hz = T.ZonedDateTime.from("2020-01-15T10:00+00:00[UTC][u-ca=hebrew]");
t("K1 PlainDateTime.from(hebrew ZonedDateTime) reads", function () { return T.PlainDateTime.from(hz); });
t("K1 PlainYearMonth.from(hebrew PlainDate)", function () { return T.PlainYearMonth.from(T.PlainDate.from("2020-01-15[u-ca=hebrew]")); });
t("K1 PlainMonthDay.from(hebrew PlainDate)", function () { return T.PlainMonthDay.from(T.PlainDate.from("2020-01-15[u-ca=hebrew]")); });
t("K1 hebrew PlainDateTime.equals(hebrew ZonedDateTime of its wall clock)", function () { return T.PlainDateTime.from("2020-01-15T10:00[u-ca=hebrew]").equals(hz); });
t("K2 ZonedDateTime.from(PlainDate 2020-01-01)", function () { return T.ZonedDateTime.from(T.PlainDate.from("2020-01-01")); });
t("K2 ZonedDateTime.compare(ZDT, PlainDateTime)", function () { return T.ZonedDateTime.compare(hz, T.PlainDateTime.from("2020-01-01T00:00")); });
t("K2 ZonedDateTime.from(PlainDate with timeZone UTC)", function () { var p = T.PlainDate.from("2020-01-01"); Object.defineProperty(p, "timeZone", { value: "UTC" }); return T.ZonedDateTime.from(p); });
t("K3 ZonedDateTime.from({-271821-04-19T12:00, offset -12:00, timeZone -12:00})", function () { return T.ZonedDateTime.from({ year: -271821, month: 4, day: 19, hour: 12, offset: "-12:00", timeZone: "-12:00" }); });
t("K3 new ZonedDateTime(-8.64e21 ns, -12:00).with({minute 1})", function () { return new T.ZonedDateTime(-8640000000000000000000n, "-12:00").with({ minute: 1 }); });
t("K3 new ZonedDateTime(-8.64e21 ns, -12:00).round(hour)", function () { return new T.ZonedDateTime(-8640000000000000000000n, "-12:00").round("hour"); });

// Not changed by the cut-over, for the record: a string's limits are tested before the overflow option is read.
t("N1 PlainDate.from(-271821-04-18, bag) reads", function () { return T.PlainDate.from("-271821-04-18", bag("o", { overflow: "constrain" })); });
t("N1 PlainYearMonth.from(-271821-03, bag) reads", function () { return T.PlainYearMonth.from("-271821-03", bag("o", { overflow: "constrain" })); });

print(out.join("\n"));
