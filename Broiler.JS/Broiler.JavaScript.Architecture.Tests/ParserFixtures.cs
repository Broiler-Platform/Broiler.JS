using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Broiler.JavaScript.Architecture.Tests;

/// <summary>
/// The behavioural fixtures of part B of bundle sjl-12-001's rule, run against the judged build: a
/// <c>for (let …)</c> loop parses to one for-statement whose initialiser is the declaration as
/// written; two parses of one source give equal tree dumps; and concurrent parses of one source on
/// four threads never end on another parse's token. Each answers null when it holds, or its
/// violation, whose text never depends on the run; what it found is logged. A fixture that throws,
/// or a build the fixtures cannot load, is reported as a violation naming the exception's type, and
/// the exception is logged.
/// </summary>
internal static class ParserFixtures
{
    internal const string ForLetSource = "for (let i = 0; i < 3; i++) { }";

    internal const string ForLetViolation =
        "fixture: `" + ForLetSource + "` does not parse to one for-statement whose initialiser is the declaration as written";

    /// <summary>
    /// A source with every loop shape the parser rewrites with temporaries today, so that each of the
    /// four places the desugaring names one is reached: the carriers of a <c>let</c> loop's bindings,
    /// a simple name and a pattern; the <c>started</c> flag of a <c>let</c> loop whose update holds a
    /// closure; the head temporary of <c>for-of</c> and <c>for-in</c>; and the renamed bindings of a
    /// <c>const</c> loop with a test.
    /// </summary>
    internal const string RepeatedSource =
        "for (let i = 0; i < 3; i++) { }\n"
        + "for (const x of [1, 2]) { }\n"
        + "for (let k in { a: 1 }) { }\n"
        + "for (let [a, b] = [1, 2]; a < b; a++) { }\n"
        + "for (let j = 0, f = () => j; j < 2; j++) { f(); }\n"
        + "for (let m = 0; m < 1; m++, (() => m)) { }\n"
        + "for (const c = 1; c < 1;) { }\n";

    internal const string RepeatedViolation = "fixture: two parses of one source give different tree dumps";

    internal const string ConcurrentSource = "a";

    internal const int Threads = 4;

    /// <summary>The fixed iteration count: each thread parses the source this many times.</summary>
    internal const int ParsesPerThread = 20_000;

    internal const string ConcurrentViolation =
        "fixture: concurrent parses of one source on four threads ended on another parse's token";

    /// <summary>
    /// Loads the parser at <paramref name="path"/>, runs every fixture against it and returns the
    /// violations, in a fixed order. Nothing a fixture throws escapes: it is the fixture's violation.
    /// </summary>
    internal static List<string> Run(string path, Action<string> log)
    {
        var violations = new List<string>();
        SubjectParser subject;
        try
        {
            subject = new SubjectParser(path);
        }
        catch (Exception exception)
        {
            log("the fixtures could not load the judged parser: " + exception);
            violations.Add("fixture: the judged parser could not be loaded to run the fixtures: " + exception.GetType().FullName);
            return violations;
        }

        using (subject)
        {
            var fixtures = new (string Name, Func<SubjectParser, Action<string>, string?> Run)[]
            {
                ("`" + ForLetSource + "`", ForLet),
                ("two parses of one source", Repeated),
                ("concurrent parses of one source", Concurrent),
            };
            foreach (var (name, fixture) in fixtures)
            {
                try
                {
                    if (fixture(subject, log) is { } violation)
                        violations.Add(violation);
                }
                catch (Exception exception)
                {
                    log("the fixture " + name + " could not run: " + exception);
                    violations.Add("fixture: " + name + " could not run: " + exception.GetType().FullName);
                }
            }
        }

        return violations;
    }

    /// <summary>The loop parses to itself: no block, no carrier declarators, no copy-back.</summary>
    private static string? ForLet(SubjectParser subject, Action<string> log)
    {
        var (program, _) = subject.Parse(ForLetSource);
        var statements = Items(subject.Read(program, "Statements"));
        string? found = null;

        if (statements.Count != 1)
            found = statements.Count + " statements";
        else if (statements[0]?.GetType().Name != "AstForStatement")
            found = "a " + statements[0]?.GetType().Name;
        else if (subject.Read(statements[0]!, "Init") is not { } init || init.GetType().Name != "AstVariableDeclaration")
            found = "a for-statement whose initialiser is not a declaration";
        else
        {
            var kind = subject.Read(init, "Kind")?.ToString();
            var names = Items(subject.Read(init, "Declarators")).Select(d => BindingName(subject, d!)).ToList();
            if (kind != "Let" || names is not ["i"])
                found = "a for-statement whose initialiser is a " + kind + " declaration of " + names.Count + " bindings (" + string.Join(", ", names) + ")";
        }

        if (found is null)
            return null;

        log("for (let …): the source declares `let i`; the parser returned " + found + ".");
        return ForLetViolation;
    }

