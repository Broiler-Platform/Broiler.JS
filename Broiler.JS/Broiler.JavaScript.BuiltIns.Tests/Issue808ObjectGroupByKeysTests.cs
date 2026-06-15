using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Object.groupBy keys the result with ToPropertyKey: a numeric key (e.g. a string's length) becomes a
// canonical array-index property, so the group is reachable as result[4]. The result has a null
// prototype, and Symbol keys are preserved. Issue #808 problem 78 (staging/sm/Array/group.js).
public class Issue808ObjectGroupByKeysTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        // Object.groupBy / Map.groupBy are gated behind the ObjectMapGroupBy feature.
        using var ctx = new JSContext(experimentalFeatures: JavaScriptFeatureFlags.ObjectMapGroupBy);
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void GroupBy_NumericKey_IndexedAccess()
        => Assert.Equal("test|foo,bar|hello", Eval("""
            var array = ["test", "foo", "bar", "hello"];
            var arr = Object.groupBy(array, function (k) { return k.length; });
            arr[4][0] + "|" + arr[3].join(",") + "|" + arr[5][0];
        """));

    [Fact]
    public void GroupBy_NullPrototype()
        => Assert.Equal("true", Eval("""
            var arr = Object.groupBy([1], function () { return 0; });
            String(Object.getPrototypeOf(arr) === null);
        """));

    [Fact]
    public void GroupBy_StringKeys()
        => Assert.Equal("1,3|2,4", Eval("""
            var s = Object.groupBy([1, 2, 3, 4], function (x) { return x % 2 ? "odd" : "even"; });
            s.odd.join(",") + "|" + s.even.join(",");
        """));

    [Fact]
    public void GroupBy_SymbolKey()
        => Assert.Equal("1", Eval("""
            var sym = Symbol("k");
            var sg = Object.groupBy([1], function () { return sym; });
            String(sg[sym][0]);
        """));

    [Fact]
    public void MapGroupBy_NumericKey()
        => Assert.Equal("test", Eval("""
            var array = ["test", "foo", "bar", "hello"];
            var m = Map.groupBy(array, function (k) { return k.length; });
            m.get(4)[0];
        """));
}
