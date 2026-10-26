using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Broiler.ECMAScript.Generation.Intl;
using Broiler.ECMAScript.Generation.Time;
using Broiler.ECMAScript.Runners;
using Broiler.Unicode.Generation;
using Xunit;

namespace Broiler.ECMAScript.Generation.Tests;

/// <summary>
/// The Intl data home's pins (milestone SJL-9.2b; decision JSD-0082, decisions D10 and D12): the CLDR archive
/// against its pin, Broiler.VM's lines in that pin unchanged, the <c>timezone.json</c> entry and copy equal to the
/// time home's, the UCD copies against their pin and the owner's, and the pin readers' refusals. Each check has a
/// control in which one hash is changed.
/// </summary>
public sealed class IntlPinTests
{
    /// <summary>
    /// The SHA-256 of Broiler.VM's <c>src/tests/cldr/pins/cldr.pin</c> lines from <c>data</c> to its last
    /// <c>file</c> line, each ended by LF, at Broiler.VM <c>4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f</c>: the lines
    /// the home's pin holds unchanged (decision D10). Broiler.VM's rule I4 reads the same lines entry by entry.
    /// </summary>
    internal const string BroilerVmPinLinesSha256 = "289c6f32acb9e9b20e4d13031ad80ba551910aea20cf8b71574d2bf5e50d2e84";

    /// <summary>
    /// The names files cldr-localenames-full 48.2.0 carries for none of these main-data locales, which the
    /// generator therefore reads none of: found by listing the package's tarball at retrieval, and listed in the
    /// pin's README.
    /// </summary>
    internal static readonly string[] AbsentNamesFiles =
    [
        "aa/languages.json", "aa/scripts.json", "aa/territories.json", "aa/variants.json", "ab/scripts.json",
        "ab/territories.json", "ab/variants.json", "af/variants.json", "az/variants.json", "en-Shaw/languages.json",
        "en-Shaw/scripts.json", "en-Shaw/territories.json", "en-Shaw/variants.json", "ff/scripts.json", "ff/variants.json",
        "gv/scripts.json", "gv/variants.json", "jbo/scripts.json", "jbo/territories.json", "jbo/variants.json",
        "ln/scripts.json", "ln/variants.json", "pap/scripts.json", "pap/variants.json", "sa/variants.json",
        "si/variants.json", "und/languages.json", "und/scripts.json", "und/territories.json", "und/variants.json",
        "ur/variants.json", "uz-Cyrl/variants.json", "uz/variants.json", "yi/variants.json",
    ];

    private static string Home => IntlDataHome.Current;

