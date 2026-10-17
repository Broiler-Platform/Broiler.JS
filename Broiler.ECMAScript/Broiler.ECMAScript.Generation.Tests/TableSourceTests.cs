using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Broiler.ECMAScript.Generation.Time;
using Broiler.ECMAScript.Runners;
using Xunit;

namespace Broiler.ECMAScript.Generation.Tests;

/// <summary>
/// The source writers: what an engine compiles when it embeds the tables as C#. With Broiler.VM's
/// options they write Broiler.VM's generated files, as its own generators wrote them before its assurance
/// pass, byte for byte, which is what its milestone SJL-8.5 will regenerate them with.
/// </summary>
public sealed class TableSourceTests
{
    /// <summary>
    /// The SHA-256 of Broiler.VM's <c>src/Broiler.VM.Profile.JavaScript.Intl/JsTzdbTables.g.cs</c> at Broiler.VM
    /// <c>9508bd5</c> as its <c>TzdbTableGenerator</c> wrote it, before its assurance generator added the
    /// header and filled the fingerprints: the file from <c>using System;</c> on, each computed
    /// <c>Fingerprint=</c> value put back to <c>TBF</c>.
    /// </summary>
    private const string BroilerVmTzdbSourceSha256 = "1f9acc12f4c987e41f07d0bf1b9297c8957174670f86af05a3f8b124f5b4135d";

    /// <summary>The same, of <c>JsCalendarTables.g.cs</c> as its <c>CalendarTableGenerator</c> wrote it.</summary>
    private const string BroilerVmCalendarSourceSha256 = "a08517ce76bea1661fdfd8c6d1498e458279c8890c51548a0527fbc928a35b45";

    private static readonly Lazy<TzdbTables> Tzdb = new(static () => TimeDataHome.GenerateTzdbTables(TimeDataHome.Current));

    private static readonly Lazy<CalendarTables> Calendars = new(static () => TimeDataHome.GenerateCalendarTables(TimeDataHome.Current));

    /// <summary>Broiler.VM's options for <c>JsTzdbTables.g.cs</c>, as its generator's header constant states them.</summary>
    private static TableSourceOptions BroilerVmTzdb => new("Broiler.VM.Profile.JavaScript.Intl", "JsTzdbTables")
    {
        Header =
        [
            "Written by TzdbTableGenerator (src/tests/Broiler.VM.Architecture.Tests) from the IANA Time",
            "Zone Database 2026e members that src/tests/tzdb/pins/tzdb.pin names and CLDR 48.2.0's time zone",
            "keys, under decision JSD-0053. Rule N30 regenerates this file and compares it byte for byte, so",
            "a change belongs in the generator or a pinned archive and never here: run the architecture",
            "tests with BROILER_TZDB_WRITE=1.",
            "",
            "The offsets are derived from the IANA Time Zone Database, which is in the public domain, and",
            "the primary identifiers from CLDR data under the Unicode License v3 (SPDX Unicode-3.0), whose",
            "text THIRD_PARTY_NOTICES.md carries.",
        ],
        ClassAnnotation =
        [
            "Broiler-AI:           Origin=Derived; Spec=JSD-0053; IP=Low; Security=Low; Resources=1; Fingerprint=TBF",
            "Broiler-Human:        PENDING",
        ],
        MemberAnnotation =
        [
            "Broiler-AI:           EXEMPT=tzdb 2026e table data written by TzdbTableGenerator from the members tzdb.pin names, compared byte for byte by rule N30",
            "Broiler-Human:        PENDING",
        ],
    };

