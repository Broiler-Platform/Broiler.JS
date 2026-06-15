using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// ArrayBuffer.prototype.transfer preserves resizability; transferToFixedLength does not
// (issue #798 problem 11).
public class ArrayBufferTransferResizableTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string expr)
    {
        using var ctx = new JSContext();
        return ctx.Eval(expr).ToString();
    }

    [Theory]
    // from-resizable-to-{same,larger,smaller,zero}: the destination stays resizable with maxByteLength.
    [InlineData("8", "true,16")]
    [InlineData("16", "true,16")]
    [InlineData("4", "true,16")]
    [InlineData("0", "true,16")]
    public void Transfer_FromResizable_StaysResizable(string newLen, string expected)
    {
        Load();
        var result = Eval($$"""
            var src = new ArrayBuffer(8, { maxByteLength: 16 });
            var dst = src.transfer({{newLen}});
            [dst.resizable, dst.maxByteLength].join(',');
        """);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TransferToFixedLength_FromResizable_BecomesFixed()
    {
        Load();
        var result = Eval("""
            var src = new ArrayBuffer(8, { maxByteLength: 16 });
            var dst = src.transferToFixedLength();
            [dst.resizable, dst.maxByteLength, dst.byteLength].join(',');
        """);
        Assert.Equal("false,8,8", result);
    }

    [Fact]
    public void Transfer_FromFixed_StaysFixed()
    {
        Load();
        var result = Eval("""
            var src = new ArrayBuffer(8);
            var dst = src.transfer();
            [dst.resizable, dst.byteLength].join(',');
        """);
        Assert.Equal("false,8", result);
    }

    [Fact]
    public void Transfer_ResizableResult_CanStillResize()
    {
        Load();
        var result = Eval("""
            var src = new ArrayBuffer(8, { maxByteLength: 16 });
            var dst = src.transfer();
            dst.resize(12);
            dst.byteLength;
        """);
        Assert.Equal("12", result);
    }
}
