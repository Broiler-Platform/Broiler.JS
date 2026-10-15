using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics;
using Broiler.ECMAScript.Time;
using V = Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Date.V;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Date;

/// <summary>
/// The operations of bundle sjl-6-001's reference-arm rule, as its revision of 2026-10-15 reads them, and for
/// each the shared implementation (arm S) and the arms' variants.
/// </summary>
/// <remarks>
/// <para>
/// <b>An arm is called as its engine calls the member, and S as that engine will call S after its cut-over</b>:
/// a parse is projected onto the time value the engine makes of it (a local time through the engine's
/// <c>UTC</c>, then <c>TimeClip</c>), Broiler.VM's with the one unit its adapter charges added to S's meter;
/// a rendering that an engine composes differently (Broiler.JS's <c>ToTimeZoneString</c> writes the "GMT" that
/// the specification puts in <c>TimeString</c>) is compared with S composed the same way.
/// </para>
/// <para>
/// <b>Local time.</b> Broiler.VM's local time is UTC by omission: it has no <c>LocalTime</c> or <c>UTC</c>
/// member, so arm V runs no E4 operation, and its parse and renderings are compared with S under a provider at
/// offset zero named "Coordinated Universal Time". Arm J reads the host's <see cref="TimeZoneInfo"/>, and S
/// reads the same zone through <see cref="HostZoneProvider"/>; the process's own zone is UTC.
/// </para>
/// </remarks>
internal static partial class DateArms
{
    public const double MaxTime = 8.64e15;

    public static readonly FixedOffsetLocalTimeProvider VmZone = new(0, "Coordinated Universal Time");

    /// <summary>The operations whose results are strings, which the rule's culture runs repeat for arm J.</summary>
    public static IReadOnlyList<(string Arm, string Operation, string Reason)> NotImplemented { get; } =
    [
        ("J,V", "the field operations over NaN and the infinities", "no engine calls them there: every getter answers NaN for an invalid date first; S runs over them alone"),
        ("J", "InLeapYear", "JSDateMath has no member: the leap test is written inline (correspondence section 1)"),
        ("V", "LocalTime and UTC", "Broiler.VM's local time is UTC by omission, with no LocalTime or UTC member (correspondence section 3)"),
        ("V", "TimeString, TimeZoneString and toString at any offset or name but zero and its own", "its zone text is the constant DateZoneText"),
        ("J", "TimeZoneString and toString with no name", "Broiler.JS always writes the host zone's name"),
        ("J", "parse, outside the format and its own renderings", "the rest of its fallback stays in Broiler.JS (decision SJL-D-17)"),
        ("V", "the renderings of NaN but toString's", "Broiler.VM's methods answer Invalid Date or throw before calling DateToCalendarText, DateToUtcText or DateToIsoText"),
        ("J", "BalanceISOYearMonth", "no member of the nine civil copies balances a year and a month"),
    ];

    /// <summary>E1's time values, E6's offsets and names, loaded once before the run.</summary>
    public static IReadOnlyList<double> TimeValues { get; private set; } = [];

    public static IReadOnlyList<double> Offsets { get; private set; } = [];

    public static IReadOnlyList<string?> Names { get; private set; } = [];

    public static void Load(DatePopulation population)
    {
        TimeValues = population.ReadLines("E1T").Select(NumericsPopulation.Double).ToList();
        Offsets = population.ReadLines("E6O").Select(NumericsPopulation.Double).ToList();
        Names = population.ReadLines("E6N").Select(static l => NumericsPopulation.Unescape(l)).Select(static n => n.Length == 0 ? null : n).ToList();
    }

    /// <summary>The operations; under a culture run, arm J's string operations alone.</summary>
    public static IReadOnlyList<DateOperation> Operations(bool cultureRun)
    {
        var all = Build();
        if (!cultureRun)
            return all;

        return all.Where(static o => o.StringOperation)
            .Select(static o => o with { Variants = o.Variants.Where(static v => v.Arm.StartsWith('J')).ToArray(), SharedOnly = null })
            .Where(static o => o.Variants.Length > 0)
            .ToList();
    }

