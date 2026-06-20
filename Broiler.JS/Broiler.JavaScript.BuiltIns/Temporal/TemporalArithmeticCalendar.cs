using System;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using UnicodeCldr.LocaleData;

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
//   indian             — the reformed Indian national (Saka) calendar: solar, 12 months anchored to
//                        the Gregorian year (Saka year + 78). Chaitra (month 1) is 30 days (31 in a
//                        leap Saka year, when Saka + 78 is a Gregorian leap year), months 2–6 are 31
//                        days and months 7–12 are 30 days. Single era "shaka".
//
// Unlike the Gregorian family these have a different month/day structure than ISO, so every date
// field is computed by converting the stored ISO date to an epoch-day count and back through the
// calendar's own arithmetic. The remaining lunisolar calendars (chinese, dangi) need astronomical
// new-moon / solar-term data and stay unimplemented.
internal static class TemporalArithmeticCalendar
{
    internal static bool IsArithmetic(string id) =>
        id is "coptic" or "ethiopic" or "ethioaa" or "islamic" or "islamic-civil" or "islamic-tbla"
            or "islamic-umalqura" or "hebrew" or "indian";

    private static bool IsSolar13(string id) => id is "coptic" or "ethiopic" or "ethioaa";

    // islamic-umalqura uses ICU's sighting-based month-length table over 1300–1600 AH and falls
    // back to the tabular (islamic-civil) arithmetic outside that span. A given Hijri year is
    // table-driven only when it is both this calendar and inside the tabulated range.
    private static bool UmalquraTable(string id, int year) => id == "islamic-umalqura" && CldrUmmAlQura.IsTabulated(year);

    // Epoch-day number (days from the ISO 1970-01-01 epoch) of the calendar's year 1, month 1,
    // day 1, derived from the standard Rata Die epochs (RD of 1970-01-01 is 719163):
    //   coptic RD 103605, ethiopic RD 2796, islamic-civil RD 227015, islamic-tbla RD 227014.
    // The ethioaa (Ethiopic Amete Alem) calendar shares the ethiopic day arithmetic but numbers its
    // years 5500 higher (ethioaa year = ethiopic year + 5500); since 5500 is divisible by 4 the leap
    // rule is unchanged, and its epoch is shifted so that ethioaa 5501/1/1 coincides with ethiopic
    // 1/1/1 (RD 2796 − (365·5500 + 1375) = RD −2006079).
    private static long Epoch(string id) => id switch
    {
        "coptic" => 103605 - 719163,
        "ethiopic" => 2796 - 719163,
        "ethioaa" => -2006079 - 719163,
        // The bare "islamic" calendar (CLDR's astronomical Hijri) is approximated by the tabular
        // civil arithmetic — the two differ by at most a day; ICU's civil (Friday) epoch is used.
        "islamic" or "islamic-civil" => 227015 - 719163,
        "islamic-tbla" => 227014 - 719163,
        "islamic-umalqura" => 227015 - 719163, // civil (Friday) epoch; used for the out-of-table fallback
        _ => throw JSEngine.NewRangeError($"Temporal: unsupported calendar \"{id}\""),
    };

    internal static int MonthsInYear(string id, int year)
        => id == "hebrew" ? (HebrewLeap(year) ? 13 : 12) : (IsSolar13(id) ? 13 : 12);

    internal static bool InLeapYear(string id, int year)
    {
        if (id == "indian") return GregorianLeap(year + 78);
        if (id == "hebrew") return HebrewLeap(year);
        if (UmalquraTable(id, year)) return CldrUmmAlQura.YearLength(year) == 355;
        return IsSolar13(id)
            ? Mod(year, 4) == 3
            : Mod(11 * year + 14, 30) < 11; // tabular Islamic 30-year intercalation
    }

    internal static int DaysInYear(string id, int year)
    {
        if (id == "indian") return GregorianLeap(year + 78) ? 366 : 365;
        if (id == "hebrew") return (int)(HebrewNewYear(year + 1) - HebrewNewYear(year));
        if (UmalquraTable(id, year)) return CldrUmmAlQura.YearLength(year);
        if (IsSolar13(id)) return InLeapYear(id, year) ? 366 : 365;
        return InLeapYear(id, year) ? 355 : 354;
    }

    internal static int DaysInMonth(string id, int year, int month)
    {
        if (id == "indian") return IndianMonthLength(GregorianLeap(year + 78), month);
        if (id == "hebrew") return HebrewMonthLength(year, month);
        if (UmalquraTable(id, year)) return CldrUmmAlQura.MonthLength(year, month);
        if (IsSolar13(id))
            return month <= 12 ? 30 : (InLeapYear(id, year) ? 6 : 5);
        if (month == 12)
            return InLeapYear(id, year) ? 30 : 29;
        return month % 2 == 1 ? 30 : 29; // odd months 30, even months 29
    }

