using System;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Purely-arithmetic (no astronomical data) non-Gregorian calendars supported by Temporal.PlainDate:
//
//   coptic / ethiopic  — solar, 13 months: months 1–12 are 30 days each, month 13 ("epagomenal"
//                        / intercalary) is 5 days (6 in a leap year); leap when year ≡ 3 (mod 4).
//   islamic-civil      — tabular lunar, 12 months alternating 30/29 days (month 12 is 30 in a leap
//   islamic-tbla         year); leap years are the 11 of each 30-year cycle where (11·y+14) mod 30
//                        < 11. islamic-tbla is islamic-civil with an epoch one day earlier.
//   hebrew             — lunisolar but fully deterministic (molad + the four dehiyyot postponement
//                        rules of the 19-year Metonic cycle): 12 months, or 13 in a leap year where
//                        the leap month Adar I (ordinal 6, month code "M05L") precedes Adar II.
//
// Unlike the Gregorian family these have a different month/day structure than ISO, so every date
// field is computed by converting the stored ISO date to an epoch-day count and back through the
// calendar's own arithmetic. The remaining lunisolar calendars (chinese, dangi) need astronomical
// new-moon / solar-term data and stay unimplemented.
internal static class TemporalArithmeticCalendar
{
    internal static bool IsArithmetic(string id) =>
        id is "coptic" or "ethiopic" or "islamic-civil" or "islamic-tbla" or "hebrew";

    private static bool IsSolar13(string id) => id is "coptic" or "ethiopic";

    // Epoch-day number (days from the ISO 1970-01-01 epoch) of the calendar's year 1, month 1,
    // day 1, derived from the standard Rata Die epochs (RD of 1970-01-01 is 719163):
    //   coptic RD 103605, ethiopic RD 2796, islamic-civil RD 227015, islamic-tbla RD 227014.
    private static long Epoch(string id) => id switch
    {
        "coptic" => 103605 - 719163,
        "ethiopic" => 2796 - 719163,
        "islamic-civil" => 227015 - 719163,
        "islamic-tbla" => 227014 - 719163,
        _ => throw JSEngine.NewRangeError($"Temporal: unsupported calendar \"{id}\""),
    };

    internal static int MonthsInYear(string id, int year)
        => id == "hebrew" ? (HebrewLeap(year) ? 13 : 12) : (IsSolar13(id) ? 13 : 12);

    internal static bool InLeapYear(string id, int year)
    {
        if (id == "hebrew") return HebrewLeap(year);
        return IsSolar13(id)
            ? Mod(year, 4) == 3
            : Mod(11 * year + 14, 30) < 11; // tabular Islamic 30-year intercalation
    }

    internal static int DaysInYear(string id, int year)
    {
        if (id == "hebrew") return (int)(HebrewNewYear(year + 1) - HebrewNewYear(year));
        if (IsSolar13(id)) return InLeapYear(id, year) ? 366 : 365;
        return InLeapYear(id, year) ? 355 : 354;
    }

    internal static int DaysInMonth(string id, int year, int month)
    {
        if (id == "hebrew") return HebrewMonthLength(year, month);
        if (IsSolar13(id))
            return month <= 12 ? 30 : (InLeapYear(id, year) ? 6 : 5);
        if (month == 12)
            return InLeapYear(id, year) ? 30 : 29;
        return month % 2 == 1 ? 30 : 29; // odd months 30, even months 29
    }

    // Epoch days of (year, month, day) via the calendar's closed-form day count.
    internal static long EpochDaysFromYmd(string id, int year, int month, int day)
    {
        if (id == "hebrew")
        {
            var epoch = HebrewNewYear(year);
            for (int m = 1; m < month; m++) epoch += HebrewMonthLength(year, m);
            return epoch + day - 1;
        }
        if (IsSolar13(id))
            return Epoch(id) - 1 + 365L * (year - 1) + FloorDiv(year, 4) + 30L * (month - 1) + day;

        // Islamic: 354 days/common year plus the intercalary days so far, then the months-before
        // count (29·(m-1) + ⌊m/2⌋ gives the 30/29 alternation) and the day.
        return Epoch(id) - 1 + 354L * (year - 1) + FloorDiv(11L * year + 3, 30)
             + 29L * (month - 1) + FloorDiv(month, 2) + day;
    }