    // ---------------------------------------------------------------------------------------------
    // Helpers.

    private static IEnumerable<DateInput> Once(DateInput input) => [input];

    /// <summary>A time value: finite, integral, within ±8.64e15, and not -0.</summary>
    public static bool IsTimeValue(double t) =>
        double.IsFinite(t) && Math.Floor(t) == t && Math.Abs(t) <= MaxTime && !(t == 0 && double.IsNegative(t));

    private static string Num(double value) => DateRender.Num(value);

    private static readonly Dictionary<string, TimeZoneInfo> Zones = new(StringComparer.Ordinal);

    public static TimeZoneInfo Zone(string id)
    {
        lock (Zones)
        {
            if (!Zones.TryGetValue(id, out var zone))
                Zones[id] = zone = TimeZoneInfo.FindSystemTimeZoneById(id);
            return zone;
        }
    }

    public static HostZoneProvider Provider(string id) => new(Zone(id));

    // ---------------------------------------------------------------------------------------------

    private static List<DateOperation> Build()
    {
        var operations = new List<DateOperation>();
        operations.AddRange(ArithmeticOperations());
        operations.AddRange(CivilOperations());
        operations.AddRange(LocalTimeOperations());
        operations.AddRange(ParseOperations());
        operations.AddRange(RenderingOperations());
        return operations;
    }

    // ---- Time arithmetic -----------------------------------------------------------------------

    private static bool Finite(DateInput i) => double.IsFinite(i.Number);

    private static DateOperation Field(string name, Func<double, double> s, Func<double, string>? j, Func<double, double>? v)
    {
        var variants = new List<DateVariant>();
        if (j is not null)
            variants.Add(new("J", "JSDateMath", i => j(i.Number), i => Num(s(i.Number)), Finite));
        if (v is not null)
            variants.Add(new("V", "JsRealm.Date", i => Num(v(i.Number)), i => Num(s(i.Number)), Finite));
        return new(name, ["E1T"], Once, variants.ToArray(), SharedOnly: i => Num(s(i.Number)));
    }

