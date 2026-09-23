using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Debugger.Tests;

public class DebuggerTests
{
    [Fact(Timeout = 600000)]
    public void V8RemoteObject_FromNumber_HasNumberType()
    {
        using var ctx = new JSContext();
        var num = new JSNumber(42);
        var remote = new V8RemoteObject(num);
        Assert.Equal("number", remote.Type);
    }

    [Fact(Timeout = 600000)]
    public void V8RemoteObject_FromString_HasStringType()
    {
        var remote = new V8RemoteObject("test");
        Assert.Equal("string", remote.Type);
    }

    [Fact(Timeout = 600000)]
    public void V8CallFrame_DefaultValues()
    {
        var frame = new V8CallFrame();
        Assert.Equal(0, frame.ColumnNumber);
        Assert.Equal(0, frame.LineNumber);
    }

    // The engine's lines and columns are 1-based (SpanLocation); the DevTools protocol's
    // columnNumber is 0-based, so a call indented by two spaces reports column 2. (The line stays
    // 1-based here, as it was before the scanner's columns were made consistent.)
    [Fact(Timeout = 600000)]
    public void V8StackTrace_ReportsZeroBasedColumns()
    {
        using var ctx = new JSContext();
        V8StackTrace? trace = null;
        ctx["capture"] = new JSFunction((in Arguments a) =>
        {
            trace = new V8StackTrace(ctx);
            return JSUndefined.Value;
        }, "capture");
        ctx.Eval("function f() {\n  capture();\n}\n  f();", "columns.js");
        Assert.NotNull(trace);
        var frames = string.Join(";", trace!.CallFrames.Select(f => $"{f.FunctionName}@{f.LineNumber}:{f.ColumnNumber}"));
        Assert.Equal("f@2:2;@4:2", frames);
    }
}
