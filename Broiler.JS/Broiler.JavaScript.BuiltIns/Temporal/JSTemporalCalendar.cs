using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Calendar support for the proleptic-Gregorian family used by the Temporal types: iso8601,
// gregory, buddhist, roc and japanese. Every one of these shares the ISO 8601 day/month/leap-year
// arithmetic and differs only in how the proleptic-Gregorian year is *numbered* and split into
// an era + era-year. The japanese calendar additionally splits the same proleptic-Gregorian
// timeline into regnal eras whose boundaries fall mid-year, so its era / eraYear depend on the
// full date (year/month/day) rather than the year alone. The lunisolar and 13-month Intl
// calendars (chinese, dangi, hebrew, islamic-*, ethiopic, coptic, …) use a different month/day
// structure and are not implemented; Canonicalize rejects them with a RangeError.
internal static class TemporalCalendar
{
    // RejectObjectWithCalendarOrTimeZone: a plain-object argument to a calendar type's with() (or
    // similar) must not itself be a Temporal object (it carries its own calendar / time-zone), nor
    // carry a "calendar" or "timeZone" own/inherited property — any of these is a TypeError.
    internal static void RejectObjectWithCalendarOrTimeZone(JSObject obj)
    {
        if (obj is JSTemporalPlainDate or JSTemporalPlainDateTime or JSTemporalPlainMonthDay
            or JSTemporalPlainTime or JSTemporalPlainYearMonth or JSTemporalZonedDateTime)
            throw JSEngine.NewTypeError("Temporal: a Temporal object is not a valid fields object");

        if (!obj[KeyStrings.GetOrCreate("calendar")].IsUndefined)
            throw JSEngine.NewTypeError("Temporal: a fields object must not have a calendar property");

        if (!obj[KeyStrings.GetOrCreate("timeZone")].IsUndefined)
            throw JSEngine.NewTypeError("Temporal: a fields object must not have a timeZone property");
    }

    // Japanese regnal eras (ICU4X / Temporal `Intl.Era-monthcode`). Each modern era begins on a
    // specific proleptic-Gregorian date (`y`/`m`/`d`); listed newest-first so the first match wins.
    // `baseYear` is the Gregorian year of era-year 1 (eraYear = isoYear − baseYear + 1). For every
    // era except Meiji this equals the start year, but the ICU Japanese calendar only begins the
    // Meiji era at the 1873 Gregorian adoption while still numbering its years from 1868, so Meiji
    // 1–5 (1868–1872) display as the Gregorian ce era. Dates before Meiji fall back to ce/bce.
    private static readonly (string code, int y, int m, int d, int baseYear)[] JapaneseEras =
    [
        ("reiwa", 2019, 5, 1, 2019),
        ("heisei", 1989, 1, 8, 1989),
        ("showa", 1926, 12, 25, 1926),
        ("taisho", 1912, 7, 30, 1912),
        ("meiji", 1873, 1, 1, 1868),
    ];

    private static int CompareDate(int y1, int m1, int d1, int y2, int m2, int d2)
    {
        if (y1 != y2) return y1 < y2 ? -1 : 1;
        if (m1 != m2) return m1 < m2 ? -1 : 1;
        if (d1 != d2) return d1 < d2 ? -1 : 1;
        return 0;
    }

    // Canonicalizes a calendar identifier (case-insensitively, folding aliases). Throws a
    // RangeError for any unsupported calendar. The non-Gregorian calendars (the arithmetic
    // coptic / ethiopic / islamic-* / hebrew and the lunisolar chinese / dangi) are only
    // implemented for Temporal.PlainDate, so they are accepted only when `includeArithmetic` is
    // set; the other Temporal types reject them.
    internal static string Canonicalize(string id, bool includeArithmetic = false)
    {
        if (TryCanonicalizeName(id, includeArithmetic, out var canonical))
            return canonical;

        throw JSEngine.NewRangeError($"Temporal: unsupported calendar \"{id}\"");
    }

    // The positional `calendar` argument of a Temporal type constructor is more restrictive than
    // a from / with property bag's `calendar` field: it must be a bare calendar identifier String,
    // NOT a full Temporal ISO string. Per ECMA-262 (Temporal.PlainDate(..., calendar) and
    // friends): undefined → iso8601, a non-String → TypeError, an unknown identifier (including
    // an ISO date / date-time string that happens to parse) → RangeError. test262
    // built-ins/Temporal/<Type>/calendar-invalid-iso-string covers every Temporal type.
    internal static string ResolveCalendarIdentifierArgument(JSValue calendar, string typeName, bool includeArithmetic = true)
    {
        if (calendar == null || calendar.IsUndefined)
            return "iso8601";
        if (!calendar.IsString)
            throw JSEngine.NewTypeError($"{typeName}: calendar must be a calendar identifier string");
        return Canonicalize(calendar.StringValue, includeArithmetic);
    }