    private static IEnumerable<DateOperation> ArithmeticOperations()
    {
        yield return Field("Day", EcmaTime.Day, JDay, V.JsRealmDate.DateDay);
        yield return Field("TimeWithinDay", EcmaTime.TimeWithinDay, JTimeWithinDay, V.JsRealmDate.DateTimeWithinDay);
        yield return Field("YearFromTime", EcmaTime.YearFromTime, JYearFromTime, V.JsRealmDate.DateYearFromTime);
        yield return Field("InLeapYear", EcmaTime.InLeapYear, null, static t => V.JsRealmDate.DateInLeapYear(t));
        yield return Field("MonthFromTime", EcmaTime.MonthFromTime, JMonthFromTime, V.JsRealmDate.DateMonthFromTime);
        yield return Field("DateFromTime", EcmaTime.DateFromTime, JDateFromTime, V.JsRealmDate.DateDateFromTime);
        yield return Field("WeekDay", EcmaTime.WeekDay, JWeekDay, V.JsRealmDate.DateWeekDay);
        yield return Field("HourFromTime", EcmaTime.HourFromTime, JHourFromTime, V.JsRealmDate.DateHourFromTime);
        yield return Field("MinFromTime", EcmaTime.MinFromTime, JMinFromTime, V.JsRealmDate.DateMinFromTime);
        yield return Field("SecFromTime", EcmaTime.SecFromTime, JSecFromTime, V.JsRealmDate.DateSecFromTime);
        yield return Field("msFromTime", EcmaTime.MsFromTime, JMsFromTime, V.JsRealmDate.DateMsFromTime);

        // DaysInYear and DayFromYear take a year: E3's years, as Numbers.
        yield return new("DaysInYear", ["E3Y"], Once,
        [
            new("J", "JSDateMath", i => JDaysInYear(i.Year), i => Num(EcmaTime.DaysInYear(i.Year))),
            new("V", "JsRealm.Date", i => Num(V.JsRealmDate.DateDaysInYear(i.Year)), i => Num(EcmaTime.DaysInYear(i.Year))),
        ]);
        yield return new("DayFromYear", ["E3Y"], Once,
        [
            new("J", "JSDateMath", i => JDayFromYear(i.Year), i => Num(EcmaTime.DayFromYear(i.Year))),
            new("V", "JsRealm.Date", i => Num(V.JsRealmDate.DateDayFromYear(i.Year)), i => Num(EcmaTime.DayFromYear(i.Year))),
        ]);

        static double C(DateInput i, int at) => i.Numbers[at];

        yield return new("MakeTime", ["E2C"], Once,
        [
            new("J", "JSDateMath", i => JMakeTime(C(i, 3), C(i, 4), C(i, 5), C(i, 6)), i => Num(EcmaTime.MakeTime(C(i, 3), C(i, 4), C(i, 5), C(i, 6)))),
            new("V", "JsRealm.Date", i => Num(V.JsRealmDate.DateMakeTime(C(i, 3), C(i, 4), C(i, 5), C(i, 6))), i => Num(EcmaTime.MakeTime(C(i, 3), C(i, 4), C(i, 5), C(i, 6)))),
        ]);
        yield return new("MakeFullYear", ["E2C"], Once,
        [
            new("J", "Date.UTC inline", i => JMakeFullYear(C(i, 0)), i => Num(EcmaTime.MakeFullYear(C(i, 0)))),
            new("V", "DateFromFields inline", i => Num(V.JsRealmDate.DateMakeFullYear(C(i, 0))), i => Num(EcmaTime.MakeFullYear(C(i, 0)))),
        ]);
        yield return new("MakeDay", ["E2C"], Once,
        [
            new("J", "JSDateMath", i => JMakeDay(C(i, 0), C(i, 1), C(i, 2)), i => Num(EcmaTime.MakeDay(C(i, 0), C(i, 1), C(i, 2))), static i => i.Flag),
            new("V", "JsRealm.Date", i => Num(V.JsRealmDate.DateMakeDay(C(i, 0), C(i, 1), C(i, 2))), i => Num(EcmaTime.MakeDay(C(i, 0), C(i, 1), C(i, 2))), static i => i.Flag),
        ], SharedOnly: i => Num(EcmaTime.MakeDay(C(i, 0), C(i, 1), C(i, 2))));
        yield return new("MakeDate", ["E2M"], Once,
        [
            new("J", "JSDateMath", i => JMakeDate(C(i, 0), C(i, 1)), i => Num(EcmaTime.MakeDate(C(i, 0), C(i, 1)))),
            new("V", "JsRealm.Date", i => Num(V.JsRealmDate.DateMakeDate(C(i, 0), C(i, 1))), i => Num(EcmaTime.MakeDate(C(i, 0), C(i, 1)))),
        ]);
        yield return new("TimeClip", ["E1T"], Once,
        [
            new("J", "JSDateMath", i => JTimeClip(i.Number), i => Num(EcmaTime.TimeClip(i.Number))),
            new("V", "JsRealm.Date", i => Num(V.JsRealmDate.DateTimeClip(i.Number)), i => Num(EcmaTime.TimeClip(i.Number))),
        ]);

        static double SCore(DateInput i) =>
            EcmaTime.TimeClip(EcmaTime.MakeDate(EcmaTime.MakeDay(C(i, 0), C(i, 1), C(i, 2)), EcmaTime.MakeTime(C(i, 3), C(i, 4), C(i, 5), C(i, 6))));

        yield return new("Date.UTC's core", ["E2C"], Once,
        [
            new("J", "JSDateMath", i => JCore(i.Numbers), i => Num(SCore(i))),
            new("V", "JsRealm.Date", i => Num(V.JsRealmDate.DateTimeClip(V.JsRealmDate.DateMakeDate(
                V.JsRealmDate.DateMakeDay(C(i, 0), C(i, 1), C(i, 2)), V.JsRealmDate.DateMakeTime(C(i, 3), C(i, 4), C(i, 5), C(i, 6))))), i => Num(SCore(i))),
        ]);
    }

