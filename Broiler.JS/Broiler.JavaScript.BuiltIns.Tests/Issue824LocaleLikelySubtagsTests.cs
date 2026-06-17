using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #824 (problems 33, 38): Intl.Locale.prototype.maximize / minimize implement the UTS #35
// Add / Remove Likely Subtags algorithms over the CLDR likelySubtags data, and preserve variants,
// extensions and private-use subtags. Vectors mirror test262 Locale/likely-subtags.js and
// Locale/prototype/minimize/removing-likely-subtags-first-adds-likely-subtags.js.
public class Issue824LocaleLikelySubtagsTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string expr)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(expr).ToString();
    }

    [Theory]
    [InlineData("en", "en-Latn-US")]
    [InlineData("en-Latn", "en-Latn-US")]
    [InlineData("en-Shaw", "en-Shaw-GB")]
    [InlineData("en-Arab", "en-Arab-US")]
    [InlineData("en-US", "en-Latn-US")]
    [InlineData("en-GB", "en-Latn-GB")]
    [InlineData("en-FR", "en-Latn-FR")]
    [InlineData("it-Kana-CA", "it-Kana-CA")]
    [InlineData("und", "en-Latn-US")]
    [InlineData("und-Thai", "th-Thai-TH")]
    [InlineData("und-419", "es-Latn-419")]
    [InlineData("und-150", "en-Latn-150")]
    [InlineData("und-AT", "de-Latn-AT")]
    [InlineData("und-Cyrl-RO", "bg-Cyrl-RO")]
    [InlineData("und-AQ", "en-Latn-AQ")]
    public void Maximize(string input, string expected)
        => Assert.Equal(expected, Eval($"new Intl.Locale('{input}').maximize().toString()"));

    [Theory]
    [InlineData("en", "en")]
    [InlineData("en-Latn", "en")]
    [InlineData("ar-Arab", "ar")]
    [InlineData("en-US", "en")]
    [InlineData("en-GB", "en-GB")]
    [InlineData("en-Latn-US", "en")]
    [InlineData("en-Shaw-GB", "en-Shaw")]
    [InlineData("en-Arab-US", "en-Arab")]
    [InlineData("en-Latn-GB", "en-GB")]
    [InlineData("th-Thai-TH", "th")]
    [InlineData("es-Latn-419", "es-419")]
    [InlineData("ru-Cyrl-RU", "ru")]
    [InlineData("de-Latn-AT", "de-AT")]
    [InlineData("bg-Cyrl-RO", "bg-RO")]
    [InlineData("und-Latn-AQ", "en-AQ")]
    // Second test262 file: minimization that first maximizes "und".
    [InlineData("und", "en")]
    [InlineData("und-Thai", "th")]
    [InlineData("und-419", "es-419")]
    [InlineData("und-150", "en-150")]
    [InlineData("und-AT", "de-AT")]
    [InlineData("aae-Latn-IT", "aae")]
    [InlineData("aae-Thai-CO", "aae-Thai-CO")]
    [InlineData("und-CW", "pap")]
    [InlineData("und-US", "en")]
    [InlineData("zh-Hant", "zh-TW")]
    [InlineData("zh-Hani", "zh-Hani")]
    public void Minimize(string input, string expected)
        => Assert.Equal(expected, Eval($"new Intl.Locale('{input}').minimize().toString()"));

    [Theory]
    // Variants, extensions and private-use subtags survive maximization unchanged.
    [InlineData("en-fonipa", "en-Latn-US-fonipa")]
    [InlineData("en-a-not-assigned", "en-Latn-US-a-not-assigned")]
    [InlineData("en-u-co-phonebk", "en-Latn-US-u-co-phonebk")]
    [InlineData("en-x-private", "en-Latn-US-x-private")]
    [InlineData("und-Cyrl-RO-fonipa", "bg-Cyrl-RO-fonipa")]
    public void MaximizePreservesExtras(string input, string expected)
        => Assert.Equal(expected, Eval($"new Intl.Locale('{input}').maximize().toString()"));

    [Theory]
    [InlineData("en-Latn-US-fonipa", "en-fonipa")]
    [InlineData("en-Latn-US-u-co-phonebk", "en-u-co-phonebk")]
    [InlineData("en-Latn-US-x-private", "en-x-private")]
    public void MinimizePreservesExtras(string input, string expected)
        => Assert.Equal(expected, Eval($"new Intl.Locale('{input}').minimize().toString()"));
}
