using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// OrdinarySet step 5.f: when the receiver has no own property P, the write must go through
// CreateDataProperty(Receiver, P, V), which mandates writable/enumerable/configurable all
// true — the TARGET's attributes never carry over, because they describe a different
// object's property.
//
// The engine copies the target's attributes instead. That is recorded as a known gap in
// docs/performance-roadmap.md (found while implementing P1-1, verified identical on the
// engine before that change, so it is pre-existing and unrelated to it).
//
// These tests exist because test262 does not reach the case at the pinned suite ref:
// Reflect/set/creates-a-data-descriptor.js exercises the receiver path only with an EMPTY
// target, where step 4.d supplies the default all-true ownDesc and the engine agrees, and
// Reflect/set/different-property-descriptors.js only covers an accessor on the receiver.
// So there is no upstream file to add to the compliance failure manifest; without these
// the gap is invisible.
public class ReflectSetReceiverAttributesTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact(Timeout = 600000)]
    public void KnownGap_ReflectSetCopiesTargetAttributesToTheReceiver()
    {
        // KNOWN GAP, pinned so it cannot change silently. Expected per spec is
        // "true,true,true"; the engine reports the target's own attributes instead.
        // When this is fixed the expectation becomes "true,true,true" and the test
        // should be renamed.
        var result = Eval("""
            var target = {};
            Object.defineProperty(target, 'p', {
                value: 1, writable: true, enumerable: false, configurable: false
            });
            var receiver = {};
            Reflect.set(target, 'p', 42, receiver);
            var d = Object.getOwnPropertyDescriptor(receiver, 'p');
            [d.writable, d.enumerable, d.configurable].join(',');
            """);

        Assert.Equal("true,false,false", result);
    }

    [Fact(Timeout = 600000)]
    public void ReflectSet_WritesTheValueToTheReceiverAndNotTheTarget()
    {
        // The value half is correct and is not part of the gap — pinned so a future fix to
        // the attributes cannot regress it.
        var result = Eval("""
            var target = {};
            Object.defineProperty(target, 'p', {
                value: 1, writable: true, enumerable: false, configurable: false
            });
            var receiver = {};
            var ok = Reflect.set(target, 'p', 42, receiver);
            [ok, receiver.p, target.p].join(',');
            """);

        Assert.Equal("true,42,1", result);
    }

    [Fact(Timeout = 600000)]
    public void ReflectSet_OnAnEmptyTarget_GivesTheReceiverAllTrueAttributes()
    {
        // The case test262 DOES cover (creates-a-data-descriptor.js): with no own property
        // on the target, step 4.d supplies the default all-true ownDesc, and the engine is
        // correct here. This is the contrast that localizes the gap above to attribute
        // propagation rather than to CreateDataProperty itself.
        var result = Eval("""
            var target = {};
            var receiver = {};
            Reflect.set(target, 'p', 42, receiver);
            var d = Object.getOwnPropertyDescriptor(receiver, 'p');
            [d.writable, d.enumerable, d.configurable].join(',');
            """);

        Assert.Equal("true,true,true", result);
    }
}
