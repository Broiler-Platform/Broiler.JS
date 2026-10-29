using System.Globalization;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/707
//
// Fixed here:
//
//   Problems 7/8/9 — Function.prototype.toString on accessor / async-generator
//   method definitions dropped the leading keyword. A getter stringified as
//   "f() {}" instead of "get f() {}", a setter as "f(a) {}" instead of
//   "set f(a) {}", and an async generator method as "*f() {}" instead of
//   "async *f() {}". The method's function-expression source span started at the
//   property name (or the "*") rather than at the "get"/"set"/"async" prefix;
//   FastParser.ObjectProperty now re-anchors it (excluding the "static" class
//   modifier, which is not part of the MethodDefinition source).
//
//   Problems 6/10 — Intl.NumberFormat notation:"scientific"/"engineering" were
//   resolved but never applied: format() and formatToParts() fell through to
//   standard notation. The mantissa/exponent are now computed (scientific keeps
//   the base-10 magnitude, engineering rounds it to a multiple of 3) and the
//   exponent is emitted as exponentSeparator/exponentMinusSign/exponentInteger.
//
// Out of scope (unchanged, documented in prior issues): scope-param eval
//   binding (P1), dynamic-super / super.x TypeError (P3), negative-SyntaxError
//   parser grab-bag (P4), DateTimeFormat formatRange CLDR (P5), sm RegExp/strict
//   grab-bag (P2).
public class Issue707Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problems 7/8/9: accessor / async-generator method toString ----

    [Fact]
    public void GetterToStringIncludesGetKeyword()
        => Assert.Equal("get f() {}",
            Eval("Object.getOwnPropertyDescriptor({ get f() {} }, 'f').get.toString();"));

    [Fact]
    public void SetterToStringIncludesSetKeyword()
        => Assert.Equal("set f(a) {}",
            Eval("Object.getOwnPropertyDescriptor({ set f(a) {} }, 'f').set.toString();"));

    [Fact]
    public void AsyncGeneratorMethodToStringIncludesAsyncStar()
        => Assert.Equal("async *f() {}",
            Eval("({ async *f() {} }).f.toString();"));

    [Fact]
    public void GeneratorMethodToStringIncludesStar()
        => Assert.Equal("*f() {}",
            Eval("({ *f() {} }).f.toString();"));

    // The "static" class modifier is part of the ClassElement, not the
    // MethodDefinition, so it must NOT appear in the function source.
    [Fact]
    public void StaticGetterToStringExcludesStaticIncludesGet()
        => Assert.Equal("get f() {}",
            Eval("Object.getOwnPropertyDescriptor(class { static get f() {} }, 'f').get.toString();"));

    [Fact]
    public void StaticMethodToStringExcludesStatic()
        => Assert.Equal("f() {}",
            Eval("Object.getOwnPropertyDescriptor(class { static f() {} }, 'f').value.toString();"));

    [Fact]
    public void PlainMethodToStringRoundTrips()
        => Assert.Equal("f() {}",
            Eval("({ f() {} }).f.toString();"));

    // Comments between the keyword and body are preserved verbatim.
    [Fact]
    public void GetterToStringPreservesComments()
        => Assert.Equal("get /* a */ f /* b */ ( /* c */ ) /* d */ { /* e */ }",
            Eval("Object.getOwnPropertyDescriptor({ get /* a */ f /* b */ ( /* c */ ) /* d */ { /* e */ } }, 'f').get.toString();"));

    // ---- Problems 6/10: scientific / engineering notation ----

    [Theory]
    [InlineData(0.000345, "345E-6")]
    [InlineData(0.345, "345E-3")]
    [InlineData(3.45, "3.45E0")]
    [InlineData(34.5, "34.5E0")]
    [InlineData(543, "543E0")]
    [InlineData(5430, "5.43E3")]
    [InlineData(543000, "543E3")]
    [InlineData(543211.1, "543.211E3")]
    public void EngineeringNotationEnUs(double value, string expected)
        => Assert.Equal(expected, Eval($"new Intl.NumberFormat('en-US', {{ notation: 'engineering' }}).format({value.ToString("R", CultureInfo.InvariantCulture)});"));

    [Theory]
    [InlineData(0.000345, "3.45E-4")]
    [InlineData(0.345, "3.45E-1")]
    [InlineData(3.45, "3.45E0")]
    [InlineData(34.5, "3.45E1")]
    [InlineData(543, "5.43E2")]
    [InlineData(543000, "5.43E5")]
    [InlineData(543211.1, "5.432E5")]
    public void ScientificNotationEnUs(double value, string expected)
        => Assert.Equal(expected, Eval($"new Intl.NumberFormat('en-US', {{ notation: 'scientific' }}).format({value.ToString("R", CultureInfo.InvariantCulture)});"));

    // German uses "," as the decimal separator in the mantissa; the exponent is
    // still "E"+digits.
    [Fact]
    public void ScientificNotationDeDeUsesCommaDecimal()
        => Assert.Equal("3,45E-4",
            Eval("new Intl.NumberFormat('de-DE', { notation: 'scientific' }).format(0.000345);"));

    [Fact]
    public void ScientificNotationFormatsInfinityAndNaN()
    {
        Assert.Equal("-∞", Eval("new Intl.NumberFormat('en-US', { notation: 'scientific' }).format(-Infinity);"));
        Assert.Equal("NaN", Eval("new Intl.NumberFormat('en-US', { notation: 'engineering' }).format(NaN);"));
    }

    [Fact]
    public void ScientificNotationFormatToPartsEmitsExponentParts()
        => Assert.Equal("integer:3|decimal:.|fraction:45|exponentSeparator:E|exponentMinusSign:-|exponentInteger:4",
            Eval("new Intl.NumberFormat('en-US', { notation: 'scientific' }).formatToParts(0.000345)" +
                 ".map(function(p){return p.type+':'+p.value;}).join('|');"));

    [Fact]
    public void EngineeringNotationFormatToPartsEmitsExponentParts()
        => Assert.Equal("integer:345|exponentSeparator:E|exponentMinusSign:-|exponentInteger:6",
            Eval("new Intl.NumberFormat('en-US', { notation: 'engineering' }).formatToParts(0.000345)" +
                 ".map(function(p){return p.type+':'+p.value;}).join('|');"));

    // ---- Problem 1: sloppy direct eval in a parameter list introduces vars ----
    //
    // A sloppy direct eval in a parameter initializer introduces a var into a
    // separate parameter environment (FunctionDeclarationInstantiation step 20).
    // The eval-introduced binding must shadow the same-named outer (here global)
    // binding for closures created in the parameter list and the body, even after
    // the function has returned. EvalShadowVariable provides this.

    [Fact]
    public void ParamEvalIntroducedVarShadowsForParamClosures()
        => Assert.Equal("inside|inside", Eval(
            "var x = 'outside'; var probe1, probe2;" +
            "(function(" +
            "  _ = probe1 = function() { return x; }," +
            "  __ = (eval('var x = \"inside\";'), probe2 = function() { return x; })" +
            ") {}());" +
            "probe1() + '|' + probe2();"));

    [Fact]
    public void ParamEvalIntroducedVarShadowsForBodyClosure()
        => Assert.Equal("inside|inside|inside", Eval(
            "var x = 'outside'; var probe1, probe2, probeBody;" +
            "(function(" +
            "  _ = (eval('var x = \"inside\";'), probe1 = function() { return x; })," +
            "  __ = probe2 = function() { return x; }" +
            ") { probeBody = function() { return x; }; }());" +
            "probe1() + '|' + probe2() + '|' + probeBody();"));

    // The outer binding is left untouched (the shadow does not leak back).
    [Fact]
    public void ParamEvalDoesNotMutateOuterBinding()
        => Assert.Equal("outside", Eval(
            "var x = 'outside';" +
            "(function(_ = eval('var x = \"inside\";')) {}());" +
            "x;"));

    // When the eval does NOT introduce the var, references forward to the live
    // outer binding (assignments too).
    [Fact]
    public void ParamEvalWithoutIntroductionForwardsToOuter()
        => Assert.Equal("outside", Eval(
            "var x = 'outside'; var probe;" +
            "(function(_ = (eval('1'), probe = function() { return x; })) {}());" +
            "probe();"));

    // The outer binding can be a function-local rather than a global.
    [Fact]
    public void ParamEvalShadowsEnclosingFunctionLocal()
        => Assert.Equal("inside", Eval(
            "function outer() {" +
            "  var x = 'outside'; var probe;" +
            "  (function(_ = (eval('var x = \"inside\";'), probe = function() { return x; })) {}());" +
            "  return probe();" +
            "}" +
            "outer();"));

    // A generator method form (one of the listed scope-param families).
    [Fact]
    public void ParamEvalShadowsForGenerator()
        => Assert.Equal("inside|inside", Eval(
            "var x = 'outside'; var probe1, probe2;" +
            "(function*(" +
            "  _ = probe1 = function() { return x; }," +
            "  __ = (eval('var x = \"inside\";'), probe2 = function() { return x; })" +
            ") {})().next();" +
            "probe1() + '|' + probe2();"));
}
