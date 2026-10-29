using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/822
//
// The global URI functions and the number parsers coerced their argument with
// the CLR ToString() (yielding "[object Object]") instead of the JS ToString
// abstract op, which performs ToPrimitive(string) and so falls back to valueOf
// when toString returns a non-primitive. Covers the decodeURI / decodeURIComponent
// / encodeURI / encodeURIComponent / parseFloat object-coercion tests.
public class Issue822Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    [Fact]
    public void DecodeUri_UsesValueOfWhenToStringReturnsObject()
        => Assert.Equal("^|^", Eval(
            "var o = { valueOf: function () { return '%5E'; }, toString: function () { return {}; } };"
            + "decodeURI(o) + '|' + decodeURIComponent(o)"));

    [Fact]
    public void EncodeUri_UsesValueOfWhenToStringReturnsObject()
        => Assert.Equal("%5E|%5E", Eval(
            "var o = { valueOf: function () { return '^'; }, toString: function () { return {}; } };"
            + "encodeURI(o) + '|' + encodeURIComponent(o)"));

    [Fact]
    public void ParseFloatAndParseInt_UseValueOfWhenToStringReturnsObject()
        => Assert.Equal("1|1", Eval(
            "var o = { valueOf: function () { return 1; }, toString: function () { return {}; } };"
            + "String(parseFloat(o)) + '|' + String(parseInt(o))"));

    [Fact]
    public void GlobalCoercion_NormalArgumentsStillWork()
        => Assert.Equal("^|%5E%20a|3.14|255|5", Eval(
            "decodeURI('%5E') + '|' + encodeURI('^ a') + '|' + String(parseFloat('3.14xyz'))"
            + " + '|' + String(parseInt('0xFF')) + '|' + String(parseInt('101', 2))"));

    // A top-level FunctionDeclaration of a script creates a non-configurable global
    // binding (deletable D = false), so `delete f` returns false — like `var`, and
    // unlike an implicit global or an eval-introduced function.
    [Fact]
    public void GlobalFunctionDeclarationIsNonDeletable()
        => Assert.Equal("false|false|function", Eval(
            "function f() {} function MyFunction() {}"
            + "String(delete f) + '|' + String(delete MyFunction) + '|' + typeof f"));

    [Fact]
    public void GlobalVarStillNonDeletable_ImplicitGlobalStillDeletable()
        => Assert.Equal("false|true", Eval(
            "var v = 1; ig = 2; String(delete v) + '|' + String(delete ig)"));
}
