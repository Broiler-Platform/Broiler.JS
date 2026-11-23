using System.Reflection;
using Broiler.ECMAScript.Syntax;
using Broiler.JavaScript.Ast.Misc;
using ParseCorpus;

// args: <manifest> <test262 root> <ids file: id TAB variant per line> <out tsv>
// One line per input variant: id, variant, outcome, then each diagnostic as code:messageId (in the library's order).
var manifest = Manifest.Read(args[0]);
var wanted = File.ReadAllLines(args[2]).Where(l => l.Length > 0).Select(l => l.Split('\t')).Select(f => (f[0], f[1])).ToHashSet();
var parser = typeof(FastParseException).Assembly.GetType("Broiler.JavaScript.Parser.Bridge.SharedSyntaxBridge", true)!;
var reqType = typeof(FastParseException).Assembly.GetType("Broiler.JavaScript.Parser.Bridge.BridgeRequest", true)!;
const BindingFlags any = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
var request = parser.GetMethod("Request", any)!;
var script = reqType.GetProperty("Script", any)!.GetValue(null);
var module = reqType.GetProperty("Module", any)!.GetValue(null);
var lines = new List<string>();
var t = new Thread(() =>
{
    foreach (var row in manifest.Rows)
        foreach (var variant in row.Variants)
        {
            if (!wanted.Contains((row.Id, variant))) continue;
            var source = manifest.Load(args[1], row);
            var composed = variant == "strict" ? manifest.StrictPrologue + source : source;
            var ps = request.GetParameters();
            var span = Activator.CreateInstance(ps[0].ParameterType.GetElementType()!, composed)!;
            var req = (ParseRequest)request.Invoke(null, [span, variant == "module" ? module : script])!;
            var result = SyntaxParser.Parse(req);
            lines.Add(string.Join('\t', row.Id, variant, result.Outcome.ToString(),
                string.Join(';', result.Diagnostics.Select(d => d.Code + ":" + d.MessageId))));
        }
}, 256 * 1024 * 1024);
t.Start(); t.Join();
File.WriteAllLines(args[3], lines);
Console.Error.WriteLine($"bridge-codes: {lines.Count} of {wanted.Count} input variant(s)");
