using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// The observable `prototype` property of a constructor and the object [[Construct]] actually
// builds instances on must never disagree (ES2024 10.2.2 / 10.1.13 OrdinaryCreateFromConstructor,
// which reads Get(newTarget, "prototype")).
//
// Broiler caches that object in a field on the function so `new f()` does not re-read the
// property each time. Two write paths kept the cache out of step with the property:
//
//   * the indexer assigned the field from the value a write ATTEMPTED, before performing the
//     store and without any success test — but a write to `prototype` is rejected outright when
//     the property is non-writable (a class constructor, a frozen function,
//     `defineProperty(f, 'prototype', { writable: false })`). The property kept its old object
//     while [[Construct]] started using the rejected one; and
//   * `DefineProperty` tested `result.BooleanValue` for success, but [[DefineOwnProperty]]
//     reports success as `undefined` and failure as `false`, so every SUCCESSFUL redefine was
//     read as a failure and the cache was never updated at all.
//
// The two defects point opposite ways — one applies a change that was refused, the other ignores
// a change that was accepted — and both are pinned here. `Reflect.construct`, which reads the
// property rather than the cache, is the cross-check: it was right throughout, so every test
// that fixes `new` also asserts the two agree.
//
// This is the roadmap retest-queue item "a rejected function-`prototype` write historically
// changing later [[Construct]] behavior". It reproduced.
public class FunctionPrototypeWriteConstructTests
{
    private static string Eval(string body)
    {
        using var ctx = new JSContext(options: new JSContextOptions { ScriptHostMode = true });
        return ctx.Eval("(function () {" + body + "})()").ToString();
    }

    // ---- A rejected write leaves BOTH the property and [[Construct]] on the old object ----

    [Theory(Timeout = 600000)]
    [InlineData("Object.freeze(F);")]                                                  // frozen function
    [InlineData("Object.defineProperty(F, 'prototype', { writable: false });")]         // explicitly non-writable
    public void ARejectedPrototypeWriteLeavesConstructOnTheOriginalObject(string makeNonWritable)
        => Assert.Equal("true|true|true", Eval(
            "function F() {} var p = F.prototype; " + makeNonWritable
            + "F.prototype = { z: 1 };"
            + "return [F.prototype === p,"
            + "        Object.getPrototypeOf(new F()) === p,"
            + "        Object.getPrototypeOf(Reflect.construct(F, [])) === p].join('|');"));

    // The sharpest form: the instance must NOT be built on the object the rejected write named.
    [Fact(Timeout = 600000)]
    public void ARejectedPrototypeWriteNeverReachesTheInstance()
        => Assert.Equal("false", Eval(
            "function F() {} Object.freeze(F); var q = { z: 1 }; F.prototype = q;"
            + "return String(Object.getPrototypeOf(new F()) === q);"));

    // In strict mode the rejected write also throws — and still must not take effect.
    [Fact(Timeout = 600000)]
    public void AStrictModeRejectedPrototypeWriteThrowsAndDoesNotTakeEffect()
        => Assert.Equal("TypeError|true|true", Eval(
            "'use strict';"
            + "function F() {} var p = F.prototype; Object.freeze(F);"
            + "var thrown = 'no throw'; try { F.prototype = { z: 1 }; } catch (e) { thrown = e.name; }"
            + "return [thrown,"
            + "        F.prototype === p,"
            + "        Object.getPrototypeOf(new F()) === p].join('|');"));

    // A class constructor's `prototype` is non-writable by specification, so the same rule holds
    // without any explicit freezing.
    [Fact(Timeout = 600000)]
    public void AClassPrototypeIsNonWritableAndConstructIgnoresTheRejectedWrite()
        => Assert.Equal("true|true", Eval(
            "class K {} var p = K.prototype;"
            + "try { K.prototype = { z: 1 }; } catch (e) {}"
            + "return [K.prototype === p, Object.getPrototypeOf(new K()) === p].join('|');"));

    // ---- An ACCEPTED change must reach [[Construct]] — the opposite failure ----

    // Object.defineProperty legally replaces the property; [[Construct]] has to follow it.
    [Fact(Timeout = 600000)]
    public void ADefinePropertyThatReplacesPrototypeIsUsedByConstruct()
        => Assert.Equal("true|true|true", Eval(
            "function F() {} var q = { z: 1 };"
            + "Object.defineProperty(F, 'prototype', { value: q });"
            + "return [F.prototype === q,"
            + "        Object.getPrototypeOf(new F()) === q,"
            + "        Object.getPrototypeOf(Reflect.construct(F, [])) === q].join('|');"));

    // An ordinary writable assignment keeps working. This is the DeltaBlue shape
    // (`this.prototype = new Inheriter()` from one repeatedly-executed site) that the cache
    // exists for, so it is pinned alongside the rejection cases.
    [Fact(Timeout = 600000)]
    public void AnOrdinaryPrototypeAssignmentIsUsedByConstruct()
        => Assert.Equal("true|true", Eval(
            "function F() {} var q = { z: 1 }; F.prototype = q;"
            + "return [F.prototype === q, Object.getPrototypeOf(new F()) === q].join('|');"));

    // Repeated assignment from one site (the cached/shape-tracked store path) stays correct.
    [Fact(Timeout = 600000)]
    public void RepeatedPrototypeAssignmentsFromOneSiteAllReachConstruct()
        => Assert.Equal("true", Eval(
            "function make() { function F() {} F.prototype = { tag: 1 }; return F; }"
            + "var ok = true;"
            + "for (var i = 0; i < 5; i++) { var F = make(); ok = ok && Object.getPrototypeOf(new F()) === F.prototype; }"
            + "return String(ok);"));

    // Successive accepted writes each take effect, mixing the assignment and defineProperty
    // paths — the cache stays live rather than latching on the first object it saw.
    [Fact(Timeout = 600000)]
    public void SuccessiveAcceptedWritesEachReachConstruct()
        => Assert.Equal("true|true|true", Eval(
            "function F() {} var results = [];"
            + "var a = { n: 1 }; F.prototype = a; results.push(Object.getPrototypeOf(new F()) === a);"
            + "var b = { n: 2 }; F.prototype = b; results.push(Object.getPrototypeOf(new F()) === b);"
            + "var c = { n: 3 }; Object.defineProperty(F, 'prototype', { value: c });"
            + "results.push(Object.getPrototypeOf(new F()) === c);"
            + "return results.join('|');"));

    // Making `prototype` non-writable is permanent for a function (the property is
    // non-configurable from creation), so a later attempt to redefine it is refused and neither
    // the property nor [[Construct]] moves.
    [Fact(Timeout = 600000)]
    public void OnceNonWritableThePrototypeCannotBeRedefinedAndConstructHoldsStill()
        => Assert.Equal("true|true|true", Eval(
            "function F() {} var p = F.prototype;"
            + "Object.defineProperty(F, 'prototype', { writable: false });"
            + "var refused = false;"
            + "try { Object.defineProperty(F, 'prototype', { value: { z: 1 } }); } catch (e) { refused = true; }"
            + "return [refused, F.prototype === p, Object.getPrototypeOf(new F()) === p].join('|');"));

    // ---- Assigning a non-object does not demote the function from being a constructor ----

    [Fact(Timeout = 600000)]
    public void AssigningANonObjectPrototypeLeavesTheFunctionConstructable()
        => Assert.Equal("object|true", Eval(
            "function F() {} F.prototype = 1;"
            + "var instance = new F();"
            + "return [typeof instance, Reflect.construct(F, []) !== null].join('|');"));
}