    internal static (int year, int month, int day) YmdFromEpochDays(string id, long epoch)
    {
        int year;
        if (id == "hebrew")
        {
            // Reingold's coarse estimate (365.2468-day mean year), then nudge to bracket the date.
            year = (int)FloorDiv((epoch - HebrewEpochDays) * 98496L, 35975351L) + 1;
            while (HebrewNewYear(year + 1) <= epoch) year++;
            while (HebrewNewYear(year) > epoch) year--;
        }
        else
        {
            year = IsSolar13(id)
                ? (int)FloorDiv(4 * (epoch - Epoch(id)) + 1463, 1461)
                : (int)FloorDiv(30 * (epoch - Epoch(id)) + 10646, 10631);
            while (EpochDaysFromYmd(id, year + 1, 1, 1) <= epoch) year++;
            while (EpochDaysFromYmd(id, year, 1, 1) > epoch) year--;
        }

        long dayOfYear = epoch - EpochDaysFromYmd(id, year, 1, 1); // 0-based
        int month = 1;
        while (dayOfYear >= DaysInMonth(id, year, month))
        {
            dayOfYear -= DaysInMonth(id, year, month);
            month++;
        }
        return (year, month, (int)dayOfYear + 1);
    }

    // 1-based ordinal day within the calendar year.
    internal static int DayOfYear(string id, int year, int month, int day)
        => (int)(EpochDaysFromYmd(id, year, month, day) - EpochDaysFromYmd(id, year, 1, 1)) + 1;

    // ── month codes ─────────────────────────────────────────────────────────────
    //
    // Coptic / ethiopic / islamic use plain M01..M13 codes. The Hebrew leap month Adar I (the 6th
    // ordinal month of a leap year) is coded "M05L"; the months after it shift the ordinal/code
    // relationship (Adar II → M06, Nisan → M07, … Elul → M12).

    internal static string MonthCode(string id, int year, int ordinalMonth)
    {
        if (id != "hebrew")
            return $"M{ordinalMonth:00}";

        if (HebrewLeap(year))
        {
            if (ordinalMonth <= 5) return $"M{ordinalMonth:00}";
            if (ordinalMonth == 6) return "M05L";       // Adar I
            return $"M{ordinalMonth - 1:00}";           // Adar II → M06, … Elul → M12
        }
        return $"M{ordinalMonth:00}";
    }

    // Maps a month code back to the ordinal month for a given year. `leapMonth` flags the trailing
    // "L" of a leap-month code.
    internal static int OrdinalFromMonthCode(string id, int year, int codeNumber, bool leapMonth)
    {
        if (id != "hebrew")
        {
            if (leapMonth)
                throw JSEngine.NewRangeError($"Temporal: the {id} calendar has no leap months");
            return codeNumber;
        }

        var leapYear = HebrewLeap(year);
        if (leapMonth)
        {
            if (codeNumber != 5 || !leapYear)
                throw JSEngine.NewRangeError("Temporal: invalid monthCode for the hebrew calendar");
            return 6; // Adar I
        }
        if (codeNumber <= 5) return codeNumber;
        return leapYear ? codeNumber + 1 : codeNumber; // M06→Adar II(7) in a leap year, else Adar(6)
    }

    // ── era ↔ year ──────────────────────────────────────────────────────────────
    //
    // coptic / hebrew — single era "am"; the year is the era-year for any sign.
    // ethiopic        — "am" (Amete Mihret / incarnation) for year ≥ 1; years ≤ 0 are shown in the
    //                   "aa" (Amete Alem) era with era-year = year + 5500.
    // islamic-*       — "ah" for year ≥ 1; years ≤ 0 are shown in the "bh" (Before Hijra) era with
    //                   era-year = 1 − year.

    internal static (string code, int eraYear) Era(string id, int calYear) => id switch
    {
        "coptic" or "hebrew" => ("am", calYear),
        "ethiopic" => calYear >= 1 ? ("am", calYear) : ("aa", calYear + 5500),
        _ => calYear >= 1 ? ("ah", calYear) : ("bh", 1 - calYear), // islamic-civil / islamic-tbla
    };