    // Folds a bare calendar identifier to its canonical form, returning false (rather than throwing)
    // for anything unrecognized — used both by Canonicalize and by the slot-value path, which falls
    // back to parsing the value as a Temporal ISO string.
    private static bool TryCanonicalizeName(string id, bool includeArithmetic, out string canonical)
    {
        var lower = id.ToLowerInvariant();
        switch (lower)
        {
            case "iso8601": canonical = "iso8601"; return true;
            case "gregory":
            case "gregorian": canonical = "gregory"; return true;
            case "buddhist": canonical = "buddhist"; return true;
            case "roc":
            case "minguo": canonical = "roc"; return true;
            case "japanese": canonical = "japanese"; return true;
        }

        if (includeArithmetic)
        {
            switch (lower)
            {
                case "coptic": canonical = "coptic"; return true;
                case "ethiopic": canonical = "ethiopic"; return true;
                case "ethioaa":
                case "ethiopic-amete-alem": canonical = "ethioaa"; return true;
                // The bare "islamic" identifier resolves to a locale-preferred variant in
                // Intl.DateTimeFormat, but Temporal requires an unambiguous calendar id; the
                // suffixed forms ("islamic-civil", "islamic-tbla", "islamic-umalqura") must be
                // used instead (test262 intl402/Temporal/*/from/islamic).
                case "islamic-civil":
                case "islamicc": canonical = "islamic-civil"; return true;
                case "islamic-tbla": canonical = "islamic-tbla"; return true;
                case "islamic-umalqura": canonical = "islamic-umalqura"; return true;
                case "hebrew": canonical = "hebrew"; return true;
                case "persian": canonical = "persian"; return true;
                case "indian": canonical = "indian"; return true;
                case "chinese": canonical = "chinese"; return true;
                case "dangi": canonical = "dangi"; return true;
            }
        }

        canonical = null;
        return false;
    }

    // ToTemporalCalendarSlotValue: resolves a calendar argument (a `calendar` property-bag field, a
    // constructor's calendar argument, or a withCalendar argument) to a canonical calendar id. The
    // value must either be a Temporal object that carries a calendar — in which case its calendar is
    // adopted directly, without coercion — or a calendar-identifier String. Anything else (null, a
    // number, a bigint, a Symbol, a plain object, or a Temporal type without a calendar such as
    // Temporal.Duration) is a TypeError; an unrecognized identifier String is a RangeError.
    internal static string ToSlotValue(JSValue calendar, bool includeArithmetic = false)
    {
        switch (calendar)
        {
            case JSTemporalPlainDate d: return d.calendarId;
            case JSTemporalPlainDateTime dt: return dt.calendarId;
            case JSTemporalPlainYearMonth ym: return ym.calendarId;
            case JSTemporalZonedDateTime zdt: return zdt.calendarId;
            case JSTemporalPlainMonthDay: return "iso8601";
        }

        if (calendar == null || !calendar.IsString)
            throw JSEngine.NewTypeError(
                "Temporal: calendar must be a calendar identifier string or a Temporal object with a calendar");

        return CanonicalizeIdentifierOrIsoString(calendar.StringValue, includeArithmetic);
    }

    // ParseTemporalCalendarString: a calendar slot value supplied as a String is either a bare
    // calendar identifier or a full Temporal ISO string, in which case its [u-ca=…] annotation is
    // adopted (defaulting to iso8601). An unrecognized non-ISO string is a RangeError.
    private static string CanonicalizeIdentifierOrIsoString(string value, bool includeArithmetic)
    {
        if (TryCanonicalizeName(value, includeArithmetic, out var canonical))
            return canonical;

        if (TemporalIsoString.TryExtractCalendar(value, out var annotation))
            return annotation == null ? "iso8601" : Canonicalize(annotation, includeArithmetic);

        throw JSEngine.NewRangeError($"Temporal: unsupported calendar \"{value}\"");
    }

    internal static bool IsSupported(string id)
    {
        var lower = id.ToLowerInvariant();
        return lower is "iso8601" or "gregory" or "gregorian" or "buddhist" or "roc" or "minguo" or "japanese"
            or "coptic" or "ethiopic" or "ethioaa" or "ethiopic-amete-alem" or "islamic-civil" or "islamicc"
            or "islamic-tbla" or "islamic-umalqura" or "hebrew" or "persian" or "indian" or "chinese" or "dangi";
    }

    // The calendar's displayed year for a proleptic-Gregorian (ISO) year. The japanese calendar's
    // `year` is the ISO year (the regnal year lives on era/eraYear), so it takes the default.
    internal static int Year(string calendarId, int isoYear) => calendarId switch
    {
        "buddhist" => isoYear + 543,
        "roc" => isoYear - 1911,
        _ => isoYear, // iso8601, gregory, japanese
    };

