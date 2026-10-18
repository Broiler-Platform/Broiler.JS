using System;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Temporal;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Temporal.Duration (Temporal proposal §7). A Duration is an immutable record of ten signed
// integer components (years … nanoseconds) sharing one sign. Its validity, sign, addition,
// compare, round and total, with and without relativeTo in any calendar and zone, are the shared
// library's DurationMath (milestone SJL-10.5, part C); what stays here is each method's reads, in
// the specification's order, and the RangeError each status is, through TemporalAdapter.
//
// Registered under the Temporal namespace (not as a global) via Register = false.
[JSClassGenerator("Duration", Register = false)]
public partial class JSTemporalDuration : JSObject
{
    internal readonly double years, months, weeks, days, hours, minutes, seconds, milliseconds, microseconds, nanoseconds;

    [JSExport(Length = 0)]
    public JSTemporalDuration(in Arguments a) : base(ResolvePrototype())
    {
        years = ToIntegerIfIntegral(a.GetAt(0));
        months = ToIntegerIfIntegral(a.GetAt(1));
        weeks = ToIntegerIfIntegral(a.GetAt(2));
        days = ToIntegerIfIntegral(a.GetAt(3));
        hours = ToIntegerIfIntegral(a.GetAt(4));
        minutes = ToIntegerIfIntegral(a.GetAt(5));
        seconds = ToIntegerIfIntegral(a.GetAt(6));
        milliseconds = ToIntegerIfIntegral(a.GetAt(7));
        microseconds = ToIntegerIfIntegral(a.GetAt(8));
        nanoseconds = ToIntegerIfIntegral(a.GetAt(9));

        TemporalAdapter.CheckDuration(new DurationFields(years, months, weeks, days, hours, minutes, seconds, milliseconds, microseconds, nanoseconds));
    }

    internal JSTemporalDuration(
        double years, double months, double weeks, double days, double hours,
        double minutes, double seconds, double milliseconds, double microseconds, double nanoseconds,
        JSObject prototype) : base(prototype)
    {
        // A Temporal.Duration field is never negative zero (CreateDurationRecord stores mathematical
        // values); operations like negated() / since / balancing can produce -0 from `-1 * 0`, so
        // canonicalize every component here.
        this.years = NoNegativeZero(years); this.months = NoNegativeZero(months); this.weeks = NoNegativeZero(weeks);
        this.days = NoNegativeZero(days); this.hours = NoNegativeZero(hours); this.minutes = NoNegativeZero(minutes);
        this.seconds = NoNegativeZero(seconds); this.milliseconds = NoNegativeZero(milliseconds);
        this.microseconds = NoNegativeZero(microseconds); this.nanoseconds = NoNegativeZero(nanoseconds);
    }

    // A duration of the shared library's record, every field already a Number.
    internal JSTemporalDuration(DurationFields fields, JSObject prototype)
        : this(fields.Years, fields.Months, fields.Weeks, fields.Days, fields.Hours, fields.Minutes, fields.Seconds,
            fields.Milliseconds, fields.Microseconds, fields.Nanoseconds, prototype) { }

    private static double NoNegativeZero(double v) => v == 0 ? 0.0 : v;

    // The ten fields as the shared library's record.
    internal DurationFields Fields => new(years, months, weeks, days, hours, minutes, seconds, milliseconds, microseconds, nanoseconds);

    private static JSObject ResolvePrototype()
    {
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError("Constructor Temporal.Duration requires 'new'");

        return JSEngine.NewTargetPrototype ?? DurationPrototype;
    }

    // The realm's %Temporal.Duration.prototype%, for instances created internally.
    private static readonly KeyString DurationIntrinsicKey = KeyStrings.GetOrCreate("Temporal.Duration");

    // %Temporal.Duration.prototype% of the current realm, never the prototype of whatever
    // `globalThis.Temporal.Duration` holds now: guest code may replace the namespace or its members.
    internal static JSObject DurationPrototype => Intrinsics.TemporalPrototype(DurationIntrinsicKey);

    // ── accessors ───────────────────────────────────────────────────────────────

    [JSExport("years")] public double YearsValue => years;
    [JSExport("months")] public double MonthsValue => months;
    [JSExport("weeks")] public double WeeksValue => weeks;
    [JSExport("days")] public double DaysValue => days;
    [JSExport("hours")] public double HoursValue => hours;
    [JSExport("minutes")] public double MinutesValue => minutes;
    [JSExport("seconds")] public double SecondsValue => seconds;
    [JSExport("milliseconds")] public double MillisecondsValue => milliseconds;
    [JSExport("microseconds")] public double MicrosecondsValue => microseconds;
    [JSExport("nanoseconds")] public double NanosecondsValue => nanoseconds;