    // ---- Civil-day helpers ---------------------------------------------------------------------

    private static bool Valid(DateInput i) => i.Flag;

    private static IEnumerable<DateInput> Months(DateInput input) =>
        Enumerable.Range(1, 12).Select(m => input with { Month = m, Detail = "month=" + m.ToString(CultureInfo.InvariantCulture) });

    private static IEnumerable<DateOperation> CivilOperations()
    {
        static string SDate(long y, int m, int d) => DateRender.Civil(y, m, d);

        yield return new("the leap year", ["E3Y"], Once,
        [
            new("V", "JsTemporalCore", i => DateRender.Bool(V.JsTemporalCore.IsLeapYear(i.Year)), i => DateRender.Bool(IsoCalendarMath.IsLeapYear(i.Year))),
            .. JLeapYear(i => DateRender.Bool(IsoCalendarMath.IsLeapYear(i.Year))),
        ]);
        yield return new("the days in a year", ["E3Y"], Once,
        [
            new("V", "JsTemporalCore", i => Num(V.JsTemporalCore.DaysInYear(i.Year)), i => Num(IsoCalendarMath.DaysInYear(i.Year))),
            .. JDaysInIsoYear(i => Num(IsoCalendarMath.DaysInYear(i.Year))),
        ]);
        yield return new("the month length", ["E3Y"], Months,
        [
            new("V", "JsTemporalCore", i => Num(V.JsTemporalCore.DaysInMonth(i.Year, i.Month)), i => Num(IsoCalendarMath.DaysInMonth(i.Year, i.Month))),
            new("V", "JsZone", i => Num(V.JsZone.MonthLength((int)i.Year, i.Month)), i => Num(IsoCalendarMath.DaysInMonth(i.Year, i.Month)), static i => i.Year is >= int.MinValue and <= int.MaxValue),
            .. JMonthLength(i => Num(IsoCalendarMath.DaysInMonth(i.Year, i.Month))),
        ]);
        yield return new("epoch days from a date", ["E3D"], Once,
        [
            new("V", "JsTemporalCore", i => Num(V.JsTemporalCore.EpochDays(i.Year, i.Month, i.Day)), i => Num(IsoCalendarMath.EpochDays(i.Year, i.Month, i.Day)), Valid),
            new("V", "JsZone", i => Num(V.JsZone.DaysFromCivil(i.Year, i.Month, i.Day)), i => Num(IsoCalendarMath.EpochDays(i.Year, i.Month, i.Day)), Valid),
            .. JDaysFromCivil(i => Num(IsoCalendarMath.EpochDays(i.Year, i.Month, i.Day))),
        ]);
        yield return new("a date from epoch days", ["E3E"], Once,
        [
            new("V", "JsTemporalCore", i => { var d = V.JsTemporalCore.FromEpochDays(i.Year); return SDate(d.Year, d.Month, d.Day); },
                i => { var d = IsoCalendarMath.FromEpochDays(i.Year); return SDate(d.Year, d.Month, d.Day); }),
            .. JCivilFromDays(i => { var d = IsoCalendarMath.FromEpochDays(i.Year); return SDate(d.Year, d.Month, d.Day); }),
        ]);
        yield return new("the weekday", ["E3D"], Once,
        [
            new("V", "JsTemporalCore", i => Num(V.JsTemporalCore.DayOfWeek(new V.JsIsoDate(i.Year, i.Month, i.Day))), i => Num(IsoCalendarMath.DayOfWeek(i.Year, i.Month, i.Day)), Valid),
            new("V", "JsZone (Monday 0)", i => Num(V.JsZone.Weekday(V.JsZone.DaysFromCivil(i.Year, i.Month, i.Day))), i => Num(IsoCalendarMath.DayOfWeek(i.Year, i.Month, i.Day) - 1), Valid),
            .. JDayOfWeek(i => Num(IsoCalendarMath.DayOfWeek(i.Year, i.Month, i.Day))),
        ]);
        yield return new("the day of the year", ["E3D"], Once,
        [
            new("V", "JsTemporalCore", i => Num(V.JsTemporalCore.DayOfYear(new V.JsIsoDate(i.Year, i.Month, i.Day))), i => Num(IsoCalendarMath.DayOfYear(i.Year, i.Month, i.Day)), Valid),
            .. JDayOfYear(i => Num(IsoCalendarMath.DayOfYear(i.Year, i.Month, i.Day))),
        ]);
        yield return new("the ISO week and its year", ["E3D"], Once,
        [
            new("V", "JsTemporalCore", i => { var w = V.JsTemporalCore.WeekOfYear(new V.JsIsoDate(i.Year, i.Month, i.Day)); return DateRender.Pair(w.Week, w.Year); },
                i => { var w = IsoCalendarMath.WeekOfYear(i.Year, i.Month, i.Day); return DateRender.Pair(w.Week, w.Year); }, Valid),
            .. JIsoWeek(i => { var w = IsoCalendarMath.WeekOfYear(i.Year, i.Month, i.Day); return DateRender.Pair(w.Week, w.Year); }),
        ]);
        yield return new("BalanceISOYearMonth", ["E3B"], Once,
        [
            new("V", "JsTemporalCore", i => { var b = V.JsTemporalCore.BalanceYearMonth(i.Year, i.Long); return DateRender.Pair(b.Year, b.Month); },
                i => { var b = IsoCalendarMath.BalanceYearMonth(i.Year, i.Long); return DateRender.Pair(b.Year, b.Month); }),
        ]);
        yield return new("the ISO date's validity", ["E3D"], Once,
        [
            new("V", "JsTemporalCore", i => DateRender.Bool(V.JsTemporalCore.IsValidIsoDate(i.Year, i.Month, i.Day)), i => DateRender.Bool(IsoCalendarMath.IsValidIsoDate(i.Year, i.Month, i.Day))),
            .. JValidity(i => DateRender.Bool(IsoCalendarMath.IsValidIsoDate(i.Year, i.Month, i.Day))),
        ]);

        // JsLocalTime.From, the civil arithmetic of Broiler.VM's date-time formatter, over E1's time values at E6's
        // offsets (whole seconds): its fields against S's fields of the local time.
        yield return new("a local time's civil fields", ["E1T"], static i => IsTimeValue(i.Number)
                ? Offsets.Select(o => i with { Offset = o, Detail = "offset=" + o.ToString("R", CultureInfo.InvariantCulture) })
                : [],
        [
            new("V", "JsLocalTime.From", i => VLocalFields(i.Number, i.Offset), i => SLocalFields(i.Number + i.Offset)),
        ]);
    }

