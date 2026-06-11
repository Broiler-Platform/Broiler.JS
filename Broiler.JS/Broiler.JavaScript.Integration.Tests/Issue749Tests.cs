using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/749
//
// Fixed here:
//
//   Problem 29 (for-of/for-in into an existing variable) — a bare-identifier loop
//   head that resolved to a known binding was passed as the enumerator MoveNext
//   out-argument (the binding's boxed JSVariable.Value). That write did not persist,
//   so `for (x of …)` / `for (x in …)` / `for ((x) of …)` over an existing var/let
//   left the variable unchanged after the loop. Such heads now go through the same
//   per-iteration assignment path used by member-expression and free-identifier heads.
//
//   Problem 28 (for-in over a string yielded Number keys) — JSString.GetAllKeys
//   returned an IntKeyEnumerator (Number indices). Property-key enumeration must
//   yield String keys, so destructuring a for-in key (`for (var {length:x} in "foo")`)
//   reads `.length` off a string. The string primitive now enumerates "0","1",… keys.
public class Issue749Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 29: for-of/for-in target persists after the loop ----

    [Fact]
    public void ForOfIntoExistingVarPersists()
        => Assert.Equal("9", Eval("var x; for (x of [9]) {} x"));

    [Fact]
    public void ForOfTargetAssignedInBodyPersists()
        => Assert.Equal("99", Eval("var x; for (x of [9]) { x = 99; } x"));

    [Fact]
    public void ForOfMultiElementLeavesLastValue()
        => Assert.Equal("3", Eval("var x; for (x of [1,2,3]) {} x"));

    [Fact]
    public void ForOfParenthesisedIdentifierTarget()
        => Assert.Equal("7", Eval("var a; for ((a) of [7]) {} a"));

    [Fact]
    public void ForInIntoExistingVarPersists()
        => Assert.Equal("b", Eval("var k; for (k in {a:1,b:2}) {} k"));

    [Fact]
    public void ForOfLetHeadStillWorks()
        => Assert.Equal("6", Eval("var s=0; for (let v of [1,2,3]) { s+=v; } s"));

    [Fact]
    public void ForOfMemberTargetStillWorks()
        => Assert.Equal("9", Eval("var o={}; for (o.p of [9]) {} o.p"));

    // ---- Problem 28: for-in over a string yields string keys ----

    [Fact]
    public void ForInStringKeyIsString()
        => Assert.Equal("string", Eval("var t; for (var k in 'foo') { t = typeof k; break; } t"));

    [Fact]
    public void ForInStringKeyDestructuringLength()
        => Assert.Equal("1", Eval("var t; for (var {length:x} in 'foo') { t = x; break; } t"));

    [Fact]
    public void ForInStringComputedKeyDestructuring()
        => Assert.Equal("1,0", Eval(
            "var t; for (var {length:x, [x-1]:y} in 'foo') { t = x + ',' + y; break; } t"));

    [Fact]
    public void ObjectKeysOfStringAreStrings()
        => Assert.Equal("string", Eval("typeof Object.keys('foo')[0]"));

    [Fact]
    public void ArrayKeysIteratorStillNumeric()
        => Assert.Equal("number", Eval("typeof [10,20].keys().next().value"));

    // ---- Problem 16: switch discriminant evaluated in the outer environment ----

    [Fact]
    public void SwitchDiscriminantCapturesOuterLexical()
        => Assert.Equal("outside,inside,inside", Eval(
            "let x = 'outside';" +
            "var probeExpr, probeSelector, probeStmt;" +
            "switch (probeExpr = function() { return x; }, null) {" +
            "  case probeSelector = function() { return x; }, null:" +
            "    probeStmt = function() { return x; };" +
            "    let x = 'inside';" +
            "}" +
            "probeExpr() + ',' + probeSelector() + ',' + probeStmt()"));

    [Fact]
    public void SwitchDiscriminantSeesOuterBindingValue()
        => Assert.Equal("outer", Eval(
            "let v = 'outer'; let r;" +
            "switch (r = v, 0) { case 0: let v = 'inner'; }" +
            "r"));
}
