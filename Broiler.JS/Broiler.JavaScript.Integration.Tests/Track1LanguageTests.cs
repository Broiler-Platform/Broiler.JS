using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

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

    // ---- Symbol-keyed own properties enumerate in property-insertion order ----
    //
    // OrdinaryOwnPropertyKeys (§10.1.11.1) lists symbol keys last, in the order the property
    // was ADDED to the object. The symbol map is keyed by the symbol's creation id and does
    // not record that order, so `getOwnPropertySymbols` sorted by creation id (`a,b,c` for
    // symbols made in that order however they were assigned) and `Reflect.ownKeys` returned
    // the map's raw hash order — both wrong. The object now tracks symbol insertion order and
    // every enumeration path reads it. Symbols are created a,b,c and assigned c,a,b, so the
    // correct order (c,a,b) differs from both creation order and any hash order.

    private const string ThreeSymbolsInsertedCAB =
        "var a = Symbol('a'), b = Symbol('b'), c = Symbol('c');"
        + "var o = {}; o[c] = 1; o[a] = 2; o[b] = 3;";

    [Fact(Timeout = 600000)]
    public void GetOwnPropertySymbolsUsesInsertionOrder()
        => Assert.Equal("c,a,b", Eval(
            ThreeSymbolsInsertedCAB + "Object.getOwnPropertySymbols(o).map(s => s.description).join(',')"));

    [Fact(Timeout = 600000)]
    public void ReflectOwnKeysUsesInsertionOrderForSymbols()
        => Assert.Equal("c,a,b", Eval(
            ThreeSymbolsInsertedCAB
            + "Reflect.ownKeys(o).filter(k => typeof k === 'symbol').map(s => s.description).join(',')"));

    [Fact(Timeout = 600000)]
    public void ReflectOwnKeysOrdersIntegersThenStringsThenSymbolsEachInInsertionOrder()
        => Assert.Equal("0,2,y,x,b,a", Eval(
            "var a = Symbol('a'), b = Symbol('b');"
            + "var o = {}; o[b] = 1; o.y = 1; o[2] = 1; o.x = 1; o[0] = 1; o[a] = 1;"
            + "Reflect.ownKeys(o).map(k => typeof k === 'symbol' ? k.description : k).join(',')"));

    // A deleted symbol that is added again is a NEW property: it goes to the end, not back to
    // its old slot (same rule as string keys).
    [Fact(Timeout = 600000)]
    public void AReaddedSymbolMovesToTheEnd()
        => Assert.Equal("b,c,a", Eval(
            "var a = Symbol('a'), b = Symbol('b'), c = Symbol('c');"
            + "var o = {}; o[a] = 1; o[b] = 2; o[c] = 3; delete o[a]; o[a] = 9;"
            + "Object.getOwnPropertySymbols(o).map(s => s.description).join(',')"));

    // Redefining an existing symbol key keeps its position.
    [Fact(Timeout = 600000)]
    public void RedefiningASymbolKeepsItsPosition()
        => Assert.Equal("a,b,c", Eval(
            "var a = Symbol('a'), b = Symbol('b'), c = Symbol('c');"
            + "var o = {}; o[a] = 1; o[b] = 2; o[c] = 3; Object.defineProperty(o, b, { value: 9 });"
            + "Object.getOwnPropertySymbols(o).map(s => s.description).join(',')"));

    // Object.assign copies symbol keys in the source's insertion order, so the copy's own
    // symbol order matches the source's rather than the symbol map's hash order.
    [Fact(Timeout = 600000)]
    public void ObjectAssignCopiesSymbolsInInsertionOrder()
        => Assert.Equal("c,a,b", Eval(
            ThreeSymbolsInsertedCAB
            + "var t = Object.assign({}, o);"
            + "Reflect.ownKeys(t).filter(k => typeof k === 'symbol').map(s => s.description).join(',')"));
    // ---- Async and generator bodies run under the strict-mode runtime flag ----
    //
    // An async or generator body runs during the rewritten driver's steps, not during the
    // call that created it, so it did not inherit the strict-mode scope an ordinary call
    // establishes: a failing [[Set]] inside a 'use strict' async/generator body did not throw
    // (it silently did nothing), and a strict async function's `this` was the global object
    // instead of undefined. The body now re-enters the function's own strict flag on each
    // step, and a strict async function's `this` is left uncoerced.

    private const string FrozenSetInBody =
        "try { Object.freeze({ x: 1 }).x = 2; r = 'no-throw'; } catch (e) { r = e.name; }";

    [Fact(Timeout = 600000)]
    public void AFailedStrictSetInAGeneratorBodyThrows()
        => Assert.Equal("TypeError", Eval(
            "var r; (function* () { 'use strict'; " + FrozenSetInBody + " })().next(); r"));

    [Fact(Timeout = 600000)]
    public void AFailedStrictSetInAnAsyncBodyThrows()
        => Assert.Equal("TypeError", Eval(
            "var r; (async function () { 'use strict'; " + FrozenSetInBody + " })(); r"));

    // The function inherits strictness from an outer directive too, not only its own.
    [Fact(Timeout = 600000)]
    public void AFailedSetInAGeneratorInsideStrictCodeThrows()
        => Assert.Equal("TypeError", Eval(
            "'use strict'; var r; (function* () { " + FrozenSetInBody + " })().next(); r"));

    [Fact(Timeout = 600000)]
    public void AFailedSetInAnAsyncGeneratorBodyThrows()
        => Assert.Equal("TypeError", Eval(
            "var r; (async function* () { 'use strict'; " + FrozenSetInBody + " })().next(); r"));

    // The strict effect still applies after a yield resumes the body on a later step.
    [Fact(Timeout = 600000)]
    public void AFailedStrictSetAfterAYieldThrows()
        => Assert.Equal("TypeError", Eval(
            "var r; var g = (function* () { 'use strict'; yield 1; " + FrozenSetInBody + " })();"
            + "g.next(); g.next(); r"));

    // A strict async function's `this` is undefined (an ordinary strict function's is), not
    // the global object a sloppy async function would coerce it to.
    [Fact(Timeout = 600000)]
    public void AStrictAsyncFunctionsThisIsUndefined()
        => Assert.Equal("undefined", Eval(
            "var r; (async function () { 'use strict'; r = typeof this; })(); r"));

    // Regression guard: a SLOPPY async/generator body keeps lenient [[Set]] semantics.
    [Fact(Timeout = 600000)]
    public void AFailedSetInASloppyGeneratorBodyIsSilent()
        => Assert.Equal("no-throw", Eval(
            "var r; (function* () { " + FrozenSetInBody + " })().next(); r"));

    [Fact(Timeout = 600000)]
    public void AFailedSetInASloppyAsyncBodyIsSilent()
        => Assert.Equal("no-throw", Eval(
            "var r; (async function () { " + FrozenSetInBody + " })(); r"));

    // ---- Early SyntaxErrors that the engine used to accept (or crash on) ----
    //
    // Three families of parse-time error were not raised. (A) VarDeclaredNames and
    // LexicallyDeclaredNames of a scope must be disjoint: a `var` and a `let`/`const`/
    // class/block-function of the same name conflict even when the `var` hoists out of
    // an inner block, and the check must hold in either declaration order and across a
    // for-head vs body and a destructured catch param vs body. The engine silently let
    // the later declaration win. (B) A labelled function declaration is never a legal
    // loop body; a `let`/`const` for-in/for-of/C-style head hid the labelled statement
    // behind a per-iteration block, so it slipped past validation. (C) An `export` in a
    // script (no module `exports` binding) is an early error; `export default <expr>`
    // dereferenced the absent binding and surfaced a NullReferenceException instead.

    private static void AssertSyntaxError(string code)
        => Assert.Throws<JSException>(() => Eval(code));

    // (A) A `var` collides with a lexical binding of the same name declared FIRST,
    // wherever the `var` hoists to.
    [Theory(Timeout = 600000)]
    [InlineData("let a; var a;")]
    [InlineData("const a = 1; var a;")]
    [InlineData("let a; { var a; }")]
    [InlineData("{ let a; { var a; } }")]
    [InlineData("function f() { let a; var a; }")]
    [InlineData("for (let a of []) { var a; }")]
    [InlineData("for (let a in {}) { var a; }")]
    [InlineData("for (let a = 0; false; ) { var a; }")]
    [InlineData("try {} catch ([a]) { var a; }")]
    [InlineData("try {} catch ({ a }) { var a; }")]
    [InlineData("switch (0) { case 1: let a; case 2: var a; }")]
    public void ALexicalThenVarOfTheSameNameIsASyntaxError(string code)
        => AssertSyntaxError(code);

    // (A) The same conflict when the `var` is declared FIRST and the lexical second —
    // even after the `var` has hoisted out of the block the lexical sits in.
    [Theory(Timeout = 600000)]
    [InlineData("var a; let a;")]
    [InlineData("var a; const a = 1;")]
    [InlineData("{ var a; let a; }")]
    [InlineData("switch (0) { case 1: var a; case 2: let a; }")]
    public void AVarThenLexicalOfTheSameNameIsASyntaxError(string code)
        => AssertSyntaxError(code);

    // (A) A block-nested function declaration is lexical, so it conflicts with a `var`
    // of the same name in that block (in either order), while two block functions of
    // one name still coexist in sloppy mode (Annex B 3.3.4).
    [Theory(Timeout = 600000)]
    [InlineData("{ function a() {} var a; }")]
    [InlineData("{ var a; function a() {} }")]
    public void ABlockFunctionAndVarOfTheSameNameConflict(string code)
        => AssertSyntaxError(code);

    // (A) Guard against over-rejecting: a `var` and a lexical of the same name in
    // DIFFERENT scopes, or a `var` deduping against a same-named parameter, are legal.
    // Each snippet ends in an expression that evaluates to "ok" once it is accepted.
    [Theory(Timeout = 600000)]
    [InlineData("var a; { let a; } 'ok'")]
    [InlineData("{ var a; } { let a; } 'ok'")]
    [InlineData("{ var a; { let a; } } 'ok'")]
    [InlineData("(function (a) { var a; return a; })(1); 'ok'")]
    [InlineData("for (var a of [1]) { var a; } 'ok'")]
    [InlineData("for (let a of []) { let b; } 'ok'")]
    [InlineData("try {} catch (e) { var e; } 'ok'")]
    [InlineData("{ function a() {} function a() {} } 'ok'")]
    public void ValidVarAndLexicalCombinationsAreStillAccepted(string code)
        => Assert.Equal("ok", Eval(code));

    // (B) A labelled function declaration as a loop body is an early error for every
    // loop head, including the `let`/`const` heads whose body is rewritten to a block.
    [Theory(Timeout = 600000)]
    [InlineData("for (let x of []) label: function h() {}")]
    [InlineData("for (const x of []) label: function h() {}")]
    [InlineData("for (let x in {}) label: function h() {}")]
    [InlineData("for (let x = 0; false; ) label: function h() {}")]
    [InlineData("for (var x of []) label: function h() {}")]
    [InlineData("for (;;) label: function h() {}")]
    [InlineData("while (0) label: function h() {}")]
    [InlineData("for (let x of []) a: b: function h() {}")]
    public void ALabelledFunctionLoopBodyIsASyntaxError(string code)
        => AssertSyntaxError(code);

    // (B) Guard: a labelled function is fine at statement/block position, and an
    // ordinary or labelled non-function loop body is unaffected.
    [Theory(Timeout = 600000)]
    [InlineData("label: function top() {} 'ok'")]
    [InlineData("{ label: function b() {} } 'ok'")]
    [InlineData("for (let x of []) { label: function h() {} } 'ok'")]
    [InlineData("for (let x of [1]) label: x; 'ok'")]
    public void ValidLabelledFunctionsAndLoopBodiesAreStillAccepted(string code)
        => Assert.Equal("ok", Eval(code));

    // (C) An `export` in script code is an early SyntaxError, not a crash — for every
    // export form, including the `export default <expr>` that used to throw a
    // NullReferenceException.
    [Theory(Timeout = 600000)]
    [InlineData("export default 1;")]
    [InlineData("export default function g() {}")]
    [InlineData("export const e = 1;")]
    [InlineData("export var v = 2;")]
    [InlineData("export { };")]
    public void ExportInAScriptIsASyntaxError(string code)
        => AssertSyntaxError(code);

    // ---- `delete` of an eval-introduced var captured by a closure re-resolves outward ----
    //
    // A sloppy direct eval's `var` is a deletable binding of the calling function's variable
    // environment (EvalDeclarationInstantiation: CreateMutableBinding(name, true)). When a nested
    // closure captures that name, the compiler binds both the function and the closure to one
    // shared EvalShadowVariable. `delete` of such a name must remove the eval-introduced binding
    // so every later read — the function's own and the closure's — re-resolves to the binding the
    // name would otherwise have (here the program-level `var x`). The compiler folded `delete x`
    // to a constant `false` (a captured binding read as non-deletable), so the binding survived
    // and every read still saw the eval's value.

    [Fact(Timeout = 600000)]
    public void DeleteOfClosureCapturedEvalVarReturnsTrue()
        => Assert.Equal("true", Eval(
            "var x = 'global';" +
            "(function () {" +
            "  eval(\"var x = 'inner';\");" +
            "  var read = function () { return x; };" +   // capture forces the shared shadow
            "  return String(delete x);" +
            "})()"));

    [Fact(Timeout = 600000)]
    public void ClosureReadAfterDeletingCapturedEvalVarReresolvesOutward()
        => Assert.Equal("global", Eval(
            "var x = 'global';" +
            "(function () {" +
            "  eval(\"var x = 'inner';\");" +
            "  var read = function () { return x; };" +
            "  delete x;" +
            "  return read();" +
            "})()"));

    [Fact(Timeout = 600000)]
    public void DirectReadAfterDeletingCapturedEvalVarReresolvesOutward()
        => Assert.Equal("global", Eval(
            "var x = 'global';" +
            "(function () {" +
            "  eval(\"var x = 'inner';\");" +
            "  var read = function () { return x; };" +   // present so `x` is the captured shadow
            "  delete x;" +
            "  return x;" +
            "})()"));

    // With no outer binding to fall back to, the name is unresolvable after the delete: a read
    // throws a ReferenceError and `typeof` answers "undefined".
    [Fact(Timeout = 600000)]
    public void ClosureReadAfterDeletingCapturedEvalVarWithNoOuterThrows()
        => Assert.Equal("ReferenceError", Eval(
            "(function () {" +
            "  eval(\"var y = 'inner';\");" +
            "  var read = function () { return y; };" +
            "  delete y;" +
            "  try { read(); return 'no throw'; } catch (e) { return e.name; }" +
            "})()"));

    [Fact(Timeout = 600000)]
    public void TypeofAfterDeletingCapturedEvalVarWithNoOuterIsUndefined()
        => Assert.Equal("undefined", Eval(
            "(function () {" +
            "  eval(\"var y = 'inner';\");" +
            "  var read = function () { return typeof y; };" +
            "  delete y;" +
            "  return read();" +
            "})()"));

    // ---- A direct eval's global `var`/function may not collide with a global lexical ----
    //
    // EvalDeclarationInstantiation: when a direct eval's variable environment is the global
    // environment, a `var` or hoisted function declaration whose name matches an existing global
    // lexical binding (a top-level `let`/`const`/class) is an early SyntaxError. The runtime
    // global-var registration (JSContext.Register) enforced this for an indirect eval, but a
    // DIRECT eval binds such a name as a captured lexical and skips that registration, so the
    // check was bypassed and the program ran — the eval's `var` silently aliasing the lexical.
    // (A function- or block-local lexical of the same name was already caught by the direct-eval
    // validator's compile-time lexical set; only the global lexical slipped through.)

    [Theory(Timeout = 600000)]
    [InlineData("let g = 1; eval('var g;'); 'ok'")]
    [InlineData("const c = 1; eval('var c;'); 'ok'")]
    [InlineData("class K {} eval('var K;'); 'ok'")]
    [InlineData("let g = 1; eval('function g(){}'); 'ok'")]      // a function declaration, too
    [InlineData("let g = 1; eval('var a, g;'); 'ok'")]           // the collision need not be first
    [InlineData("let g = 1; eval('{ var g; }'); 'ok'")]         // a block `var` still hoists to the global var-env
    public void ADirectEvalVarCollidingWithAGlobalLexicalIsASyntaxError(string code)
        => AssertSyntaxError(code);

    // An indirect eval already rejected the same collision — keep it rejected.
    [Fact(Timeout = 600000)]
    public void AnIndirectEvalVarCollidingWithAGlobalLexicalStaysASyntaxError()
        => AssertSyntaxError("let g = 1; (0,eval)('var g;'); 'ok'");

    // Guards: valid neighbours that must still be accepted.
    [Theory(Timeout = 600000)]
    [InlineData("(function () { var g = 1; eval('var g;'); return 'ok'; })()")]  // function-local var, not a lexical
    [InlineData("let g = 1; eval('var h;'); 'ok'")]                              // no collision
    [InlineData("let g = 1; eval('let g;'); 'ok'")]                             // the eval's own lexical scope
    [InlineData("eval('var g;'); let h = 1; 'ok'")]                            // different names
    public void ValidDirectEvalVarDeclarationsAreStillAccepted(string code)
        => Assert.Equal("ok", Eval(code));
}