    // Epoch days of (year, month, day) via the calendar's closed-form day count.
    internal static long EpochDaysFromYmd(string id, int year, int month, int day)
    {
        if (id == "indian") return IndianEpochDays(year, month, day);
        if (id == "hebrew")
        {
            var epoch = HebrewNewYear(year);
            for (int m = 1; m < month; m++) epoch += HebrewMonthLength(year, m);
            return epoch + day - 1;
        }
        if (UmalquraTable(id, year))
        {
            var epoch = Epoch(id) + CldrUmmAlQura.YearStartDay(year);
            for (int m = 1; m < month; m++) epoch += CldrUmmAlQura.MonthLength(year, m);
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
        if (id == "indian") return IndianYmdFromEpochDays(epoch);
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
        "ethioaa" => ("aa", calYear), // single Amete Alem era for any sign
        "indian" => ("shaka", calYear), // single Shaka era for any sign
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
            case "ethioaa":
                if (e is "aa" or "mundi" or "ethioaa") return eraYear;
                throw JSEngine.NewRangeError($"Temporal: invalid era \"{era}\" for the ethioaa calendar");
            case "indian":
                if (e is "saka" or "shaka") return eraYear;
                throw JSEngine.NewRangeError($"Temporal: invalid era \"{era}\" for the indian calendar");
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

    // ── Indian national (Saka) calendar arithmetic ──────────────────────────────
    //
    // Anchored to the Gregorian year: Saka year S begins on 1 Chaitra, which is 22 March of
    // Gregorian year S + 78 in a common year and 21 March when that Gregorian year is a leap year
    // (so the extra day is absorbed by a 31-day Chaitra and the following months are unchanged).

    private static bool GregorianLeap(int y) => (y % 4 == 0 && y % 100 != 0) || y % 400 == 0;

    private static int IndianMonthLength(bool leap, int month)
        => month == 1 ? (leap ? 31 : 30) : (month <= 6 ? 31 : 30);

    private static long IndianEpochDays(int year, int month, int day)
    {
        var gregorianYear = year + 78;
        var leap = GregorianLeap(gregorianYear);
        var epoch = DaysFromCivil(gregorianYear, 3, leap ? 21 : 22);
        for (var m = 1; m < month; m++) epoch += IndianMonthLength(leap, m);
        return epoch + day - 1;
    }

    private static (int year, int month, int day) IndianYmdFromEpochDays(long epoch)
    {
        var (gy, _, _) = CivilFromDays(epoch);
        var year = (int)gy - 78;
        while (IndianEpochDays(year + 1, 1, 1) <= epoch) year++;
        while (IndianEpochDays(year, 1, 1) > epoch) year--;

        var leap = GregorianLeap(year + 78);
        var dayOfYear = epoch - IndianEpochDays(year, 1, 1);
        var month = 1;
        while (dayOfYear >= IndianMonthLength(leap, month))
        {
            dayOfYear -= IndianMonthLength(leap, month);
            month++;
        }
        return (year, month, (int)dayOfYear + 1);
    }

    // Howard Hinnant's days_from_civil / civil_from_days about the ISO 1970-01-01 epoch (used by the
    // Gregorian-anchored Indian calendar).
    private static long DaysFromCivil(long y, long m, long d)
    {
        y -= m <= 2 ? 1 : 0;
        var era = (y >= 0 ? y : y - 399) / 400;
        var yoe = y - era * 400;
        var doy = (153 * (m > 2 ? m - 3 : m + 9) + 2) / 5 + d - 1;
        var doe = yoe * 365 + yoe / 4 - yoe / 100 + doy;
        return era * 146097 + doe - 719468;
    }

    private static (long y, long m, long d) CivilFromDays(long z)
    {
        z += 719468;
        var era = (z >= 0 ? z : z - 146096) / 146097;
        var doe = z - era * 146097;
        var yoe = (doe - doe / 1460 + doe / 36524 - doe / 146096) / 365;
        var y = yoe + era * 400;
        var doy = doe - (365 * yoe + yoe / 4 - yoe / 100);
        var mp = (5 * doy + 2) / 153;
        var d = doy - (153 * mp + 2) / 5 + 1;
        var m = mp < 10 ? mp + 3 : mp - 9;
        return (m <= 2 ? y + 1 : y, m, d);
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
