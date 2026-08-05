using System;
using System.Collections.Generic;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    /// <summary>
    /// A nested function's body, and everything needed to compile it <em>later</em> — item 1-1's
    /// remaining half (docs/performance-roadmap.md).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The enclosing scope is kept alive rather than snapshotted, and that is the whole design.</b>
    /// A deferred body must be compiled against the scope it was written in, and reproducing that
    /// scope means reproducing <em>twenty-eight</em> separate pieces of compiler state — fourteen
    /// <c>CreateFunction</c> parameters, nine reads of the enclosing <see cref="FastFunctionScope"/>
    /// and five <see cref="FastCompiler"/> fields. A snapshot has to enumerate all of them and stays
    /// correct only until someone adds a twenty-ninth; every omission is a silent miscompile on the
    /// least-exercised path in the engine.
    /// </para>
    /// <para>
    /// <b>Keeping it alive costs nothing, which is what makes the choice easy.</b>
    /// <see cref="FastFunctionScope"/> is not pooled — <c>Dispose</c> is
    /// <see cref="ExpressionCompiler.Core.LinkedStackItem{T}"/>'s, which only pops the stack — so a
    /// scope object outlives its frame already and holding a reference to it retains the entire
    /// <c>Parent</c> chain, valid and unrecycled. Re-entry is then
    /// <see cref="ExpressionCompiler.Core.LinkedStack{T}.Switch"/>, which the stack already has.
    /// </para>
    /// <para>
    /// <b>What is still saved and restored is the compiler's own instance state</b>, because that
    /// genuinely does move on: strictness, the script-info parameter, the member-initializer flag
    /// and the parameter-initializer depth. They are restored around the re-entry rather than
    /// reconstructed, so the body sees what it would have seen.
    /// </para>
    /// <para>
    /// A <c>with</c> boundary is refused rather than restored. <c>withBoundaries</c> is a mutable
    /// stack whose contents at the deferred body's own position cannot be recovered from its depth,
    /// and a body inside one resolves names dynamically anyway — the same reason
    /// <see cref="FreeNameScan"/> reports such a body undeferrable.
    /// </para>
    /// </remarks>
    internal sealed class DeferredBodyContext
    {
        private readonly FastCompiler compiler;
        private readonly FastFunctionScope enclosing;
        private readonly AstFunctionExpression function;

        private readonly bool strictMode;
        private readonly BParameterExpression savedScriptInfo;
        private readonly bool savedInMemberInitializer;
        private readonly int savedParameterInitializerDepth;

        internal DeferredBodyContext(
            FastCompiler compiler,
            FastFunctionScope enclosing,
            AstFunctionExpression function)
        {
            this.compiler = compiler;
            this.enclosing = enclosing;
            this.function = function;

            strictMode = compiler.IsStrictMode;
            savedScriptInfo = compiler.scriptInfo;
            savedInMemberInitializer = compiler.inMemberInitializer;
            savedParameterInitializerDepth = compiler.parameterInitializerDepth;
        }

        /// <summary>The function whose body this would build.</summary>
        internal AstFunctionExpression Function => function;

        /// <summary>
        /// Compiles the body now, against the retained enclosing scope, exactly as the eager path
        /// would have.
        /// </summary>
        /// <remarks>
        /// Every piece of state this touches is restored afterwards, including on the throwing
        /// path — a deferred compile happens in the middle of somebody else's work, and leaving
        /// the scope stack moved would corrupt a compilation that has nothing to do with this one.
        /// </remarks>
        internal BExpression Compile()
        {
            var previousTop = compiler.scope.Switch(enclosing);
            var previousStrict = compiler.IsStrictMode;
            var previousScriptInfo = compiler.scriptInfo;
            var previousInMemberInitializer = compiler.inMemberInitializer;
            var previousDepth = compiler.parameterInitializerDepth;

            compiler.IsStrictMode = strictMode;
            compiler.scriptInfo = savedScriptInfo;
            compiler.inMemberInitializer = savedInMemberInitializer;
            compiler.parameterInitializerDepth = savedParameterInitializerDepth;

            try
            {
                // The CORE, not the wrapper: re-entering through the wrapper would retain a
                // fresh context for the same function on every replay, without bound.
                return compiler.CreateFunctionCore(function);
            }
            finally
            {
                compiler.parameterInitializerDepth = previousDepth;
                compiler.inMemberInitializer = previousInMemberInitializer;
                compiler.scriptInfo = previousScriptInfo;
                compiler.IsStrictMode = previousStrict;
                compiler.scope.Switch(previousTop);
            }
        }
    }

    /// <summary>
    /// Records a retained context per nested function while
    /// <see cref="DeferredTreeCompilation.Retaining"/> is on, so re-entry can be checked against
    /// the eager result before anything is built on it.
    /// </summary>
    private void RetainDeferredBody(AstFunctionExpression function, FastFunctionScope enclosing, BExpression eager)
    {
        // A `with` boundary is refused rather than restored — see the type's remarks.
        if (withBoundaries.Count != 0)
            return;

        DeferredTreeCompilation.Retain(
            new DeferredBodyContext(this, enclosing, function),
            eager);
    }
}