    // Resolves a calendar year from an { era, eraYear } pair (the displayed era is later recomputed
    // from the resolved date, so e.g. ethiopic { era:"am", eraYear:0 } resolves to year 0 / aa 5500).
    internal static int YearFromEra(string id, string era, int eraYear)
    {
        var e = era.ToLowerInvariant();
        switch (id)
        {
            case "coptic":
                if (e is "am" or "coptic") return eraYear;
                throw JSEngine.NewRangeError($"Temporal: invalid era \"{era}\" for the coptic calendar");
            case "hebrew":
                if (e is "am" or "hebrew") return eraYear;
                throw JSEngine.NewRangeError($"Temporal: invalid era \"{era}\" for the hebrew calendar");
            case "ethiopic":
                return e switch
                {
                    "am" or "incarnation" or "ethiopic" => eraYear,
                    "aa" or "mundi" or "ethioaa" => eraYear - 5500,
                    _ => throw JSEngine.NewRangeError($"Temporal: invalid era \"{era}\" for the ethiopic calendar"),
                };
            default: // islamic-civil / islamic-tbla
                return e switch
                {
                    "ah" => eraYear,
                    "bh" => 1 - eraYear,
                    _ => throw JSEngine.NewRangeError($"Temporal: invalid era \"{era}\" for the {id} calendar"),
                };
        }
    }

    // ── Hebrew calendar arithmetic (Reingold & Dershowitz, integer molad/dehiyyot) ─────────────

    // RD of 1 Tishrei AM 1 is -1373427; shifted to the ISO 1970-01-01 epoch (RD 719163).
    private const long HebrewEpochDays = -1373427 - 719163;

    private static bool HebrewLeap(int year) => Mod(7 * year + 1, 19) < 7;

    // Days from the epoch to 1 Tishrei of `year` before the year-length postponements.
    private static long HebrewElapsedDays(int year)
    {
        long monthsElapsed = FloorDiv(235L * year - 234, 19);
        long partsElapsed = 12084 + 13753 * monthsElapsed;
        long day = monthsElapsed * 29 + FloorDiv(partsElapsed, 25920);
        return Mod((int)(3 * (day + 1)), 7) < 3 ? day + 1 : day; // molad-zaken / lo-ADU-rosh combined
    }

    // Epoch day of 1 Tishrei of `year`, applying the GaTaRaD / BeTUTaKPaT length corrections.
    private static long HebrewNewYear(int year)
    {
        long ny0 = HebrewElapsedDays(year - 1);
        long ny1 = HebrewElapsedDays(year);
        long ny2 = HebrewElapsedDays(year + 1);
        int correction = (ny2 - ny1 == 356) ? 2 : (ny1 - ny0 == 382 ? 1 : 0);
        return HebrewEpochDays + ny1 + correction;
    }

    // Length of the `ordinalMonth`-th month (1 = Tishrei) of a Hebrew year. Heshvan (2) is long in a
    // "complete" year and Kislev (3) is short in a "deficient" year; the rest are fixed, with the
    // leap month Adar I (6, leap years) and Adar / Adar II (29) the only structural variation.
    private static int HebrewMonthLength(int year, int ordinalMonth)
    {
        var leap = HebrewLeap(year);
        var yearLength = (int)(HebrewNewYear(year + 1) - HebrewNewYear(year));
        switch (ordinalMonth)
        {
            case 1: return 30;                                         // Tishrei
            case 2: return yearLength is 355 or 385 ? 30 : 29;         // Heshvan (complete year)
            case 3: return yearLength is 353 or 383 ? 29 : 30;         // Kislev (deficient year)
            case 4: return 29;                                         // Tevet
            case 5: return 30;                                         // Shevat
        }
        if (leap)
            return ordinalMonth switch
            {
                6 => 30,        // Adar I
                7 => 29,        // Adar II
                8 => 30,        // Nisan
                9 => 29,        // Iyar
                10 => 30,       // Sivan
                11 => 29,       // Tammuz
                12 => 30,       // Av
                _ => 29,        // 13 Elul
            };
        return ordinalMonth switch
        {
            6 => 29,            // Adar
            7 => 30,            // Nisan
            8 => 29,            // Iyar
            9 => 30,            // Sivan
            10 => 29,           // Tammuz
            11 => 30,           // Av
            _ => 29,            // 12 Elul
        };
    }

    private static long FloorDiv(long a, long b)
    {
        var q = a / b;
        if (a % b != 0 && (a < 0) != (b < 0)) q -= 1;
        return q;
    }

    private static int Mod(int a, int b)
    {
        var r = a % b;
        return r < 0 ? r + b : r;
    }
}
