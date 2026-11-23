using System.Diagnostics;
using System.Reflection;
using Broiler.ECMAScript.Syntax;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Parser;

// args: <entry: P | L | B> <source: 1 | 2 | 3>
// The test's three eval sources, built as it builds them: (1) "{}" doubled 21 times; (2) that, then s += s.slice(0, -4);
// (3) that, then s += "{}". Each parsed once under the script goal (the test's direct eval is at the global level,
// sloppy; the eval goal's inherited context is empty there, so the script goal reads the same text), on a thread of
// 256 MiB, in this process alone. One line: entry, source, length, outcome, wall ms, this process's processor ms, peak
// working set MiB, bytes allocated.
var entry = args[0];
var which = int.Parse(args[1]);
var s = "{}";
for (var i = 0; i < 21; i++) s += s;
if (which >= 2) s += s[..^4];
if (which >= 3) s += "{}";

var bridge = typeof(FastParser).Assembly.GetType("Broiler.JavaScript.Parser.Bridge.SharedSyntaxBridge", true)!;
var reqType = typeof(FastParser).Assembly.GetType("Broiler.JavaScript.Parser.Bridge.BridgeRequest", true)!;
const BindingFlags any = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
var script = reqType.GetProperty("Script", any)!.GetValue(null);
var request = bridge.GetMethod("Request", any)!;
var parse = bridge.GetMethod("Parse", any, null, [typeof(StringSpan).MakeByRefType(), reqType], null)!;

var outcome = "accepted";
var process = Process.GetCurrentProcess();
var cpu0 = process.TotalProcessorTime;
var alloc0 = GC.GetTotalAllocatedBytes(true);
var clock = Stopwatch.StartNew();
var t = new Thread(() =>
{
    try
    {
        switch (entry)
        {
            case "P":
                new FastParser(new FastTokenStream(new StringSpan(s), (FastKeywordMap?)null)).ParseProgram();
                break;
            case "L":
                var req = (ParseRequest)request.Invoke(null, [new StringSpan(s), script])!;
                var result = SyntaxParser.Parse(req);
                outcome = result.Outcome.ToString() + (result.Diagnostics.Count > 0 ? " " + result.Diagnostics[0].Code : "");
                break;
            case "B":
                parse.Invoke(null, [new StringSpan(s), script]);
                break;
            default:
                throw new ArgumentException("entry: P, L or B");
        }
    }
    catch (Exception e)
    {
        var inner = e is TargetInvocationException { InnerException: { } x } ? x : e;
        outcome = "threw " + inner.GetType().FullName + ": " + inner.Message.Split('\n')[0];
    }
}, 256 * 1024 * 1024);
t.Start(); t.Join();
clock.Stop();
process.Refresh();
var cpu = (process.TotalProcessorTime - cpu0).TotalMilliseconds;
var alloc = GC.GetTotalAllocatedBytes(true) - alloc0;
Console.WriteLine(string.Join('\t', entry, which, s.Length, outcome, clock.ElapsedMilliseconds, (long)cpu,
    process.PeakWorkingSet64 / (1024 * 1024), alloc));