    [JSExport("sign")] public double Sign => DurationSign();
    [JSExport("blank")] public bool Blank => DurationSign() == 0;

    private int DurationSign() => DurationMath.Sign(Fields);

    // ── methods ─────────────────────────────────────────────────────────────────

    [JSExport("negated", Length = 0)]
    public JSValue Negated(in Arguments a)
        => new JSTemporalDuration(-years, -months, -weeks, -days, -hours, -minutes, -seconds, -milliseconds, -microseconds, -nanoseconds, DurationPrototype);

    [JSExport("abs", Length = 0)]
    public JSValue Abs(in Arguments a)
        => new JSTemporalDuration(Math.Abs(years), Math.Abs(months), Math.Abs(weeks), Math.Abs(days), Math.Abs(hours), Math.Abs(minutes), Math.Abs(seconds), Math.Abs(milliseconds), Math.Abs(microseconds), Math.Abs(nanoseconds), DurationPrototype);

    [JSExport("with", Length = 1)]
    public JSValue With(in Arguments a)
    {
        if (a.GetAt(0) is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.Duration.prototype.with requires an object");

        // At least one recognized field must be present (PrepareTemporalFields with a
        // partial record rejects a wholly empty bag).
        var any = false;
        double Read(string name, double current)
        {
            var v = obj[KeyStrings.GetOrCreate(name)];
            if (v.IsUndefined) return current;
            any = true;
            return ToIntegerIfIntegral(v);
        }

        // ToTemporalPartialDurationRecord reads the fields in alphabetical order.
        var d = Read("days", days);
        var h = Read("hours", hours);
        var us = Read("microseconds", microseconds);
        var ms = Read("milliseconds", milliseconds);
        var mi = Read("minutes", minutes);
        var mo = Read("months", months);
        var ns = Read("nanoseconds", nanoseconds);
        var s = Read("seconds", seconds);
        var w = Read("weeks", weeks);
        var y = Read("years", years);

        if (!any)
            throw JSEngine.NewTypeError("Temporal.Duration.prototype.with requires at least one duration property");

        TemporalAdapter.CheckDuration(new DurationFields(y, mo, w, d, h, mi, s, ms, us, ns));
        return new JSTemporalDuration(y, mo, w, d, h, mi, s, ms, us, ns, DurationPrototype);
    }

    [JSExport("toString", Length = 0)]
    public JSValue ToStringMethod(in Arguments a)
    {
        var optionsArg = a.GetAt(0);
        var digits = TemporalFormat.AutoPrecision;
        var roundingMode = RoundingMode.Trunc;
        string smallestUnit = null;

        if (optionsArg != null && !optionsArg.IsUndefined)
        {
            if (optionsArg is not JSObject options)
                throw JSEngine.NewTypeError("Temporal.Duration.toString options must be an object or undefined");

            // The options are read in alphabetical order — fractionalSecondDigits, roundingMode, smallestUnit —
            // each validated by its rule as it is read, the unit's group (no hour, no minute) after the last read.
            digits = TemporalAdapter.ReadFractionalSecondDigits(options);
            roundingMode = TemporalRoundingOptions.GetRoundingMode(options, RoundingMode.Trunc);
            smallestUnit = TemporalAdapter.ReadSmallestUnitText(options);
        }

        var precision = TemporalAdapter.ToStringPrecision(smallestUnit, digits, duration: true, "Temporal.Duration");

        // The time part rounded (RoundTimeDuration), balanced up to the larger of the default largest unit and
        // second, its validity checked, and TemporalDurationToString: the shared library's. Rounding up can push
        // the duration past its limits, a RangeError.
        TemporalAdapter.Check(TemporalFormat.TryDurationToString(Fields, precision, roundingMode, out var text),
            "Temporal.Duration: rounded duration is out of range");
        return new JSString(text);
    }

    [JSExport("toJSON", Length = 0)]
    public JSValue ToJSON(in Arguments a) => new JSString(TemporalFormat.DurationToString(Fields, TemporalFormat.AutoPrecision));

    [JSExport("toLocaleString", Length = 0)]
    public JSValue ToLocaleString(in Arguments a)
        => Intl.JSIntlDurationFormat.TemporalToLocaleString(this, a.Get1(), a.GetAt(1));

    [JSExport("valueOf", Length = 0)]
    public JSValue ValueOf(in Arguments a)
        => throw JSEngine.NewTypeError("Called Temporal.Duration.prototype.valueOf, which is not supported. Use Temporal.Duration.compare for comparison.");

    [JSExport("add", Length = 1)]
    public JSValue Add(in Arguments a) => AddDurations(a.GetAt(0), subtract: false);

    [JSExport("subtract", Length = 1)]
    public JSValue Subtract(in Arguments a) => AddDurations(a.GetAt(0), subtract: true);

    // AddDurations (s7.5.41): ToTemporalDuration of the one argument - the pinned revision's add and subtract take one
    // parameter, so a second argument is never read (row D3) - negated for subtract, then the library's sum: years,
    // months or weeks in either duration are the RangeError, and the sum past the limits too.
    private JSValue AddDurations(JSValue otherValue, bool subtract)
    {
        var other = ((JSTemporalDuration)ToTemporalDuration(otherValue)).Fields;
        if (subtract)
            other = DurationMath.Negate(other);

        TemporalAdapter.CheckArithmetic(DurationMath.AddDurations(Fields, other, out var result), "Temporal.Duration",
            "Temporal.Duration.prototype.add/subtract: a duration with years, months or weeks cannot be added or subtracted");
        return TemporalAdapter.DurationOf(result);
    }

    // Temporal.Duration.prototype.round (s7.3.20): a String is { smallestUnit }, undefined the TypeError; then the reads in
    // code unit order - largestUnit, relativeTo (GetTemporalRelativeToOption), roundingIncrement, roundingMode (default
    // halfExpand), smallestUnit - each converted as it is read; then the units' rules (TryGetDurationRoundUnits) and the
    // library's rounding: relative to a zoned instant over its zone's real days, to a plain date in its calendar, and
    // otherwise in days of 24 hours.
    [JSExport("round", Length = 1)]
    public JSValue Round(in Arguments a)
    {
        const string typeName = "Temporal.Duration.prototype.round";
        var roundTo = a.GetAt(0);
        if (roundTo == null || roundTo.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.Duration.prototype.round requires an options argument");

        var largestUnit = TemporalUnit.Unset;
        var relativeTo = RelativeTo.None;
        var roundingIncrement = TemporalOptionRules.DefaultRoundingIncrement;
        var roundingMode = TemporalOptionRules.DefaultRoundRoundingMode;
        TemporalUnit smallestUnit;
        if (roundTo.IsString)
        {
            smallestUnit = TemporalRoundingOptions.ParseUnit(roundTo.StringValue, "smallestUnit", typeName);
        }
        else if (roundTo is JSObject options)
        {
            largestUnit = TemporalRoundingOptions.GetUnit(options, "largestUnit", typeName);
            relativeTo = TemporalAdapter.RelativeToOption(options);
            roundingIncrement = TemporalRoundingOptions.GetRoundingIncrement(options);
            roundingMode = TemporalRoundingOptions.GetRoundingMode(options, TemporalOptionRules.DefaultRoundRoundingMode);
            smallestUnit = TemporalRoundingOptions.GetUnit(options, "smallestUnit", typeName);
        }
        else
        {
            throw JSEngine.NewTypeError("Temporal.Duration.prototype.round requires an options object or string");
        }

        if (!TemporalOptionRules.TryGetDurationRoundUnits(Fields, largestUnit, roundingIncrement, smallestUnit, out var largest, out var smallest))
            throw JSEngine.NewRangeError($"{typeName}: the largestUnit, smallestUnit and roundingIncrement options are not valid together");

        TemporalAdapter.CheckArithmetic(DurationMath.Round(Fields, largest, roundingIncrement, smallest, roundingMode, relativeTo, out var result), typeName);
        return TemporalAdapter.DurationOf(result);
    }

    // Temporal.Duration.prototype.total (s7.3.21): a String is { unit }, undefined the TypeError; then relativeTo, then the
    // required unit, a unit of any group; then the library's total, an exact ratio rounded once.
    [JSExport("total", Length = 1)]
    public JSValue Total(in Arguments a)
    {
        const string typeName = "Temporal.Duration.prototype.total";
        var totalOf = a.GetAt(0);
        if (totalOf == null || totalOf.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.Duration.prototype.total requires an options argument");

        var relativeTo = RelativeTo.None;
        TemporalUnit unit;
        if (totalOf.IsString)
        {
            unit = TemporalRoundingOptions.ParseUnit(totalOf.StringValue, "unit", typeName);
        }
        else if (totalOf is JSObject options)
        {
            relativeTo = TemporalAdapter.RelativeToOption(options);
            unit = TemporalRoundingOptions.GetUnit(options, "unit", typeName, required: true);
        }
        else
        {
            throw JSEngine.NewTypeError("Temporal.Duration.prototype.total requires an options object or string");
        }

        if (!TemporalOptionRules.IsValidTotalUnit(unit))
            throw JSEngine.NewRangeError($"{typeName}: invalid unit");

        TemporalAdapter.CheckArithmetic(DurationMath.Total(Fields, unit, relativeTo, out var total), typeName);
        return new JSNumber(total);
    }

    // ── statics ─────────────────────────────────────────────────────────────────

    [JSExport("from", Length = 1)]
    internal static JSValue From(in Arguments a)
    {
        var item = a.GetAt(0);
        if (item is JSTemporalDuration d)
            return new JSTemporalDuration(d.years, d.months, d.weeks, d.days, d.hours, d.minutes, d.seconds, d.milliseconds, d.microseconds, d.nanoseconds, DurationPrototype);

        return ToTemporalDuration(item);
    }

    // Temporal.Duration.compare (s7.2.3): both ToTemporalDurations, GetOptionsObject and GetTemporalRelativeToOption -
    // relativeTo is resolved, and refused if invalid, even where the comparison does not need it - then the library's
    // comparison: equal fields are 0; relative to a zoned instant, each added to it; relative to a plain date, by
    // DateDurationDays, which adds only the years, months and weeks (row D4); otherwise in days of 24 hours.
    [JSExport("compare", Length = 2)]
    internal static JSValue Compare(in Arguments a)
    {
        var one = (JSTemporalDuration)ToTemporalDuration(a.GetAt(0));
        var two = (JSTemporalDuration)ToTemporalDuration(a.GetAt(1));
        var options = TemporalAdapter.OptionsObject(a.GetAt(2), "Temporal.Duration.compare");
        var relativeTo = options == null ? RelativeTo.None : TemporalAdapter.RelativeToOption(options);

        TemporalAdapter.CheckArithmetic(DurationMath.Compare(one.Fields, two.Fields, relativeTo, out var result), "Temporal.Duration.compare");
        return new JSNumber(result);
    }

    // ── helpers ─────────────────────────────────────────────────────────────────

    private static double ToIntegerIfIntegral(JSValue value)
    {
        if (value == null || value.IsUndefined)
            return 0;

        var number = value.DoubleValue; // ToNumber (throws TypeError for BigInt/Symbol)
        if (double.IsNaN(number) || double.IsInfinity(number))
            throw JSEngine.NewRangeError("Temporal.Duration field must be a finite integer");

        if (Math.Truncate(number) != number)
            throw JSEngine.NewRangeError("Temporal.Duration field must be an integer");

        return number == 0 ? 0 : number; // normalize -0 → 0
    }

    internal static JSValue ToTemporalDuration(JSValue item)
    {
        if (item is JSTemporalDuration d)
            return new JSTemporalDuration(d.years, d.months, d.weeks, d.days, d.hours, d.minutes, d.seconds, d.milliseconds, d.microseconds, d.nanoseconds, DurationPrototype);

        if (item.IsString)
            return ParseTemporalDurationString(item.ToString());

        if (item is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.Duration: invalid duration value");

        // ToTemporalDurationRecord: read each field; at least one must be present.
        var any = false;
        double Field(string name)
        {
            var v = obj[KeyStrings.GetOrCreate(name)];
            if (v.IsUndefined) return 0;
            any = true;
            return ToIntegerIfIntegral(v);
        }

        // ToTemporalDurationRecord reads the fields in alphabetical order.
        var dd = Field("days"); var h = Field("hours"); var us = Field("microseconds");
        var ms = Field("milliseconds"); var mi = Field("minutes"); var mo = Field("months");
        var ns = Field("nanoseconds"); var s = Field("seconds"); var w = Field("weeks");
        var y = Field("years");

        if (!any)
            throw JSEngine.NewTypeError("Temporal.Duration: object has no duration properties");

        TemporalAdapter.CheckDuration(new DurationFields(y, mo, w, dd, h, mi, s, ms, us, ns));
        return new JSTemporalDuration(y, mo, w, dd, h, mi, s, ms, us, ns, DurationPrototype);
    }

    // ParseTemporalDurationString, the shared library's grammar (each part exact, a fraction carried into the
    // smaller units exactly, and a time designator with no time part refused), then CreateTemporalDuration's
    // IsValidDuration.
    private static JSValue ParseTemporalDurationString(string text)
    {
        var d = TemporalAdapter.ParseDuration(text);
        TemporalAdapter.CheckDuration(d);
        return new JSTemporalDuration(d.Years, d.Months, d.Weeks, d.Days, d.Hours, d.Minutes, d.Seconds, d.Milliseconds, d.Microseconds,
            d.Nanoseconds, DurationPrototype);
    }
}
