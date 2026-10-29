using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Tests;

// The Annex B.2 legacy `f.caller` / `f.arguments` own data properties are backed by
// deferred property values rather than written into the function's property table on
// every invocation (docs/performance-roadmap.md P0-3). These lock down the observable
// behaviour that rewrite has to preserve: they must stay DATA properties with the same
// attributes, report the in-progress invocation while the function is on the stack and
// null otherwise, hide a strict caller, and freeze once script redefines them.
public class LegacyCallerArgumentsTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void Arguments_IsNullOffStack_AndReportsTheInvocationWhileRunning()
    {
        var result = Eval("""
            function f(a, b) {
                return [f.arguments === null, f.arguments.length, f.arguments[0], f.arguments[1]].join(',');
            }
            var before = f.arguments === null;
            var during = f(7, 8);
            var after = f.arguments === null;
            [before, during, after].join('|');
            """);

        Assert.Equal("true|false,2,7,8|true", result);
    }

    [Fact]
    public void Arguments_ReadTwiceInOneInvocation_ReportsTheSameContents()
    {
        // The value is recomputed per read, so two reads produce distinct objects; their
        // contents must agree.
        var result = Eval("""
            function f(a) {
                var first = f.arguments;
                var second = f.arguments;
                return [first.length === second.length, first[0] === second[0], first[0]].join(',');
            }
            f(42);
            """);

        Assert.Equal("true,true,42", result);
    }

    [Fact]
    public void Arguments_IsAnOrdinaryDataProperty_WithLegacyAttributes()
    {
        var result = Eval("""
            function f() {}
            var d = Object.getOwnPropertyDescriptor(f, 'arguments');
            var c = Object.getOwnPropertyDescriptor(f, 'caller');
            [
                typeof d, 'value' in d, 'get' in d, d.writable, d.enumerable, d.configurable, d.value === null,
                typeof c, 'value' in c, 'get' in c, c.writable, c.enumerable, c.configurable, c.value === null
            ].join('|');
            """);

        Assert.Equal(
            "object|true|false|false|false|false|true|object|true|false|false|false|false|true",
            result);
    }

    [Fact]
    public void Arguments_DescriptorReadWhileOnStack_ReportsTheInvocation()
    {
        // GetOwnPropertyDescriptor must resolve the deferred value exactly as [[Get]] does.
        var result = Eval("""
            function f(a) {
                var d = Object.getOwnPropertyDescriptor(f, 'arguments');
                return [d.value === null, d.value.length, d.value[0], d.writable, d.configurable].join(',');
            }
            f('x');
            """);

        Assert.Equal("false,1,x,false,false", result);
    }

    [Fact]
    public void Caller_ReportsANonStrictOrdinaryCaller_AndNullAtTopLevel()
    {
        var result = Eval("""
            function callee() { return callee.caller; }
            function caller() { return callee(); }
            [callee.caller === null, caller() === caller, callee.caller === null].join('|');
            """);

        Assert.Equal("true|true|true", result);
    }

    [Fact]
    public void Caller_HidesAStrictCaller_AsNull()
    {
        // Annex B forbidden extension: the observed value must never be a strict function,
        // and reading it must not throw.
        var result = Eval("""
            function callee() { return callee.caller; }
            function strictCaller() { 'use strict'; return callee(); }
            (strictCaller() === null) + '|' + (typeof strictCaller() === 'object');
            """);

        Assert.Equal("true|true", result);
    }

    [Fact]
    public void RecursiveInvocation_RestoresTheOuterFrameWhenTheInnerReturns()
    {
        // Each invocation saves and restores the previous frame, so after an inner
        // recursive call returns, the outer call's own arguments are visible again.
        var result = Eval("""
            var seen = [];
            function f(n) {
                seen.push('enter' + n + ':' + f.arguments[0]);
                if (n > 0) f(n - 1);
                seen.push('exit' + n + ':' + f.arguments[0]);
            }
            f(2);
            seen.join(',') + '|' + (f.arguments === null);
            """);

        Assert.Equal("enter2:2,enter1:1,enter0:0,exit0:0,exit1:1,exit2:2|true", result);
    }

    [Fact]
    public void ThrowingInvocation_StillRestoresTheFrame()
    {
        var result = Eval("""
            function f() { throw new Error('boom'); }
            var threw = false;
            try { f(); } catch (e) { threw = true; }
            [threw, f.arguments === null, f.caller === null].join('|');
            """);

        Assert.Equal("true|true|true", result);
    }

    [Fact]
    public void ConstructInvocation_ReportsTheFrame_AndRestoresIt()
    {
        var result = Eval("""
            var during;
            function F(a) { during = [F.arguments === null, F.arguments.length, F.arguments[0]].join(','); }
            new F(5);
            during + '|' + (F.arguments === null);
            """);

        Assert.Equal("false,1,5|true", result);
    }

    [Fact]
    public void Redefining_FreezesTheValue_AndStopsLiveUpdates()
    {
        // A non-writable, non-configurable data property's value must stay constant once
        // script has redefined it (test262 Object/internals/DefineOwnProperty/
        // consistent-value-function-caller and -arguments). Redefining is only legal with
        // the property's current value (null), which is exactly the invariant at issue:
        // after the redefine, entering the function must NOT make the value observable.
        var result = Eval("""
            function f() { return [f.arguments === null, f.caller === null].join(','); }
            Object.defineProperty(f, 'arguments', { value: null });
            Object.defineProperty(f, 'caller', { value: null });
            var during = f(1, 2);
            [during, f.arguments === null, f.caller === null].join('|');
            """);

        Assert.Equal("true,true|true|true", result);
    }

    [Fact]
    public void StrictFunctions_DoNotExposeLegacyProperties()
    {
        // A strict function has no own caller/arguments; it inherits Function.prototype's
        // poison-pill accessors, so reading throws.
        var result = Eval("""
            function f() { 'use strict'; }
            function threw(fn) { try { fn(); return 'no-throw'; } catch (e) { return e.constructor.name; } }
            [
                Object.getOwnPropertyDescriptor(f, 'arguments') === undefined,
                Object.getOwnPropertyDescriptor(f, 'caller') === undefined,
                threw(function () { return f.arguments; }),
                threw(function () { return f.caller; })
            ].join('|');
            """);

        Assert.Equal("true|true|TypeError|TypeError", result);
    }

    [Fact]
    public void AssigningLegacyProperties_IsRejected()
    {
        var result = Eval("""
            function f() {}
            function threw(fn) { try { fn(); return 'no-throw'; } catch (e) { return e.constructor.name; } }
            var sloppy = (function () { f.arguments = 1; return f.arguments === null; })();
            [sloppy, threw(function () { 'use strict'; f.arguments = 1; })].join('|');
            """);

        Assert.Equal("true|TypeError", result);
    }

    [Fact]
    public void LegacyArgumentsObject_IsAPlainUnmappedObject()
    {
        var result = Eval("""
            function f(a) {
                var args = f.arguments;
                a = 'reassigned';
                var d = Object.getOwnPropertyDescriptor(args, 'length');
                return [args[0], d.writable === false, d.enumerable, d.configurable].join(',');
            }
            f('original');
            """);

        // Unmapped: writing the parameter does not write through to the captured object.
        Assert.Equal("original,false,false,true", result);
    }

    [Fact]
    public void DeferredValue_DoesNotLeakThroughPropertyEnumeration()
    {
        var result = Eval("""
            function f(a) {
                return [
                    Object.keys(f).join(','),
                    Object.getOwnPropertyNames(f).indexOf('arguments') >= 0,
                    JSON.stringify(f) === undefined
                ].join('|');
            }
            f(1);
            """);

        Assert.Equal("|true|true", result);
    }
}
