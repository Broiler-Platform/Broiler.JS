using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Broiler.ECMAScript.Generation.Time;
using Broiler.ECMAScript.Runners;
using Xunit;

namespace Broiler.ECMAScript.Generation.Tests;

/// <summary>
/// The home's pinned inputs (milestone SJL-8.2): each archive and member hashes as its pin says, each pin
/// is Broiler.VM's, and nothing else sits beside it. The refusals are Broiler.VM's rules N29 and N31,
/// ported with the readers, over the home's copies.
/// </summary>
public sealed class PinTests
{
    /// <summary>
    /// The SHA-256 of Broiler.VM's <c>src/tests/tzdb/pins/tzdb.pin</c> at Broiler.VM <c>9508bd5</c>. The home's
    /// pin is a byte-for-byte copy of it; this test cannot read the other repository, so it holds the copy
    /// to the digest, and Broiler.VM's rule I4 holds its own pin to the home's from the other side.
    /// </summary>
    private const string BroilerVmTzdbPinSha256 = "ef9c7443115a802db68895b29ad6c584ddec666280120b61e38061fcf5215f6f";

    /// <summary>The SHA-256 of Broiler.VM's <c>src/tests/calendars/pins/calendars.pin</c> at <c>9508bd5</c>.</summary>
    private const string BroilerVmCalendarsPinSha256 = "53c5b2d380a8c69b06648ed2b57db76684619d8fd3493582c196942423d795da";

    /// <summary>The SHA-256 of Broiler.VM's <c>src/tests/cldr/pins/cldr-LICENSE.txt</c> at <c>9508bd5</c>.</summary>
    private const string BroilerVmCldrLicenceSha256 = "b49d0e9f8ead51ca8b7df6fec89cc3ae6809198b4b9d43c74216da0118f23f5b";

    /// <summary>Line 127 of Broiler.VM's <c>src/tests/cldr/pins/cldr.pin</c> at <c>9508bd5</c>: its entry for the time zone keys.</summary>
    private const string BroilerVmCldrPinTimeZoneLine =
        "file cldr-48.2.0/json/cldr-bcp47/bcp47/timezone.json 59702 c11423884420f00cf52733c854de5c898ddb6657ce560334c638a2bd994a1448";

    private static string Home => TimeDataHome.Current;

