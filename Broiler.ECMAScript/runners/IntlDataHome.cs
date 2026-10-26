using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Broiler.ECMAScript.Generation.Intl;

namespace Broiler.ECMAScript.Runners;

/// <summary>
/// The Intl data home, <c>Broiler.ECMAScript/data/intl/</c> in this repository (milestone SJL-9.2b): its pins,
/// archives and locale lists read from disk, the tables <c>Broiler.ECMAScript.Generation</c> writes from them for
/// each embedding, and the files of its generated area, <c>generated/</c>, as they are committed and as the
/// generator writes them.
/// </summary>
/// <remarks>
/// <b>This file is linked into the test projects that read the home from disk and is never compiled into a
/// library.</b> The generator takes each pin's text and each file's bytes and does no I/O (convention 8), so the
/// reading lives here, once, for rule SL7 in the architecture tests and for the generator's own tests.
/// </remarks>
internal static class IntlDataHome
{
    /// <summary>The environment variable that turns rule SL7's comparison of the generated area into the generator's write.</summary>
    internal const string WriteVariable = "BROILER_ECMASCRIPT_INTL_WRITE";

    /// <summary>Broiler.JS's locale list, the one whose tables the generated area holds.</summary>
    internal const string BroilerJsList = "broiler-js.txt";

    /// <summary>Broiler.VM's locale list, whose tables Broiler.VM embeds as its <c>JsCldrTables.g.cs</c>.</summary>
    internal const string BroilerVmList = "broiler-vm.txt";

    /// <summary>The comment lines of the generated area's manifest.</summary>
    internal static IReadOnlyList<string> ManifestHeader { get; } =
    [
        "broiler-js intl data tables 1",
        string.Empty,
        "GENERATED - DO NOT EDIT. Broiler.ECMAScript.Generation writes this file and the tables beside it from",
        "the pins in ../cldr/ and ../ucd/, for Broiler.JS's locale list, ../locales/broiler-js.txt. Rule SL7",
        "regenerates every file of this directory and compares it byte for byte; run the architecture tests",
        "with " + WriteVariable + "=1 to write them instead. No package carries these files (decision",
        "SJL-D-6 (c)): Broiler.JS embeds them. A `table` line is `table <file> <bytes> <sha256>`.",
    ];

