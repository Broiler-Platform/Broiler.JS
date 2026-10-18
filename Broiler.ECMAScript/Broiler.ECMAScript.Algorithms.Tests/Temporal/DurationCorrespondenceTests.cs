using System;
using System.Collections.Generic;
using System.Numerics;
using Broiler.ECMAScript.Runners;
using Broiler.ECMAScript.Temporal;
using Xunit;
using static Broiler.ECMAScript.Algorithms.Tests.DurationTestSupport;

namespace Broiler.ECMAScript.Algorithms.Tests;

/// <summary>
/// The duration rows D1 to D11 and difference rows F1 to F8 of correspondence <c>sjl-10-001</c> at library level
/// (milestone SJL-10.3, part C), with rows R7 and B5 where they add a duration: every line of Broiler.VM's recorded
/// answers to the rows' probes is computed through the library, by the runner's Temporal adapter as an engine calls it,
/// and held to Broiler.VM's answer, which the correspondence records as the specification's on these rows (the pinned
/// text's on row D8, decision D7). A refusal is compared by its constructor alone; messages stay each engine's.
/// </summary>
/// <remarks>
/// Each probe's JavaScript is <c>correspondence-runs/core/probes/&lt;name&gt;.js</c> in Broiler.VM's bundle; each line
/// below is that call. A line only an engine answers is named in the row's list and counted.
/// </remarks>
public class DurationCorrespondenceTests
{
    private const string NySpring = "2026-03-08T00:00-05:00[America/New_York]";
    private const string NyFall = "2026-11-01T00:00-04:00[America/New_York]";

    private static TemporalAdapter A => Adapter;

    private static TemporalAdapter.Duration D(string text) => A.ToDuration(text);

    private static TemporalAdapter.Duration D(params (string Key, object? Value)[] fields) => A.ToDuration(Bag(fields));

    private static TemporalAdapter.PlainDate PD(string text) => A.ToPlainDate(text);

    private static TemporalAdapter.PlainDateTime PDT(string text) => A.ToPlainDateTime(text);

    private static TemporalAdapter.PlainTime PT(string text) => A.ToPlainTime(text);

    private static TemporalAdapter.PlainYearMonth PYM(string text) => A.ToPlainYearMonth(text);

    private static TemporalAdapter.Instant I(string text) => A.ToInstant(text);

    private static TemporalAdapter.ZonedDateTime Z(string text) => A.ToZonedDateTime(text);

    private static TemporalAdapter.Bag O(params (string Key, object? Value)[] options) => Bag(options);

    private static TemporalAdapter.Duration Round(TemporalAdapter.Duration duration, params (string Key, object? Value)[] options) => A.Round(duration, Bag(options));

    private static double Total(TemporalAdapter.Duration duration, params (string Key, object? Value)[] options) => A.Total(duration, Bag(options));

    private const DifferenceOperation Until = DifferenceOperation.Until;
    private const DifferenceOperation Since = DifferenceOperation.Since;

    private static readonly double MaxSafe = 9007199254740991;

    // ---- D1 to D4 -------------------------------------------------------------------------------------------------

    [Fact]
    public void D1_sign_blank_negation_and_abs_answer_as_Broiler_VM()
    {
        Replay("d1-sign-and-shape.vm.txt", "5578f6d455af9cc7724c4b6eecdecba645697b7922d413dc8f8d1bb8f12ec7e9", new Dictionary<string, Func<object?>>
        {
            ["new Duration(0,0,0,0,0,0,0,0,0,-1).sign"] = () => (double)DurationMath.Sign(TemporalAdapter.NewDuration(0, 0, 0, 0, 0, 0, 0, 0, 0, -1).Fields),
            ["new Duration().sign"] = () => (double)DurationMath.Sign(TemporalAdapter.NewDuration().Fields),
            ["new Duration().blank"] = () => DurationMath.Sign(TemporalAdapter.NewDuration().Fields) == 0,
            ["Duration(\"P1D\").negated()"] = () => new TemporalAdapter.Duration(DurationMath.Negate(D("P1D").Fields)),
            ["Object.is(Duration(\"P1D\").negated().hours, 0)"] = () => IsPositiveZero(DurationMath.Negate(D("P1D").Fields).Hours),
            ["Object.is(new Duration().negated().years, 0)"] = () => IsPositiveZero(DurationMath.Negate(TemporalAdapter.NewDuration().Fields).Years),
            ["Duration(\"-P1DT1H\").abs()"] = () => TemporalAdapter.CreateDuration(DurationMath.Abs(D("-P1DT1H").Fields)),
            ["Duration(\"P1D\").with({hours: -1})"] = () => TemporalAdapter.With(D("P1D"), Bag(("hours", -1))),
            ["Duration(\"P1D\").with({days: -1}).sign"] = () => (double)DurationMath.Sign(TemporalAdapter.With(D("P1D"), Bag(("days", -1))).Fields),
            ["Duration(\"P1D\").with({})"] = () => TemporalAdapter.With(D("P1D"), Bag()),
        });
    }

    private static bool IsPositiveZero(double value) => value == 0 && !double.IsNegative(value);

    [Fact]
    public void D2_default_largest_unit_and_balance_answer_as_Broiler_VM()
    {
        var p53 = 9007199254740991.0;
        Replay("d2-largest-unit-and-balance.vm.txt", "67faa656eb9df7b16e4d8ee2ae40b2d8b8d8b4539ef78541a6c5cb59051da33c", new Dictionary<string, Func<object?>>
        {
            ["Duration({minutes: 30}).add({minutes: 90})"] = () => A.AddDurations(D(("minutes", 30)), Bag(("minutes", 90)), false),
            ["Duration({hours: 1}).add({minutes: 90})"] = () => A.AddDurations(D(("hours", 1)), Bag(("minutes", 90)), false),
            ["Duration({days: 1}).add({hours: 24})"] = () => A.AddDurations(D(("days", 1)), Bag(("hours", 24)), false),
            ["Duration({days: 1}).add({hours: 25})"] = () => A.AddDurations(D(("days", 1)), Bag(("hours", 25)), false),
            ["Duration({seconds: 59}).add({milliseconds: 1001})"] = () => A.AddDurations(D(("seconds", 59)), Bag(("milliseconds", 1001)), false),
            ["Duration({microseconds: 1}).add({nanoseconds: 2000})"] = () => A.AddDurations(D(("microseconds", 1)), Bag(("nanoseconds", 2000)), false),
            ["Duration({hours: 1}).subtract({hours: 2})"] = () => A.AddDurations(D(("hours", 1)), Bag(("hours", 2)), true),
            ["Duration({days: 1, hours: -0}).subtract({days: 1})"] = () => A.AddDurations(D(("days", 1)), Bag(("days", 1)), true),
            ["Duration({seconds: 2**53 - 1}).add({nanoseconds: 999999999})"] = () => A.AddDurations(D(("seconds", p53)), Bag(("nanoseconds", 999999999)), false),
            ["Duration({nanoseconds: 2**53 - 1}).add({nanoseconds: 2})"] = () => A.AddDurations(D(("nanoseconds", p53)), Bag(("nanoseconds", 2)), false).Fields.Nanoseconds,
            ["Duration({nanoseconds: 2**53 - 1}).add({nanoseconds: 4})"] = () => A.AddDurations(D(("nanoseconds", p53)), Bag(("nanoseconds", 4)), false).Fields.Nanoseconds,
            ["Duration({milliseconds: 2**53 - 1}).add({milliseconds: 2**53 - 1}).milliseconds"] = () =>
                A.AddDurations(D(("milliseconds", p53)), Bag(("milliseconds", p53)), false).Fields.Milliseconds,
            ["Duration({seconds: 2**53 - 1}).add({seconds: 1})"] = () => A.AddDurations(D(("seconds", p53)), Bag(("seconds", 1)), false),
            ["Duration({days: 104249991374}).add({days: 1})"] = () => A.AddDurations(D(("days", 104249991374.0)), Bag(("days", 1)), false),
            ["Duration({days: 104249991374}).add({hours: 1})"] = () => A.AddDurations(D(("days", 104249991374.0)), Bag(("hours", 1)), false),
        });
    }

    [Fact]
    public void D3_add_refuses_calendar_units_whatever_the_second_argument_as_Broiler_VM_answers()
    {
        // THE PINNED REVISION'S add TAKES ONE PARAMETER: the adapter, as Broiler.VM, never reads a second argument, so
        // the probe's getter logs no read.
        Replay("d3-add-calendar-units.vm.txt", "e386f476e1e032a9d56e5064ae96e1b8e6941af15276f0554b29f81ea3984b2a", new Dictionary<string, Func<object?>>
        {
            ["Duration({years: 1}).add({months: 1})"] = () => A.AddDurations(D(("years", 1)), Bag(("months", 1)), false),
            ["Duration({years: 1}).add({months: 1}, {relativeTo: \"2020-01-01\"})"] = () => A.AddDurations(D(("years", 1)), Bag(("months", 1)), false),
            ["Duration({years: 1}).subtract({months: 1}, {relativeTo: \"2020-01-01\"})"] = () => A.AddDurations(D(("years", 1)), Bag(("months", 1)), true),
            ["Duration({years: 1}).add({months: 1}, {relativeTo: ZonedDateTime})"] = () => A.AddDurations(D(("years", 1)), Bag(("months", 1)), false),
            ["Duration({weeks: 1}).add({days: 1})"] = () => A.AddDurations(D(("weeks", 1)), Bag(("days", 1)), false),
            ["Duration({hours: 1}).add({months: 1})"] = () => A.AddDurations(D(("hours", 1)), Bag(("months", 1)), false),
            ["Duration({years: 1}).add({years: -1})"] = () => A.AddDurations(D(("years", 1)), Bag(("years", -1)), false),
            ["Duration({hours: 1}).add({hours: 1}, 5)"] = () => A.AddDurations(D(("hours", 1)), Bag(("hours", 1)), false),
            ["Duration({hours: 1}).add({hours: 1}, null)"] = () => A.AddDurations(D(("hours", 1)), Bag(("hours", 1)), false),
            ["Duration({hours: 1}).add({hours: 1}, {get relativeTo() {...}}) and the reads"] = () =>
                A.Render(A.AddDurations(D(("hours", 1)), Bag(("hours", 1)), false)) + " reads=[]",
        }, engineOnly: ["Duration.prototype.add.length"]);
    }

