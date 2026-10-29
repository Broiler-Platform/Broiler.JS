using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #871: three test262 script-host clusters.
//  - Intl.Collator must reject out-of-range values for the constrained string options usage /
//    sensitivity / caseFirst with a RangeError (ECMA-402 GetOption). Mirrors test262
//    intl402/Collator/test-option-{usage,sensitivity,numeric-and-caseFirst}.js and
//    intl402/String/prototype/localeCompare/throws-same-exceptions-as-Collator.js.
//  - Iterator.concat must throw a TypeError when an argument's @@iterator is missing or nullish
//    (GetMethod returns undefined), not only when it is present-but-not-callable. Mirrors test262
//    built-ins/Iterator/concat/throws-typeerror-when-iterator-method-not-callable.js.
//  - Temporal date-time / offset strings must use a consistent separator style: a mixed form such
//    as "00:0000" or an offset "+00:0000" is a RangeError. Mirrors test262
//    built-ins/Temporal/{PlainDateTime,ZonedDateTime}/from/argument-string-invalid.js.
public class Issue871Tests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    // Evaluates source under the experimental ES2026 surface (Iterator.concat / Temporal) and returns
    // its string result.
    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext(experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026);
        return ctx.Eval(source).ToString();
    }

    // Returns the constructor name of the error thrown by evaluating expr, or "no throw".
    private static string ThrownErrorName(string expr) => Eval($$"""
        (function () {
            try { {{expr}}; return "no throw"; }
            catch (e) { return e && e.constructor ? e.constructor.name : String(e); }
        })()
        """);

    // ---- Cluster 1: Intl.Collator option validation -------------------------------------------

    [Theory]
    [InlineData("new Intl.Collator('en', { usage: 'invalidValue' })")]
    [InlineData("new Intl.Collator('en', { sensitivity: 'invalidValue' })")]
    [InlineData("new Intl.Collator('en', { caseFirst: 'invalidValue' })")]
    [InlineData("'a'.localeCompare('b', 'en', { sensitivity: 'invalidValue' })")]
    public void Collator_RejectsInvalidOptionValue(string expr)
        => Assert.Equal("RangeError", ThrownErrorName(expr));

    [Theory]
    [InlineData("new Intl.Collator('en', { usage: 'search' }).resolvedOptions().usage", "search")]
    [InlineData("new Intl.Collator('en', { sensitivity: 'base' }).resolvedOptions().sensitivity", "base")]
    [InlineData("new Intl.Collator('en', { caseFirst: 'upper' }).resolvedOptions().caseFirst", "upper")]
    [InlineData("new Intl.Collator('en').resolvedOptions().usage", "sort")]
    [InlineData("new Intl.Collator('en').resolvedOptions().sensitivity", "variant")]
    [InlineData("new Intl.Collator('en').resolvedOptions().caseFirst", "false")]
    public void Collator_AcceptsValidOptionValue(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr})"));

    // ---- Cluster 2: Iterator.concat callable @@iterator ----------------------------------------

    [Theory]
    [InlineData("Iterator.concat({})")]
    [InlineData("Iterator.concat({ [Symbol.iterator]: undefined })")]
    [InlineData("Iterator.concat({ [Symbol.iterator]: null })")]
    [InlineData("Iterator.concat({ [Symbol.iterator]: true })")]
    [InlineData("Iterator.concat({ [Symbol.iterator]: 123 })")]
    [InlineData("Iterator.concat({ [Symbol.iterator]: 'abc' })")]
    public void IteratorConcat_RejectsUncallableIteratorMethod(string expr)
        => Assert.Equal("TypeError", ThrownErrorName(expr));

    [Fact]
    public void IteratorConcat_ConcatenatesValidIterables()
        => Assert.Equal("1,2,3,4", Eval("Array.from(Iterator.concat([1, 2], [3, 4])).join(',')"));

    // ---- Cluster 3: Temporal consistent date-time / offset separators --------------------------

    [Theory]
    [InlineData("Temporal.PlainDateTime.from('2025-01-01T00:00:00+00:0000')")]
    [InlineData("Temporal.PlainDateTime.from('2025-01-01T00:00:00+0000:00')")]
    [InlineData("Temporal.PlainDateTime.from('2025-01-01T00:0000')")]
    [InlineData("Temporal.PlainDateTime.from('2025-01-01T0000:00')")]
    [InlineData("Temporal.ZonedDateTime.from('2025-01-01T00:0000+00:00[UTC]')")]
    [InlineData("Temporal.ZonedDateTime.from('2025-01-01T0000:00+00:00[UTC]')")]
    [InlineData("Temporal.ZonedDateTime.from('2025-01-01T00:00:00+00:0000[UTC]')")]
    [InlineData("Temporal.PlainTime.from('12:3045')")]
    public void Temporal_RejectsMixedSeparators(string expr)
        => Assert.Equal("RangeError", ThrownErrorName(expr));

    [Theory]
    // Both forms (all-':' extended and separator-less basic) remain valid, with or without offsets.
    [InlineData("Temporal.PlainDateTime.from('2020-01-01T12:30:45').toString()", "2020-01-01T12:30:45")]
    [InlineData("Temporal.PlainDateTime.from('2020-01-01T123045').toString()", "2020-01-01T12:30:45")]
    [InlineData("Temporal.PlainDateTime.from('2020-01-01T12:30:45+05:30').toString()", "2020-01-01T12:30:45")]
    [InlineData("Temporal.PlainDateTime.from('2020-01-01T123045-080000').toString()", "2020-01-01T12:30:45")]
    [InlineData("Temporal.ZonedDateTime.from('2020-01-01T12:30:45+00:00[UTC]').toString()", "2020-01-01T12:30:45+00:00[UTC]")]
    [InlineData("Temporal.ZonedDateTime.from('2020-01-01T123045+0000[UTC]').toString()", "2020-01-01T12:30:45+00:00[UTC]")]
    [InlineData("Temporal.PlainTime.from('12:30:45').toString()", "12:30:45")]
    [InlineData("Temporal.PlainTime.from('123045').toString()", "12:30:45")]
    public void Temporal_AcceptsConsistentSeparators(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr})"));
}
