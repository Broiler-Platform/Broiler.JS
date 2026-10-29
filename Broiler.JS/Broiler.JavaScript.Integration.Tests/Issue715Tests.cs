using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/715
//
// Fixed here:
//
//   Problem 2 — A for-of / for-in head whose left-hand side is an object or
//   array *literal* destructuring target was never reinterpreted as a pattern,
//   so a CoverInitializedName (`{ x = 1 }`) raised "Invalid shorthand property
//   initializer in object literal" (the assignment expression path already
//   reinterprets; the for-head did not). The for-head now converts an
//   object/array literal target to a pattern at parse time, mirroring the
//   assignment path. In addition, a destructuring default containing a `yield`
//   (or `await`) — `for ({ x = yield } of ...)` — produced invalid IL: the
//   default was emitted as a value-position coalesce (`temp = temp ?? default`),
//   and the generator/async state machine cannot suspend inside a value-position
//   sub-expression without corrupting the IL evaluation stack. Destructuring
//   defaults are now emitted as a statement (`if (temp === undefined) temp =
//   default;`), keeping the yield/await at a statement boundary.
//
//   Problem 4 — `new Object(primitive)` boxed the primitive correctly but the
//   [[Construct]] machinery then overwrote the box's prototype with
//   %Object.prototype%, so `new Object(1).constructor` was Object instead of
//   Number. A native ctor that returns a boxed primitive when invoked directly
//   now keeps the wrapper's own (type-specific) prototype.
//
//   Problem 5 — RegExp.prototype.exec stored the raw argument in the result's
//   `input` property instead of the ToString-coerced subject string, so
//   `/undefined/.exec(undefined).input` was the value `undefined` rather than
//   the string "undefined".
//
// Out of scope (unchanged, documented in prior issues): sm grab-bag + CLDR (P1),
//   negative-SyntaxError regex modifiers (P3), with/@@unscopables in nested fn
//   (P6/P10), per-evaluation private brand shadowing (P7), Symbol/Function
//   toString iteration (P8), yield* delegation edge cases (P9).
public class Issue715Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 2: for-of/for-in destructuring target reinterpretation ----

    [Fact]
    public void ForOfObjectAssignmentPatternWithDefault()
        => Assert.Equal("1", Eval("var x; for ({ x = 1 } of [{}]) {} x;"));

    [Fact]
    public void ForOfNestedArrayObjectAssignmentPattern()
        => Assert.Equal("1", Eval("var x; for ([{ x = 1 }] of [[{}]]) {} x;"));

    [Fact]
    public void ForInObjectCoverInitPattern()
        // The for-in key ("a") is destructured: property "k" is absent, so the
        // default applies. The point is that the cover-init `{ k = 5 }` parses.
        => Assert.Equal("5", Eval("var k; for ({ k = 5 } in { a: 1 }) {} k;"));

    // ---- Problem 2: yield inside a destructuring default (generator) ----

    [Fact]
    public void YieldInObjectAssignmentDefault()
        => Assert.Equal("7", Eval(
            "var a; function* g(){ ({ a = yield } = {}); } var it = g(); it.next(); it.next(7); a;"));

    [Fact]
    public void YieldInArrayAssignmentDefault()
        => Assert.Equal("7", Eval(
            "var a; function* g(){ [ a = yield ] = []; } var it = g(); it.next(); it.next(7); a;"));

    [Fact]
    public void YieldInForOfObjectDestructuringDefault()
        => Assert.Equal("5", Eval(
            "var x; function* g(){ for ({ x = yield } of [{}]) {} } var it = g(); it.next(); it.next(5); x;"));

    [Fact]
    public void DestructuringDefaultStillAppliesWhenPresent()
        => Assert.Equal("11", Eval(
            "var a; function* g(){ for ({ a = yield } of [{ a: 11 }]) {} } var it = g(); it.next(); it.next(99); a;"));

    // ---- Problem 4: new Object(primitive) keeps the wrapper's prototype ----

    [Fact]
    public void NewObjectNumberConstructorIsNumber()
        => Assert.Equal("Number", Eval("(new Object(1)).constructor.name;"));

    [Fact]
    public void NewObjectStringConstructorIsString()
        => Assert.Equal("String", Eval("(new Object('x')).constructor.name;"));

    [Fact]
    public void NewObjectBooleanInstanceOfBoolean()
        => Assert.Equal("true", Eval("((new Object(true)) instanceof Boolean).toString();"));

    [Fact]
    public void ObjectCallFormStillBoxes()
        => Assert.Equal("Number", Eval("Object(1).constructor.name;"));

    // ---- Problem 5: exec stores the ToString-coerced input ----

    [Fact]
    public void ExecInputIsCoercedUndefinedString()
        => Assert.Equal("undefined", Eval("/undefined/.exec(undefined).input;"));

    [Fact]
    public void ExecInputIsCoercedNumberString()
        => Assert.Equal("123", Eval("/2/.exec(123).input;"));

    [Fact]
    public void ExecInputIsAString()
        => Assert.Equal("string", Eval("typeof /2/.exec(123).input;"));
}