    /// <summary>
    /// The home, found from <paramref name="start"/> upwards: the first directory with a
    /// Broiler.ECMAScript/data/intl/cldr/cldr.pin below it.
    /// </summary>
    internal static string Home(string start)
    {
        for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "Broiler.ECMAScript", "data", "intl");
            if (File.Exists(Path.Combine(candidate, "cldr", "cldr.pin")))
                return candidate;
        }

        throw new DirectoryNotFoundException("no Broiler.ECMAScript/data/intl/cldr/cldr.pin above " + start);
    }

    /// <summary>The home this test run reads: the one above the test assembly.</summary>
    internal static string Current => Home(AppContext.BaseDirectory);

    /// <summary>The generated area of <paramref name="home"/>.</summary>
    internal static string Generated(string home) => Path.Combine(home, "generated");

    internal static string CldrPinPath(string home) => Path.Combine(home, "cldr", "cldr.pin");

    internal static string UcdPinPath(string home) => Path.Combine(home, "ucd", "ucd.pin");

    internal static string ListPath(string home, string list) => Path.Combine(home, "locales", list);

    internal static IntlCldrPin ReadCldrPin(string home) => IntlCldrPin.Parse(File.ReadAllText(CldrPinPath(home)));

    internal static IntlUcdPin ReadUcdPin(string home) => IntlUcdPin.Parse(File.ReadAllText(UcdPinPath(home)));

    internal static IntlLocaleList ReadList(string home, string list) => IntlLocaleList.Parse(File.ReadAllText(ListPath(home, list)));

    /// <summary>Every file the CLDR pin names, off disk, by the pin's own list and nothing else.</summary>
    internal static IReadOnlyDictionary<string, byte[]> CldrFiles(string home, IntlCldrPin pin) =>
        pin.Entries.ToDictionary(
            static entry => entry.Name,
            entry => File.ReadAllBytes(Path.Combine(home, "cldr", entry.Name.Replace('/', Path.DirectorySeparatorChar))),
            StringComparer.Ordinal);

    /// <summary>Every file the UCD pin names, off disk.</summary>
    internal static IReadOnlyDictionary<string, byte[]> UcdFiles(string home, IntlUcdPin pin) =>
        pin.Entries.ToDictionary(
            static entry => entry.Name,
            entry => File.ReadAllBytes(Path.Combine(home, "ucd", entry.Name.Replace('/', Path.DirectorySeparatorChar))),
            StringComparer.Ordinal);

    /// <summary>The UCD subset, read from the home's copies after they are verified against the home's UCD pin.</summary>
    internal static CldrUcd Ucd(string home)
    {
        var pin = ReadUcdPin(home);
        return CldrUcd.Read(pin.Verified(UcdFiles(home, pin)));
    }

    /// <summary>
    /// Broiler.VM's lines of the home's CLDR pin: the line <c>data ...</c> and every line after it up to the
    /// first blank one, which the pin's header says are Broiler.VM's pin's lines from <c>data</c> to its last
    /// <c>file</c> line, unchanged and in order.
    /// </summary>
    internal static string BroilerVmPinLines(string home)
    {
        var lines = File.ReadAllText(CldrPinPath(home)).Split('\n');
        var first = Array.FindIndex(lines, static line => line.StartsWith("data ", StringComparison.Ordinal));
        var end = Array.FindIndex(lines, first, static line => line.Length == 0);
        return string.Join('\n', lines[first..end]) + "\n";
    }

    /// <summary>Broiler.JS's options: its list, and every numbering system's symbols (decision D21).</summary>
    internal static IntlGeneratorOptions BroilerJsOptions(string home) =>
        new(ReadList(home, BroilerJsList)) { NumberingSymbols = IntlNumberingSymbols.EveryNumberingSystem };

    /// <summary>Broiler.VM's options: its list, and every other parameter at its default.</summary>
    internal static IntlGeneratorOptions BroilerVmOptions(string home) => new(ReadList(home, BroilerVmList));

    /// <summary>Broiler.JS's tables, from the home's whole pin.</summary>
    internal static IntlTables GenerateBroilerJs(string home)
    {
        var pin = ReadCldrPin(home);
        return CldrTableGenerator.Generate(pin, CldrFiles(home, pin), Ucd(home), BroilerJsOptions(home));
    }

    /// <summary>
    /// Broiler.VM's tables, from the home's copy of Broiler.VM's pinned files alone: a pin of Broiler.VM's lines
    /// and the files they name, with the home's UCD copies.
    /// </summary>
    internal static IntlTables GenerateBroilerVm(string home)
    {
        var pin = IntlCldrPin.Parse(BroilerVmPinLines(home));
        return CldrTableGenerator.Generate(pin, CldrFiles(home, pin), Ucd(home), BroilerVmOptions(home));
    }

    /// <summary>Every file of the generated area for these tables, by file name.</summary>
    internal static IReadOnlyDictionary<string, byte[]> Files(IntlTables tables) => IntlTableFiles.Write(tables, ManifestHeader);

    /// <summary>Every file committed in the generated area, by file name.</summary>
    internal static IReadOnlyDictionary<string, byte[]> Committed(string home) =>
        Directory.Exists(Generated(home))
            ? Directory.EnumerateFiles(Generated(home)).ToDictionary(static path => Path.GetFileName(path), File.ReadAllBytes, StringComparer.Ordinal)
            : new Dictionary<string, byte[]>(StringComparer.Ordinal);

    /// <summary>Writes the generated area: every file the generator writes, and no other file left beside them.</summary>
    internal static void Write(string home, IReadOnlyDictionary<string, byte[]> files)
    {
        Directory.CreateDirectory(Generated(home));
        foreach (var stale in Directory.EnumerateFiles(Generated(home)).Where(path => !files.ContainsKey(Path.GetFileName(path))))
            File.Delete(stale);
        foreach (var (name, bytes) in files)
            File.WriteAllBytes(Path.Combine(Generated(home), name), bytes);
    }
}