    [Fact]
    public void D4_compare_answers_as_Broiler_VM_and_adds_only_years_months_and_weeks_to_relativeTo()
    {
        var hebrewDate = A.ToPlainDate(Bag(("year", 5780), ("monthCode", "M05"), ("day", 1), ("calendar", "hebrew")));
        Replay("d4-compare.vm.txt", "b97ebe856b7a0b49a94c34bfe99fac1c38273f8a0f3b953d096d6b57fd577c2a", new Dictionary<string, Func<object?>>
        {
            ["compare({hours: 24}, {days: 1})"] = () => (double)A.Compare(Bag(("hours", 24)), Bag(("days", 1))),
            ["compare({days: 1}, {hours: 25})"] = () => (double)A.Compare(Bag(("days", 1)), Bag(("hours", 25))),
            ["compare({months: 1}, {days: 30})"] = () => (double)A.Compare(Bag(("months", 1)), Bag(("days", 30))),
            ["compare({months: 1}, {days: 30}, {relativeTo: \"2020-02-01\"})"] = () => (double)A.Compare(Bag(("months", 1)), Bag(("days", 30)), O(("relativeTo", "2020-02-01"))),
            ["compare({months: 1}, {days: 30}, {relativeTo: \"2020-01-01\"})"] = () => (double)A.Compare(Bag(("months", 1)), Bag(("days", 30)), O(("relativeTo", "2020-01-01"))),
            ["compare({days: 1}, {hours: 24}, {relativeTo: NY spring day})"] = () => (double)A.Compare(Bag(("days", 1)), Bag(("hours", 24)), O(("relativeTo", NySpring))),
            ["compare({days: 1}, {hours: 23}, {relativeTo: NY spring day})"] = () => (double)A.Compare(Bag(("days", 1)), Bag(("hours", 23)), O(("relativeTo", NySpring))),
            ["compare({months: 1}, {months: 1}) (identical, no relativeTo)"] = () => (double)A.Compare(Bag(("months", 1)), Bag(("months", 1))),
            ["compare({months: 1}, {months: 1}, {relativeTo: \"bad\"})"] = () => (double)A.Compare(Bag(("months", 1)), Bag(("months", 1)), O(("relativeTo", "bad"))),
            ["compare({years: 1, seconds: 9e15}, {years: 1}, {relativeTo: \"2020-01-01\"})"] = () =>
                (double)A.Compare(Bag(("years", 1), ("seconds", 9e15)), Bag(("years", 1)), O(("relativeTo", "2020-01-01"))),
            ["compare({years: 1, seconds: 9e15}, {years: 1, seconds: 9e15 - 1}, {relativeTo: \"2020-01-01\"})"] = () =>
                (double)A.Compare(Bag(("years", 1), ("seconds", 9e15)), Bag(("years", 1), ("seconds", 9e15 - 1)), O(("relativeTo", "2020-01-01"))),
            ["compare({years: 1, seconds: 9007199254740991 - 31622399}, {years: 1}, {relativeTo: \"2020-01-01\"})"] = () =>
                (double)A.Compare(Bag(("years", 1), ("seconds", 9007199254740991.0 - 31622399)), Bag(("years", 1)), O(("relativeTo", "2020-01-01"))),
            ["compare({days: 200000000}, {days: 1}, {relativeTo: \"2020-01-01\"})"] = () => (double)A.Compare(Bag(("days", 200000000)), Bag(("days", 1)), O(("relativeTo", "2020-01-01"))),
            ["compare({weeks: 1}, {days: 7}, {relativeTo: \"2020-01-01\"})"] = () => (double)A.Compare(Bag(("weeks", 1)), Bag(("days", 7)), O(("relativeTo", "2020-01-01"))),
            ["compare({years: 1}, {days: 365}, {relativeTo: \"2020-01-01[u-ca=hebrew]\"})"] = () =>
                (double)A.Compare(Bag(("years", 1)), Bag(("days", 365)), O(("relativeTo", "2020-01-01[u-ca=hebrew]"))),
            ["compare({months: 1}, {days: 29}, {relativeTo: PlainDate hebrew 5780-05-01})"] = () => (double)A.Compare(Bag(("months", 1)), Bag(("days", 29)), O(("relativeTo", hebrewDate))),
        });
    }

    // ---- D5 to D11 ------------------------------------------------------------------------------------------------

    [Fact]
    public void D5_round_with_no_relativeTo_answers_as_Broiler_VM_in_every_mode()
    {
        var lines = new Dictionary<string, Func<object?>>();
        foreach (var mode in new[] { "ceil", "floor", "expand", "trunc", "halfCeil", "halfFloor", "halfExpand", "halfTrunc", "halfEven" })
        {
            lines["PT1H30M round hour " + mode] = () => Round(D("PT1H30M"), ("smallestUnit", "hour"), ("roundingMode", mode));
            lines["-PT1H30M round hour " + mode] = () => Round(D("-PT1H30M"), ("smallestUnit", "hour"), ("roundingMode", mode));
            lines["PT2H30M round hour " + mode] = () => Round(D("PT2H30M"), ("smallestUnit", "hour"), ("roundingMode", mode));
            lines["-PT1H20M round 30 minutes " + mode] = () => Round(D("-PT1H20M"), ("smallestUnit", "minute"), ("roundingIncrement", 30), ("roundingMode", mode));
        }

        lines["Duration({hours: 25, minutes: 30}).round({largestUnit: \"day\"})"] = () => Round(D(("hours", 25), ("minutes", 30)), ("largestUnit", "day"));
        lines["Duration({days: 3, hours: 12}).round({smallestUnit: \"day\", roundingIncrement: 2, largestUnit: \"day\"})"] = () =>
            Round(D(("days", 3), ("hours", 12)), ("smallestUnit", "day"), ("roundingIncrement", 2), ("largestUnit", "day"));
        lines["Duration({minutes: 125}).round({smallestUnit: \"minute\", roundingIncrement: 15})"] = () => Round(D(("minutes", 125)), ("smallestUnit", "minute"), ("roundingIncrement", 15));
        lines["Duration({hours: 47}).round({largestUnit: \"day\"})"] = () => Round(D(("hours", 47)), ("largestUnit", "day"));
        lines["Duration({days: 2}).round({largestUnit: \"hour\"})"] = () => Round(D(("days", 2)), ("largestUnit", "hour"));
        lines["Duration({days: 2}).round({largestUnit: \"auto\", smallestUnit: \"hour\"})"] = () => Round(D(("days", 2)), ("largestUnit", "auto"), ("smallestUnit", "hour"));
        lines["Duration({hours: 1}).round(\"minutes\")"] = () => A.Round(D(("hours", 1)), "minutes");
        lines["Duration({hours: 1}).round({})"] = () => Round(D(("hours", 1)));
        lines["Duration({hours: 1}).round()"] = () => A.Round(D(("hours", 1)), null);
        lines["Duration({hours: 1}).round({largestUnit: \"hour\", smallestUnit: \"day\"})"] = () => Round(D(("hours", 1)), ("largestUnit", "hour"), ("smallestUnit", "day"));
        lines["Duration({hours: 1}).round({smallestUnit: \"hour\", roundingIncrement: 5})"] = () => Round(D(("hours", 1)), ("smallestUnit", "hour"), ("roundingIncrement", 5));
        lines["Duration({hours: 1}).round({smallestUnit: \"hour\", roundingIncrement: 24})"] = () => Round(D(("hours", 1)), ("smallestUnit", "hour"), ("roundingIncrement", 24));
        lines["Duration({days: 1}).round({smallestUnit: \"day\", roundingIncrement: 2, largestUnit: \"month\"})"] = () =>
            Round(D(("days", 1)), ("smallestUnit", "day"), ("roundingIncrement", 2), ("largestUnit", "month"), ("relativeTo", "2020-01-01"));
        lines["Duration({days: 1}).round({smallestUnit: \"month\"})"] = () => Round(D(("days", 1)), ("smallestUnit", "month"));
        lines["Duration({years: 1}).round({smallestUnit: \"hour\"})"] = () => Round(D(("years", 1)), ("smallestUnit", "hour"));
        lines["Duration({weeks: 1}).round({largestUnit: \"day\"})"] = () => Round(D(("weeks", 1)), ("largestUnit", "day"));
        lines["Duration({days: 1}).round({largestUnit: \"week\"})"] = () => Round(D(("days", 1)), ("largestUnit", "week"));
        lines["Duration({seconds: 2**53 - 1}).round({smallestUnit: \"hour\", roundingMode: \"ceil\"})"] = () => Round(D(("seconds", MaxSafe)), ("smallestUnit", "hour"), ("roundingMode", "ceil"));
        lines["Duration({seconds: 2**53 - 1}).round({smallestUnit: \"day\", largestUnit: \"day\", roundingMode: \"floor\"})"] = () =>
            Round(D(("seconds", MaxSafe)), ("smallestUnit", "day"), ("largestUnit", "day"), ("roundingMode", "floor"));
        lines["Duration({days: 1e8}).round({smallestUnit: \"day\", roundingIncrement: 1e9, roundingMode: \"ceil\", largestUnit: \"day\"})"] = () =>
            Round(D(("days", 1e8)), ("smallestUnit", "day"), ("roundingIncrement", 1e9), ("roundingMode", "ceil"), ("largestUnit", "day"));
        lines["Duration({days: 1e8}).round({smallestUnit: \"day\", roundingIncrement: 2.5, largestUnit: \"day\"})"] = () =>
            Round(D(("days", 1e8 + 1)), ("smallestUnit", "day"), ("roundingIncrement", 2.5), ("largestUnit", "day"));

        Replay("d5-round-time.vm.txt", "bbd8251488b6a40eb6292aa234976a1de1b9db36ed3745ea3209d008af361c9a", lines);
    }

