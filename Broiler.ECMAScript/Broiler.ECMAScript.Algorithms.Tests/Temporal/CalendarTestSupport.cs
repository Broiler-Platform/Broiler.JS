using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;
using Xunit;

namespace Broiler.ECMAScript.Algorithms.Tests;

/// <summary>
/// What the calendar tests (milestone SJL-10.3, part B) share: one <see cref="TemporalCalendars"/> over the data
/// home's calendar table, the string forms an engine writes from the library's records, and the readers of
/// Broiler.VM's recorded answers.
/// </summary>
/// <remarks>
/// The string forms are the tests' own, written from the Temporal proposal's TemporalDateToString,
/// TemporalYearMonthToString, TemporalMonthDayToString and TemporalDurationToString, so that a line of an answer
/// file can be compared as the engine printed it. They are not the library's serialization, which is another
/// sub-area's.
/// </remarks>
internal static class CalendarTestSupport
{
    private static readonly Lazy<TemporalCalendars> Shared = new(() => new TemporalCalendars(TimeTablesFixture.CalendarTables));

    /// <summary>One set of calendars over the data home's table, shared by the tests.</summary>
    internal static TemporalCalendars Calendars => Shared.Value;

    /// <summary>The year of each calendar the correspondence's probes take as a leap year (its prelude's <c>LEAPYEAR</c>).</summary>
    internal static readonly Dictionary<string, long> LeapYear = new(StringComparer.Ordinal)
    {
        ["buddhist"] = 2567, ["chinese"] = 2023, ["coptic"] = 1739, ["dangi"] = 2023, ["ethioaa"] = 7515, ["ethiopic"] = 2015,
        ["gregory"] = 2024, ["hebrew"] = 5784, ["indian"] = 1946, ["islamic-civil"] = 1445, ["islamic-tbla"] = 1445,
        ["islamic-umalqura"] = 1445, ["iso8601"] = 2024, ["japanese"] = 2024, ["persian"] = 1403, ["roc"] = 113,
    };

    /// <summary>The fifteen calendars of Table 1 other than ISO 8601, in the probes' order.</summary>
    internal static readonly string[] NonIso =
    [
        "buddhist", "chinese", "coptic", "dangi", "ethioaa", "ethiopic", "gregory", "hebrew", "indian", "islamic-civil",
        "islamic-tbla", "islamic-umalqura", "japanese", "persian", "roc",
    ];

    /// <summary>A canonical identifier's calendar.</summary>
    internal static CalendarId Id(string identifier)
    {
        Assert.Equal(CalendarStatus.Completed, TemporalCalendars.Canonicalize(identifier, out var calendar));
        return calendar;
    }

    /// <summary>The answer an engine prints for a refusal: the error's constructor alone.</summary>
    internal static string Throws(CalendarStatus status) =>
        TemporalCalendars.IsTypeError(status) ? "THROWS TypeError" : "THROWS RangeError";

    /// <summary>An answer with a refusal's message dropped, so that only its constructor is compared.</summary>
    internal static string WithoutMessage(string answer)
    {
        if (!answer.StartsWith("THROWS ", StringComparison.Ordinal))
            return answer;

        var colon = answer.IndexOf(':', StringComparison.Ordinal);
        return colon < 0 ? answer : answer[..colon];
    }

    /// <summary>PadISOYear (s3.5.10).</summary>
    internal static string PadYear(long year) =>
        year is >= 0 and <= 9999
            ? year.ToString("0000", CultureInfo.InvariantCulture)
            : (year < 0 ? "-" : "+") + Math.Abs(year).ToString("000000", CultureInfo.InvariantCulture);

    /// <summary>An ISO date's string, with no annotation.</summary>
    internal static string Iso(IsoDate date) =>
        PadYear(date.Year) + "-" + date.Month.ToString("00", CultureInfo.InvariantCulture) + "-" + date.Day.ToString("00", CultureInfo.InvariantCulture);

    /// <summary>An ISO date's string from <c>-271821-04-19</c>, <c>0000-01-01</c> or <c>+275760-09-13</c>.</summary>
    internal static IsoDate ParseIso(string text)
    {
        var parts = text.Split('-', StringSplitOptions.None);
        var negative = text.StartsWith('-');
        var offset = negative ? 1 : 0;
        var year = long.Parse(parts[offset].TrimStart('+'), CultureInfo.InvariantCulture);
        return new IsoDate(negative ? -year : year, int.Parse(parts[offset + 1], CultureInfo.InvariantCulture), int.Parse(parts[offset + 2], CultureInfo.InvariantCulture));
    }

    /// <summary>FormatCalendarAnnotation (s12.3.15) under <c>auto</c>, <c>always</c> or <c>never</c>.</summary>
    internal static string Annotation(CalendarId calendar, string show) => show switch
    {
        "never" => string.Empty,
        "auto" when calendar == CalendarId.Iso8601 => string.Empty,
        _ => "[u-ca=" + TemporalCalendars.Identifier(calendar) + "]",
    };

    /// <summary>TemporalDateToString (s3.5.11).</summary>
    internal static string DateString(IsoDate date, CalendarId calendar, string show = "auto") => Iso(date) + Annotation(calendar, show);

    /// <summary>TemporalYearMonthToString (s9.5.6): the reference day shown outside ISO 8601 or when the annotation is forced.</summary>
    internal static string YearMonthString(IsoDate date, CalendarId calendar, string show = "auto")
    {
        var text = PadYear(date.Year) + "-" + date.Month.ToString("00", CultureInfo.InvariantCulture);
        if (show is "always" or "critical" || calendar != CalendarId.Iso8601)
            text += "-" + date.Day.ToString("00", CultureInfo.InvariantCulture);

        return text + Annotation(calendar, show);
    }

