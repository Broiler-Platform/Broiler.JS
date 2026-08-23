using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Minimal reproductions for the core-language items of the JavaScript gaps roadmap's
// track 1. Each test states one observable value the specification fixes; the comment on
// it says what the engine answered before, so a regression reads as the old answer coming
// back rather than as an unexplained assertion.
public class Track1LanguageTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext(options: new JSContextOptions { ScriptHostMode = true });
        return ctx.Eval(code).ToString();
    }

    // ---- `undefined` is a global PROPERTY, so a binding of that name shadows it ----
    //
    // The compiler folded every identifier named `undefined` to the undefined value, so a
    // parameter, `var`, `let` or catch parameter of that name read as undefined however it
    // had been initialised. The fold is still taken for a free reference — the global
    // property is non-writable and non-configurable, so it cannot be anything else.

    [Fact(Timeout = 600000)]
    public void AParameterNamedUndefinedShadowsTheGlobalProperty()
        => Assert.Equal("5", Eval("(function (undefined) { return undefined; })(5)"));

    [Fact(Timeout = 600000)]
    public void TypeofSeesTheParameterRatherThanTheGlobalProperty()
        => Assert.Equal("number", Eval("(function (undefined) { return typeof undefined; })(5)"));

    [Fact(Timeout = 600000)]
    public void AParameterNamedUndefinedIsAssignable()
        => Assert.Equal("3", Eval("(function (undefined) { undefined = 3; return undefined; })(1)"));

    [Fact(Timeout = 600000)]
    public void ALexicalBindingNamedUndefinedShadowsTheGlobalProperty()
        => Assert.Equal("4", Eval("(function () { { let undefined = 4; return undefined; } })()"));

    [Fact(Timeout = 600000)]
    public void AVarNamedUndefinedShadowsTheGlobalPropertyInsideAFunction()
        => Assert.Equal("7", Eval("(function () { var undefined = 7; return undefined; })()"));

    [Fact(Timeout = 600000)]
    public void ACatchParameterNamedUndefinedShadowsTheGlobalProperty()
        => Assert.Equal("9", Eval("(function () { try { throw 9; } catch (undefined) { return undefined; } })()"));

    [Fact(Timeout = 600000)]
    public void AClosureCapturesTheBindingNamedUndefined()
        => Assert.Equal("6", Eval(
            "(function (undefined) { return (function () { return undefined; })(); })(6)"));

    [Fact(Timeout = 600000)]
    public void AWithObjectSuppliesUndefinedDynamically()
        => Assert.Equal("8", Eval("(function () { with ({ undefined: 8 }) { return undefined; } })()"));

    [Fact(Timeout = 600000)]
    public void AFreeUndefinedIsStillTheUndefinedValue()
        => Assert.Equal("true", Eval("(function () { var x; return x === undefined; })()"));

    [Fact(Timeout = 600000)]
    public void AWithObjectWithoutUndefinedFallsBackToTheGlobalProperty()
        => Assert.Equal("true", Eval(
            "(function () { with ({ other: 1 }) { var x; return x === undefined; } })()"));

    // ---- An empty statement ends a Directive Prologue ----
    //
    // The prologue is the leading run of string-literal ExpressionStatements; an empty
    // statement ends it, so a `'use strict'` after one is an ordinary expression statement.
    // A statement that already ended at its own `;` was consuming a SECOND one, which
    // deleted exactly that empty statement — so `'x'; ; 'use strict';` parsed as two
    // adjacent directives and ran strict, while `'x'; ; ;` (whose second `;` survived) did
    // not. `typeof this` is the witness: a sloppy function called with undefined `this`
    // gets the global object, a strict one gets undefined.

    [Fact(Timeout = 600000)]
    public void AnEmptyStatementAfterADirectiveEndsThePrologue()
        => Assert.Equal("object", Eval(
            "(function () { 'x'; ; 'use strict'; return typeof this; }).call(undefined)"));

    [Fact(Timeout = 600000)]
    public void TwoEmptyStatementsAfterADirectiveEndThePrologue()
        => Assert.Equal("object", Eval(
            "(function () { 'x'; ; ; 'use strict'; return typeof this; }).call(undefined)"));

    [Fact(Timeout = 600000)]
    public void AnEmptyStatementBeforeAnyDirectiveEndsThePrologue()
        => Assert.Equal("object", Eval(
            "(function () { ; 'use strict'; return typeof this; }).call(undefined)"));

    [Fact(Timeout = 600000)]
    public void AnEmptyStatementEndsAProgramsPrologueToo()
        => Assert.Equal("object", Eval(
            "eval(\"'x'; ; 'use strict'; (function () { return typeof this; }).call(undefined)\")"));

    // The prologue itself still works: adjacent directives stay directives.
    [Fact(Timeout = 600000)]
    public void AdjacentDirectivesStillMakeAFunctionStrict()
        => Assert.Equal("undefined", Eval(
            "(function () { 'x'; 'use strict'; return typeof this; }).call(undefined)"));

    [Fact(Timeout = 600000)]
    public void ALeadingUseStrictStillMakesAFunctionStrict()
        => Assert.Equal("undefined", Eval(
            "(function () { 'use strict'; return typeof this; }).call(undefined)"));

    // An empty statement is a statement wherever it appears, not only in a prologue.
    [Fact(Timeout = 600000)]
    public void AnEmptyStatementAfterAnOrdinaryStatementIsStillParsed()
        => Assert.Equal("3", Eval("(function () { var n = 0; n++; ; n += 2; return n; })()"));

    // Automatic semicolon insertion is unchanged: a statement without its own `;` still
    // ends at the line terminator.
    [Fact(Timeout = 600000)]
    public void AutomaticSemicolonInsertionStillEndsAStatementAtALineBreak()
        => Assert.Equal("7", Eval("(function () { var n = 3\n n = n + 4\n return n })()"));

    // ---- Reflect.set creates the receiver's property with CreateDataProperty attributes ----
    //
    // OrdinarySetWithOwnDescriptor ends in CreateDataProperty(Receiver, P, V) when the
    // receiver has no own property, and CreateDataProperty does not consult the base: the
    // property is writable, enumerable and configurable whatever the base's own property
    // was. The base's attributes were being passed down instead.

    private const string DescribeReceiverProperty =
        "var base = {};"
        + "Object.defineProperty(base, 'x', {"
        + "  value: 1, writable: true, enumerable: false, configurable: false });"
        + "var receiver = {};"
        + "Reflect.set(base, 'x', 2, receiver);"
        + "var d = Object.getOwnPropertyDescriptor(receiver, 'x');";

    [Fact(Timeout = 600000)]
    public void ReflectSetGivesANewReceiverPropertyTheAllTrueDescriptor()
        => Assert.Equal("true,true,true,2", Eval(
            DescribeReceiverProperty
            + "[d.writable, d.enumerable, d.configurable, receiver.x].join(',')"));

    [Fact(Timeout = 600000)]
    public void ReflectSetGivesANewIndexedReceiverPropertyTheAllTrueDescriptor()
        => Assert.Equal("true,true,true", Eval(
            "var base = {};"
            + "Object.defineProperty(base, '0', {"
            + "  value: 1, writable: true, enumerable: false, configurable: false });"
            + "var receiver = {};"
            + "Reflect.set(base, '0', 2, receiver);"
            + "var d = Object.getOwnPropertyDescriptor(receiver, '0');"
            + "[d.writable, d.enumerable, d.configurable].join(',')"));

    [Fact(Timeout = 600000)]
    public void ReflectSetGivesANewSymbolKeyedReceiverPropertyTheAllTrueDescriptor()
        => Assert.Equal("true,true,true", Eval(
            "var s = Symbol('s');"
            + "var base = {};"
            + "Object.defineProperty(base, s, {"
            + "  value: 1, writable: true, enumerable: false, configurable: false });"
            + "var receiver = {};"
            + "Reflect.set(base, s, 2, receiver);"
            + "var d = Object.getOwnPropertyDescriptor(receiver, s);"
            + "[d.writable, d.enumerable, d.configurable].join(',')"));

    // An assignment through the prototype chain is the same algorithm with the receiver
    // implied, so it creates the same all-true own property on the object written to.
    [Fact(Timeout = 600000)]
    public void AssigningThroughThePrototypeChainCreatesAnAllTrueOwnProperty()
        => Assert.Equal("true,true,true,3", Eval(
            "var proto = {};"
            + "Object.defineProperty(proto, 'x', {"
            + "  value: 1, writable: true, enumerable: false, configurable: false });"
            + "var o = Object.create(proto);"
            + "o.x = 3;"
            + "var d = Object.getOwnPropertyDescriptor(o, 'x');"
            + "[d.writable, d.enumerable, d.configurable, o.x].join(',')"));

    // The receiver's OWN property is not re-described: [[DefineOwnProperty]] is called with
    // a value-only descriptor, so the attributes it already has survive the write.
    [Fact(Timeout = 600000)]
    public void ReflectSetKeepsTheAttributesOfAPropertyTheReceiverAlreadyHas()
        => Assert.Equal("true,true,false,false,5", Eval(
            "var base = { x: 1 };"
            + "var receiver = {};"
            + "Object.defineProperty(receiver, 'x', {"
            + "  value: 0, writable: true, enumerable: false, configurable: false });"
            + "var set = Reflect.set(base, 'x', 5, receiver);"
            + "var d = Object.getOwnPropertyDescriptor(receiver, 'x');"
            + "[set, d.writable, d.enumerable, d.configurable, receiver.x].join(',')"));

    // A non-writable data property on the BASE still refuses the write before any of this.
    [Fact(Timeout = 600000)]
    public void ANonWritableBasePropertyStillBlocksTheWrite()
        => Assert.Equal("false,false", Eval(
            "var base = {};"
            + "Object.defineProperty(base, 'x', { value: 1, writable: false });"
            + "var receiver = {};"
            + "[Reflect.set(base, 'x', 2, receiver), 'x' in receiver].join(',')"));

    [Fact(Timeout = 600000)]
    public void ANonExtensibleReceiverStillRefusesANewProperty()
        => Assert.Equal("false,false", Eval(
            "var base = { x: 1 };"
            + "var receiver = Object.preventExtensions({});"
            + "[Reflect.set(base, 'x', 2, receiver), 'x' in receiver].join(',')"));
}