    /// <summary>The same source twice, in one process: equal dumps, whatever was parsed between.</summary>
    private static string? Repeated(SubjectParser subject, Action<string> log)
    {
        var tree = new TreeText(subject);
        var first = tree.Dump(subject.Parse(RepeatedSource).Program);
        var second = tree.Dump(subject.Parse(RepeatedSource).Program);
        log("two parses: " + first.Count(static c => c == '\n') + " dump lines.");
        if (first == second)
            return null;

        var a = first.Split('\n');
        var b = second.Split('\n');
        var line = Enumerable.Range(0, Math.Min(a.Length, b.Length)).FirstOrDefault(i => a[i] != b[i]);
        log("two parses: the dumps differ first at line " + (line + 1) + ":\n  first:  " + a[line] + "\n  second: " + b[line]);
        return RepeatedViolation;
    }

    /// <summary>
    /// Four threads, released together for each parse, each parsing its own copy of one source: a
    /// copy, not the same string, so that a token's source tells which parse it came from. Every
    /// token the tree holds, and the token the end of the input links back to, must be this parse's.
    /// </summary>
    /// <remarks>
    /// A race shows only when the threads interleave at the wrong moment, so seeing one is a matter
    /// of chance: a run that finds no foreign token does not prove there is no race. Over a parser
    /// that shares its tokens (the rule commit's) a run on four cores sees one in a few percent of
    /// the parses, but a runner with little real parallelism could see none; at the commit that
    /// listed this fixture's violation, before item 3, such a run would have reported it as gone.
    /// </remarks>
    private static string? Concurrent(SubjectParser subject, Action<string> log)
    {
        var tree = new TreeText(subject);
        long parses = 0;
        long foreign = 0;
        long nodes = 0;
        long links = 0;
        string? example = null;
        Exception? failure = null;

        using var barrier = new Barrier(Threads);
        var workers = Enumerable.Range(0, Threads).Select(_ => new Thread(() =>
        {
            try
            {
                for (var i = 0; i < ParsesPerThread; i++)
                {
                    barrier.SignalAndWait();
                    var source = new string(ConcurrentSource.AsSpan());
                    var (program, end) = subject.Parse(source);

                    bool Foreign(object token, string where)
                    {
                        if (ReferenceEquals(token, end) || ReferenceEquals(subject.Read(subject.Read(token, "Span")!, "Source"), source))
                            return false;
                        Interlocked.CompareExchange(ref example, where + " is " + token + ", from another parse", null);
                        return true;
                    }

                    var inTree = false;
                    tree.Tokens(program, t => inTree |= Foreign(t, "a token of the tree"));
                    var inLink = subject.Read(end, "Previous") is { } previous && Foreign(previous, "the token the end of the input links back to");

                    Interlocked.Increment(ref parses);
                    if (inTree)
                        Interlocked.Increment(ref nodes);
                    if (inLink)
                        Interlocked.Increment(ref links);
                    if (inTree || inLink)
                        Interlocked.Increment(ref foreign);
                }
            }
            catch (Exception exception)
            {
                Interlocked.CompareExchange(ref failure, exception, null);
            }
            finally
            {
                barrier.RemoveParticipant();
            }
        })).ToList();

        workers.ForEach(static w => w.Start());
        workers.ForEach(static w => w.Join());

        // A parse that threw is the fixture's failure, reported by Run under the exception's own type.
        if (failure is not null)
            ExceptionDispatchInfo.Throw(failure);

        log("concurrent parses: " + parses + " parses of `" + ConcurrentSource + "` on " + Threads + " threads; " + foreign
            + " held another parse's token (" + nodes + " in the tree, " + links + " as the end of the input's link back)"
            + (example is null ? "." : "; the first seen: " + example + "."));
        return foreign == 0 ? null : ConcurrentViolation;
    }

    private static List<object?> Items(object? sequence) =>
        sequence is IEnumerable items ? items.Cast<object?>().ToList() : [];

    private static string BindingName(SubjectParser subject, object declarator)
    {
        var identifier = subject.Read(declarator, "Identifier");
        if (identifier?.GetType().Name != "AstIdentifier")
            return identifier?.GetType().Name ?? "null";
        return subject.Read(subject.Read(identifier, "Name")!, "Value") as string ?? "null";
    }
}
