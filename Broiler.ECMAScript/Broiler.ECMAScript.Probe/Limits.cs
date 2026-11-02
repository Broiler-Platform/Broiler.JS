using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Broiler.ECMAScript.Runners;
using Broiler.ECMAScript.Syntax;

namespace Broiler.ECMAScript.Probe;

/// <summary>
/// The probe's limits modes (stage SJL-13.2e; Broiler.VM's JSD-0083, decision D14, and the home rule's part G):
/// <c>--parse-one</c> parses one source alone in its process, on a thread of the stack the library documents, and exits
/// with a code that names its outcome; <c>--measure-frames</c> measures, in the process, the frames and the stack each
/// level of the parser's recursion costs at the point the library refuses a source past its bound. Neither mode judges:
/// the deep-nesting corpus's runner and the derivation read what they write.
/// </summary>
internal static class Limits
{
    /// <summary>The exit code of a source the library accepts.</summary>
    internal const int Accepted = 0;

    /// <summary>The exit code of a source refused with the contract's nesting diagnostic (catalogue row EX0011, JSE9201).</summary>
    internal const int RefusedNesting = 10;

    /// <summary>The exit code of a source refused with the tree-depth diagnostic (catalogue row EX0012, JSE9202).</summary>
    internal const int RefusedTreeDepth = 11;

    /// <summary>The exit code of a source refused with any other diagnostic.</summary>
    internal const int RefusedOther = 12;

    /// <summary>The exit code of an exception that escaped the library.</summary>
    internal const int Escaped = 13;

