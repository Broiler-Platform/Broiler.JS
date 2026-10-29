using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// ES2025 duplicate named capture groups (issue #805 problem 24): groups in mutually-exclusive
// alternatives may share a name. The capture must reset on each quantifier repetition so a trailing
// \k<name> backreference follows whichever same-named group last participated — including when a
// repetition takes an alternative with no named group at all.
public class RegExpDuplicateNamedGroupsTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);
    private static string E(string e) { Load(); using var c = new JSContext(); return c.Eval(e).ToString(); }

    [Theory]
    [InlineData("JSON.stringify(/(?<x>a)|(?<x>b)/.exec('bab'))", "[\"b\",null,\"b\"]")]
    [InlineData("JSON.stringify(/(?<x>b)|(?<x>a)/.exec('bab'))", "[\"b\",\"b\",null]")]
    [InlineData("JSON.stringify(/(?:(?<x>a)|(?<x>b))\\k<x>/.exec('aa'))", "[\"aa\",\"a\",null]")]
    [InlineData("JSON.stringify(/(?:(?<x>a)|(?<x>b))\\k<x>/.exec('bb'))", "[\"bb\",null,\"b\"]")]
    [InlineData("JSON.stringify(/(?:(?:(?<x>a)|(?<x>b))\\k<x>){2}/.exec('aabb'))", "[\"aabb\",null,\"b\"]")]
    [InlineData("/(?:(?:(?<x>a)|(?<x>b))\\k<x>){2}/.exec('aabb').groups.x", "b")]
    [InlineData("String(/(?:(?:(?<x>a)|(?<x>b))\\k<x>){2}/.exec('abab'))", "null")]
    [InlineData("String(/(?:(?<x>a)|(?<x>b))\\k<x>/.exec('abab'))", "null")]
    [InlineData("String(/(?:(?<x>a)|(?<x>b))\\k<x>/.exec('cdef'))", "null")]
    [InlineData("JSON.stringify(/^(?:(?<a>x)|(?<a>y)|z)\\k<a>$/.exec('xx'))", "[\"xx\",\"x\",null]")]
    [InlineData("JSON.stringify(/^(?:(?<a>x)|(?<a>y)|z)\\k<a>$/.exec('z'))", "[\"z\",null,null]")]
    [InlineData("String(/^(?:(?<a>x)|(?<a>y)|z)\\k<a>$/.exec('zz'))", "null")]
    [InlineData("JSON.stringify(/(?<a>x)|(?:zy\\k<a>)/.exec('zy'))", "[\"zy\",null]")]
    // The quantified-group-with-a-bare-alternative cases that were the actual bug:
    [InlineData("JSON.stringify(/^(?:(?<a>x)|(?<a>y)|z){2}\\k<a>$/.exec('xz'))", "[\"xz\",null,null]")]
    [InlineData("JSON.stringify(/^(?:(?<a>x)|(?<a>y)|z){2}\\k<a>$/.exec('yz'))", "[\"yz\",null,null]")]
    [InlineData("String(/^(?:(?<a>x)|(?<a>y)|z){2}\\k<a>$/.exec('xzx'))", "null")]
    [InlineData("String(/^(?:(?<a>x)|(?<a>y)|z){2}\\k<a>$/.exec('yzy'))", "null")]
    public void Exec(string expr, string expected) => Assert.Equal(expected, E(expr));

    [Theory]
    // String.prototype.match delegates to the same matching machinery (duplicate-names-match.js).
    [InlineData("JSON.stringify('bab'.match(/(?<x>a)|(?<x>b)/))", "[\"b\",null,\"b\"]")]
    [InlineData("JSON.stringify('xz'.match(/^(?:(?<a>x)|(?<a>y)|z){2}\\k<a>$/))", "[\"xz\",null,null]")]
    public void Match(string expr, string expected) => Assert.Equal(expected, E(expr));

    [Fact]
    public void DuplicateNameInSameAlternative_IsSyntaxError()
    {
        // Two same-named groups reachable together (not in separate alternatives) remain a SyntaxError.
        var result = E("""
            var err = "none";
            try { new RegExp("(?<a>x)(?<a>y)"); }
            catch (e) { err = e.constructor.name; }
            err;
        """);
        Assert.Equal("SyntaxError", result);
    }
}