    private static string VLocalFields(double t, double offset)
    {
        var f = V.JsLocalTime.From(t, (int)(offset / 1000));
        return string.Join(",", new long[] { f.Year, f.Month, f.Day, f.Weekday, f.Hour, f.Minute, f.Second, f.Millisecond, f.DayOfYear }.Select(static x => x.ToString(CultureInfo.InvariantCulture)));
    }

    private static string SLocalFields(double local) =>
        string.Join(",", new[]
        {
            EcmaTime.YearFromTime(local), EcmaTime.MonthFromTime(local) + 1, EcmaTime.DateFromTime(local), EcmaTime.WeekDay(local),
            EcmaTime.HourFromTime(local), EcmaTime.MinFromTime(local), EcmaTime.SecFromTime(local), EcmaTime.MsFromTime(local),
            EcmaTime.DayWithinYear(local) + 1,
        }.Select(static x => ((long)x).ToString(CultureInfo.InvariantCulture)));

    // ---- Local time ----------------------------------------------------------------------------

    private static IEnumerable<DateInput> WithOffsets(DateInput input) =>
        [input with { Detail = Provider(input.Zone).Offsets(input.Kind, input.Number) }];

    private static IEnumerable<DateOperation> LocalTimeOperations()
    {
        yield return new("LocalTime", ["E4L"], i => i.Kind == 'u' ? WithOffsets(i) : [],
        [
            new("J", "JSDateMath", i => JLocalTime(i.Number, Zone(i.Zone)), i => Num(EcmaLocalTime.LocalTime(i.Number, Provider(i.Zone)))),
        ]);
        yield return new("UTC", ["E4L"], i => i.Kind == 'l' ? WithOffsets(i) : [],
        [
            new("J", "JSDateMath", i => JUtc(i.Number, Zone(i.Zone)), i => Num(EcmaLocalTime.Utc(i.Number, Provider(i.Zone)))),
        ]);
    }