/// <summary>
/// Item 1-1's remaining half: the switch that retains enclosing scopes so a nested body can be
/// compiled after the enclosing compilation has finished, and the record of what was retained.
/// </summary>
/// <remarks>
/// <para>
/// <b>Off by default, and retention alone changes no output.</b> With it on, the compiler keeps a
/// <see cref="FastCompiler.DeferredBodyContext"/> per nested function and compiles the body eagerly
/// as before; nothing is deferred yet. That is deliberate — the re-entry is the load-bearing and
/// riskiest half of the mechanism, and it can be proven against the eager result while both exist,
/// which is impossible once one of them stops being produced.
/// </para>
/// <para>
/// The contexts are held in a list rather than handed to a caller because the thing under test is
/// whether re-entry reproduces the eager tree <em>for every function in a program</em>, not for one
/// chosen by the test.
/// </para>
/// </remarks>
public static class DeferredTreeCompilation
{
    public const string EnvironmentVariable = "BROILER_JS_DEFER_TREE";

    private static int retaining = string.Equals(
        Environment.GetEnvironmentVariable(EnvironmentVariable),
        "1",
        StringComparison.Ordinal)
        ? 1
        : 0;

    /// <summary>Whether the compiler retains an enclosing scope per nested function.</summary>
    public static bool Retaining
    {
        get => System.Threading.Volatile.Read(ref retaining) != 0;
        set => System.Threading.Volatile.Write(ref retaining, value ? 1 : 0);
    }

    private static readonly List<(object Context, string Eager)> retained = [];

    internal static void Retain(FastCompiler.DeferredBodyContext context, BExpression eager)
    {
        lock (retained)
            retained.Add((context, Print(eager)));
    }

    /// <summary>Retained contexts since the last <see cref="Reset"/>.</summary>
    public static int RetainedCount
    {
        get { lock (retained) return retained.Count; }
    }

    /// <summary>
    /// Re-enters every retained context and reports how many reproduced the eager tree exactly.
    /// </summary>
    /// <remarks>
    /// The comparison is the <em>printed</em> tree, which is the strongest equality available here:
    /// two runs of the same compilation produce structurally distinct objects, so reference or
    /// value equality would report a difference that is not one, while the printer is what the
    /// expression compiler itself uses to describe a tree.
    /// </remarks>
    public static (int Total, int Reproduced, string FirstDifference) Recompile()
    {
        (object Context, string Eager)[] snapshot;
        lock (retained)
            snapshot = retained.ToArray();

        var reproduced = 0;
        string firstDifference = null;
        foreach (var (context, eager) in snapshot)
        {
            string actual;
            try
            {
                actual = Print(((FastCompiler.DeferredBodyContext)context).Compile());
            }
            catch (Exception e)
            {
                actual = e.GetType().Name + ": " + e.Message;
            }

            if (string.Equals(actual, eager, StringComparison.Ordinal))
            {
                reproduced++;
                continue;
            }

            firstDifference ??= FirstDifferingLine(eager, actual);
        }

        return (snapshot.Length, reproduced, firstDifference);
    }

    public static void Reset()
    {
        lock (retained)
            retained.Clear();
    }

    private static string Print(BExpression expression)
    {
        if (expression == null)
            return "<null>";

        using var writer = new System.IO.StringWriter();
        using var indented = new System.CodeDom.Compiler.IndentedTextWriter(writer);
        expression.Print(indented);
        indented.Flush();
        return Canonicalise(writer.ToString());
    }