    private static string Sha256(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    /// <summary>A pin text with one recorded digest changed: the first hexadecimal digit after the name.</summary>
    private static string WithOneHashChanged(string text, string name)
    {
        var at = text.IndexOf("file " + name + " ", StringComparison.Ordinal);
        var digest = text.IndexOf(' ', at + 5 + name.Length + 1) + 1;
        var changed = text[digest] == '0' ? '1' : '0';
        return text[..digest] + changed + text[(digest + 1)..];
    }

    [Fact]
    public void The_CLDR_archive_is_the_one_its_pin_describes_and_the_pin_names_every_file_beside_it()
    {
        var pin = IntlDataHome.ReadCldrPin(Home);
        var archive = IntlDataHome.CldrFiles(Home, pin);

        Assert.Empty(pin.Violations(archive));
        Assert.Equal(IntlDataVersions.CldrVersion, pin.Version);
        Assert.Equal(IntlDataVersions.CldrMajor, pin.Values["cldr-major"]);
        Assert.Equal(IntlDataVersions.UcaVersion, pin.Values["uca-version"]);
        Assert.Equal("2 byte-identical", pin.Values["retrievals"]);
        Assert.Equal("yes", pin.Values["archived"]);

        // EVERY PACKAGE A MAIN-DATA FILE CAME FROM states its integrity: the extension added files, not packages.
        var packages = pin.Integrities.Select(static line => line.Split(' ')[0]).ToHashSet(StringComparer.Ordinal);
        foreach (var entry in pin.Entries.Where(static entry => entry.Name.StartsWith("cldr-48.2.0/json/", StringComparison.Ordinal)))
            Assert.Contains(entry.Name.Split('/')[2], packages);

        // THE PIN NAMES EVERY FILE BESIDE IT, as Broiler.VM's rule N27 holds its own.
        var directory = Path.Combine(Home, "cldr");
        var archived = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(directory, file).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(static name => name is not "cldr.pin" and not "README.md")
            .Order(StringComparer.Ordinal);
        Assert.Equal(pin.Entries.Select(static entry => entry.Name).Order(StringComparer.Ordinal), archived);

        // EVERY VERSION-BEARING KIND IS PRESENT, so the version clause is exercised on real files.
        Assert.Contains(pin.Entries, static entry => entry.Name.EndsWith("/package.json", StringComparison.Ordinal));
        Assert.Contains(pin.Entries, static entry => entry.Name.Contains("/supplemental/", StringComparison.Ordinal));
        Assert.Contains(pin.Entries, static entry => entry.Name.EndsWith("/common/uca/allkeys_CLDR.txt", StringComparison.Ordinal));

        var licence = ArchiveReading.Decode(archive["cldr-LICENSE.txt"]);
        Assert.StartsWith("UNICODE LICENSE V3", licence, StringComparison.Ordinal);
    }

    /// <summary>
    /// Decision D10: every line of Broiler.VM's pin from <c>data</c> to its last <c>file</c> line is in the home's
    /// pin, unchanged and in order - so each of its entries, its <c>version</c>, <c>uca-version</c> and
    /// <c>json-integrity</c> lines equal the home's - and its files are the home's copies.
    /// </summary>
    [Fact]
    public void Broiler_VM_s_pin_lines_are_in_the_home_s_pin_unchanged()
    {
        var lines = IntlDataHome.BroilerVmPinLines(Home);
        Assert.Equal(BroilerVmPinLinesSha256, Sha256(lines));

        var vm = IntlCldrPin.Parse(lines);
        var home = IntlDataHome.ReadCldrPin(Home);
        Assert.NotEmpty(vm.Entries);
        Assert.All(vm.Entries, entry => Assert.Equal(entry, home.Entry(entry.Name)));
        Assert.Equal(vm.Integrities, home.Integrities);
        Assert.Equal(vm.Values, home.Values);

        // THE ENTRY-WISE CLAUSE, the shape of Broiler.VM's rule I4: the home's files satisfy Broiler.VM's lines alone.
        Assert.Empty(vm.Violations(IntlDataHome.CldrFiles(Home, vm)));

        // The control: one hash changed in Broiler.VM's lines.
        var changed = WithOneHashChanged(lines, "cldr-48.2.0/json/cldr-core/supplemental/plurals.json");
        Assert.NotEqual(BroilerVmPinLinesSha256, Sha256(changed));
        var refused = IntlCldrPin.Parse(changed);
        Assert.Contains(refused.Violations(IntlDataHome.CldrFiles(Home, vm)), static violation => violation.Contains("plurals.json hashes to", StringComparison.Ordinal));
        Assert.Contains(refused.Entries, entry => !home.Entries.Contains(entry));
    }

    /// <summary>
    /// Decision D10: the time home's pin is unchanged, and its one entry, <c>timezone.json</c>, and its copy equal
    /// the Intl home's, byte for byte.
    /// </summary>
    [Fact]
    public void The_timezone_json_entry_and_copy_equal_the_time_home_s()
    {
        const string Name = "cldr-48.2.0/json/cldr-bcp47/bcp47/timezone.json";
        var timeHome = TimeDataHome.Current;
        var time = TimeDataHome.ReadCldrPin(timeHome);
        var intl = IntlDataHome.ReadCldrPin(Home);
        var timeEntry = time.Entry(Name);
        var intlEntry = intl.Entry(Name);

        Assert.Equal((timeEntry.Name, timeEntry.Bytes, timeEntry.Sha256), (intlEntry.Name, intlEntry.Bytes, intlEntry.Sha256));
        Assert.Equal(TimeDataHome.CldrFiles(timeHome, time)[Name], IntlDataHome.CldrFiles(Home, intl)[Name]);

        // The control: the Intl pin's entry with one hash changed no longer equals the time home's.
        var changed = IntlCldrPin.Parse(WithOneHashChanged(File.ReadAllText(IntlDataHome.CldrPinPath(Home)), Name)).Entry(Name);
        Assert.NotEqual(timeEntry.Sha256, changed.Sha256);
    }

    /// <summary>
    /// Decision D12: the UCD copies are the files their pin describes, and each <c>file</c> line equals the line
    /// Broiler.Unicode's own pin (the owner's, <c>UnicodePin.Owner()</c>) gives the same file.
    /// </summary>
    [Fact]
    public void The_UCD_copies_equal_their_pin_and_the_owner_s_entries()
    {
        var pin = IntlDataHome.ReadUcdPin(Home);
        var files = IntlDataHome.UcdFiles(Home, pin);

        Assert.Empty(pin.Violations(files));
        Assert.Equal(IntlDataVersions.UnicodeVersion, pin.Version);
        Assert.Equal(UnicodePin.PinnedVersion, pin.Version);
        Assert.Contains(pin.Entries, static entry => entry.Name == CldrUcd.UnicodeDataFile);
        Assert.Contains(pin.Entries, static entry => entry.Name == CldrUcd.PropListFile);

        var owner = UnicodePin.Owner();
        Assert.All(pin.Entries, entry =>
        {
            var owned = owner.Entry(entry.Name);
            Assert.Equal((entry.Bytes, entry.Sha256), (owned.Bytes, owned.Sha256));
        });

        var directory = Path.Combine(Home, "ucd");
        var archived = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(directory, file).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(static name => name is not "ucd.pin" and not "README.md")
            .Order(StringComparer.Ordinal);
        Assert.Equal(pin.Entries.Select(static entry => entry.Name).Order(StringComparer.Ordinal), archived);

        // The control: one hash changed no longer equals the owner's line, and the copy is refused under it.
        var changed = IntlUcdPin.Parse(WithOneHashChanged(File.ReadAllText(IntlDataHome.UcdPinPath(Home)), CldrUcd.UnicodeDataFile));
        Assert.NotEqual(owner.Entry(CldrUcd.UnicodeDataFile).Sha256, changed.Entry(CldrUcd.UnicodeDataFile).Sha256);
        Assert.Throws<InvalidDataException>(() => changed.Verified(files));
    }

    /// <summary>The hash clause: one changed byte of one input is reported for that file alone, and nothing is generated.</summary>
    [Fact]
    public void A_changed_input_byte_is_refused_before_anything_is_generated()
    {
        var pin = IntlDataHome.ReadCldrPin(Home);
        var archive = new Dictionary<string, byte[]>(IntlDataHome.CldrFiles(Home, pin), StringComparer.Ordinal);
        const string Name = "cldr-48.2.0/json/cldr-numbers-full/main/fr/numbers.json";
        var changed = (byte[])archive[Name].Clone();

        // A LETTER INSIDE A VALUE, so the file is still JSON and only the digest tells.
        changed[Array.IndexOf(changed, (byte)'n', Array.IndexOf(changed, (byte)'"', changed.Length / 2))] = (byte)'m';
        archive[Name] = changed;

        var violations = pin.Violations(archive).ToArray();
        Assert.Single(violations);
        Assert.Contains("fr/numbers.json hashes to", violations[0], StringComparison.Ordinal);

        var refused = Assert.Throws<InvalidDataException>(() =>
            CldrTableGenerator.Generate(pin, archive, IntlDataHome.Ucd(Home), IntlDataHome.BroilerJsOptions(Home)));
        Assert.Contains("nothing is generated from it", refused.Message, StringComparison.Ordinal);

        archive.Remove("cldr-48.2.0/json/cldr-core/supplemental/likelySubtags.json");
        Assert.Contains(pin.Violations(archive), static violation => violation.Contains("likelySubtags.json is pinned and no file is there", StringComparison.Ordinal));
    }

    /// <summary>
    /// The generator reads the files its pin names and no other: a BCP 47 file handed in beside Broiler.VM's pinned
    /// files, under the directory the extensions table enumerates, changes no table. The control: the same file,
    /// once a pin line names it, adds its key.
    /// </summary>
    [Fact]
    public void A_file_the_pin_does_not_name_is_never_read()
    {
        const string Name = "cldr-48.2.0/json/cldr-bcp47/bcp47/zz-unpinned.json";
        var bytes = "{\"keyword\":{\"u\":{\"zz\":{\"_description\":\"unpinned\",\"yes\":{\"_description\":\"unpinned\"}}}}}"u8.ToArray();
        var vmLines = IntlDataHome.BroilerVmPinLines(Home);
        var pin = IntlCldrPin.Parse(vmLines);
        var files = IntlDataHome.CldrFiles(Home, pin);
        var withExtra = new Dictionary<string, byte[]>(files, StringComparer.Ordinal) { [Name] = bytes };
        var ucd = IntlDataHome.Ucd(Home);
        var options = IntlDataHome.BroilerVmOptions(Home);

        var pinned = CldrTableGenerator.Generate(pin, files, ucd, options);
        var handedIn = CldrTableGenerator.Generate(pin, withExtra, ucd, options);
        Assert.Equal(pinned.Tables.Select(static table => Hash(table.Data.ToArray())), handedIn.Tables.Select(static table => Hash(table.Data.ToArray())));
        Assert.DoesNotContain("u|zz|", Encoding.ASCII.GetString(handedIn["Extensions"].Data.Span), StringComparison.Ordinal);

        // The control: a pin that names the file reads it.
        var naming = IntlCldrPin.Parse(vmLines + "file " + Name + " " + bytes.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + Hash(bytes) + "\n");
        var read = CldrTableGenerator.Generate(naming, withExtra, ucd, options);
        Assert.Contains("u|zz||", Encoding.ASCII.GetString(read["Extensions"].Data.Span), StringComparison.Ordinal);
    }

    /// <summary>
    /// The version clause: a package, a supplemental file or a UCA file of another release is refused even under a
    /// pin that records exactly its bytes, and a pin of another CLDR or UCA version is refused.
    /// </summary>
    [Fact]
    public void A_file_or_pin_of_another_release_is_refused()
    {
        Assert.NotNull(IntlCldrPin.VersionViolation("cldr-48.2.0/json/cldr-core/package.json", "{\"version\":\"47.0.0\"}"u8.ToArray()));
        Assert.Null(IntlCldrPin.VersionViolation("cldr-48.2.0/json/cldr-core/package.json", "{\"version\":\"48.2.0\"}"u8.ToArray()));
        Assert.NotNull(IntlCldrPin.VersionViolation("cldr-48.2.0/json/cldr-core/supplemental/plurals.json", "{\"supplemental\":{\"version\":{\"_cldrVersion\":\"45\"}}}"u8.ToArray()));
        Assert.Null(IntlCldrPin.VersionViolation("cldr-48.2.0/json/cldr-core/supplemental/plurals.json", "{\"supplemental\":{\"version\":{\"_cldrVersion\":\"48\"}}}"u8.ToArray()));
        Assert.NotNull(IntlCldrPin.VersionViolation("cldr-48.2.0/common/uca/allkeys_CLDR.txt", "# UCA Version: 16.0.0\n"u8.ToArray()));
        Assert.Null(IntlCldrPin.VersionViolation("cldr-48.2.0/common/uca/allkeys_CLDR.txt", "# x\n# UCA Version: 17.0.0\n"u8.ToArray()));

        var text = File.ReadAllText(IntlDataHome.CldrPinPath(Home));
        var archive = IntlDataHome.CldrFiles(Home, IntlDataHome.ReadCldrPin(Home));
        Assert.Contains(IntlCldrPin.Parse(text.Replace("\nversion 48.2.0\n", "\nversion 48.1.0\n", StringComparison.Ordinal)).Violations(archive),
            static violation => violation.Contains("names version `48.1.0`", StringComparison.Ordinal));
        Assert.Contains(IntlCldrPin.Parse(text.Replace("\nuca-version 17.0.0\n", "\nuca-version 16.0.0\n", StringComparison.Ordinal)).Violations(archive),
            static violation => violation.Contains("names UCA version `16.0.0`", StringComparison.Ordinal));
        Assert.Throws<InvalidDataException>(() => IntlCldrPin.Parse("version 48.2.0\nversion 48.2.0\n"));
        Assert.Throws<InvalidDataException>(() => IntlCldrPin.Parse("file a.json 12\n"));
    }

    /// <summary>The archives and the UCD copies are binary in .gitattributes, so no checkout filter rewrites a line ending under a recorded hash.</summary>
    [Fact]
    public void The_archives_are_declared_binary()
    {
        var attributes = File.ReadAllLines(Path.Combine(Home, "..", "..", "..", ".gitattributes"));
        Assert.Contains("Broiler.ECMAScript/data/intl/cldr/cldr-48.2.0/** binary", attributes);
        Assert.Contains("Broiler.ECMAScript/data/intl/cldr/cldr-LICENSE.txt binary", attributes);
        Assert.Contains("Broiler.ECMAScript/data/intl/ucd/ucd-17.0.0/** binary", attributes);
        Assert.Contains("Broiler.ECMAScript/data/intl/ucd/unicode-LICENSE.txt binary", attributes);
    }

    /// <summary>
    /// The optional names files Broiler.JS's options ask for and the pin does not name are exactly the ones
    /// cldr-localenames-full 48.2.0 does not carry, so no file was left out of the archive by mistake.
    /// </summary>
    [Fact]
    public void The_absent_names_files_are_the_ones_CLDR_does_not_carry()
    {
        var tables = IntlDataHome.GenerateBroilerJs(Home);
        Assert.Equal(AbsentNamesFiles.Select(static name => "cldr-48.2.0/json/cldr-localenames-full/main/" + name), tables.AbsentFiles);

        var readme = File.ReadAllText(Path.Combine(Home, "cldr", "README.md"));
        Assert.All(AbsentNamesFiles, name => Assert.Contains(name, readme, StringComparison.Ordinal));
    }

    /// <summary>
    /// Decision D5's lists: Broiler.VM's unchanged; Broiler.JS's main-data locales exactly the tags of its list
    /// cldr-core's availableLocales.json names among its <c>full</c> locales, and every other tag one of CLDR's
    /// default-content locales.
    /// </summary>
    [Fact]
    public void The_locale_lists_are_decision_D5_s()
    {
        var vm = IntlDataHome.ReadList(Home, IntlDataHome.BroilerVmList);
        Assert.Equal(["de", "de-DE", "en", "en-US", "und"], vm.Locales);
        Assert.Equal(["de", "en"], vm.MainDataLocales);
        Assert.Equal(["de", "en"], vm.SupplementalLanguages);
        Assert.False(vm.EverySupplementalLanguage);

        var js = IntlDataHome.ReadList(Home, IntlDataHome.BroilerJsList);
        var archive = IntlDataHome.CldrFiles(Home, IntlDataHome.ReadCldrPin(Home));
        using var available = JsonDocument.Parse(archive["cldr-48.2.0/json/cldr-core/availableLocales.json"]);
        using var defaults = JsonDocument.Parse(archive["cldr-48.2.0/json/cldr-core/defaultContent.json"]);
        var full = available.RootElement.GetProperty("availableLocales").GetProperty("full").EnumerateArray().Select(static tag => tag.GetString()!).ToHashSet(StringComparer.Ordinal);
        var content = defaults.RootElement.GetProperty("defaultContent").EnumerateArray().Select(static tag => tag.GetString()!).ToHashSet(StringComparer.Ordinal);

        Assert.True(js.EverySupplementalLanguage);
        Assert.Equal(js.Locales.Where(full.Contains), js.MainDataLocales);
        Assert.All(js.Locales.Except(js.MainDataLocales), tag => Assert.Contains(tag, content));
        Assert.Contains("en-US", js.Locales);
        Assert.Contains("und", js.MainDataLocales);

        // Source 1 of decision D5: the languages Broiler.Unicode.Cldr.LocaleData serves today.
        Assert.All(new[] { "ar", "de", "en", "es", "fr", "it", "ja", "ko", "nl", "pl", "pt", "ru", "sv", "tr", "zh" }, language => Assert.Contains(language, js.MainDataLocales));
    }

    /// <summary>The list reader refuses a list out of order, a repeated or malformed tag, an unknown statement and an empty list.</summary>
    [Fact]
    public void The_locale_list_reader_refuses_a_malformed_list()
    {
        Assert.Throws<InvalidDataException>(() => IntlLocaleList.Parse("locale en\nlocale de\n"));
        Assert.Throws<InvalidDataException>(() => IntlLocaleList.Parse("locale de\nlocale de main\n"));
        Assert.Throws<InvalidDataException>(() => IntlLocaleList.Parse("locale de_DE\n"));
        Assert.Throws<InvalidDataException>(() => IntlLocaleList.Parse("locale de primary\n"));
        Assert.Throws<InvalidDataException>(() => IntlLocaleList.Parse("locale de\nsupplemental *\nsupplemental de\n"));
        Assert.Throws<InvalidDataException>(() => IntlLocaleList.Parse("# nothing\n"));
        Assert.Equal(["de", "en"], IntlLocaleList.Parse("# a list\nlocale de main\n\nlocale en\n").Locales);
    }

    /// <summary>The UCD subset reads the properties the generator uses as the UCD states them.</summary>
    [Fact]
    public void The_UCD_subset_reads_what_the_generator_uses()
    {
        var ucd = IntlDataHome.Ucd(Home);

        Assert.Equal([0x65, 0x301], ucd.CanonicalDecomposition[0xE9]);
        Assert.Equal([0x41, 0x30A], ucd.CanonicalDecomposition[0x212B]);
        Assert.False(ucd.CanonicalDecomposition.ContainsKey(0xAC00));
        Assert.Equal(230, ucd.CombiningClass(0x301));
        Assert.Equal(0, ucd.CombiningClass(0x41));
        Assert.True(ucd.IsSymbolOrSeparator('$'));
        Assert.True(ucd.IsSymbolOrSeparator(0xA0));
        Assert.False(ucd.IsSymbolOrSeparator('A'));
        Assert.Contains(ucd.UnifiedIdeographs, static range => range.First <= 0x4E00 && 0x9FFF <= range.Last);
        Assert.Equal([0x1100, 0x1161], CldrUcd.Hangul(0xAC00));

        var files = IntlDataHome.UcdFiles(Home, IntlDataHome.ReadUcdPin(Home));
        var broken = new Dictionary<string, byte[]>(files, StringComparer.Ordinal)
        {
            [CldrUcd.UnicodeDataFile] = Encoding.ASCII.GetBytes("0041;LATIN CAPITAL LETTER A;Lu;0;L;;;;;N;;;;0061\n"),
        };
        Assert.Throws<InvalidDataException>(() => CldrUcd.Read(broken));
        Assert.NotEqual(Hash(files[CldrUcd.UnicodeDataFile]), Hash(broken[CldrUcd.UnicodeDataFile]));
    }
}