    // ---- Parsing -------------------------------------------------------------------------------

    /// <summary>A parse as Broiler.VM's adapter will call S: the dialect, its meter, one unit more, and UTC as local time.</summary>
    public static string SVmParse(string text, DateParseDialect dialect)
    {
        var meter = new Meter();
        var parsed = EcmaDateParser.Parse(text, dialect, meter.Charge);
        var value = parsed.Kind switch
        {
            EcmaParsedKind.Utc => parsed.Value,
            EcmaParsedKind.Local => EcmaTime.TimeClip(EcmaLocalTime.Utc(parsed.Value, VmZone)),
            _ => double.NaN,
        };
        return Render.Charged(Num(value), meter.Total + 1);
    }

    public static string VParse(string text)
    {
        var engine = new V.JsEngine();
        var value = V.JsRealmDate.DateParseText(engine, text);
        return Render.Charged(Num(value), engine.Charged);
    }

    /// <summary>A parse as Broiler.JS will call S: the strict dialect, then (after its own fallback, which stays) Broiler.VM's, local time the process zone's.</summary>
    public static double SJsParse(string text)
    {
        var parsed = EcmaDateParser.Parse(text, DateParseDialect.Strict);
        if (parsed.Kind == EcmaParsedKind.Invalid)
            parsed = EcmaDateParser.Parse(text, DateParseDialect.BroilerVM);

        return parsed.Kind switch
        {
            EcmaParsedKind.Utc => parsed.Value,
            EcmaParsedKind.Local => EcmaTime.TimeClip(EcmaLocalTime.Utc(parsed.Value, Provider(TimeZoneInfo.Local.Id))),
            _ => double.NaN,
        };
    }

    private static bool StartsLikeTheFormat(string text) =>
        text.Length > 0 && (text[0] is '+' or '-' || (text.Length >= 4 && text.AsSpan(0, 4).IndexOfAnyExceptInRange('0', '9') < 0));

    /// <summary>Each engine's renderings of E1's time values, built from the arms: the strings E5 reads back.</summary>
    private static IEnumerable<DateInput> Renderings(DateInput input)
    {
        if (!IsTimeValue(input.Number))
            yield break;

        var t = input.Number;
        var tv = "tv=" + Render.F64(t);
        yield return input with { Text = V.JsRealmDate.DateToFullText(t), Source = "V", Detail = tv + " V:toString" };
        yield return input with { Text = V.JsRealmDate.DateToUtcText(t), Source = "V", Detail = tv + " V:toUTCString" };
        foreach (var rendering in JRenderings(t))
            yield return input with { Text = rendering.Text, Source = "J", Detail = tv + " J:" + rendering.Form };
    }