    [Fact]
    public void D6_D7_round_relative_to_a_plain_date_nudges_and_bubbles_as_Broiler_VM_answers()
    {
        Replay("d6-round-plain-relative.vm.txt", "213c3ffd3b3ac209b3d100dd53ea5b2d9edec0ad821946300b7553fa2537f22a", new Dictionary<string, Func<object?>>
        {
            ["{days: 45} round largest month, relativeTo 2020-01-31"] = () => Round(D(("days", 45)), ("largestUnit", "month"), ("relativeTo", "2020-01-31")),
            ["{days: 15} round smallest month, relativeTo 2020-02-01"] = () => Round(D(("days", 15)), ("smallestUnit", "month"), ("relativeTo", "2020-02-01")),
            ["{days: 15} round smallest month, relativeTo 2020-01-01"] = () => Round(D(("days", 15)), ("smallestUnit", "month"), ("relativeTo", "2020-01-01")),
            ["{months: 11, days: 30} round smallest month largest year, relativeTo 2020-01-01"] = () =>
                Round(D(("months", 11), ("days", 30)), ("smallestUnit", "month"), ("largestUnit", "year"), ("relativeTo", "2020-01-01")),
            ["{days: 40} round smallest week largest month, relativeTo 2020-01-01"] = () => Round(D(("days", 40)), ("smallestUnit", "week"), ("largestUnit", "month"), ("relativeTo", "2020-01-01")),
            ["{days: 10} round smallest week, relativeTo 2020-01-01"] = () => Round(D(("days", 10)), ("smallestUnit", "week"), ("relativeTo", "2020-01-01")),
            ["{months: 10, days: 29, hours: 12} round month/year trunc, relativeTo 2019-03-31"] = () =>
                Round(D(("months", 10), ("days", 29), ("hours", 12)), ("smallestUnit", "month"), ("largestUnit", "year"), ("roundingMode", "trunc"), ("relativeTo", "2019-03-31")),
            ["{months: 10, days: 29, hours: 12} round month/year halfExpand, relativeTo 2019-03-31"] = () =>
                Round(D(("months", 10), ("days", 29), ("hours", 12)), ("smallestUnit", "month"), ("largestUnit", "year"), ("roundingMode", "halfExpand"), ("relativeTo", "2019-03-31")),
            ["{months: 10, days: 29, hours: 12} round month/month trunc, relativeTo 2019-03-31"] = () =>
                Round(D(("months", 10), ("days", 29), ("hours", 12)), ("smallestUnit", "month"), ("largestUnit", "month"), ("roundingMode", "trunc"), ("relativeTo", "2019-03-31")),
            ["{hours: 36} round largest day, relativeTo 2020-01-01"] = () => Round(D(("hours", 36)), ("largestUnit", "day"), ("relativeTo", "2020-01-01")),
            ["{years: 1} round largest day, relativeTo 2020-01-01"] = () => Round(D(("years", 1)), ("largestUnit", "day"), ("relativeTo", "2020-01-01")),
            ["{years: 1} round largest day, relativeTo 2021-01-01"] = () => Round(D(("years", 1)), ("largestUnit", "day"), ("relativeTo", "2021-01-01")),
            ["{months: 1} round smallest day, relativeTo 2020-01-31"] = () => Round(D(("months", 1)), ("smallestUnit", "day"), ("relativeTo", "2020-01-31")),
            ["{months: -1, days: -15} round smallest month, relativeTo 2020-03-31"] = () => Round(D(("months", -1), ("days", -15)), ("smallestUnit", "month"), ("relativeTo", "2020-03-31")),
            ["{weeks: 3} round largest month, relativeTo 2020-02-15"] = () => Round(D(("weeks", 3)), ("largestUnit", "month"), ("relativeTo", "2020-02-15")),
            ["{days: 31} round smallest month increment 2 halfExpand, relativeTo 2020-01-01"] = () =>
                Round(D(("days", 31)), ("smallestUnit", "month"), ("largestUnit", "month"), ("roundingIncrement", 2), ("relativeTo", "2020-01-01")),
            ["{days: 30} round smallest month increment 2 halfExpand, relativeTo 2020-01-01"] = () =>
                Round(D(("days", 30)), ("smallestUnit", "month"), ("largestUnit", "month"), ("roundingIncrement", 2), ("relativeTo", "2020-01-01")),
            ["{hours: 23, minutes: 59, seconds: 59, nanoseconds: 999999999, months: 23, days: 30} round microsecond, largest year, relativeTo 2020-01-01"] = () =>
                Round(D(("months", 23), ("days", 30), ("hours", 23), ("minutes", 59), ("seconds", 59), ("milliseconds", 999), ("microseconds", 999), ("nanoseconds", 999)),
                    ("smallestUnit", "microsecond"), ("largestUnit", "year"), ("relativeTo", "2020-01-01")),
            ["{days: -1, hours: -12} round smallest day halfExpand, relativeTo 2020-01-01"] = () => Round(D(("days", -1), ("hours", -12)), ("smallestUnit", "day"), ("relativeTo", "2020-01-01")),
            ["{years: 1, months: 6} round smallest year halfEven, relativeTo 2020-01-01"] = () =>
                Round(D(("years", 1), ("months", 6)), ("smallestUnit", "year"), ("roundingMode", "halfEven"), ("relativeTo", "2020-01-01")),
            ["{days: 366} round largest year, relativeTo PlainDateTime 2020-01-01T12:00"] = () => Round(D(("days", 366)), ("largestUnit", "year"), ("relativeTo", PDT("2020-01-01T12:00"))),
            ["{days: 1} round largest year, relativeTo -271821-04-19"] = () => Round(D(("days", 1)), ("largestUnit", "year"), ("relativeTo", "-271821-04-19")),
            ["{days: 1} round largest year, relativeTo -271821-04-20"] = () => Round(D(("days", 1)), ("largestUnit", "year"), ("relativeTo", "-271821-04-20")),
            ["{} round largest year, relativeTo -271821-04-19"] = () => Round(TemporalAdapter.NewDuration(), ("largestUnit", "year"), ("relativeTo", "-271821-04-19")),
        });
    }

    [Fact]
    public void D8_the_nudge_after_a_shifted_window_follows_the_pinned_text()
    {
        // DECISION D7: P11M in all five, the text's answer; the polyfill gives P1Y.
        var d = new (string, object?)[] { ("months", 10), ("days", 29), ("hours", 12) };
        Replay("d8-shifted-window.vm.txt", "5c490096c3c974030dc698ac8b651e9a2241704364fbc65dac82195129038cf0", new Dictionary<string, Func<object?>>
        {
            ["Duration round month/year trunc, relativeTo \"2019-03-31\""] = () =>
                Round(D(d), ("smallestUnit", "month"), ("largestUnit", "year"), ("roundingMode", "trunc"), ("relativeTo", "2019-03-31")),
            ["Duration round month/year trunc, relativeTo \"2019-03-31T00:00[UTC]\""] = () =>
                Round(D(d), ("smallestUnit", "month"), ("largestUnit", "year"), ("roundingMode", "trunc"), ("relativeTo", "2019-03-31T00:00[UTC]")),
            ["Duration round month/year trunc, relativeTo ZonedDateTime 2019-03-31[UTC]"] = () =>
                Round(D(d), ("smallestUnit", "month"), ("largestUnit", "year"), ("roundingMode", "trunc"), ("relativeTo", Z("2019-03-31T00:00[UTC]"))),
            ["PlainDateTime 2019-03-31T00:00 until 2020-02-29T12:00 month/year trunc"] = () =>
                A.Difference(Until, PDT("2019-03-31T00:00"), "2020-02-29T12:00", O(("smallestUnit", "month"), ("largestUnit", "year"))),
            ["ZonedDateTime 2019-03-31T00:00[UTC] until 2020-02-29T12:00[UTC] month/year trunc"] = () =>
                A.Difference(Until, Z("2019-03-31T00:00[UTC]"), "2020-02-29T12:00[UTC]", O(("smallestUnit", "month"), ("largestUnit", "year"))),
            ["Duration total month, relativeTo \"2019-03-31\""] = () => Total(D(d), ("unit", "month"), ("relativeTo", "2019-03-31")),
            ["2019-03-31 + P1Y"] = () => A.Add(PD("2019-03-31"), Bag(("years", 1)), null, false),
        });
    }

    [Fact]
    public void D9_round_relative_to_a_zoned_date_uses_real_day_lengths_as_Broiler_VM_answers()
    {
        Replay("d9-round-zoned-relative.vm.txt", "61a0319f895587b109136baa867f54c5c9d5676b151e64adbb63737eabfaec89", new Dictionary<string, Func<object?>>
        {
            ["{hours: 23} round smallest day, NY spring"] = () => Round(D(("hours", 23)), ("smallestUnit", "day"), ("relativeTo", NySpring)),
            ["{hours: 24} round largest day, NY spring"] = () => Round(D(("hours", 24)), ("largestUnit", "day"), ("relativeTo", NySpring)),
            ["{days: 1} round largest hour, NY spring"] = () => Round(D(("days", 1)), ("largestUnit", "hour"), ("relativeTo", NySpring)),
            ["{hours: 25} round largest day, NY fall"] = () => Round(D(("hours", 25)), ("largestUnit", "day"), ("relativeTo", NyFall)),
            ["{hours: 12} round smallest day, NY spring"] = () => Round(D(("hours", 12)), ("smallestUnit", "day"), ("relativeTo", NySpring)),
            ["{hours: 12} round smallest day, NY fall"] = () => Round(D(("hours", 12)), ("smallestUnit", "day"), ("relativeTo", NyFall)),
            ["{hours: 11, minutes: 45} round smallest day, NY 2026-03-07T12:00"] = () =>
                Round(D(("hours", 11), ("minutes", 45)), ("smallestUnit", "day"), ("relativeTo", "2026-03-07T12:00-05:00[America/New_York]")),
            ["{hours: 12, minutes: 15} round smallest day, NY 2026-10-31T12:00"] = () =>
                Round(D(("hours", 12), ("minutes", 15)), ("smallestUnit", "day"), ("relativeTo", "2026-10-31T12:00-04:00[America/New_York]")),
            ["{months: 10, days: 29, hours: 12} round month/year trunc, relativeTo 2019-03-31T00:00[UTC]"] = () =>
                Round(D(("months", 10), ("days", 29), ("hours", 12)), ("smallestUnit", "month"), ("largestUnit", "year"), ("roundingMode", "trunc"), ("relativeTo", "2019-03-31T00:00[UTC]")),
            ["{months: 10, days: 29, hours: 12} round month/year trunc, relativeTo ZonedDateTime 2019-03-31[UTC]"] = () =>
                Round(D(("months", 10), ("days", 29), ("hours", 12)), ("smallestUnit", "month"), ("largestUnit", "year"), ("roundingMode", "trunc"), ("relativeTo", Z("2019-03-31T00:00[UTC]"))),
            ["{days: 45} round largest month, relativeTo 2020-01-31T00:00[UTC]"] = () => Round(D(("days", 45)), ("largestUnit", "month"), ("relativeTo", "2020-01-31T00:00[UTC]")),
            ["{hours: 30} round smallest hour largest day, NY spring"] = () => Round(D(("hours", 30)), ("smallestUnit", "hour"), ("largestUnit", "day"), ("relativeTo", NySpring)),
            ["{hours: 22, minutes: 40} round smallest hour increment 2 largest day, NY spring"] = () =>
                Round(D(("hours", 22), ("minutes", 40)), ("smallestUnit", "hour"), ("roundingIncrement", 2), ("largestUnit", "day"), ("relativeTo", NySpring)),
            ["{days: 1, hours: 1} round smallest day largest year, NY spring halfExpand"] = () =>
                Round(D(("days", 1), ("hours", 1)), ("smallestUnit", "day"), ("largestUnit", "year"), ("relativeTo", NySpring)),
            ["{days: 1} round smallest hour, relativeTo +275760-09-12T00:00[UTC]"] = () => Round(D(("days", 1)), ("smallestUnit", "hour"), ("relativeTo", "+275760-09-12T00:00[UTC]")),
            ["{hours: 1} round smallest hour largest day, relativeTo +275760-09-12T12:00[UTC]"] = () =>
                Round(D(("hours", 1)), ("smallestUnit", "hour"), ("largestUnit", "day"), ("relativeTo", "+275760-09-12T12:00[UTC]")),
            ["{} round largest year, relativeTo +275760-09-13T00:00[UTC]"] = () => Round(TemporalAdapter.NewDuration(), ("largestUnit", "year"), ("relativeTo", "+275760-09-13T00:00[UTC]")),
            ["{hours: 1} round smallest hour largest year, relativeTo +275760-09-12T12:00[UTC]"] = () =>
                Round(D(("hours", 1)), ("smallestUnit", "hour"), ("largestUnit", "year"), ("relativeTo", "+275760-09-12T12:00[UTC]")),
        });
    }