    private static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static IEnumerable<string> Listing(string directory) =>
        Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(directory, path).Replace(Path.DirectorySeparatorChar, '/'))
            .Order(StringComparer.Ordinal);

    // ---------------------------------------------------------------------------------------------
    // The tz database.

    [Fact]
    public void The_tz_archive_is_the_one_the_pin_describes_and_nothing_else_is_beside_it()
    {
        var pin = TimeDataHome.ReadTzdbPin(Home);
        var tarball = TimeDataHome.Tarball(Home, pin);
        var archive = pin.Extract(tarball);

        Assert.Empty(pin.TarballViolations(tarball));
        Assert.Empty(pin.Violations(archive));
        Assert.Equal(TzdbPin.PinnedVersion, pin.Version);
        Assert.Equal(TimeDataVersions.TzdbRelease, pin.Version);
        Assert.Equal("tzdata2026e.tar.gz", pin.ArchiveName);
        Assert.Equal("2 byte-identical", pin.Values["retrievals"]);
        Assert.Equal("public-domain", pin.Values["licence"]);
        Assert.Equal(12, pin.Members.Count);
        Assert.Equal(pin.Members.Select(static member => member.Name).Order(StringComparer.Ordinal), archive.Keys.Order(StringComparer.Ordinal));
        Assert.All(TzdbCompiler.SourceFiles, file => Assert.Contains(pin.Members, member => member.Name == file));
        Assert.DoesNotContain(pin.Members, static member => member.Name is "backzone" or "factory");
        Assert.StartsWith("Unless specified below, all files in the tz code and data", TzdbPin.Decode(archive["LICENSE"]), StringComparison.Ordinal);

        // NOTHING IS BESIDE THE PIN but the archive, its README and the zic comparison.
        Assert.Equal(
            new[] { "README.md", "check-zic.py", pin.ArchiveName, "tzdb.pin" }.Order(StringComparer.Ordinal),
            Listing(Path.Combine(Home, "tzdb")));
    }

    [Fact]
    public void The_tz_pin_is_Broiler_VM_s_byte_for_byte()
    {
        Assert.Equal(BroilerVmTzdbPinSha256, Sha256(File.ReadAllBytes(TimeDataHome.TzdbPinPath(Home))));
    }

    /// <summary>The hash clauses: one changed byte of the tarball, or of a member, is refused.</summary>
    [Fact]
    public void A_changed_byte_of_the_tz_archive_or_a_member_is_refused()
    {
        var pin = TimeDataHome.ReadTzdbPin(Home);
        var tarball = TimeDataHome.Tarball(Home, pin);
        var archive = new Dictionary<string, byte[]>(pin.Extract(tarball), StringComparer.Ordinal);
        tarball[tarball.Length / 2] ^= 1;

        var refused = Assert.Throws<InvalidDataException>(() => pin.Extract(tarball));
        Assert.Contains("nothing is generated from it", refused.Message, StringComparison.Ordinal);
        Assert.Contains("tzdata2026e.tar.gz hashes to", refused.Message, StringComparison.Ordinal);

        var europe = (byte[])archive["europe"].Clone();
        var at = Array.IndexOf(europe, (byte)'R', europe.Length / 2);
        europe[at + 1] = europe[at + 1] == (byte)'u' ? (byte)'v' : (byte)'u';
        archive["europe"] = europe;

        var violations = pin.Violations(archive).ToArray();
        Assert.Single(violations);
        Assert.Contains("`europe` hashes to", violations[0], StringComparison.Ordinal);

        archive.Remove("europe");
        Assert.Contains(pin.Violations(archive), static violation => violation.Contains("no member `europe`", StringComparison.Ordinal));
    }

    /// <summary>The version clause: a release stating another version is refused, whatever its hashes.</summary>
    [Fact]
    public void Another_tz_release_is_refused()
    {
        var pin = TimeDataHome.ReadTzdbPin(Home);
        var tarball = TimeDataHome.Tarball(Home, pin);
        var archive = new Dictionary<string, byte[]>(pin.Extract(tarball), StringComparer.Ordinal)
        {
            ["version"] = "2026d\n"u8.ToArray(),
        };

        Assert.Contains(pin.Violations(archive), static violation => violation.Contains("states version `2026d`", StringComparison.Ordinal));
        Assert.Contains(
            TzdbPin.Parse(File.ReadAllText(TimeDataHome.TzdbPinPath(Home)).Replace("version 2026e", "version 2026d", StringComparison.Ordinal))
                .TarballViolations(tarball),
            static violation => violation.Contains("names version `2026d`", StringComparison.Ordinal));
    }

    [Fact]
    public void A_malformed_tz_pin_is_refused()
    {
        Assert.Throws<InvalidDataException>(() => TzdbPin.Parse("member europe 12\n"));
        Assert.Throws<InvalidDataException>(() => TzdbPin.Parse("version 2026e\nversion 2026e\n"));
        Assert.Contains(TzdbPin.Parse("version 2026e\n").TarballViolations([]), static v => v.Contains("does not say `archived yes`", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------------------------------
    // CLDR's time zone keys.

    [Fact]
    public void The_CLDR_file_is_the_one_the_pin_describes_and_nothing_else_is_beside_it()
    {
        var pin = TimeDataHome.ReadCldrPin(Home);
        var files = TimeDataHome.CldrFiles(Home, pin);

        Assert.Empty(pin.Violations(files));
        Assert.Equal(CldrPin.PinnedVersion, pin.Version);
        Assert.Equal(TimeDataVersions.CldrVersion, pin.Version);
        Assert.Equal([TzdbTableGenerator.TimeZoneKeys], pin.Entries.Select(static entry => entry.Name));
        Assert.Equal("Unicode-3.0", pin.Values["licence"]);
        Assert.Single(pin.Integrities, static line => line.StartsWith("cldr-bcp47 sha512-", StringComparison.Ordinal));

        Assert.Equal(
            new[] { "README.md", "cldr-LICENSE.txt", "cldr.pin", TzdbTableGenerator.TimeZoneKeys }.Order(StringComparer.Ordinal),
            Listing(Path.Combine(Home, "cldr")));
    }

    [Fact]
    public void The_CLDR_pin_s_file_line_and_licence_are_Broiler_VM_s()
    {
        var lines = File.ReadAllText(TimeDataHome.CldrPinPath(Home)).Split('\n');
        Assert.Equal([BroilerVmCldrPinTimeZoneLine], lines.Where(static line => line.StartsWith("file ", StringComparison.Ordinal)));
        Assert.Equal(BroilerVmCldrLicenceSha256, Sha256(File.ReadAllBytes(Path.Combine(Home, "cldr", "cldr-LICENSE.txt"))));
        Assert.StartsWith("UNICODE LICENSE V3", File.ReadAllText(Path.Combine(Home, "cldr", "cldr-LICENSE.txt")), StringComparison.Ordinal);
    }

    [Fact]
    public void A_changed_CLDR_file_or_another_CLDR_version_is_refused_and_nothing_is_generated()
    {
        var pin = TimeDataHome.ReadCldrPin(Home);
        var files = new Dictionary<string, byte[]>(TimeDataHome.CldrFiles(Home, pin), StringComparer.Ordinal);
        var keys = (byte[])files[TzdbTableGenerator.TimeZoneKeys].Clone();
        keys[keys.Length / 2] ^= 1;
        files[TzdbTableGenerator.TimeZoneKeys] = keys;

        Assert.Contains(pin.Violations(files), static v => v.Contains("timezone.json hashes to", StringComparison.Ordinal));

        var tzdb = TimeDataHome.ReadTzdbPin(Home);
        var archive = tzdb.Extract(TimeDataHome.Tarball(Home, tzdb));
        var refused = Assert.Throws<InvalidDataException>(() => TzdbTableGenerator.Generate(tzdb, archive, pin, files));
        Assert.Contains("The CLDR archive is not the one cldr.pin describes", refused.Message, StringComparison.Ordinal);

        var other = CldrPin.Parse(File.ReadAllText(TimeDataHome.CldrPinPath(Home)).Replace("version 48.2.0", "version 47.0.0", StringComparison.Ordinal));
        Assert.Contains(other.Violations(TimeDataHome.CldrFiles(Home, pin)), static v => v.Contains("names version `47.0.0`", StringComparison.Ordinal));

        // A pin that does not name the time zone keys is refused, whatever it names.
        var none = CldrPin.Parse("version 48.2.0\narchived yes\n");
        Assert.Throws<InvalidDataException>(() => TzdbTableGenerator.Generate(tzdb, archive, none, TimeDataHome.CldrFiles(Home, pin)));
    }

    // ---------------------------------------------------------------------------------------------
    // The calendar crates.

    [Fact]
    public void The_calendar_crates_are_the_ones_the_pin_describes_and_nothing_else_is_beside_them()
    {
        var pin = TimeDataHome.ReadCalendarPin(Home);
        var crates = TimeDataHome.Crates(Home, pin);
        var members = pin.Extract(crates);

        Assert.Empty(pin.ArchiveViolations(crates));
        Assert.Empty(pin.Violations(members));
        Assert.Equal("2 byte-identical", pin.Values["retrievals"]);
        Assert.Equal("Unicode-3.0", pin.Values["licence icu_calendar-2.3.0.crate"]);
        Assert.Equal("Apache-2.0", pin.Values["licence calendrical_calculations-0.2.4.crate"]);
        Assert.Equal(TimeDataVersions.IcuCalendarVersion, CalendarPin.PinnedVersions["icu_calendar"]);
        Assert.Equal(TimeDataVersions.CalendricalCalculationsVersion, CalendarPin.PinnedVersions["calendrical_calculations"]);
        Assert.Equal(9, pin.Members.Count);
        Assert.Equal(pin.Members.Select(static member => member.Name).Order(StringComparer.Ordinal), members.Keys.Order(StringComparer.Ordinal));

        foreach (var file in new[] { CalendarTableGenerator.China, CalendarTableGenerator.Korea, CalendarTableGenerator.Qing, CalendarTableGenerator.UmmAlQura, CalendarTableGenerator.Persian })
            Assert.Contains(pin.Members, member => member.Name == file);

        Assert.StartsWith("UNICODE LICENSE V3", CalendarPin.Decode(members["icu_calendar-2.3.0/LICENSE"]), StringComparison.Ordinal);
        Assert.Contains("Apache License", CalendarPin.Decode(members["calendrical_calculations-0.2.4/LICENSE"]), StringComparison.Ordinal);
        Assert.Equal(("icu_calendar", "2.3.0"), CalendarPin.PackageOf(CalendarPin.Decode(members["icu_calendar-2.3.0/Cargo.toml"])));

        // NOTHING IS BESIDE THE PIN but the two crates and the README.
        Assert.Equal(
            new[] { "README.md", "calendars.pin" }.Concat(pin.Archives.Select(static archive => archive.Name)).Order(StringComparer.Ordinal),
            Listing(Path.Combine(Home, "calendars")));
    }

    [Fact]
    public void The_calendar_pin_is_Broiler_VM_s_byte_for_byte()
    {
        Assert.Equal(BroilerVmCalendarsPinSha256, Sha256(File.ReadAllBytes(TimeDataHome.CalendarPinPath(Home))));
    }

    /// <summary>The hash clauses: one changed byte of a crate, or of a member, is refused.</summary>
    [Fact]
    public void A_changed_byte_of_a_crate_or_a_member_is_refused()
    {
        var pin = TimeDataHome.ReadCalendarPin(Home);
        var crates = new Dictionary<string, byte[]>(TimeDataHome.Crates(Home, pin), StringComparer.Ordinal);
        var members = new Dictionary<string, byte[]>(pin.Extract(crates), StringComparer.Ordinal);
        var crate = (byte[])crates["icu_calendar-2.3.0.crate"].Clone();
        crate[crate.Length / 2] ^= 1;
        crates["icu_calendar-2.3.0.crate"] = crate;

        var refused = Assert.Throws<InvalidDataException>(() => pin.Extract(crates));
        Assert.Contains("nothing is generated from them", refused.Message, StringComparison.Ordinal);
        Assert.Contains("icu_calendar-2.3.0.crate hashes to", refused.Message, StringComparison.Ordinal);

        var china = (byte[])members[CalendarTableGenerator.China].Clone();
        var at = Array.IndexOf(china, (byte)'l', china.Length / 2);
        china[at] = (byte)'s';
        members[CalendarTableGenerator.China] = china;

        var violations = pin.Violations(members).ToArray();
        Assert.Single(violations);
        Assert.Contains("china_data.rs` hashes to", violations[0], StringComparison.Ordinal);
        Assert.Throws<InvalidDataException>(() => CalendarTableGenerator.Generate(pin, members));

        members.Remove(CalendarTableGenerator.China);
        Assert.Contains(pin.Violations(members), static violation => violation.Contains("no member `icu_calendar-2.3.0/src/cal/east_asian_traditional/china_data.rs`", StringComparison.Ordinal));
    }

    /// <summary>The version clause: a crate stating another version is refused, whatever its hashes.</summary>
    [Fact]
    public void Another_crate_release_is_refused()
    {
        var pin = TimeDataHome.ReadCalendarPin(Home);
        var members = new Dictionary<string, byte[]>(pin.Extract(TimeDataHome.Crates(Home, pin)), StringComparer.Ordinal);
        var manifest = CalendarPin.Decode(members["icu_calendar-2.3.0/Cargo.toml"]);
        members["icu_calendar-2.3.0/Cargo.toml"] = Encoding.UTF8.GetBytes(manifest.Replace("version = \"2.3.0\"", "version = \"2.2.1\"", StringComparison.Ordinal));

        Assert.Contains(pin.Violations(members), static violation => violation.Contains("states the package `icu_calendar` 2.2.1", StringComparison.Ordinal));
        Assert.Contains(
            CalendarPin.Parse(File.ReadAllText(TimeDataHome.CalendarPinPath(Home)).Replace("archive icu_calendar-2.3.0.crate", "archive icu_calendar-2.2.1.crate", StringComparison.Ordinal))
                .ArchiveViolations(new Dictionary<string, byte[]>(StringComparer.Ordinal)),
            static violation => violation.Contains("`icu_calendar-2.2.1.crate`, which is not a pinned crate version", StringComparison.Ordinal));
    }
}