    /// <summary>TemporalMonthDayToString (s10.5.3): the reference year shown outside ISO 8601 or when the annotation is forced.</summary>
    internal static string MonthDayString(IsoDate date, CalendarId calendar, string show = "auto")
    {
        var text = date.Month.ToString("00", CultureInfo.InvariantCulture) + "-" + date.Day.ToString("00", CultureInfo.InvariantCulture);
        if (show is "always" or "critical" || calendar != CalendarId.Iso8601)
            text = PadYear(date.Year) + "-" + text;

        return text + Annotation(calendar, show);
    }

    /// <summary>TemporalDurationToString (s7.5.40) of a date duration.</summary>
    internal static string DurationString(CalendarDuration duration)
    {
        long[] fields = [duration.Years, duration.Months, duration.Weeks, duration.Days];
        var sign = fields.Select(Math.Sign).FirstOrDefault(static s => s != 0);
        if (sign == 0)
            return "PT0S";

        var text = new StringBuilder(sign < 0 ? "-P" : "P");
        var units = "YMWD";
        for (var i = 0; i < 4; i++)
        {
            if (fields[i] != 0)
                text.Append(Math.Abs(fields[i]).ToString(CultureInfo.InvariantCulture)).Append(units[i]);
        }

        return text.ToString();
    }

    /// <summary>A duration negated, as CreateNegatedTemporalDuration (s7.5.20) negates it.</summary>
    internal static CalendarDuration Negated(CalendarDuration duration) =>
        new(-duration.Years, -duration.Months, -duration.Weeks, -duration.Days);

    /// <summary>A date duration from a JSON object of <c>years</c>, <c>months</c>, <c>weeks</c> and <c>days</c>.</summary>
    internal static CalendarDuration ParseDuration(string json)
    {
        using var document = JsonDocument.Parse(json);
        long Get(string name) => document.RootElement.TryGetProperty(name, out var value) ? value.GetInt64() : 0;
        return new CalendarDuration(Get("years"), Get("months"), Get("weeks"), Get("days"));
    }

    /// <summary>A JSON string literal's value, as a probe's label carries a string.</summary>
    internal static string JsonString(string literal) => JsonSerializer.Deserialize<string>(literal)!;

    /// <summary>The calendar's date of an ISO date, which must complete.</summary>
    internal static CalendarDate Parts(CalendarId calendar, IsoDate date)
    {
        Assert.Equal(CalendarStatus.Completed, Calendars.IsoToDate(calendar, date, out var parts));
        return parts;
    }

    /// <summary>The probes' <c>ymd(d)</c>: <c>year/monthCode/day (month) iso date</c>.</summary>
    internal static string Ymd(CalendarId calendar, IsoDate date)
    {
        var d = Parts(calendar, date);
        return d.Year.ToString(CultureInfo.InvariantCulture) + "/" + d.MonthCode + "/" + d.Day.ToString(CultureInfo.InvariantCulture) +
            " (" + d.Month.ToString(CultureInfo.InvariantCulture) + ") iso " + Iso(date);
    }

    /// <summary>A number as JavaScript's <c>String</c> writes an integer, or <c>undefined</c>.</summary>
    internal static string Js(long? value) => value is { } v ? v.ToString(CultureInfo.InvariantCulture) : "undefined";

    /// <summary>A Boolean as JavaScript's <c>String</c> writes it.</summary>
    internal static string Js(bool value) => value ? "true" : "false";

    /// <summary>
    /// The lines of one of Broiler.VM's answer files in <c>Temporal/Correspondence/</c>, after its SHA-256 is checked
    /// against the copies' table.
    /// </summary>
    internal static IReadOnlyList<(string Label, string Answer)> Correspondence(string file, string sha256)
    {
        var path = Path.Combine(TestDirectory("Temporal", "Correspondence"), file);
        var bytes = File.ReadAllBytes(path);
        Assert.Equal(sha256, Convert.ToHexStringLower(SHA256.HashData(bytes)));
        return Lines(Encoding.UTF8.GetString(bytes));
    }

    /// <summary>A dataset file of the conformance corpus's <c>datasets/time/</c>, as committed.</summary>
    internal static string DatasetText(string file) =>
        File.ReadAllText(Path.Combine(RepositoryDirectory("conformance", "datasets", "time"), file), Encoding.UTF8);

    /// <summary>The <c>label => answer</c> lines of a text, comments and blank lines left out.</summary>
    internal static IReadOnlyList<(string Label, string Answer)> Lines(string text)
    {
        var result = new List<(string, string)>();
        foreach (var line in text.Split('\n'))
        {
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var arrow = line.IndexOf(" => ", StringComparison.Ordinal);
            Assert.True(arrow > 0, "not a line of an answer: " + line);
            result.Add((line[..arrow], line[(arrow + 4)..]));
        }

        return result;
    }

    /// <summary>A directory of this test project's sources, found from the build output upwards.</summary>
    private static string TestDirectory(params string[] parts) =>
        Path.Combine([RepositoryDirectory("Broiler.ECMAScript", "Broiler.ECMAScript.Algorithms.Tests"), .. parts]);

    /// <summary>A directory of the repository, found from the build output upwards.</summary>
    private static string RepositoryDirectory(params string[] parts)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine([directory.FullName, .. parts]);
            if (Directory.Exists(candidate))
                return candidate;
        }

        throw new DirectoryNotFoundException("no " + string.Join('/', parts) + " above " + AppContext.BaseDirectory);
    }
}