    [Fact]
    public void D10_totals_are_exact_ratios_rounded_once_as_Broiler_VM_answers()
    {
        Replay("d10-total.vm.txt", "8f20b0b5de4b225f037b6413b1d0a3d781c2436984523b21f4c468e814819dcd", new Dictionary<string, Func<object?>>
        {
            ["{hours: 1, minutes: 30}.total(\"hour\")"] = () => A.Total(D(("hours", 1), ("minutes", 30)), "hour"),
            ["{days: 45}.total({unit: \"month\", relativeTo: \"2020-01-01\"})"] = () => Total(D(("days", 45)), ("unit", "month"), ("relativeTo", "2020-01-01")),
            ["{days: 45}.total({unit: \"month\", relativeTo: \"2020-01-01T00:00[UTC]\"})"] = () => Total(D(("days", 45)), ("unit", "month"), ("relativeTo", "2020-01-01T00:00[UTC]")),
            ["{nanoseconds: MAX_SAFE_INTEGER}.total(\"second\")"] = () => A.Total(D(("nanoseconds", MaxSafe)), "second"),
            ["{seconds: 2**53 - 1, nanoseconds: 999999999}.total(\"nanosecond\")"] = () => A.Total(D(("seconds", 9007199254740991.0), ("nanoseconds", 999999999)), "nanosecond"),
            ["{seconds: 2**53 - 1, nanoseconds: 999999999}.total(\"day\")"] = () => A.Total(D(("seconds", 9007199254740991.0), ("nanoseconds", 999999999)), "day"),
            ["{days: 1}.total({unit: \"hour\", relativeTo: NY spring})"] = () => Total(D(("days", 1)), ("unit", "hour"), ("relativeTo", NySpring)),
            ["{hours: 12}.total({unit: \"day\", relativeTo: NY spring})"] = () => Total(D(("hours", 12)), ("unit", "day"), ("relativeTo", NySpring)),
            ["{hours: 12}.total({unit: \"day\", relativeTo: NY fall})"] = () => Total(D(("hours", 12)), ("unit", "day"), ("relativeTo", NyFall)),
            ["{months: 1}.total({unit: \"day\", relativeTo: \"2020-02-01\"})"] = () => Total(D(("months", 1)), ("unit", "day"), ("relativeTo", "2020-02-01")),
            ["{years: 1}.total({unit: \"week\", relativeTo: \"2020-01-01\"})"] = () => Total(D(("years", 1)), ("unit", "week"), ("relativeTo", "2020-01-01")),
            ["{months: 1}.total(\"day\")"] = () => A.Total(D(("months", 1)), "day"),
            ["{days: 1}.total(\"day\")"] = () => A.Total(D(("days", 1)), "day"),
            ["{days: 1}.total({unit: \"month\"})"] = () => Total(D(("days", 1)), ("unit", "month")),
            ["{days: 1}.total({})"] = () => Total(D(("days", 1))),
            ["{months: 10, days: 29, hours: 12}.total({unit: \"month\", relativeTo: \"2019-03-31\"})"] = () =>
                Total(D(("months", 10), ("days", 29), ("hours", 12)), ("unit", "month"), ("relativeTo", "2019-03-31")),
            ["{months: 10, days: 29, hours: 12}.total({unit: \"year\", relativeTo: \"2019-03-31T00:00[UTC]\"})"] = () =>
                Total(D(("months", 10), ("days", 29), ("hours", 12)), ("unit", "year"), ("relativeTo", "2019-03-31T00:00[UTC]")),
            ["{days: -45}.total({unit: \"month\", relativeTo: \"2020-03-15\"})"] = () => Total(D(("days", -45)), ("unit", "month"), ("relativeTo", "2020-03-15")),
            ["{days: 1}.total({unit: \"day\", relativeTo: \"+275760-09-12T00:00:01[UTC]\"})"] = () =>
                Total(D(("days", 1)), ("unit", "day"), ("relativeTo", "+275760-09-12T00:00:01+00:00[UTC]")),
            ["{hours: 1}.total({unit: \"day\", relativeTo: \"+275760-09-12T12:00[UTC]\"})"] = () => Total(D(("hours", 1)), ("unit", "day"), ("relativeTo", "+275760-09-12T12:00[UTC]")),
            ["{days: 1}.total({unit: \"day\", relativeTo: \"+275760-09-12\"})"] = () => Total(D(("days", 1)), ("unit", "day"), ("relativeTo", "+275760-09-12")),
        });
    }

    [Fact]
    public void D11_relativeTo_in_another_calendar_rounds_and_totals_in_that_calendar_as_Broiler_VM_answers()
    {
        const string Hebrew = "2020-01-01[u-ca=hebrew]";
        Replay("d11-non-iso-relative.vm.txt", "865f38efaabf020594de990749050eab4d5c8f410c29216971c9284951671542", new Dictionary<string, Func<object?>>
        {
            ["{months: 1}.round({smallestUnit: \"day\", relativeTo: hebrew 2020-01-01})"] = () => Round(D(("months", 1)), ("smallestUnit", "day"), ("relativeTo", Hebrew)),
            ["{days: 45}.round({largestUnit: \"month\", relativeTo: hebrew 2020-01-01})"] = () => Round(D(("days", 45)), ("largestUnit", "month"), ("relativeTo", Hebrew)),
            ["{days: 45}.total({unit: \"month\", relativeTo: hebrew 2020-01-01})"] = () => Total(D(("days", 45)), ("unit", "month"), ("relativeTo", Hebrew)),
            ["{days: 45}.total({unit: \"month\", relativeTo: PlainDate.withCalendar(\"islamic-civil\")})"] = () =>
                Total(D(("days", 45)), ("unit", "month"), ("relativeTo", TemporalAdapter.WithCalendar(PD("2020-01-01"), "islamic-civil"))),
            ["{months: 13}.round({largestUnit: \"year\", relativeTo: hebrew 5784-01-01 (a leap year)})"] = () =>
                Round(D(("months", 13)), ("largestUnit", "year"), ("relativeTo", A.ToPlainDate(Bag(("year", 5784), ("monthCode", "M01"), ("day", 1), ("calendar", "hebrew"))))),
            ["{days: 45}.round({largestUnit: \"month\", relativeTo: \"2020-01-01T00:00[UTC][u-ca=coptic]\"})"] = () =>
                Round(D(("days", 45)), ("largestUnit", "month"), ("relativeTo", "2020-01-01T00:00[UTC][u-ca=coptic]")),
            ["{days: 45}.round({largestUnit: \"month\", relativeTo: {year: 5780, month: 4, day: 1, calendar: \"hebrew\"}})"] = () =>
                Round(D(("days", 45)), ("largestUnit", "month"), ("relativeTo", Bag(("year", 5780), ("month", 4), ("day", 1), ("calendar", "hebrew")))),
            ["{days: 45}.round({largestUnit: \"month\", relativeTo: \"2020-01-01[u-ca=gregory]\"})"] = () => Round(D(("days", 45)), ("largestUnit", "month"), ("relativeTo", "2020-01-01[u-ca=gregory]")),
            ["{days: 45}.round({largestUnit: \"month\", relativeTo: \"2020-01-01[u-ca=japanese]\"})"] = () => Round(D(("days", 45)), ("largestUnit", "month"), ("relativeTo", "2020-01-01[u-ca=japanese]")),
        });
    }

    // ---- F1 to F8 -------------------------------------------------------------------------------------------------

