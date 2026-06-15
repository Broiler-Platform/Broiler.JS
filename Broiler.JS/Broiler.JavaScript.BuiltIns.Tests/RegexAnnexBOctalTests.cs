using Broiler.JavaScript.Engine;
namespace Broiler.JavaScript.BuiltIns.Tests;

// Annex B (non-Unicode): a numeric escape that exceeds the capture count is a legacy octal escape
// (\1-\7), not a backreference; \8/\9 degrade to the literal digit (issue #798 problem 13 subset).
public class RegexAnnexBOctalTests
{
    private static string Eval(string e){ using var c=new JSContext(); return c.Eval(e).ToString(); }

    [Theory]
    // The non-unicode-malformed.js assertions plus octal edge cases.
    [InlineData(@"/\k<a>\1/.test('k<a>\x01')", "true")]   // the originally failing assertion
    [InlineData(@"/\1/.test('\x01')", "true")]            // \1 with no group -> octal \x01
    [InlineData(@"/\7/.test('\x07')", "true")]            // \7 -> octal \x07
    [InlineData(@"/\10/.test('\x08')", "true")]           // \10 -> octal 010 = \x08
    [InlineData(@"/7\89/.test('789')", "true")]           // \8/\9 stay literal digits
    [InlineData(@"/(a)\1/.test('aa')", "true")]           // real backreference unaffected
    [InlineData(@"/\1(b)\k<a>/.test('bk<a>')", "true")]   // \1 forward-ref to existing group still empty-matches
    public void AnnexBOctal(string expr, string expected)
    {
        Assert.Equal(expected, Eval(expr));
    }
}
