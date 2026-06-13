using System;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Calendar support for the proleptic-Gregorian family used by the Temporal types: iso8601,
// gregory, buddhist and roc. Every one of these shares the ISO 8601 day/month/leap-year
// arithmetic and differs only in how the proleptic-Gregorian year is *numbered* and split into
// an era + era-year. The lunisolar and other Intl calendars (chinese, hebrew, japanese, …) are
// not implemented; Canonicalize rejects them with a RangeError.
internal static class TemporalCalendar
{
    // Canonicalizes a calendar identifier (case-insensitively, folding aliases). Throws a
    // RangeError for any calendar outside the supported proleptic-Gregorian family.
    internal static string Canonicalize(string id)
    {
        var lower = id.ToLowerInvariant();
        return lower switch
        {
            "iso8601" => "iso8601",
            "gregory" or "gregorian" => "gregory",
            "buddhist" => "buddhist",
            "roc" or "minguo" => "roc",
            _ => throw JSEngine.NewRangeError($"Temporal: unsupported calendar \"{id}\""),
        };
    }

    internal static bool IsSupported(string id)
    {
        var lower = id.ToLowerInvariant();
        return lower is "iso8601" or "gregory" or "gregorian" or "buddhist" or "roc" or "minguo";
    }

    // The calendar's displayed year for a proleptic-Gregorian (ISO) year.
    internal static int Year(string calendarId, int isoYear) => calendarId switch
    {
        "buddhist" => isoYear + 543,
        "roc" => isoYear - 1911,
        _ => isoYear, // iso8601, gregory
    };

    // The era code for an ISO year, or null for the era-less ISO calendar.
    internal static JSValue Era(string calendarId, int isoYear) => calendarId switch
    {
        "gregory" => new JSString(isoYear >= 1 ? "ce" : "bce"),
        "buddhist" => new JSString("be"),
        "roc" => new JSString(isoYear >= 1912 ? "roc" : "broc"),
        _ => JSUndefined.Value,
    };

    // The era-year for an ISO year, or undefined for the ISO calendar.
    internal static JSValue EraYear(string calendarId, int isoYear) => calendarId switch
    {
        "gregory" => new JSNumber(isoYear >= 1 ? isoYear : 1 - isoYear),
        "buddhist" => new JSNumber(isoYear + 543),
        "roc" => new JSNumber(isoYear >= 1912 ? isoYear - 1911 : 1912 - isoYear),
        _ => JSUndefined.Value,
    };

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
            if (hasEra || hasEraYear)
                throw JSEngine.NewRangeError("Temporal: the iso8601 calendar does not use eras");
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
            default:
                throw JSEngine.NewRangeError($"Temporal: invalid era \"{era}\"");
        }
    }
}