    [Fact]
    public void F1_the_difference_settings_default_and_refuse_as_Broiler_VM_answers()
    {
        Replay("f1-settings-and-defaults.vm.txt", "1b579d2baf0886db1eb972969755f0bffb9b86b29ee7fd05e5deaaf8e77c42e4", new Dictionary<string, Func<object?>>
        {
            ["PlainDate 2020-01-01 until 2021-03-15"] = () => A.Difference(Until, PD("2020-01-01"), "2021-03-15", null),
            ["PlainDateTime 2020-01-01T00:00 until 2020-01-03T12:30"] = () => A.Difference(Until, PDT("2020-01-01T00:00"), "2020-01-03T12:30", null),
            ["PlainTime 01:00 until 23:30:15"] = () => A.Difference(Until, PT("01:00"), "23:30:15", null),
            ["Instant until +1 day 1 h 0.5 s"] = () => A.Difference(Until, I("2020-01-01T00:00Z"), "2020-01-02T01:00:00.5Z", null),
            ["ZonedDateTime until +25 h"] = () => A.Difference(Until, Z("2020-01-01T00:00[UTC]"), "2020-01-02T01:00[UTC]", null),
            ["PlainYearMonth 2019-05 until 2021-03"] = () => A.Difference(Until, PYM("2019-05"), "2021-03", null),
            ["PlainDate until largestUnit hour"] = () => A.Difference(Until, PD("2020-01-01"), "2020-01-02", O(("largestUnit", "hour"))),
            ["PlainDate until smallestUnit hour"] = () => A.Difference(Until, PD("2020-01-01"), "2020-01-02", O(("smallestUnit", "hour"))),
            ["PlainTime until largestUnit day"] = () => A.Difference(Until, PT("01:00"), "02:00", O(("largestUnit", "day"))),
            ["Instant until smallestUnit day"] = () => A.Difference(Until, I("2020-01-01T00:00Z"), "2020-01-02T00:00Z", O(("smallestUnit", "day"))),
            ["Instant until largestUnit day"] = () => A.Difference(Until, I("2020-01-01T00:00Z"), "2020-01-02T00:00Z", O(("largestUnit", "day"))),
            ["PlainYearMonth until smallestUnit week"] = () => A.Difference(Until, PYM("2019-05"), "2021-03", O(("smallestUnit", "week"))),
            ["PlainYearMonth until largestUnit day"] = () => A.Difference(Until, PYM("2019-05"), "2021-03", O(("largestUnit", "day"))),
            ["PlainDate until smallestUnit year largestUnit month"] = () => A.Difference(Until, PD("2020-01-01"), "2021-03-15", O(("smallestUnit", "year"), ("largestUnit", "month"))),
            ["PlainTime until smallestUnit minute increment 7"] = () => A.Difference(Until, PT("01:00"), "02:00", O(("smallestUnit", "minute"), ("roundingIncrement", 7))),
            ["PlainTime until smallestUnit minute increment 60"] = () => A.Difference(Until, PT("01:00"), "02:00", O(("smallestUnit", "minute"), ("roundingIncrement", 60))),
            ["Instant until smallestUnit hour increment 24"] = () => A.Difference(Until, I("2020-01-01T00:00Z"), "2020-01-03T00:00Z", O(("smallestUnit", "hour"), ("roundingIncrement", 24))),
            ["Instant until smallestUnit hour increment 12"] = () =>
                A.Difference(Until, I("2020-01-01T00:00Z"), "2020-01-03T05:00Z", O(("smallestUnit", "hour"), ("roundingIncrement", 12), ("largestUnit", "hour"))),
            ["PlainDate until smallestUnit day increment 1e9"] = () => A.Difference(Until, PD("2020-01-01"), "2020-01-02", O(("smallestUnit", "day"), ("roundingIncrement", 1e9))),
            ["PlainDate until roundingIncrement 0"] = () => A.Difference(Until, PD("2020-01-01"), "2020-01-02", O(("roundingIncrement", 0))),
            ["PlainDate until roundingIncrement 1e9 + 1"] = () => A.Difference(Until, PD("2020-01-01"), "2020-01-02", O(("roundingIncrement", 1e9 + 1))),
            ["PlainDate until roundingMode bogus"] = () => A.Difference(Until, PD("2020-01-01"), "2020-01-02", O(("roundingMode", "bogus"))),
            ["PlainDate until largestUnit auto smallestUnit month"] = () => A.Difference(Until, PD("2020-01-01"), "2021-03-15", O(("largestUnit", "auto"), ("smallestUnit", "month"))),
            ["ZonedDateTime until smallestUnit day (largest auto)"] = () => A.Difference(Until, Z("2020-01-01T00:00[UTC]"), "2020-01-03T12:00[UTC]", O(("smallestUnit", "day"))),
            ["Instant until smallestUnit minute (largest auto)"] = () => A.Difference(Until, I("2020-01-01T00:00Z"), "2020-01-01T02:00Z", O(("smallestUnit", "minute"))),
            ["PlainDate until options 5"] = () => A.Difference(Until, PD("2020-01-01"), "2020-01-02", 5.0),
            ["PlainDate until other calendar"] = () => A.Difference(Until, PD("2020-01-01"), TemporalAdapter.WithCalendar(PD("2020-02-01"), "gregory"), null),
            ["ZonedDateTime until other zone, largestUnit day"] = () =>
                A.Difference(Until, Z("2020-01-01T00:00[UTC]"), "2020-01-02T00:00+01:00[Europe/Berlin]", O(("largestUnit", "day"))),
            ["ZonedDateTime until other zone, largestUnit hour"] = () =>
                A.Difference(Until, Z("2020-01-01T00:00[UTC]"), "2020-01-02T00:00+01:00[Europe/Berlin]", O(("largestUnit", "hour"))),
        });
    }

    [Fact]
    public void F2_plain_date_differences_anchor_on_the_receiver_as_Broiler_VM_answers()
    {
        static TemporalAdapter.Duration DateDiff(DifferenceOperation op, string one, string two, string? largest) =>
            A.Difference(op, PD(one), two, largest is null ? null : O(("largestUnit", largest)));

        Replay("f2-date-differences.vm.txt", "c7f8d64560fa115f68fc7e75ae38fea346264399645c780fb8e45fdcbebb5338", new Dictionary<string, Func<object?>>
        {
            ["2020-01-31 until 2020-03-01 month"] = () => DateDiff(Until, "2020-01-31", "2020-03-01", "month"),
            ["2020-03-01 since 2020-01-31 month"] = () => DateDiff(Since, "2020-03-01", "2020-01-31", "month"),
            ["2020-01-31 since 2020-03-01 month"] = () => DateDiff(Since, "2020-01-31", "2020-03-01", "month"),
            ["2020-03-01 until 2020-01-31 month"] = () => DateDiff(Until, "2020-03-01", "2020-01-31", "month"),
            ["2020-01-01 until 2020-03-01 week"] = () => DateDiff(Until, "2020-01-01", "2020-03-01", "week"),
            ["2019-02-28 until 2020-02-29 year"] = () => DateDiff(Until, "2019-02-28", "2020-02-29", "year"),
            ["2020-02-29 until 2021-02-28 year"] = () => DateDiff(Until, "2020-02-29", "2021-02-28", "year"),
            ["2021-02-28 until 2020-02-29 year"] = () => DateDiff(Until, "2021-02-28", "2020-02-29", "year"),
            ["2020-01-29 until 2020-02-28 month"] = () => DateDiff(Until, "2020-01-29", "2020-02-28", "month"),
            ["2019-01-29 until 2019-03-01 month"] = () => DateDiff(Until, "2019-01-29", "2019-03-01", "month"),
            ["-271821-04-19 until +275760-09-13 year"] = () => DateDiff(Until, "-271821-04-19", "+275760-09-13", "year"),
            ["-271821-04-19 until +275760-09-13 day"] = () => DateDiff(Until, "-271821-04-19", "+275760-09-13", null),
            ["+275760-09-13 since -271821-04-19 week"] = () => DateDiff(Since, "+275760-09-13", "-271821-04-19", "week"),
            ["2020-01-01 until 2020-01-01"] = () => DateDiff(Until, "2020-01-01", "2020-01-01", "year"),
        });
    }

    [Fact]
    public void F3_plain_date_rounding_answers_as_Broiler_VM_in_modes_increments_and_at_the_limits()
    {
        static TemporalAdapter.Duration DateDiff(DifferenceOperation op, string one, string two, params (string, object?)[] options) =>
            A.Difference(op, PD(one), two, O(options));

        Replay("f3-date-rounding.vm.txt", "22b993fb4eb3b8f0a1afd8fadcd29d8c4414d228960cda7c6b71d99dc74ad8b3", new Dictionary<string, Func<object?>>
        {
            ["2020-01-01 until 2020-07-20 smallest month (trunc)"] = () => DateDiff(Until, "2020-01-01", "2020-07-20", ("smallestUnit", "month")),
            ["2020-01-01 until 2020-07-20 smallest month halfExpand"] = () => DateDiff(Until, "2020-01-01", "2020-07-20", ("smallestUnit", "month"), ("roundingMode", "halfExpand")),
            ["2020-01-01 until 2020-07-02 smallest year halfExpand"] = () => DateDiff(Until, "2020-01-01", "2020-07-02", ("smallestUnit", "year"), ("roundingMode", "halfExpand")),
            ["2020-01-01 until 2020-07-02 smallest year halfTrunc"] = () => DateDiff(Until, "2020-01-01", "2020-07-02", ("smallestUnit", "year"), ("roundingMode", "halfTrunc")),
            ["2020-01-17 since 2020-01-01 smallest week floor"] = () => DateDiff(Since, "2020-01-17", "2020-01-01", ("smallestUnit", "week"), ("roundingMode", "floor")),
            ["2020-01-01 since 2020-01-17 smallest week floor"] = () => DateDiff(Since, "2020-01-01", "2020-01-17", ("smallestUnit", "week"), ("roundingMode", "floor")),
            ["2020-01-01 until 2020-01-17 smallest week ceil"] = () => DateDiff(Until, "2020-01-01", "2020-01-17", ("smallestUnit", "week"), ("roundingMode", "ceil")),
            ["2020-01-17 until 2020-01-01 smallest week ceil"] = () => DateDiff(Until, "2020-01-17", "2020-01-01", ("smallestUnit", "week"), ("roundingMode", "ceil")),
            ["2020-01-01 until 2020-03-19 month/day increment 5 halfExpand"] = () =>
                DateDiff(Until, "2020-01-01", "2020-03-19", ("largestUnit", "month"), ("smallestUnit", "day"), ("roundingIncrement", 5), ("roundingMode", "halfExpand")),
            ["2020-01-01 until 2020-03-30 month/day increment 5 halfExpand"] = () =>
                DateDiff(Until, "2020-01-01", "2020-03-30", ("largestUnit", "month"), ("smallestUnit", "day"), ("roundingIncrement", 5), ("roundingMode", "halfExpand")),
            ["2020-01-01 until 2020-04-15 smallest month increment 2 (trunc)"] = () => DateDiff(Until, "2020-01-01", "2020-04-15", ("smallestUnit", "month"), ("roundingIncrement", 2)),
            ["2020-01-01 until 2020-04-15 smallest month increment 2 halfExpand"] = () =>
                DateDiff(Until, "2020-01-01", "2020-04-15", ("smallestUnit", "month"), ("roundingIncrement", 2), ("roundingMode", "halfExpand")),
            ["2020-01-01 until 2021-12-20 smallest month largest year halfExpand"] = () =>
                DateDiff(Until, "2020-01-01", "2021-12-20", ("smallestUnit", "month"), ("largestUnit", "year"), ("roundingMode", "halfExpand")),
            ["2020-01-01 until 2020-01-29 smallest week largest month halfExpand"] = () =>
                DateDiff(Until, "2020-01-01", "2020-01-29", ("smallestUnit", "week"), ("largestUnit", "month"), ("roundingMode", "halfExpand")),
            ["2020-01-31 until 2020-02-29 smallest month"] = () => DateDiff(Until, "2020-01-31", "2020-02-29", ("smallestUnit", "month")),
            ["2020-01-31 until 2020-02-29 smallest month halfExpand"] = () => DateDiff(Until, "2020-01-31", "2020-02-29", ("smallestUnit", "month"), ("roundingMode", "halfExpand")),
            ["2020-01-01 until 2020-01-02 smallest day increment 1e9 ceil"] = () =>
                DateDiff(Until, "2020-01-01", "2020-01-02", ("smallestUnit", "day"), ("roundingIncrement", 1e9), ("roundingMode", "ceil")),
            ["+275760-09-12 until +275760-09-13 smallest month ceil"] = () => DateDiff(Until, "+275760-09-12", "+275760-09-13", ("smallestUnit", "month"), ("roundingMode", "ceil")),
            ["-271821-04-20 since -271821-04-19 smallest year ceil"] = () => DateDiff(Since, "-271821-04-20", "-271821-04-19", ("smallestUnit", "year"), ("roundingMode", "ceil")),
        });
    }

