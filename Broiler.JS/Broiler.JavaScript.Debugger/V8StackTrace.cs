using Broiler.JavaScript.Engine;
using System.Collections.Generic;

namespace Broiler.JavaScript.Debugger;

public class V8StackTrace
{
    public V8StackTrace(JSContext context)
    {
        List<V8CallFrame> cflist = [];
        var frames = context.Frames;

        for (var i = frames.Depth - 1; i >= 0; i--)
        {
            frames.Describe(i, out var function, out var fileName, out var line, out var column);
            cflist.Add(new V8CallFrame
            {
                FunctionName = function.Value,
                ScriptId = fileName,
                Url = fileName,
                LineNumber = line,
                ColumnNumber = column
            });
        }

        CallFrames = cflist;
    }

    public List<V8CallFrame> CallFrames { get; set; }
}

public class V8CallFrame
{
    public int ColumnNumber { get; set; }
    public int LineNumber { get; set; }
    public string Url { get; set; }
    public string ScriptId { get; set; }
    public string FunctionName { get; set; }
}
