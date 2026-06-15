using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// The Segments object returned by Intl.Segmenter.prototype.segment exposes a %Segments.prototype%
// [ @@iterator ] method, named "[Symbol.iterator]", returning a Segment Iterator over the segment data
// objects. Issue #808 problem 81.
public class Issue808SegmenterIteratorTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void Segments_HasIteratorMethod_WithCorrectName()
        => Assert.Equal("function|[Symbol.iterator]|0", Eval("""
            var segs = new Intl.Segmenter("en").segment("hi");
            var it = segs[Symbol.iterator];
            typeof it + "|" + it.name + "|" + it.length;
        """));

    [Fact]
    public void Segments_Iterator_YieldsSegments()
        => Assert.Equal("h|e|l|l|o", Eval("""
            var segs = new Intl.Segmenter("en").segment("hello");
            var out = [];
            for (var s of segs[Symbol.iterator]()) out.push(s.segment);
            out.join("|");
        """));

    [Fact]
    public void Segments_Iterator_IsSelfIterable()
        => Assert.Equal("true", Eval("""
            var segs = new Intl.Segmenter("en").segment("hi");
            var it = segs[Symbol.iterator]();
            String(it[Symbol.iterator]() === it);
        """));

    [Fact]
    public void Segments_SpreadAndForOf_StillWork()
        => Assert.Equal("h,i|h,i", Eval("""
            var segs = new Intl.Segmenter("en").segment("hi");
            var spread = [...segs].map(function (s) { return s.segment; }).join(",");
            var loop = [];
            for (var s of segs) loop.push(s.segment);
            spread + "|" + loop.join(",");
        """));
}