    // The era code for an ISO date, or null for the era-less ISO calendar. Only the japanese
    // calendar reads month/day (its era boundaries are mid-year); the others depend on the year.
    internal static JSValue Era(string calendarId, int isoYear, int isoMonth, int isoDay) => calendarId switch
    {
        "gregory" => new JSString(isoYear >= 1 ? "ce" : "bce"),
        "buddhist" => new JSString("be"),
        "roc" => new JSString(isoYear >= 1912 ? "roc" : "broc"),
        "japanese" => new JSString(JapaneseEra(isoYear, isoMonth, isoDay).code),
        _ => JSUndefined.Value,
    };

    // The era-year for an ISO date, or undefined for the ISO calendar.
    internal static JSValue EraYear(string calendarId, int isoYear, int isoMonth, int isoDay) => calendarId switch
    {
        "gregory" => new JSNumber(isoYear >= 1 ? isoYear : 1 - isoYear),
        "buddhist" => new JSNumber(isoYear + 543),
        "roc" => new JSNumber(isoYear >= 1912 ? isoYear - 1911 : 1912 - isoYear),
        "japanese" => new JSNumber(JapaneseEra(isoYear, isoMonth, isoDay).eraYear),
        _ => JSUndefined.Value,
    };

    // Resolves a proleptic-Gregorian (ISO) date to its japanese era code and era-year. Dates on or
    // after a regnal era's start belong to that era (eraYear counts from 1 at the era's start year);
    // dates before Meiji use the Gregorian ce/bce eras.
    internal static (string code, int eraYear) JapaneseEra(int isoYear, int isoMonth, int isoDay)
    {
        foreach (var e in JapaneseEras)
            if (CompareDate(isoYear, isoMonth, isoDay, e.y, e.m, e.d) >= 0)
                return (e.code, isoYear - e.baseYear + 1);
        return isoYear >= 1 ? ("ce", isoYear) : ("bce", 1 - isoYear);
    }

    // Resolves a property-bag's year fields (year and/or era + eraYear) to a proleptic-Gregorian
    // (ISO) year. era and eraYear must be supplied together; when both `year` and the era pair are
    // present they must agree.
    internal static int ResolveIsoYear(
        string calendarId,
        bool hasYear, int year,
        bool hasEra, string era,
        bool hasEraYear, int eraYear)
    {
        if (calendarId == "iso8601")
        {
            // The ISO calendar has no eras: era / eraYear are not ISO calendar fields, so they are
            // ignored entirely (not an error) and only `year` resolves the ISO year.
            if (!hasYear)
                throw JSEngine.NewTypeError("Temporal: missing year");
            return year;
        }

        if (hasEra != hasEraYear)
            throw JSEngine.NewTypeError("Temporal: era and eraYear must be provided together");

        int? fromYear = hasYear ? YearToIso(calendarId, year) : null;
        int? fromEra = hasEra ? EraToIso(calendarId, era, eraYear) : null;

        if (fromYear == null && fromEra == null)
            throw JSEngine.NewTypeError("Temporal: missing year (or era and eraYear)");
        if (fromYear != null && fromEra != null && fromYear.Value != fromEra.Value)
            throw JSEngine.NewRangeError("Temporal: year and era/eraYear do not agree");

        return fromYear ?? fromEra.Value;
    }

    private static int YearToIso(string calendarId, int year) => calendarId switch
    {
        "buddhist" => year - 543,
        "roc" => year + 1911,
        _ => year, // gregory
    };

    private static int EraToIso(string calendarId, string era, int eraYear)
    {
        var e = era.ToLowerInvariant();
        switch (calendarId)
        {
            case "gregory":
                return e switch
                {
                    "ce" or "ad" or "gregory" => eraYear,
                    "bce" or "bc" or "gregory-inverse" => 1 - eraYear,
                    _ => throw JSEngine.NewRangeError($"Temporal: invalid era \"{era}\" for the gregory calendar"),
                };
            case "buddhist":
                if (e != "be") throw JSEngine.NewRangeError($"Temporal: invalid era \"{era}\" for the buddhist calendar");
                return eraYear - 543;
            case "roc":
                return e switch
                {
                    "roc" or "minguo" => eraYear + 1911,
                    "broc" or "before-roc" => 1912 - eraYear,
                    _ => throw JSEngine.NewRangeError($"Temporal: invalid era \"{era}\" for the roc calendar"),
                };
            case "japanese":
                // A regnal era + eraYear maps to the ISO year regardless of month/day: the era's
                // start year is eraYear 1. The era named here only seeds the year; the date's
                // *displayed* era is recomputed from the resulting (year, month, day), so e.g.
                // { era: "reiwa", eraYear: 1 } in April resolves to Heisei 31.
                foreach (var je in JapaneseEras)
                    if (e == je.code) return je.baseYear + eraYear - 1;
                return e switch
                {
                    "ce" or "ad" => eraYear,
                    "bce" or "bc" => 1 - eraYear,
                    _ => throw JSEngine.NewRangeError($"Temporal: invalid era \"{era}\" for the japanese calendar"),
                };
            default:
                throw JSEngine.NewRangeError($"Temporal: invalid era \"{era}\"");
        }
    }
}
