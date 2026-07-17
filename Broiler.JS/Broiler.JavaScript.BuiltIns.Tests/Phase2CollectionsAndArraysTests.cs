using Broiler.JavaScript.Engine;
using Broiler.JavaScript.BuiltIns.Map;
using Broiler.JavaScript.BuiltIns.Set;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.ExpressionCompiler;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.BuiltIns.Tests;

public class Phase2CollectionsAndArraysTests
{
    private static string Eval(string source)
    {
        using var context = new JSContext();
        return context.Eval(source, "phase2-collections-and-arrays.js").ToString();
    }

    [Fact]
    public void MapAndSetUseSameValueZeroWithoutTextualIdentityKeys()
        => Assert.Equal(
            "true|true|true|true|true|false|true",
            Eval("""
                var object = {};
                var otherObject = {};
                var map = new Map();
                map.set(NaN, 'nan');
                map.set(-0, 'zero');
                map.set('value', 'string');
                map.set(10n, 'bigint');
                map.set(object, 'object');
                [
                    map.get(0) === 'zero',
                    map.get(Number('x')) === 'nan',
                    map.get(('va' + 'lue')) === 'string',
                    map.get(BigInt('10')) === 'bigint',
                    map.get(object) === 'object',
                    map.has(otherObject),
                    Object.is(map.keys().next().value, NaN)
                ].join('|');
                """));

    [Fact]
    public void MapAndSetDeleteReaddAtTheEndOfIterationOrder()
        => Assert.Equal(
            "a,c,b|a,c,b",
            Eval("""
                var map = new Map([['a', 1], ['b', 2], ['c', 3]]);
                map.delete('b'); map.set('b', 4);
                var set = new Set(['a', 'b', 'c']);
                set.delete('b'); set.add('b');
                Array.from(map.keys()).join(',') + '|' + Array.from(set.values()).join(',');
                """));

    [Fact]
    public void WeakCollectionsSupportDeleteReinsertAndRejectPrimitiveKeys()
        => Assert.Equal(
            "true|true|false|true|true|false",
            Eval("""
                var key = {};
                var map = new WeakMap();
                map.set(key, 1);
                var before = map.has(key);
                var deleted = map.delete(key);
                map.set(key, 2);
                var set = new WeakSet([key]);
                [before, deleted, map.get(key) === 1, map.get(key) === 2,
                 set.has(key), set.has(1)].join('|');
                """));

    [Fact]
    public void WeakMapValueToKeyCycleDoesNotRetainKey()
    {
        using var context = new JSContext();
        var map = Assert.IsType<JSWeakMap>(context.Eval("new WeakMap()"));
        var weakKey = AddSelfCycle(map);

        CollectFully(weakKey);

        Assert.False(weakKey.IsAlive);
        GC.KeepAlive(map);
        GC.KeepAlive(context);
    }

    [Fact]
    public void WeakSetDoesNotRetainMember()
    {
        using var context = new JSContext();
        var set = Assert.IsType<JSWeakSet>(context.Eval("new WeakSet()"));
        var weakKey = AddMember(set);

        CollectFully(weakKey);

        Assert.False(weakKey.IsAlive);
        GC.KeepAlive(set);
        GC.KeepAlive(context);
    }

    [Fact]
    public void ObjectEnumerationPreservesOrderAndDescriptorInterleaving()
        => Assert.Equal(
            "1,3,4|1",
            Eval("""
                var ordered = { 1: 'one', first: 1, second: 2, third: 3 };
                delete ordered.second;
                ordered.second = 4;
                var dynamic = {
                    get a() {
                        Object.defineProperty(dynamic, 'b', { enumerable: false });
                        return 1;
                    },
                    b: 2
                };
                Object.values(ordered).slice(1).join(',') + '|' + Object.values(dynamic).join(',');
                """));

    [Fact]
    public void DenseArrayFastPathsRespectHolesAndIndexedPrototypeInvalidation()
        => Assert.Equal(
            "false|9|9|3,2,1|7,7,7",
            Eval("""
                var first = [, 2, 3];
                first.copyWithin(1, 0, 1);
                var firstHasOne = 1 in first;

                Array.prototype[0] = 9;
                var second = [, 2, 3];
                second.copyWithin(1, 0, 1);
                var inheritedCopy = second[1];
                delete Array.prototype[0];

                var reverse = [1, 2, 3];
                reverse.reverse();
                var fill = [1, 2, 3];
                fill.fill(7);
                [firstHasOne, inheritedCopy, second[1], reverse.join(','), fill.join(',')].join('|');
                """));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference AddSelfCycle(JSWeakMap map)
    {
        var key = new JSObject();
        map.Set(new Arguments(map, key, key));
        return new WeakReference(key);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference AddMember(JSWeakSet set)
    {
        var key = new JSObject();
        set.Add(new Arguments(set, key));
        return new WeakReference(key);
    }

    private static void CollectFully(WeakReference reference)
    {
        for (var i = 0; i < 8 && reference.IsAlive; i++)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
        }
    }
}