    [Fact]
    public void F4_differences_in_other_calendars_round_in_the_calendar_as_Broiler_VM_answers()
    {
        static (string, object?)[] Opt(params (string, object?)[] options) => options;
        var hebrewFrom = A.ToPlainYearMonth(Bag(("year", 5780), ("monthCode", "M04"), ("calendar", "hebrew")));
        var hebrewTo = A.ToPlainYearMonth(Bag(("year", 5781), ("monthCode", "M03"), ("calendar", "hebrew")));
        Replay("f4-non-iso-rounding.vm.txt", "3c3f729ab5f99a110931dc616aaf2dc094650ac14c31a9907c87ec070bfb909c", new Dictionary<string, Func<object?>>
        {
            ["hebrew PlainDate 2020-01-01 until 2020-07-20 smallest month halfExpand"] = () =>
                A.Difference(Until, PD("2020-01-01[u-ca=hebrew]"), "2020-07-20[u-ca=hebrew]", O(Opt(("smallestUnit", "month"), ("roundingMode", "halfExpand")))),
            ["hebrew PlainDate 2020-01-01 until 2020-07-20 largest month (no rounding)"] = () =>
                A.Difference(Until, PD("2020-01-01[u-ca=hebrew]"), "2020-07-20[u-ca=hebrew]", O(("largestUnit", "month"))),
            ["hebrew PlainDate 2020-01-01 until 2021-10-20 smallest year halfExpand"] = () =>
                A.Difference(Until, PD("2020-01-01[u-ca=hebrew]"), "2021-10-20[u-ca=hebrew]", O(("smallestUnit", "year"), ("roundingMode", "halfExpand"))),
            ["hebrew PlainDate 2020-01-01 since 2020-07-20 smallest month halfExpand"] = () =>
                A.Difference(Since, PD("2020-01-01[u-ca=hebrew]"), "2020-07-20[u-ca=hebrew]", O(("smallestUnit", "month"), ("roundingMode", "halfExpand"))),
            ["coptic PlainDate 2020-01-01 until 2020-03-20 smallest week ceil"] = () =>
                A.Difference(Until, PD("2020-01-01[u-ca=coptic]"), "2020-03-20[u-ca=coptic]", O(("smallestUnit", "week"), ("roundingMode", "ceil"))),
            ["islamic-civil PlainDate 2020-01-01 until 2020-03-20 smallest day increment 7"] = () =>
                A.Difference(Until, PD("2020-01-01[u-ca=islamic-civil]"), "2020-03-20[u-ca=islamic-civil]", O(("smallestUnit", "day"), ("roundingIncrement", 7), ("roundingMode", "halfExpand"))),
            ["gregory PlainDate 2020-01-01 until 2020-07-20 smallest month halfExpand"] = () =>
                A.Difference(Until, PD("2020-01-01[u-ca=gregory]"), "2020-07-20[u-ca=gregory]", O(("smallestUnit", "month"), ("roundingMode", "halfExpand"))),
            ["hebrew PlainDateTime 2020-01-01T00:00 until 2020-01-03T13:00 smallest day halfExpand"] = () =>
                A.Difference(Until, PDT("2020-01-01T00:00[u-ca=hebrew]"), "2020-01-03T13:00[u-ca=hebrew]", O(("smallestUnit", "day"), ("roundingMode", "halfExpand"))),
            ["hebrew PlainDateTime 2020-01-01T00:00 until 2020-07-20T00:00 smallest month halfExpand"] = () =>
                A.Difference(Until, PDT("2020-01-01T00:00[u-ca=hebrew]"), "2020-07-20T00:00[u-ca=hebrew]", O(("smallestUnit", "month"), ("roundingMode", "halfExpand"))),
            ["hebrew PlainDateTime 2020-01-01T00:00 until 2020-01-03T13:20 largest day smallest hour halfExpand"] = () =>
                A.Difference(Until, PDT("2020-01-01T00:00[u-ca=hebrew]"), "2020-01-03T13:20[u-ca=hebrew]", O(("largestUnit", "day"), ("smallestUnit", "hour"), ("roundingMode", "halfExpand"))),
            ["hebrew ZonedDateTime until +2 d 13 h smallest day halfExpand"] = () =>
                A.Difference(Until, Z("2020-01-01T00:00[UTC][u-ca=hebrew]"), "2020-01-03T13:00[UTC][u-ca=hebrew]", O(("smallestUnit", "day"), ("roundingMode", "halfExpand"))),
            ["hebrew ZonedDateTime until 2020-07-20 largest month smallest month halfExpand"] = () =>
                A.Difference(Until, Z("2020-01-01T00:00[UTC][u-ca=hebrew]"), "2020-07-20T00:00[UTC][u-ca=hebrew]",
                    O(("largestUnit", "month"), ("smallestUnit", "month"), ("roundingMode", "halfExpand"))),
            ["hebrew ZonedDateTime until +2 d 13 h 20 min largest day smallest hour halfExpand"] = () =>
                A.Difference(Until, Z("2020-01-01T00:00[UTC][u-ca=hebrew]"), "2020-01-03T13:20[UTC][u-ca=hebrew]",
                    O(("largestUnit", "day"), ("smallestUnit", "hour"), ("roundingMode", "halfExpand"))),
            ["hebrew PlainYearMonth 5780-04 until 5781-03 smallest year halfExpand"] = () =>
                A.Difference(Until, hebrewFrom, hebrewTo, O(("smallestUnit", "year"), ("roundingMode", "halfExpand"))),
            ["hebrew PlainYearMonth 5780-04 until 5781-03 (no rounding)"] = () => A.Difference(Until, hebrewFrom, hebrewTo, null),
            ["hebrew PlainDateTime 2020-01-01T00:00 until 2020-01-16T00:00 smallest month halfExpand"] = () =>
                A.Difference(Until, PDT("2020-01-01T00:00[u-ca=hebrew]"), "2020-01-16T00:00[u-ca=hebrew]", O(("smallestUnit", "month"), ("roundingMode", "halfExpand"))),
            ["hebrew PlainDate 2020-01-01 until 2020-01-16 smallest month halfExpand"] = () =>
                A.Difference(Until, PD("2020-01-01[u-ca=hebrew]"), "2020-01-16[u-ca=hebrew]", O(("smallestUnit", "month"), ("roundingMode", "halfExpand"))),
            ["hebrew PlainDateTime 2020-01-01T00:00 until 2020-01-16T00:00 smallest month largest year halfExpand"] = () =>
                A.Difference(Until, PDT("2020-01-01T00:00[u-ca=hebrew]"), "2020-01-16T00:00[u-ca=hebrew]",
                    O(("smallestUnit", "month"), ("largestUnit", "year"), ("roundingMode", "halfExpand"))),
        });
    }

    [Fact]
    public void F5_plain_date_time_differences_borrow_a_day_and_round_as_Broiler_VM_answers()
    {
        static TemporalAdapter.Duration DateTimeDiff(DifferenceOperation op, string one, string two, params (string, object?)[] options) =>
            A.Difference(op, PDT(one), two, O(options));

        Replay("f5-datetime-differences.vm.txt", "f70fc434f59e592b092ff789ae87b6ef2a6591a81780ca15f64a75818a11f671", new Dictionary<string, Func<object?>>
        {
            ["2020-01-01T12:00 until 2020-01-03T06:00"] = () => DateTimeDiff(Until, "2020-01-01T12:00", "2020-01-03T06:00"),
            ["2020-01-03T06:00 since 2020-01-01T12:00"] = () => DateTimeDiff(Since, "2020-01-03T06:00", "2020-01-01T12:00"),
            ["2020-01-03T06:00 until 2020-01-01T12:00"] = () => DateTimeDiff(Until, "2020-01-03T06:00", "2020-01-01T12:00"),
            ["2020-01-01T12:00 until 2020-01-03T06:00 largest hour"] = () => DateTimeDiff(Until, "2020-01-01T12:00", "2020-01-03T06:00", ("largestUnit", "hour")),
            ["2020-01-01T12:00 until 2020-01-03T06:00 smallest day halfExpand"] = () =>
                DateTimeDiff(Until, "2020-01-01T12:00", "2020-01-03T06:00", ("smallestUnit", "day"), ("roundingMode", "halfExpand")),
            ["2020-01-31T12:00 until 2020-03-01T06:00 largest month"] = () => DateTimeDiff(Until, "2020-01-31T12:00", "2020-03-01T06:00", ("largestUnit", "month")),
            ["2019-03-31T00:00 until 2020-02-29T12:00 month/year trunc"] = () =>
                DateTimeDiff(Until, "2019-03-31T00:00", "2020-02-29T12:00", ("smallestUnit", "month"), ("largestUnit", "year")),
            ["2019-03-31T00:00 until 2020-02-29T12:00 month/year halfExpand"] = () =>
                DateTimeDiff(Until, "2019-03-31T00:00", "2020-02-29T12:00", ("smallestUnit", "month"), ("largestUnit", "year"), ("roundingMode", "halfExpand")),
            ["2020-02-29T12:00 since 2019-03-31T00:00 month/year trunc"] = () =>
                DateTimeDiff(Since, "2020-02-29T12:00", "2019-03-31T00:00", ("smallestUnit", "month"), ("largestUnit", "year")),
            ["2019-01-01T00:00 until 2020-12-31T23:59:59.999999999 microsecond/year halfExpand"] = () =>
                DateTimeDiff(Until, "2019-01-01T00:00", "2020-12-31T23:59:59.999999999", ("smallestUnit", "microsecond"), ("largestUnit", "year"), ("roundingMode", "halfExpand")),
            ["2020-01-01T00:00 until 2020-01-01T23:59:59.5 second/day halfExpand"] = () =>
                DateTimeDiff(Until, "2020-01-01T00:00", "2020-01-01T23:59:59.5", ("smallestUnit", "second"), ("largestUnit", "day"), ("roundingMode", "halfExpand")),
            ["2020-01-01T00:00 until 2020-01-01T23:30 hour/day increment 3 ceil"] = () =>
                DateTimeDiff(Until, "2020-01-01T00:00", "2020-01-01T23:30", ("smallestUnit", "hour"), ("largestUnit", "day"), ("roundingIncrement", 3), ("roundingMode", "ceil")),
            ["2020-01-01T00:00 until 2020-01-08T12:00 week/week halfExpand"] = () =>
                DateTimeDiff(Until, "2020-01-01T00:00", "2020-01-08T12:00", ("smallestUnit", "week"), ("roundingMode", "halfExpand")),
            ["-271821-04-19T00:00:00.000000001 until +275760-09-13T23:59:59.999999999 largest nanosecond"] = () =>
                DateTimeDiff(Until, "-271821-04-19T00:00:00.000000001", "+275760-09-13T23:59:59.999999999", ("largestUnit", "nanosecond")),
            ["-271821-04-19T00:00:00.000000001 until +275760-09-13T23:59:59.999999999 largest year"] = () =>
                DateTimeDiff(Until, "-271821-04-19T00:00:00.000000001", "+275760-09-13T23:59:59.999999999", ("largestUnit", "year")),
        });
    }