    /// <summary>
    /// Renames compiler-generated temporaries to their order of first appearance, so two
    /// compilations of the same source compare equal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Alpha-equivalence is the strongest equality that CAN hold here, and finding that out is
    /// the first thing the re-entry measured.</b> A second compilation necessarily draws fresh
    /// names from the compiler's monotonic counter, so the eager tree says <c>Context3</c>,
    /// <c>StackItem3</c>, <c>a-3</c>, <c>LABEL_3</c> and the re-entered one says <c>…4</c>
    /// throughout — identical trees, different gensyms. Comparing the raw text reports every
    /// function as a difference and says nothing.
    /// </para>
    /// <para>
    /// The four families share one counter, so mapping the NUMBER is enough and is deliberately
    /// done by first appearance rather than by value: that way a re-entry which produced the same
    /// structure with the numbers in a different ORDER would still be caught, which a
    /// "strip all digits" normalisation would hide.
    /// </para>
    /// </remarks>
    private static string Canonicalise(string printed)
    {
        // Item 4-2b's inline-cache site range, which a second compilation necessarily re-allocates.
        //
        // **Named as an exception rather than normalised away with the gensyms, because it is the
        // one difference that would MATTER if it arose in the real mechanism.** The range comes
        // from PropertyInlineCacheSite.NextReadSite, a process-wide monotonic counter that 4-2b
        // uses to address item 4-1's feedback; compiling the same function twice takes two ranges
        // — here [0,0) eagerly and [1,1) on re-entry. A genuine deferral compiles the body ONCE,
        // so the divergence cannot occur; it is an artifact of comparing an eager tree against a
        // re-entered one, which is a thing only this check ever does.
        // The SAME artifact as the tiering range above, and the one the corpus is made of: every
        // property access carries its own site index, drawn from the same process-wide counter, so
        // a second compilation of a body with fifty property accesses differs in fifty places and
        // in nothing else. Canonicalised by first appearance rather than erased, so a re-entry
        // that emitted the sites in a different ORDER — which would be a real defect — still
        // shows. The -1 sentinel maps like any other value and stays distinguishable from a site.
        printed = System.Text.RegularExpressions.Regex.Replace(
            printed,
            @"(?<=PropertyInlineCacheSite\.(?:Get|Set)\()-?\d+",
            Ordinal("#site"));

        printed = System.Text.RegularExpressions.Regex.Replace(
            printed,
            @"EnableTiering\(([^()]*), \d+, \d+\)",
            "EnableTiering($1, #site, #site)");

        return System.Text.RegularExpressions.Regex.Replace(
            printed,
            @"(?<=(?:Context|StackItem|LABEL_|a-|#Temp[A-Za-z]*|#function|#tieredFunction|IElementEnumerator_))\d+",
            Ordinal("#"));
    }

    /// <summary>
    /// The first line on which the two trees differ, with a little context.
    /// </summary>
    /// <remarks>
    /// Two truncated blobs are what this used to report, and on a 13 000-character tree the
    /// truncation lands nowhere near the difference — so the diagnostic said only that there was
    /// one. A line pair names the construct.
    /// </remarks>
    private static string FirstDifferingLine(string eager, string actual)
    {
        var a = eager.Split('\n');
        var b = actual.Split('\n');
        for (var i = 0; i < Math.Max(a.Length, b.Length); i++)
        {
            var left = i < a.Length ? a[i].Trim() : "<end>";
            var right = i < b.Length ? b[i].Trim() : "<end>";
            if (!string.Equals(left, right, StringComparison.Ordinal))
                return $"line {i}:\n  eager: {Cap(left)}\n  re-entered: {Cap(right)}";
        }

        return $"identical line by line; lengths {eager.Length} vs {actual.Length}";
    }

    /// <summary>
    /// A replacer that maps each distinct matched value to its order of FIRST APPEARANCE.
    /// </summary>
    /// <remarks>
    /// First appearance rather than value, so a re-entry that produced the same structure with the
    /// numbers in a different order is still reported as a difference — which erasing the digits,
    /// or hashing them, would hide. Each call gets its own table, so the two sides are canonicalised
    /// independently and equal output means equal structure.
    /// </remarks>
    private static System.Text.RegularExpressions.MatchEvaluator Ordinal(string prefix)
    {
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);
        return match =>
        {
            if (!seen.TryGetValue(match.Value, out var canonical))
            {
                canonical = prefix + seen.Count;
                seen[match.Value] = canonical;
            }

            return canonical;
        };
    }

    private static string Cap(string value)
        => value.Length <= 220 ? value : value[..220] + " …";
}
