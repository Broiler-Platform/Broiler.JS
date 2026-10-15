using System.Runtime.CompilerServices;
using Broiler.JavaScript.BuiltIns.RegExp;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// The text area's corrections that the shared corpus does not hold (SJL-5.5): its vectors pad no
// further than 2^24 + 1, test neither split nor string search, and stop at the depth both engines
// read. Each case failed before the cut-over and passes after it.
public class SharedTextAlgorithmTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    // P4: a pad or a repeat past the longest string this engine builds is a RangeError, under the
    // length policy, where the builder let .NET's OutOfMemoryException through as a plain Error.
    [Theory]
    [InlineData("'x'.padEnd(2 ** 30 + 1)")]
    [InlineData("'x'.padStart(2 ** 30 + 1, 'ab')")]
    [InlineData("'x'.repeat(2 ** 30 + 1)")]
    [InlineData("'xy'.repeat(2 ** 29 + 1)")]
    public void A_string_past_the_length_policy_is_a_RangeError(string expression)
        => Assert.Equal("RangeError", Eval($"try {{ {expression}; 'no error' }} catch (e) {{ e.constructor.name }}"));

    // P4's other side: the policy is a ceiling, and an empty filler answers the string whatever the
    // length (StringPad step 4).
    [Theory]
    [InlineData("'x'.padEnd(2 ** 40, '')", "x")]
    [InlineData("'x'.padStart(5, 'ab')", "ababx")]
    [InlineData("'x'.padEnd(5, 'ab')", "xabab")]
    public void A_pad_within_the_policy_is_built(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // X1: the empty separator's branch honours the limit.
    [Theory]
    [InlineData("'abc'.split('', 2).join()", "a,b")]
    [InlineData("'abc'.split('', 0).length", "0")]
    [InlineData("'abc'.split('').join()", "a,b,c")]
    public void Split_with_an_empty_separator_honours_the_limit(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // X2: the string searches compare code units, whatever the current culture; under ICU the
    // culture's comparison read "\r\n" as one unit and ignored ignorable characters.
    [Theory]
    [InlineData("'a\\r\\nb'.indexOf('\\n')", "2")]
    [InlineData("'a\\r\\nb'.includes('\\n')", "true")]
    [InlineData("'a\\r\\nb'.startsWith('\\n', 2)", "true")]
    [InlineData("'a\\r'.endsWith('\\r')", "true")]
    [InlineData("'ab'.indexOf('\\u00AD')", "-1")]
    [InlineData("'a\\u00ADb'.startsWith('ab')", "false")]
    [InlineData("'a\\u00ADb'.endsWith('ab')", "false")]
    public void String_search_is_ordinal(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // J1: JSON nesting is this engine's policy, 512, where System.Text.Json's default of 64 refused
    // deeper documents with SyntaxError; past the policy the parse is a RangeError.
    [Theory]
    [InlineData("JSON.parse('['.repeat(512) + ']'.repeat(512)).length", "1")]
    [InlineData("try { JSON.parse('['.repeat(514) + ']'.repeat(514)); 'parsed' } catch (e) { e.constructor.name }", "RangeError")]
    [InlineData("try { JSON.rawJSON('[1') } catch (e) { e.constructor.name }", "SyntaxError")]
    public void Json_nesting_is_the_engine_policy(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // JSRegExp.Replace(string, string) is public and String.prototype.replace no longer reaches it
    // (S3). It is kept under decision SJL-D-25, its template the shared parser's: groups substitute,
    // an unmatched group is empty, and `$<` stays literal, as it did.
    [Fact]
    public void The_public_regexp_replace_keeps_its_templates()
    {
        Load();
        using var ctx = new JSContext();
        var regexp = new JSRegExp("(a)(x)?(b)", "g");
        Assert.Equal("b|a|-$<n>$0$$b|a|-$<n>$0$$", regexp.Replace("abab", "$3|$1|$2-$<n>$0$$$"));
    }
}
