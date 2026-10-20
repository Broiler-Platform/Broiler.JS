// Scratch probe for SJL-12.3 task 3.11 (never committed): Broiler.JS's acceptance of each listed input at two entry
// points (FastParser alone; CoreScript.Compile), with the source line that threw the refusal (first-chance stacks).
using System.Text;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.ExpressionCompiler.Runtime;
using Broiler.JavaScript.Parser;
using Broiler.JavaScript.Runtime;

static string ReadUtf8(string path)
{
    var bytes = File.ReadAllBytes(path);
    var start = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
    return Encoding.UTF8.GetString(bytes, start, bytes.Length - start);
}
static string Clean(string s) => (s ?? "").Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ');
var suite = args[0]; var lines = File.ReadAllLines(args[1]).Where(l => l.Length > 0).ToArray(); var output = args[2];
var results = new List<string>(lines.Length);
var thrown = new List<(Exception ex, string where)>();
AppDomain.CurrentDomain.FirstChanceException += (s, e) =>
{
    if (e.Exception is not (FastParseException or JSException)) return;
    var st = new System.Diagnostics.StackTrace(1, true);
    var sb = new StringBuilder();
    foreach (var f in st.GetFrames())
    {
        var fn = f.GetFileName();
        if (fn == null) continue;
        var i = fn.IndexOf("/Broiler.JS/", StringComparison.Ordinal);
        sb.Append(i >= 0 ? fn[(i + 12)..] : Path.GetFileName(fn)).Append(':').Append(f.GetFileLineNumber()).Append(' ');
        if (sb.Length > 500) break;
    }
    lock (thrown) thrown.Add((e.Exception, sb.ToString().Trim()));
};
string Site(Exception final)
{
    lock (thrown)
    {
        Exception target = null;
        if (final is not FastParseException)
            for (var i = thrown.Count - 1; i >= 0 && target == null; i--)
                if (thrown[i].ex is FastParseException fp && final.Message.Contains(fp.Message, StringComparison.Ordinal)) target = fp;
        target ??= final;
        for (var i = 0; i < thrown.Count; i++)
            if (ReferenceEquals(thrown[i].ex, target)) return thrown[i].where;
        return thrown.Count > 0 ? "last " + thrown[^1].where : "?";
    }
}
var thread = new Thread(() =>
{
    using var context = new JSContext(experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026);
    context.CodeCache = new NoCache();
    foreach (var line in lines)
    {
        var parts = line.Split('\t'); var rel = parts[0]; var variant = parts[1];
        var text = ReadUtf8(Path.Combine(suite, rel));
        if (variant == "strict") text = "\"use strict\";\n" + text;
        var module = variant == "module";
        string parse, compile;
        lock (thrown) thrown.Clear();
        try
        {
            if (module) FastParser.ParseModule(new StringSpan(text));
            else new FastParser(new FastTokenStream(new StringSpan(text))).ParseProgram();
            parse = "accepted\t-\t-";
        }
        catch (FastParseException e) { parse = "refused\t" + Clean(e.Message) + "\t" + Site(e); }
        catch (Exception e) { parse = "crash\t" + e.GetType().Name + ": " + Clean(e.Message) + "\t-"; }
        lock (thrown) thrown.Clear();
        try
        {
            CoreScript.Compile(text, "input.js", codeCache: new NoCache(), isModule: module);
            compile = "accepted\t-\t-";
        }
        catch (JSException e) { compile = "refused\t" + Clean(e.Error?.ToString() ?? e.Message) + "\t" + Site(e); }
        catch (Exception e) { compile = "crash\t" + e.GetType().Name + ": " + Clean(e.Message) + "\t-"; }
        results.Add($"{rel}\t{variant}\t{parse}\t{compile}");
    }
}, 256 * 1024 * 1024);
thread.Start(); thread.Join();
File.WriteAllLines(output, results);
Console.WriteLine($"jssiteprobe: {lines.Length} inputs");

sealed class NoCache : ICodeCache
{
    public JSFunctionDelegate GetOrCreate(in JSCode code) => code.Compiler().CompileWithNestedLambdas();
}