    [Fact]
    public void F6_plain_time_and_instant_differences_answer_as_Broiler_VM()
    {
        Replay("f6-time-and-instant.vm.txt", "626eacb13cd50ada7c794304c7454b9e90a6f319370cc48fc0e1ff9ad9aadf2b", new Dictionary<string, Func<object?>>
        {
            ["PlainTime 10:00 until 08:30"] = () => A.Difference(Until, PT("10:00"), "08:30", null),
            ["PlainTime 10:00 since 08:29:31 minute halfExpand"] = () => A.Difference(Since, PT("10:00"), "08:29:31", O(("smallestUnit", "minute"), ("roundingMode", "halfExpand"))),
            ["PlainTime 10:00 since 08:29:31 largest minute"] = () => A.Difference(Since, PT("10:00"), "08:29:31", O(("largestUnit", "minute"))),
            ["PlainTime 08:29:31 since 10:00 minute floor"] = () => A.Difference(Since, PT("08:29:31"), "10:00", O(("smallestUnit", "minute"), ("roundingMode", "floor"))),
            ["PlainTime 00:00 until 23:59:59.999999999 hour increment 6 halfEven"] = () =>
                A.Difference(Until, PT("00:00"), "15:00", O(("smallestUnit", "hour"), ("roundingIncrement", 6), ("roundingMode", "halfEven"))),
            ["PlainTime 00:00 until 21:00 hour increment 6 halfEven"] = () =>
                A.Difference(Until, PT("00:00"), "21:00", O(("smallestUnit", "hour"), ("roundingIncrement", 6), ("roundingMode", "halfEven"))),
            ["PlainTime 00:00 until 23:59:59.999999999 largest nanosecond"] = () => A.Difference(Until, PT("00:00"), "23:59:59.999999999", O(("largestUnit", "nanosecond"))),
            ["Instant min until max largest hour"] = () => A.Difference(Until, I("-271821-04-20T00:00Z"), "+275760-09-13T00:00Z", O(("largestUnit", "hour"))),
            ["Instant min until max largest nanosecond"] = () => A.Difference(Until, I("-271821-04-20T00:00Z"), "+275760-09-13T00:00Z", O(("largestUnit", "nanosecond"))),
            ["Instant min until max largest microsecond"] = () =>
                A.Difference(Until, I("-271821-04-20T00:00Z"), "+275760-09-13T00:00Z", O(("largestUnit", "microsecond"))).Fields.Microseconds,
            ["Instant until 1 h 29 min hour halfExpand"] = () => A.Difference(Until, I("2020-01-01T00:00Z"), "2020-01-01T01:29Z", O(("smallestUnit", "hour"), ("roundingMode", "halfExpand"))),
            ["Instant since 1 h 30 min hour halfTrunc"] = () => A.Difference(Since, I("2020-01-01T01:30Z"), "2020-01-01T00:00Z", O(("smallestUnit", "hour"), ("roundingMode", "halfTrunc"))),
            ["Instant since -1 h 30 min hour halfTrunc"] = () => A.Difference(Since, I("2020-01-01T00:00Z"), "2020-01-01T01:30Z", O(("smallestUnit", "hour"), ("roundingMode", "halfTrunc"))),
            ["Instant since -1 h 30 min hour halfCeil"] = () => A.Difference(Since, I("2020-01-01T00:00Z"), "2020-01-01T01:30Z", O(("smallestUnit", "hour"), ("roundingMode", "halfCeil"))),
            ["Instant until 3 h 29 min, 30-minute increments, expand"] = () =>
                A.Difference(Until, I("2020-01-01T00:00Z"), "2020-01-01T03:29Z", O(("smallestUnit", "minute"), ("roundingIncrement", 30), ("roundingMode", "expand"), ("largestUnit", "hour"))),
        });
    }

    [Fact]
    public void F7_zoned_differences_use_real_day_lengths_as_Broiler_VM_answers()
    {
        const string NY = "[America/New_York]";
        static TemporalAdapter.Duration ZonedDiff(DifferenceOperation op, string one, string two, params (string, object?)[] options) =>
            A.Difference(op, Z(one), two, options.Length == 0 ? null : O(options));

        Replay("f7-zoned-differences.vm.txt", "409aa4b9b02f6baf8307c7a881a5489292269419ce9723ce343f0b194187d063", new Dictionary<string, Func<object?>>
        {
            ["2026-03-08T00:00-05:00 until 2026-03-09T00:00-04:00"] = () => ZonedDiff(Until, "2026-03-08T00:00-05:00" + NY, "2026-03-09T00:00-04:00" + NY),
            ["2026-03-08T00:00-05:00 until 2026-03-09T00:00-04:00 largest day"] = () => ZonedDiff(Until, "2026-03-08T00:00-05:00" + NY, "2026-03-09T00:00-04:00" + NY, ("largestUnit", "day")),
            ["2026-11-01T00:00-04:00 until 2026-11-02T00:00-05:00 largest day"] = () => ZonedDiff(Until, "2026-11-01T00:00-04:00" + NY, "2026-11-02T00:00-05:00" + NY, ("largestUnit", "day")),
            ["2026-03-07T12:00-05:00 until 2026-03-07T23:45-05:00 smallest day halfExpand"] = () =>
                ZonedDiff(Until, "2026-03-07T12:00-05:00" + NY, "2026-03-07T23:45-05:00" + NY, ("smallestUnit", "day"), ("roundingMode", "halfExpand")),
            ["2026-03-07T23:45-05:00 since 2026-03-07T12:00-05:00 smallest day halfExpand"] = () =>
                ZonedDiff(Since, "2026-03-07T23:45-05:00" + NY, "2026-03-07T12:00-05:00" + NY, ("smallestUnit", "day"), ("roundingMode", "halfExpand")),
            ["2026-10-31T12:00-04:00 until 2026-11-01T00:15-04:00 smallest day halfExpand"] = () =>
                ZonedDiff(Until, "2026-10-31T12:00-04:00" + NY, "2026-11-01T00:15-04:00" + NY, ("smallestUnit", "day"), ("roundingMode", "halfExpand")),
            ["2026-03-07T00:00-05:00 until 2026-03-08T11:45-04:00 smallest day halfExpand"] = () =>
                ZonedDiff(Until, "2026-03-07T00:00-05:00" + NY, "2026-03-08T11:45-04:00" + NY, ("smallestUnit", "day"), ("roundingMode", "halfExpand")),
            ["2026-03-07T12:00-05:00 until 2026-03-07T23:45-05:00 smallest day largest month halfExpand"] = () =>
                ZonedDiff(Until, "2026-03-07T12:00-05:00" + NY, "2026-03-07T23:45-05:00" + NY, ("smallestUnit", "day"), ("largestUnit", "month"), ("roundingMode", "halfExpand")),
            ["2026-03-07T12:00-05:00 until 2026-03-08T12:30-04:00 hour/day halfExpand"] = () =>
                ZonedDiff(Until, "2026-03-07T12:00-05:00" + NY, "2026-03-08T12:30-04:00" + NY, ("largestUnit", "day"), ("smallestUnit", "hour"), ("roundingMode", "halfExpand")),
            ["2026-03-07T12:00-05:00 until 2026-03-08T11:30-04:00 hour/day halfExpand"] = () =>
                ZonedDiff(Until, "2026-03-07T12:00-05:00" + NY, "2026-03-08T11:30-04:00" + NY, ("largestUnit", "day"), ("smallestUnit", "hour"), ("roundingMode", "halfExpand")),
            ["2026-02-07T12:00-05:00 until 2026-03-07T23:45-05:00 smallest week largest month halfExpand"] = () =>
                ZonedDiff(Until, "2026-02-07T12:00-05:00" + NY, "2026-03-14T23:45-04:00" + NY, ("smallestUnit", "week"), ("largestUnit", "month"), ("roundingMode", "halfExpand")),
            ["2019-03-31T00:00[UTC] until 2020-02-29T12:00[UTC] month/year trunc"] = () =>
                ZonedDiff(Until, "2019-03-31T00:00[UTC]", "2020-02-29T12:00[UTC]", ("smallestUnit", "month"), ("largestUnit", "year")),
            ["2019-03-31T00:00[UTC] until 2020-02-29T12:00[UTC] year/year trunc"] = () => ZonedDiff(Until, "2019-03-31T00:00[UTC]", "2020-02-29T12:00[UTC]", ("smallestUnit", "year")),
            ["2026-03-08T01:30-05:00 until 2026-03-08T03:30-04:00 largest day"] = () => ZonedDiff(Until, "2026-03-08T01:30-05:00" + NY, "2026-03-08T03:30-04:00" + NY, ("largestUnit", "day")),
            ["2026-11-01T01:30-04:00 until 2026-11-01T01:30-05:00 largest day"] = () => ZonedDiff(Until, "2026-11-01T01:30-04:00" + NY, "2026-11-01T01:30-05:00" + NY, ("largestUnit", "day")),
            ["2026-11-01T01:45-04:00 until 2026-11-02T01:15-05:00 largest day"] = () => ZonedDiff(Until, "2026-11-01T01:45-04:00" + NY, "2026-11-02T01:15-05:00" + NY, ("largestUnit", "day")),
            ["2026-11-02T01:15-05:00 since 2026-11-01T01:45-04:00 largest day"] = () => ZonedDiff(Since, "2026-11-02T01:15-05:00" + NY, "2026-11-01T01:45-04:00" + NY, ("largestUnit", "day")),
            ["ZonedDateTime.round smallest day, NY 2026-03-08T12:00-04:00"] = () => A.Round(Z("2026-03-08T12:00-04:00" + NY), O(("smallestUnit", "day"))),
            ["ZonedDateTime.round smallest day, NY 2026-03-08T11:29-04:00"] = () => A.Round(Z("2026-03-08T11:29-04:00" + NY), O(("smallestUnit", "day"))),
            ["ZonedDateTime.round smallest day, NY 2026-11-01T12:29-05:00"] = () => A.Round(Z("2026-11-01T12:29-05:00" + NY), O(("smallestUnit", "day"))),
            ["ZonedDateTime.hoursInDay NY 2026-03-08 and 2026-11-01"] = () =>
                Show(A.HoursInDay(Z("2026-03-08T12:00-04:00" + NY))) + " " + Show(A.HoursInDay(Z("2026-11-01T12:00-05:00" + NY))),
        });
    }