    private static IEnumerable<DateInput> Strings(DateInput input) => [input with { Detail = "-" }];

    /// <summary>
    /// A string for Broiler.VM's dialect: when it holds a token of a minus sign and digits (correspondence row
    /// P9's shape), the listing also carries S's reading of it without those tokens, which Broiler.VM's reader
    /// skips, so that the classifier can tell a difference P9 makes from any other.
    /// </summary>
    private static IEnumerable<DateInput> VmStrings(DateInput input)
    {
        var unsigned = WithoutSignedYears(input.Text);
        return [input with { Detail = unsigned is null ? "-" : "unsigned=" + SVmParse(unsigned, DateParseDialect.BroilerVM).Split('|')[0] }];
    }

    /// <summary>The string with every token of a minus sign and digits removed, its separators kept; null when it has none.</summary>
    public static string? WithoutSignedYears(string text)
    {
        static bool Separator(char c) => c is ' ' or ',' or '\t';
        var kept = new System.Text.StringBuilder(text.Length);
        var removed = false;
        var at = 0;
        while (at < text.Length)
        {
            if (Separator(text[at]))
            {
                kept.Append(text[at++]);
                continue;
            }

            var start = at;
            while (at < text.Length && !Separator(text[at]))
                at++;

            var token = text.AsSpan(start, at - start);
            if (token.Length > 1 && token[0] == '-' && token[1..].IndexOfAnyExceptInRange('0', '9') < 0)
                removed = true;
            else
                kept.Append(token);
        }

        return removed ? kept.ToString() : null;
    }

    private static IEnumerable<DateOperation> ParseOperations()
    {
        yield return new("parse, Broiler.VM's dialect", ["E5F", "E5V", "E5G"], VmStrings,
        [
            new("V", "DateParseText", i => VParse(i.Text), i => SVmParse(i.Text, DateParseDialect.BroilerVM)),
        ]);
        yield return new("parse, Broiler.VM's dialect, the renderings", ["E1T"], Renderings,
        [
            new("V", "DateParseText", i => VParse(i.Text), i => SVmParse(i.Text, DateParseDialect.BroilerVM)),
        ]);
        yield return new("parse, Broiler.VM's format path", ["E5F"], Strings,
        [
            new("V", "DateParseText", i => VParse(i.Text), i => SVmParse(i.Text, DateParseDialect.Strict), static i => StartsLikeTheFormat(i.Text)),
        ]);
        yield return new("parse, Broiler.JS's order", ["E5F"], Strings,
            [.. JParse(i => Num(SJsParse(i.Text)))], StringOperation: true);
        yield return new("parse, Broiler.JS's order, its renderings", ["E1T"], Renderings,
            [.. JParse(i => Num(SJsParse(i.Text)), static i => i.Source == "J")], StringOperation: true);
    }

    // ---- Renderings ----------------------------------------------------------------------------

    private static bool TimeValueInput(DateInput i) => IsTimeValue(i.Number);

    private static IEnumerable<DateInput> AtOffsets(DateInput input)
    {
        if (!IsTimeValue(input.Number))
            yield break;

        foreach (var offset in Offsets)
        {
            foreach (var name in Names)
            {
                yield return input with
                {
                    Offset = offset,
                    Name = name,
                    Detail = "offset=" + offset.ToString("R", CultureInfo.InvariantCulture) + " name=" + (name is null ? "-" : Population.Escape(name)),
                };
            }
        }
    }

    private static IEnumerable<DateInput> InHostZones(DateInput input) =>
        IsTimeValue(input.Number)
            ? new[] { "UTC", "America/New_York" }.Select(z => input with { Zone = z, Detail = Provider(z).Offsets('u', input.Number) })
            : [];

    private static string SIso(double t) => EcmaDateFormatter.TryToIsoString(t, out var text) ? DateRender.Str(text) : DateRender.Error("RangeError");