    /// <summary>
    /// <c>--parse-one file --nesting n [--goal script|module] [--stack-kib k]</c>: parses the file under configuration V's
    /// request (Broiler.VM's: no proposal, no lexer extension, the goal's default switches) with the nesting parameter
    /// <c>n</c>, on a thread of <see cref="ParseLimits.StackBytes"/> or of <c>k</c> KiB, writes one line - the outcome, the
    /// first diagnostic's code, its argument and its line and column, tab-separated - and exits with the outcome's code.
    /// A process that ends with no line and no such code lost its stack.
    /// </summary>
    internal static int ParseOne(string[] args)
    {
        string? file = null;
        var nesting = ParseLimits.MaximumSupportedNestingDepth;
        var goal = ParseGoal.Script;
        var stack = ParseLimits.StackBytes;
        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--nesting" when i + 1 < args.Length:
                    nesting = int.Parse(args[++i], System.Globalization.NumberStyles.None, System.Globalization.NumberFormatInfo.InvariantInfo);
                    break;
                case "--goal" when i + 1 < args.Length:
                    goal = args[++i] == "module" ? ParseGoal.Module : ParseGoal.Script;
                    break;
                case "--stack-kib" when i + 1 < args.Length:
                    stack = 1024 * int.Parse(args[++i], System.Globalization.NumberStyles.None, System.Globalization.NumberFormatInfo.InvariantInfo);
                    break;
                default:
                    if (file is not null)
                        return Usage();
                    file = args[i];
                    break;
            }
        }

        if (file is null)
            return Usage();

        var text = File.ReadAllText(file, new UTF8Encoding(false, true));
        var tables = UnicodeDataHome.Tables;
        ParseResult? result = null;
        Exception? escaped = null;
        var worker = new Thread(
            () =>
            {
                try
                {
                    result = SyntaxParser.Parse(Request(text, goal, nesting, tables));
                }
                catch (Exception e)
                {
                    escaped = e;
                }
            },
            stack);
        worker.Start();
        worker.Join();

        if (escaped is not null || result is null)
        {
            Console.Out.Write("escaped\t-\t-\t-\n");
            return Escaped;
        }

        if (result.Outcome == ParseOutcome.Accepted)
        {
            Console.Out.Write("accepted\t-\t-\t-\n");
            return Accepted;
        }

        var first = result.Diagnostics[0];
        Console.Out.Write("refused\t" + first.Code + "\t" + (first.Arguments.Count > 0 ? first.Arguments[0] : "-") + "\t"
            + Program.Decimal(first.Span.Line) + ":" + Program.Decimal(first.Span.Column) + "\n");
        return first.Code switch
        {
            "JSE9201" => RefusedNesting,
            "JSE9202" => RefusedTreeDepth,
            _ => RefusedOther,
        };
    }

    /// <summary>Configuration V's request (the home rule's section "The two configurations") with a stated nesting parameter.</summary>
    internal static ParseRequest Request(string text, ParseGoal goal, int nesting, Broiler.Unicode.Algorithms.IUnicodeTables tables) =>
        new(new SourceText(text), goal, tables, AlgorithmsLibrary.RunnerRegularExpressions.Instance)
        {
            Limits = new ParseLimits { NestingDepth = new DepthLimit(nesting) },
        };

    // ---------------------------------------------------------------------------------------------------------------
    // The frames and the stack each level costs (decision D14, "as JSD-0022 derived Broiler.VM's").

    /// <summary>A recursive construct: its name, the text that opens one level and the text that closes it, around a core.</summary>
    private readonly record struct Construct(string Name, string Prefix, string Open, string Core, string Close, string Suffix);

    /// <summary>
    /// Every recursion of the parser the nesting bound charges, by the construct that reaches it: the corpus's six shapes,
    /// and each other production that recurses with no statement or assignment between, which the parser charges too.
    /// </summary>
    private static Construct[] Constructs() =>
    [
        new("brackets", "x = ", "[", "1", "]", ";"),
        new("parentheses", "x = ", "(", "1", ")", ";"),
        new("blocks", "", "{", "", "}", ""),
        new("functions", "", "function f() {", "", "}", ""),
        new("templates", "x = ", "`${", "1", "}`", ";"),
        new("substitutions", "x = `${", "`${", "1", "}`", "}`;"),
        new("objects", "x = ", "{a: ", "1", "}", ";"),
        new("calls", "x = ", "f(", "1", ")", ";"),
        new("assignments", "", "a = ", "1", "", ";"),
        new("conditionals", "x = ", "a ? b : ", "c", "", ";"),
        new("arrows", "x = ", "y => ", "y", "", ";"),
        new("unary", "x = ", "!", "a", "", ";"),
        new("new", "x = ", "new ", "a", "", ";"),
        new("exponent", "x = ", "2 ** ", "2", "", ";"),
        new("binding-patterns", "var ", "[", "a", "]", " = b;"),
        new("class-heritage", "x = ", "class extends ", "Object", "{}", ";"),
        new("if-statements", "", "if (a) ", "b;", "", ""),
        new("while-statements", "", "while (a) ", "b;", "", ""),
        new("arrow-blocks", "x = ", "() => {return ", "1", "}", ";"),
        new("computed-keys", "x = ", "{[", "a", "]: 1}", ";"),
        new("class-methods", "x = ", "class { m() { return ", "1", "} }", ";"),
        new("spread", "x = ", "[...", "a", "]", ";"),
        new("yield", "function* g() { ", "yield ", "1", "", " }"),
        new("import-calls", "x = ", "import(", "a", ")", ";"),
        new("async-arrows", "x = ", "async () => ", "1", "", ";"),
        new("object-patterns", "var ", "{a: ", "b", "}", " = c;"),

        // The precedence ladder: a source that climbs the grammar's ten binary precedences, from || to *, before each
        // charged level. Read by recursion, as stage SJL-13.2e first read it, each precedence added a ParseBinary and
        // ParseBinaryRest pair to the level's frames; read with an explicit stack, it adds none. #x in reads its right
        // operand by a recursion of its own, once per level.
        new("ladder-computed-keys", "x = ", Ladder + "{[", "a", "]: 1}", ";"),
        new("ladder-parentheses", "x = ", Ladder + "(", "1", ")", ";"),
        new("ladder-objects", "x = ", Ladder + "{a: ", "1", "}", ";"),
        new("ladder-calls", "x = ", Ladder + "f(", "1", ")", ";"),
        new("ladder-brackets", "x = ", Ladder + "[", "1", "]", ";"),
        new("ladder-templates", "x = ", Ladder + "`${", "1", "}`", ";"),
        new("ladder-class-methods", "x = ", Ladder + "class { m() { return ", "1", "} }", ";"),
        new("ladder-import-calls", "x = ", Ladder + "import(", "a", ")", ";"),
        new("ladder-private-in", "x = class { #x; m() { return ", PrivateInLadder + "(", "1", ")", "; } };"),
    ];

    /// <summary>One operator of each binary precedence from <c>||</c> to <c>*</c>, each operand's right operand the next precedence's.</summary>
    internal const string Ladder = "a || a && a | a ^ a & a == a < a << a + a * ";

    /// <summary>The ladder with <c>#x in</c> at the relational precedence, whose right operand is read by a recursion of its own.</summary>
    internal const string PrivateInLadder = "a || a && a | a ^ a & a == #x in a << a + a * ";

    /// <summary>
    /// <c>--measure-frames</c>: for each construct and each of four nesting parameters, the parse of a source nested past
    /// the parameter, on a thread of <see cref="ParseLimits.StackBytes"/>, observed at the moment the library throws its
    /// nesting refusal, the deepest point of its recursion: the frames on the thread's stack (<see cref="StackTrace"/>) and
    /// the stack left below that point. The differences between parameters give the frames and the bytes per level, and
    /// the stack the deepest parameter has used. For the tree-depth bound, the same is observed at the tree-depth refusal of
    /// chains of different shapes and lengths. Tab-separated, one line per observation.
    /// </summary>
    internal static int MeasureFrames()
    {
        var tables = UnicodeDataHome.Tables;
        var measurement = new Measurement();
        var output = new StringBuilder();
        output.Append("# probe-bytes\t").Append(Program.Decimal(measurement.BytesPerProbe)).Append('\n');
        output.Append("# stack-bytes\t").Append(Program.Decimal(ParseLimits.StackBytes)).Append('\n');
        output.Append("# construct\tparameter\toutcome\tframes\tremaining-bytes\n");
        AppDomain.CurrentDomain.FirstChanceException += measurement.OnFirstChance;
        try
        {
            var baseline = measurement.Observe(static () => throw new InvalidOperationException("the baseline"));
            Line(output, "baseline", "-", "-", baseline);

            foreach (var construct in Constructs())
            {
                foreach (var parameter in new[] { 64, 128, 256, ParseLimits.MaximumSupportedNestingDepth })
                {
                    var text = construct.Prefix + Repeat(construct.Open, parameter + 8) + construct.Core + Repeat(construct.Close, parameter + 8) + construct.Suffix;
                    var outcome = "?";
                    var observation = measurement.Observe(() => outcome = Outcome(SyntaxParser.Parse(Request(text, ParseGoal.Script, parameter, tables))));
                    Line(output, construct.Name, Program.Decimal(parameter), outcome, observation);
                }
            }

            // The tree depth: chains refused one node past the constant, a left operand of a nodes beginning a chain of b
            // nodes. A chain is built in a loop, so the frames at the refusal do not grow with a or b.
            var chains = new (string Name, string Open, string Inner, string Middle, string Outer, string Close, int A, int B)[]
            {
                ("binary-chain", "x = a", "+a", "", "+a", ";", 0, ParseLimits.MaximumTreeDepth + 1),
                ("binary-chain", "x = a", "+a", "", "+a", ";", 0, ParseLimits.MaximumTreeDepth * 10),
                ("member-chain", "x = o", ".a", "", ".a", ";", 0, ParseLimits.MaximumTreeDepth + 1),
                ("left-parenthesised-chain", "x = (a", "+a", ")", "+b", ";", 1, ParseLimits.MaximumTreeDepth),
                ("left-parenthesised-chain", "x = (a", "+a", ")", "+b", ";", ParseLimits.MaximumTreeDepth / 2, ParseLimits.MaximumTreeDepth / 2 + 1),
                ("left-parenthesised-chain", "x = (a", "+a", ")", "+b", ";", ParseLimits.MaximumTreeDepth, 1),
                ("left-template-chain", "x = `${(a", "+a", ")", "+b", "}`;", ParseLimits.MaximumTreeDepth / 2, ParseLimits.MaximumTreeDepth / 2 + 1),
                ("left-member-chain", "x = o", ".a", "", "+b", ";", ParseLimits.MaximumTreeDepth / 2, ParseLimits.MaximumTreeDepth / 2 + 1),
                ("object-parenthesised-chain", "x = (a", "+a", ")", ".b", ";", ParseLimits.MaximumTreeDepth / 2, ParseLimits.MaximumTreeDepth / 2 + 1),
            };
            foreach (var (name, open, inner, middle, outer, close, a, b) in chains)
            {
                var text = open + Repeat(inner, a) + middle + Repeat(outer, b) + close;
                var outcome = "?";
                var observation = measurement.Observe(() => outcome = Outcome(SyntaxParser.Parse(Request(text, ParseGoal.Script, 64, tables))));
                Line(output, name, Program.Decimal(a) + "+" + Program.Decimal(b), outcome, observation);
            }
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= measurement.OnFirstChance;
        }

        Console.Out.Write(output.ToString());
        return 0;
    }

    private static string Outcome(ParseResult result) => result.Outcome == ParseOutcome.Accepted ? "accepted" : result.Diagnostics[0].Code;

    private static void Line(StringBuilder output, string name, string parameter, string outcome, (int Frames, long Remaining) observation) =>
        output.Append(name).Append('\t').Append(parameter).Append('\t').Append(outcome).Append('\t').Append(Program.Decimal(observation.Frames)).Append('\t')
            .Append(observation.Remaining.ToString(System.Globalization.NumberFormatInfo.InvariantInfo)).Append('\n');

    private static string Repeat(string text, int count)
    {
        var builder = new StringBuilder(text.Length * count);
        for (var i = 0; i < count; i++)
            builder.Append(text);
        return builder.ToString();
    }

    /// <summary>One measurement: the thread it watches, and what it found at the first exception thrown there.</summary>
    private sealed class Measurement
    {
        private Thread? watched;
        private bool captured;
        private int frames;
        private long remaining;

        internal Measurement() => BytesPerProbe = Calibrate();

        /// <summary>The bytes of stack one frame of <see cref="Probe"/> takes, measured on two threads of known stacks.</summary>
        internal int BytesPerProbe { get; }

        /// <summary>Runs <paramref name="work"/> on a fresh thread of <see cref="ParseLimits.StackBytes"/>; answers what was captured at its first exception.</summary>
        internal (int Frames, long Remaining) Observe(Action work)
        {
            captured = false;
            frames = -1;
            remaining = -1;
            var thread = new Thread(
                () =>
                {
                    try
                    {
                        work();
                    }
                    catch (InvalidOperationException)
                    {
                    }
                },
                ParseLimits.StackBytes);
            watched = thread;
            thread.Start();
            thread.Join();
            watched = null;
            return (frames, remaining);
        }

        /// <summary>At the throw, before any handler runs: the stack is the thrower's, at its deepest.</summary>
        internal void OnFirstChance(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            if (captured || !ReferenceEquals(Thread.CurrentThread, watched))
                return;
            captured = true;
            frames = new StackTrace(false).FrameCount;
            remaining = (long)Probe(0) * BytesPerProbe;
        }

        // The bytes one probe frame takes: the probes two threads of known stacks hold at their start, differenced.
        private static int Calibrate()
        {
            static int Count(int stack)
            {
                var count = 0;
                var thread = new Thread(() => count = Probe(0), stack);
                thread.Start();
                thread.Join();
                return count;
            }

            var small = Count(4 * 1024 * 1024);
            var large = Count(8 * 1024 * 1024);
            return 4 * 1024 * 1024 / (large - small);
        }
    }

    // The stack left below the caller, in probes: a recursion of fixed frames until the runtime says the stack is nearly
    // spent. Each frame holds a buffer the recursion writes, so that no frame is optimised away; compiled optimised from its
    // first call, so that its frame does not change size when the JIT's tiers replace it during a run.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Probe(int depth)
    {
        Span<byte> pad = stackalloc byte[256];
        pad[depth & 255] = (byte)depth;
        if (!RuntimeHelpers.TryEnsureSufficientExecutionStack())
            return depth + pad[0] - pad[0];
        return Probe(depth + 1) + pad[(depth + 1) & 255] - pad[(depth + 1) & 255];
    }

    private static int Usage()
    {
        Console.Error.Write("usage: Broiler.ECMAScript.Probe --parse-one file [--nesting n] [--goal script|module] [--stack-kib k] | --measure-frames\n");
        return 2;
    }
}