    /// <summary>Broiler.VM's options for <c>JsCalendarTables.g.cs</c>, its member's summary naming its own reader.</summary>
    private static TableSourceOptions BroilerVmCalendars => new("Broiler.VM.Profile.JavaScript.Intl", "JsCalendarTables")
    {
        Header =
        [
            "Written by CalendarTableGenerator (src/tests/Broiler.VM.Architecture.Tests) from the members of",
            "the icu_calendar 2.3.0 and calendrical_calculations 0.2.4 crates that",
            "src/tests/calendars/pins/calendars.pin names, under decision JSD-0056. Rule N32 regenerates this",
            "file and compares it byte for byte, so a change belongs in the generator or a pinned archive and",
            "never here: run the architecture tests with BROILER_CALENDARS_WRITE=1.",
            "",
            "The years are ICU4X's, under the Unicode License v3 (SPDX Unicode-3.0), and the Persian",
            "corrections calendrical_calculations', under the Apache License 2.0; THIRD_PARTY_NOTICES.md",
            "carries both.",
        ],
        ClassAnnotation =
        [
            "Broiler-AI:           Origin=Derived; Spec=JSD-0056; IP=Low; Security=Low; Resources=1; Fingerprint=TBF",
            "Broiler-Human:        PENDING",
        ],
        MemberAnnotation =
        [
            "Broiler-AI:           EXEMPT=calendar year data written by CalendarTableGenerator from the crate members calendars.pin names, compared byte for byte by rule N32",
            "Broiler-Human:        PENDING",
        ],
        MemberSummaries = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Calendars"] = "The Chinese, Korean and Qing years, the Umm al-Qura years, and the Persian calendar's non-leap corrections, in the layout JsCalendarData reads.",
        },
    };

    private static string Sha256(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    [Fact]
    public void With_Broiler_VM_s_options_the_writers_write_Broiler_VM_s_files()
    {
        Assert.Equal(BroilerVmTzdbSourceSha256, Sha256(TzdbTableSource.Write(Tzdb.Value, BroilerVmTzdb)));
        Assert.Equal(BroilerVmCalendarSourceSha256, Sha256(CalendarTableSource.Write(Calendars.Value, BroilerVmCalendars)));
    }

    [Fact]
    public void The_writers_write_the_caller_s_namespace_and_class_with_no_comment_of_their_own()
    {
        var text = TzdbTableSource.Write(Tzdb.Value, new TableSourceOptions("Example.Engine.Time", "TimeZoneTables"));

        Assert.StartsWith("using System;\n\nnamespace Example.Engine.Time;\n\n/// <summary>The tzdb 2026e tables, as the generator wrote them.</summary>\ninternal static class TimeZoneTables\n{\n", text, StringComparison.Ordinal);
        Assert.Contains("    internal const string Version = \"2026e\";\n", text, StringComparison.Ordinal);
        Assert.DoesNotContain("//", text.Replace("/// <summary>", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.DoesNotContain('\r', text);
        Assert.EndsWith("        \"\"\"u8;\n}\n", text, StringComparison.Ordinal);

        var calendars = CalendarTableSource.Write(Calendars.Value, new TableSourceOptions("Example.Engine.Time", "CalendarTables") { ClassSummary = "Years." });
        Assert.Contains("/// <summary>Years.</summary>\ninternal static class CalendarTables\n{\n    /// <summary>The Chinese, Korean", calendars, StringComparison.Ordinal);
        Assert.EndsWith(",\n    };\n}\n", calendars, StringComparison.Ordinal);
    }

    /// <summary>The C# text holds each table's bytes: the byte table's literals and the raw string literals' lines are the data.</summary>
    [Fact]
    public void The_written_literals_hold_the_tables_bytes()
    {
        var text = TzdbTableSource.Write(Tzdb.Value, new TableSourceOptions("N", "C"));

        Assert.Equal(Tzdb.Value.TimeZones.ToArray(), ByteLiteral(text, "TimeZones"));
        Assert.Equal(Tzdb.Value.TimeZoneIds.ToArray(), RawLiteral(text, "TimeZoneIds"));
        Assert.Equal(Tzdb.Value.TimeZoneRegions.ToArray(), RawLiteral(text, "TimeZoneRegions"));
        Assert.Equal(Calendars.Value.Calendars.ToArray(), ByteLiteral(CalendarTableSource.Write(Calendars.Value, new TableSourceOptions("N", "C")), "Calendars"));
    }

    [Fact]
    public void The_options_refuse_an_empty_namespace_or_class()
    {
        Assert.ThrowsAny<ArgumentException>(() => new TableSourceOptions(string.Empty, "C"));
        Assert.ThrowsAny<ArgumentException>(() => new TableSourceOptions("N", null!));
    }

    private static byte[] ByteLiteral(string text, string name)
    {
        var start = text.IndexOf("ReadOnlySpan<byte> " + name + " => new byte[]\n    {\n", StringComparison.Ordinal);
        var body = text[(text.IndexOf('{', start) + 1)..text.IndexOf("    };", start, StringComparison.Ordinal)];
        return body.Split([',', ' ', '\n'], StringSplitOptions.RemoveEmptyEntries).Select(static b => byte.Parse(b, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
    }

    private static byte[] RawLiteral(string text, string name)
    {
        var open = text.IndexOf("ReadOnlySpan<byte> " + name + " =>\n        \"\"\"\n", StringComparison.Ordinal);
        var from = text.IndexOf("\"\"\"\n", open, StringComparison.Ordinal) + 4;
        var to = text.IndexOf("\n        \"\"\"u8;", from, StringComparison.Ordinal);
        var lines = text[from..to].Split('\n').Select(static line => line[8..]);
        return Encoding.ASCII.GetBytes(string.Join('\n', lines));
    }
}