    private static IEnumerable<DateOperation> RenderingOperations()
    {
        yield return new("DateString", ["E1T"], i => TimeValueInput(i) ? [i] : [],
        [
            .. JDateString(i => DateRender.Str(EcmaDateFormatter.DateString(i.Number))),
            new("V", "DateToCalendarText", i => DateRender.Str(V.JsRealmDate.DateToCalendarText(i.Number)), i => DateRender.Str(EcmaDateFormatter.DateString(i.Number))),
        ], StringOperation: true);
        yield return new("TimeString", ["E1T"], i => TimeValueInput(i) ? [i] : [],
        [
            .. JTimeString(i => DateRender.Str(EcmaDateFormatter.TimeString(i.Number))),
            new("V", "DateToClockText and its zone text's GMT", i => DateRender.Str(V.JsRealmDate.DateToClockText(i.Number) + " GMT"), i => DateRender.Str(EcmaDateFormatter.TimeString(i.Number))),
        ], StringOperation: true);
        yield return new("TimeZoneString", ["E1T"], AtOffsets,
        [
            .. JTimeZoneString(i => DateRender.Str("GMT" + EcmaDateFormatter.TimeZoneString(i.Number, new FixedOffsetLocalTimeProvider(i.Offset, i.Name)))),
        ], SharedOnly: i => DateRender.Str(EcmaDateFormatter.TimeZoneString(i.Number, new FixedOffsetLocalTimeProvider(i.Offset, i.Name))), StringOperation: true);
        yield return new("TimeZoneString, Broiler.VM's zone", ["E1T"], i => TimeValueInput(i) ? [i] : [],
        [
            new("V", "DateZoneText", static _ => DateRender.Str(V.JsRealmDate.DateZoneText), i => DateRender.Str(" GMT" + EcmaDateFormatter.TimeZoneString(i.Number, VmZone))),
        ]);
        yield return new("toString", ["E1T"], AtOffsets,
        [
            .. JToStringAtOffset(i => DateRender.Str(EcmaDateFormatter.ToDateString(i.Number, new FixedOffsetLocalTimeProvider(i.Offset, i.Name)))),
        ], SharedOnly: i => DateRender.Str(EcmaDateFormatter.ToDateString(i.Number, new FixedOffsetLocalTimeProvider(i.Offset, i.Name))), StringOperation: true);
        yield return new("toString, a host zone", ["E1T"], InHostZones,
        [
            .. JToStringInZone(i => DateRender.Str(EcmaDateFormatter.ToDateString(i.Number, Provider(i.Zone)))),
        ], StringOperation: true);
        yield return new("toString, Broiler.VM's zone", ["E1T"], Once,
        [
            new("V", "DateToFullText", i => DateRender.Str(V.JsRealmDate.DateToFullText(i.Number)), i => DateRender.Str(EcmaDateFormatter.ToDateString(i.Number, VmZone)),
                static i => IsTimeValue(i.Number) || double.IsNaN(i.Number)),
        ]);
        yield return new("toUTCString", ["E1T"], i => TimeValueInput(i) || double.IsNaN(i.Number) ? [i] : [],
        [
            .. JToUtcString(i => DateRender.Str(EcmaDateFormatter.ToUtcString(i.Number))),
            new("V", "DateToUtcText", i => DateRender.Str(V.JsRealmDate.DateToUtcText(i.Number)), i => DateRender.Str(EcmaDateFormatter.ToUtcString(i.Number)), TimeValueInput),
        ], StringOperation: true);
        yield return new("toISOString", ["E1T"], i => TimeValueInput(i) || double.IsNaN(i.Number) ? [i] : [],
        [
            .. JToIsoString(i => SIso(i.Number)),
            new("V", "DateToIsoText", i => DateRender.Str(V.JsRealmDate.DateToIsoText(i.Number)), i => SIso(i.Number), TimeValueInput),
        ], StringOperation: true);
    }
}
