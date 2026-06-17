using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #830 (problems 62, 67): Iterator.zipKeyed enumerates the iterables object's
// [[OwnPropertyKeys]] (string AND symbol keys), snapshotting them before reading any value
// so a getter that mutates the object cannot drop a later key. Mirrors test262
// Iterator/zipKeyed/{iterables-iteration-symbol-key,iterables-iteration-deleted}.
public class Issue830ZipKeyedTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    // Symbol-keyed iterables are included, after the string keys.
    public void SymbolKeysIncluded()
    {
        var r = Eval("""
            const a = Symbol("a");
            const obj = { b: [1], [a]: [2] };
            const zipped = Iterator.zipKeyed(obj);
            const first = zipped.next().value;
            // Keys of the produced record: string "b" then symbol a.
            const keys = Reflect.ownKeys(first);
            String(keys.length) + "|" + String(keys[0]) + "|" + (keys[1] === a) +
              "|" + first.b + "|" + first[a];
        """);
        Assert.Equal("2|b|true|1|2", r);
    }

    [Fact]
    // The key list is snapshotted before values are read: a getter deleting a later key
    // does not stop that key's own getter from running.
    public void KeysSnapshottedBeforeReadingValues()
    {
        var r = Eval("""
            const log = [];
            const obj = {};
            Object.defineProperty(obj, "a", { enumerable: true, configurable: true,
              get() { log.push("a"); delete obj.b; return [1]; } });
            Object.defineProperty(obj, "b", { enumerable: true, configurable: true,
              get() { log.push("b"); return [2]; } });
            Object.defineProperty(obj, "c", { enumerable: true, configurable: true,
              get() { log.push("c"); return [3]; } });
            Iterator.zipKeyed(obj).next();
            log.join(",");
        """);
        Assert.Equal("a,c", r);
    }

    [Theory]
    // Basic zipKeyed still works and pairs values by key.
    [InlineData("const z = Iterator.zipKeyed({ a: [1, 2], b: [3, 4] }); const r = z.next().value; r.a + ',' + r.b;", "1,3")]
    // A non-enumerable own property is skipped.
    [InlineData("const o = {}; Object.defineProperty(o, 'a', { value: [1], enumerable: false }); o.b = [9]; const r = Iterator.zipKeyed(o).next().value; ('a' in r) + '|' + r.b;", "false|9")]
    public void Behavior(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));
}