    [Fact]
    public void F8_year_month_differences_count_from_the_months_first_days_as_Broiler_VM_answers()
    {
        static TemporalAdapter.Duration YearMonthDiff(DifferenceOperation op, string one, object? two, params (string, object?)[] options) =>
            A.Difference(op, PYM(one), two, options.Length == 0 ? null : O(options));

        Replay("f8-yearmonth-differences.vm.txt", "14f8eb93963bf12c2614bcfe5783038f37e3bb4ec4f630c6d8b0da690c637bea", new Dictionary<string, Func<object?>>
        {
            ["2019-05 until 2021-03 largest month"] = () => YearMonthDiff(Until, "2019-05", "2021-03", ("largestUnit", "month")),
            ["2019-05 until 2021-03 smallest year halfExpand"] = () => YearMonthDiff(Until, "2019-05", "2021-03", ("smallestUnit", "year"), ("roundingMode", "halfExpand")),
            ["2021-03 since 2019-05 smallest year halfExpand"] = () => YearMonthDiff(Since, "2021-03", "2019-05", ("smallestUnit", "year"), ("roundingMode", "halfExpand")),
            ["2019-05 since 2021-03 smallest year floor"] = () => YearMonthDiff(Since, "2019-05", "2021-03", ("smallestUnit", "year"), ("roundingMode", "floor")),
            ["2019-05 until 2021-03 smallest month increment 5 halfExpand"] = () =>
                YearMonthDiff(Until, "2019-05", "2021-03", ("smallestUnit", "month"), ("roundingIncrement", 5), ("roundingMode", "halfExpand")),
            ["-271821-04 until -271821-05"] = () => YearMonthDiff(Until, "-271821-04", "-271821-05"),
            ["-271821-05 until -271821-04"] = () => YearMonthDiff(Until, "-271821-05", "-271821-04"),
            ["-271821-04 until -271821-04"] = () => YearMonthDiff(Until, "-271821-04", "-271821-04"),
            ["+275760-09 since +275760-08"] = () => YearMonthDiff(Since, "+275760-09", "+275760-08"),
            ["+275760-08 until +275760-09 smallest year ceil"] = () => YearMonthDiff(Until, "+275760-08", "+275760-09", ("smallestUnit", "year"), ("roundingMode", "ceil")),
            ["2019-05 until 2021-03 other calendar"] = () =>
                YearMonthDiff(Until, "2019-05", A.DateToPlainYearMonth(TemporalAdapter.WithCalendar(A.YearMonthToPlainDate(PYM("2021-03"), Bag(("day", 1))), "gregory"))),
        });
    }

    // ---- R7 and B5, where they add a duration ---------------------------------------------------------------------

    [Fact]
    public void R7_narrowing_inside_addition_refuses_as_Broiler_VM_answers()
    {
        Replay("r7-narrowing-in-addition.vm.txt", "23b4b3080ce92266a519cb2f183046ba509fb095ebd81eb92cd2e794fd1107ec", new Dictionary<string, Func<object?>>
        {
            ["PlainDateTime(\"2020-06-15T12:00\").add({years: 4294967295})"] = () => A.Add(PDT("2020-06-15T12:00"), Bag(("years", 4294967295.0)), null, false),
            ["PlainDateTime(\"2020-06-15T12:00\").subtract({years: 4294967295})"] = () => A.Add(PDT("2020-06-15T12:00"), Bag(("years", 4294967295.0)), null, true),
            ["PlainDateTime(\"2020-06-15T12:00\").add({years: 4294967295 - 2020 - 1000})"] = () => A.Add(PDT("2020-06-15T12:00"), Bag(("years", 4294967295.0 - 2020 - 1000)), null, false),
            ["PlainDateTime(\"2020-06-15T12:00\").add({months: 4294967295})"] = () => A.Add(PDT("2020-06-15T12:00"), Bag(("months", 4294967295.0)), null, false),
            ["PlainYearMonth(\"2020-06\").add({years: 4294967295})"] = () => A.Add(PYM("2020-06"), Bag(("years", 4294967295.0)), null, false),
            ["PlainYearMonth(\"2020-06\").subtract({years: 4294967295})"] = () => A.Add(PYM("2020-06"), Bag(("years", 4294967295.0)), null, true),
            ["PlainDate(\"2020-06-15\").add({years: 4294967295})"] = () => A.Add(PD("2020-06-15"), Bag(("years", 4294967295.0)), null, false),
            ["ZonedDateTime(\"2020-06-15T12:00[UTC]\").add({years: 4294967295})"] = () => A.Add(Z("2020-06-15T12:00[UTC]"), Bag(("years", 4294967295.0)), null, false),
            ["PlainDate(\"2020-06-15[u-ca=hebrew]\").add({years: 4294967295})"] = () => A.Add(PD("2020-06-15[u-ca=hebrew]"), Bag(("years", 4294967295.0)), null, false),
            ["PlainDate(\"2020-06-15[u-ca=coptic]\").subtract({years: 4294967295})"] = () => A.Add(PD("2020-06-15[u-ca=coptic]"), Bag(("years", 4294967295.0)), null, true),
            ["PlainDate(\"2020-01-01\").add({nanoseconds: 9e24})"] = () => A.Add(PD("2020-01-01"), Bag(("nanoseconds", 9e24)), null, false),
            ["PlainDate(\"2020-01-01\").add({microseconds: 9e21})"] = () => A.Add(PD("2020-01-01"), Bag(("microseconds", 9e21)), null, false),
            ["PlainDate(\"2020-01-01\").add({milliseconds: 9e18})"] = () => A.Add(PD("2020-01-01"), Bag(("milliseconds", 9e18)), null, false),
            ["PlainDateTime(\"2020-01-01T00:00\").add({nanoseconds: 9e24})"] = () => A.Add(PDT("2020-01-01T00:00"), Bag(("nanoseconds", 9e24)), null, false),
            ["PlainDateTime(\"2020-01-01T00:00\").add({microseconds: 9e21})"] = () => A.Add(PDT("2020-01-01T00:00"), Bag(("microseconds", 9e21)), null, false),
            ["ZonedDateTime(\"2020-01-01T00:00[UTC]\").add({nanoseconds: 9e24})"] = () => A.Add(Z("2020-01-01T00:00[UTC]"), Bag(("nanoseconds", 9e24)), null, false),
            ["Duration({nanoseconds: 9e24}).round({smallestUnit: \"second\", relativeTo: \"2020-01-01\"})"] = () =>
                Round(D(("nanoseconds", 9e24)), ("smallestUnit", "second"), ("relativeTo", "2020-01-01")),
            ["Duration({nanoseconds: 9e24}).round({largestUnit: \"year\", relativeTo: \"2020-01-01\"})"] = () =>
                Round(D(("nanoseconds", 9e24)), ("largestUnit", "year"), ("relativeTo", "2020-01-01")),
            ["Duration({nanoseconds: 9e24}).total({unit: \"day\", relativeTo: \"2020-01-01\"})"] = () => Total(D(("nanoseconds", 9e24)), ("unit", "day"), ("relativeTo", "2020-01-01")),
            ["Duration({nanoseconds: 9e24}).total(\"day\")"] = () => A.Total(D(("nanoseconds", 9e24)), "day"),
        });
    }

    [Fact]
    public void B5_addition_at_the_limits_answers_as_Broiler_VM()
    {
        Replay("b5-limits-in-addition.vm.txt", "3a790c0bc97e3f72340440c24103d82334067034627ad2482f16a3f2da1cbff2", new Dictionary<string, Func<object?>>
        {
            ["PlainDate(\"+275760-09-13\").add({days: 1})"] = () => A.Add(PD("+275760-09-13"), Bag(("days", 1)), null, false),
            ["PlainDate(\"-271821-04-19\").subtract({days: 1})"] = () => A.Add(PD("-271821-04-19"), Bag(("days", 1)), null, true),
            ["PlainDate(\"1970-01-01\").add({days: 100000000})"] = () => A.Add(PD("1970-01-01"), Bag(("days", 100000000)), null, false),
            ["PlainDate(\"1970-01-01\").add({days: 100000001})"] = () => A.Add(PD("1970-01-01"), Bag(("days", 100000001)), null, false),
            ["PlainDate(\"-271821-04-19\").add({days: 0})"] = () => A.Add(PD("-271821-04-19"), Bag(("days", 0)), null, false),
            ["PlainDateTime(\"+275760-09-13T00:00\").add({hours: 23, minutes: 59})"] = () => A.Add(PDT("+275760-09-13T00:00"), Bag(("hours", 23), ("minutes", 59)), null, false),
            ["PlainDateTime(\"+275760-09-13T00:00\").add({days: 1})"] = () => A.Add(PDT("+275760-09-13T00:00"), Bag(("days", 1)), null, false),
            ["PlainDateTime(\"-271821-04-19T00:00:00.000000001\").subtract({nanoseconds: 1})"] = () => A.Add(PDT("-271821-04-19T00:00:00.000000001"), Bag(("nanoseconds", 1)), null, true),
            ["PlainYearMonth(\"+275760-09\").add({months: 1})"] = () => A.Add(PYM("+275760-09"), Bag(("months", 1)), null, false),
            ["PlainYearMonth(\"-271821-04\").add({months: 0})"] = () => A.Add(PYM("-271821-04"), Bag(("months", 0)), null, false),
            ["PlainYearMonth(\"-271821-05\").subtract({months: 1})"] = () => A.Add(PYM("-271821-05"), Bag(("months", 1)), null, true),
            ["PlainYearMonth(\"+275760-08\").add({months: 1})"] = () => A.Add(PYM("+275760-08"), Bag(("months", 1)), null, false),
            ["Instant(\"+275760-09-13T00:00Z\").add({nanoseconds: 1})"] = () => A.Add(I("+275760-09-13T00:00Z"), Bag(("nanoseconds", 1)), false),
            ["ZonedDateTime(max, \"UTC\").add({nanoseconds: 1})"] = () =>
                A.Add(A.NewZonedDateTime(BigInteger.Parse("8640000000000000000000"), "UTC"), Bag(("nanoseconds", 1)), null, false),
            ["ZonedDateTime(max - 1n, \"UTC\").add({days: 0, nanoseconds: 1})"] = () =>
                A.Add(A.NewZonedDateTime(BigInteger.Parse("8639999999999999999999"), "UTC"), Bag(("nanoseconds", 1)), null, false),
            ["ZonedDateTime(\"+275760-09-12T12:00[UTC]\").add({days: 1})"] = () => A.Add(Z("+275760-09-12T12:00[UTC]"), Bag(("days", 1)), null, false),
            ["ZonedDateTime(\"+275760-09-12T00:00[UTC]\").add({days: 1})"] = () => A.Add(Z("+275760-09-12T00:00[UTC]"), Bag(("days", 1)), null, false),
        });
    }
}
