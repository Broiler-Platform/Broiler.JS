using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #830 (problem 95): an Intl constructor canonicalizes its locales argument exactly
// once, so an array-like locales object's "length" and index getters are observed a single
// time (not once for validation and again for resolution). Mirrors test262
// DisplayNames/locales-symbol-length.
public class Issue830LocalesReadOnceTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    private static string AccessLog(string construct) => Eval($$"""
        const log = [];
        const locales = {};
        Object.defineProperty(locales, "length", { get() { log.push("length"); return 1; } });
        Object.defineProperty(locales, "0", { get() { log.push("0"); return "en"; } });
        {{construct}};
        log.join(",");
    """);

    [Theory]
    [InlineData("new Intl.DisplayNames(locales, { type: 'language' })")]
    [InlineData("new Intl.NumberFormat(locales)")]
    [InlineData("new Intl.DateTimeFormat(locales)")]
    [InlineData("new Intl.Collator(locales)")]
    [InlineData("new Intl.PluralRules(locales)")]
    [InlineData("new Intl.RelativeTimeFormat(locales)")]
    [InlineData("new Intl.ListFormat(locales)")]
    public void LocalesGettersObservedOnce(string construct)
        => Assert.Equal("length,0", AccessLog(construct));

    [Theory]
    // The single canonicalization still selects the requested locale.
    [InlineData("new Intl.NumberFormat('fr').resolvedOptions().locale", "fr")]
    [InlineData("new Intl.DateTimeFormat(['de','en']).resolvedOptions().locale", "de")]
    public void RequestedLocaleStillResolved(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr})"));
}
