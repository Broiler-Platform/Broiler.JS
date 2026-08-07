#nullable enable
using System;
using System.Collections.Generic;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.ClosureSeparator;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.ExpressionCompiler;

public static class ClosureRepositoryExtensions
{
    public static ClosureRepository GetClosureRepository(this BLambdaExpression lambda) => ClosureRepository.For(lambda);
}

public class ClosureRepository
{
    private static System.Runtime.CompilerServices.ConditionalWeakTable<BLambdaExpression, ClosureRepository> cache =
        [];

    public readonly Dictionary<BParameterExpression, (BParameterExpression local, BExpression value, int index, int argIndex)>
        Closures = new(Core.ReferenceEqualityComparer.Instance);

    public List<BParameterExpression> Inputs 
        = [];

    private BLambdaExpression lambda;

    protected ClosureRepository(BLambdaExpression lambda) => this.lambda = lambda;

    public static ClosureRepository For(BLambdaExpression lambda)
    {
        if (cache.TryGetValue(lambda, out var value))
            return value;
        value = new ClosureRepository(lambda);
        cache.Add(lambda, value);
        return value;
    }

    public bool TryGet(BParameterExpression pe, out BExpression exp)
    {
        if (Closures.TryGetValue(pe, out var ve))
        {
            exp = ve.value;
            return true;
        }
        exp = default!;
        return false;
    }

    internal BParameterExpression Setup(BParameterExpression pe, Func<BParameterExpression> source)
    {
        if (Closures.TryGetValue(pe, out var value))
            return value.local;
        Runtime.ClosureRewriteDiagnostics.CaptureCreated();
        var s = source();
        bool isBox = typeof(Box).IsAssignableFrom(pe.Type);
        var boxType = isBox ? pe.Type : BoxHelper.For(pe.Type).BoxType;
        var converted = BExpression.Parameter(boxType, pe.Name + "`");
        BExpression valueField = isBox ? converted : BExpression.Field(converted, "Value");
        Closures[pe] = (converted, valueField, Inputs.Count, -1);
        Inputs.Add(s);
        return converted;
    }

    internal BParameterExpression Convert(BParameterExpression pe)
    {
        if (Closures.TryGetValue(pe, out var value))
            return value.local;
        Runtime.ClosureRewriteDiagnostics.CaptureCreated();
        bool isBox = typeof(Box).IsAssignableFrom(pe.Type);
        var boxType = isBox ? pe.Type : BoxHelper.For(pe.Type).BoxType;
        var converted = BExpression.Parameter(boxType, pe.Name + "`");
        BExpression valueField = isBox ? converted : BExpression.Field(converted, "Value");
        var argIndex = Array.IndexOf(lambda.Parameters, pe);
        Closures[pe] = (converted, valueField, -1, argIndex);
        return converted;
    }
}


public class LambdaRewriter: BExpressionMapVisitor
{
    /// <summary>
    /// The variables in scope at the current point of one lambda, as a reference-keyed
    /// multiset.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This was a <see cref="List{T}"/>, and the two operations performed on it are the two a
    /// list is worst at: <c>Contains</c>, which <see cref="CheckForClosure"/> runs for
    /// <em>every parameter reference in the tree</em>, and <c>Remove</c>, which runs once per
    /// variable as each block scope ends. Both are linear scans, so emission cost grew as the
    /// square of the number of bindings in a lambda — and a script's top level is one lambda,
    /// which makes the count of top-level declarations the term that squares.
    /// </para>
    /// <para>
    /// Measured on synthetic top-level declarations, emitting N of them at N = 500 / 1 000 /
    /// 2 000 took 797 / 2 981 / 13 865 ms — a little under 4x per doubling, against parse and
    /// tree construction that stayed flat at a few milliseconds and tens of milliseconds. That
    /// is the whole of Mandreel's front-end cost: its 5 MB is 1 364 top-level function
    /// declarations plus a matching <c>var</c> apiece, and compiling it went 21 307 -> 7 015 ms.
    /// Note what that does <em>not</em> buy: neither Mandreel nor MandreelLatency moves, because
    /// Octane compiles the file at script load and times only the benchmark's run function. The
    /// saving is real and shows up in the suite's wall clock (358.2 -> 350.0 s, non-overlapping
    /// over four runs an arm); it is simply not what those two scores measure.
    /// </para>
    /// <para>
    /// A multiset rather than a set, because the list held duplicates and both operations
    /// depended on it: a variable registered by two nested block scopes was added twice, and
    /// the inner scope's exit had to leave the outer scope's registration behind. A
    /// <see cref="HashSet{T}"/> would have collapsed the pair and let the inner exit take the
    /// binding out of scope early. Reference identity is the comparison the list already used —
    /// <c>BParameterExpression</c> does not override <c>Equals</c> — and is what
    /// <c>ClosureRepository</c> keys on for the same expressions.
    /// </para>
    /// <para>
    /// The list is <em>kept</em> for small scopes rather than replaced outright, because the
    /// dictionary is not free and most scopes are small: hashing a reference costs a runtime
    /// call, while scanning a handful of them is a few compares against warm memory. Measured
    /// on the real corpora, going dictionary-only bought Mandreel 3.6x and cost jQuery — one
    /// large IIFE full of small function scopes — about 20%. Promoting on size takes both:
    /// under the threshold nothing changed, over it the scan is gone. The list is abandoned
    /// once the dictionary exists, so nothing is maintained twice.
    /// </para>
    /// </remarks>
    public class Scope
    {
        /// <summary>
        /// Scope size at which the linear scan stops being the cheaper answer. Chosen an order
        /// of magnitude above an ordinary function's binding count and an order of magnitude
        /// below the widths where the scan showed up at all, so neither end is near it.
        /// </summary>
        public const int DefaultIndexThreshold = 32;

        /// <summary>
        /// Environment override for <see cref="IndexThreshold"/>, read once. Present for the
        /// same reason <c>BROILER_JS_COMPILE_STACK_BYTES</c> is: setting it above any real
        /// scope size restores the pure linear scan, so the promotion can be measured against a
        /// build that is otherwise identical rather than against a second build that differs in
        /// unknown other ways.
        /// </summary>
        public const string IndexThresholdEnvironmentVariable = "BROILER_JS_REWRITER_INDEX_THRESHOLD";

        private static readonly int IndexThreshold =
            int.TryParse(
                Environment.GetEnvironmentVariable(IndexThresholdEnvironmentVariable),
                out var configured) && configured > 0
                ? configured
                : DefaultIndexThreshold;

        public readonly BLambdaExpression Root;

        // Exactly one of these is live: `variables` until the scope outgrows the threshold,
        // `index` from then on. Reference identity in both, matching the List.Contains this
        // replaced.
        private readonly List<BParameterExpression> variables = [];
        private Dictionary<BParameterExpression, int> index;

        public Scope(BLambdaExpression exp)
        {
            Root = exp;
            AddRange(exp.Parameters.AsSequence());
        }

        public static implicit operator Scope(BLambdaExpression e) => new(e);

        public bool Contains(BParameterExpression variable)
        {
            if (index != null)
                return index.ContainsKey(variable);

            // Spelled out rather than List.Contains so the comparison cannot become a virtual
            // Equals call if BParameterExpression ever gains one — the closure rewrite is about
            // binding identity, and two distinct bindings that compared equal would be merged.
            for (var i = 0; i < variables.Count; i++)
            {
                if (ReferenceEquals(variables[i], variable))
                    return true;
            }

            return false;
        }

        private void AddRange(IFastEnumerable<BParameterExpression> added)
        {
            var e = added.GetFastEnumerator();
            while (e.MoveNext(out var variable))
                Add(variable);
        }

        private void Add(BParameterExpression variable)
        {
            if (index != null)
            {
                index.TryGetValue(variable, out var existing);
                index[variable] = existing + 1;
                return;
            }

            variables.Add(variable);
            if (variables.Count <= IndexThreshold)
                return;

            index = new Dictionary<BParameterExpression, int>(
                variables.Count * 2,
                Core.ReferenceEqualityComparer.Instance);
            foreach (var registered in variables)
            {
                index.TryGetValue(registered, out var count);
                index[registered] = count + 1;
            }

            variables.Clear();
        }

        private void Remove(BParameterExpression variable)
        {
            if (index == null)
            {
                // First occurrence only, exactly as List.Remove did: a variable registered by
                // two nested scopes must survive the inner one's exit.
                for (var i = 0; i < variables.Count; i++)
                {
                    if (!ReferenceEquals(variables[i], variable))
                        continue;

                    variables.RemoveAt(i);
                    return;
                }

                return;
            }

            // A variable that is not registered is left alone rather than driving the count
            // negative — again what List.Remove did, which returns false and changes nothing.
            if (!index.TryGetValue(variable, out var registered))
                return;

            if (registered <= 1)
                index.Remove(variable);
            else
                index[variable] = registered - 1;
        }

        internal IDisposable Register(IFastEnumerable<BParameterExpression> added)
        {
            AddRange(added);
            return new DisposableAction(() => {
                var ve = added.GetFastEnumerator();
                while(ve.MoveNext(out var v))
                    Remove(v);
            });
        }
    }

    private ScopedStack<Scope> lambdaStack = new();
    private BLambdaExpression RootExpression;

    // When false, the rewriter processes only the root lambda's own body and does
    // NOT descend into nested lambdas. Used by the async pre-rewrite (which runs in
    // isolation, before the enclosing scope exists): descending there would convert
    // a nested lambda's references to an OUTER variable into boxed closure accesses
    // and finalize/cache that nested lambda's closure repository against an
    // incomplete scope chain — stranding the capture (the enclosing scope never
    // learns to box the variable). Leaving nested lambdas untouched lets the later
    // full top-down rewrite (which has the whole scope chain) thread them correctly.
    private bool rewriteNestedLambdas = true;

    public Scope Root => lambdaStack.TopItem;

    public LambdaRewriter()
    {

    }

    

    protected override BExpression VisitLambda(BLambdaExpression node)
    {
        /// we will not mark nested lambda as relay for two reasons
        /// 1.  In case of Runtime Execution, IMethodRepository will be
        ///     available as global static variable to directly run and
        ///     register the method.
        /// 2.  In case of Assembly builder, there is no need to maintain
        ///     global repository as AssemblyBuilder will become Method 
        ///     Repository
        using var scope = lambdaStack.Push(node);
        if (node != RootExpression)
        {
            node.SetupAsClosure();
            if (!rewriteNestedLambdas)
                return node;
        }
        if (node.This != null)
        {
            Root.Register(node.This.AsSequence());
        }
        Root.Register(node.Parameters.AsSequence());
        // Recorded only on a walk that descends through nested lambdas, so a RewriteRootOnly
        // pass — which stops at each of them on purpose — does not claim to have set them up.
        // RuntimeMethodBuilder.Relay reads this to avoid walking a subtree a second time.
        node.ClosureRewritten |= rewriteNestedLambdas;
        return base.VisitLambda(node);
    }

    protected override BExpression VisitBlock(BBlockExpression yBlockExpression)
    {
        var variables = Root.Root.Name.Name == "body" || Root.Root.Name.Name == "body_outer"
            ? CollectBlockVariables(yBlockExpression)
            : yBlockExpression.FlattenVariables.AsSequence();
        using var scope = Root.Register(variables);
        return base.VisitBlock(yBlockExpression);
    }

    private static Sequence<BParameterExpression> CollectBlockVariables(BExpression expression)
    {
        var variables = new Sequence<BParameterExpression>();
        new BlockVariableCollector(variables).Visit(expression);
        return variables;
    }

    private sealed class BlockVariableCollector(Sequence<BParameterExpression> variables) : BExpressionMapVisitor
    {
        protected override BExpression VisitBlock(BBlockExpression yBlockExpression)
        {
            variables.AddRange(yBlockExpression.FlattenVariables);
            return base.VisitBlock(yBlockExpression);
        }

        protected override BExpression VisitLambda(BLambdaExpression yLambdaExpression) => yLambdaExpression;
    }

    private static void CollectBlockVariables(BExpression expression, Sequence<BParameterExpression> variables)
    {
        switch (expression)
        {
            case BBlockExpression block:
                variables.AddRange(block.FlattenVariables);
                foreach (var (child, _) in block.FlattenExpressions)
                    CollectBlockVariables(child, variables);
                break;

            case BConvertExpression convert:
                CollectBlockVariables(convert.Target, variables);
                break;

            case BReturnExpression @return when @return.Default != null:
                CollectBlockVariables(@return.Default, variables);
                break;

            case BConditionalExpression conditional:
                CollectBlockVariables(conditional.test, variables);
                CollectBlockVariables(conditional.@true, variables);
                if (conditional.@false != null)
                    CollectBlockVariables(conditional.@false, variables);
                break;

            case BLoopExpression loop:
                CollectBlockVariables(loop.Body, variables);
                break;

            case BTryCatchFinallyExpression tryCatchFinally:
                CollectBlockVariables(tryCatchFinally.Try, variables);
                if (tryCatchFinally.Catch != null)
                    CollectBlockVariables(tryCatchFinally.Catch.Body, variables);
                if (tryCatchFinally.Finally != null)
                    CollectBlockVariables(tryCatchFinally.Finally, variables);
                break;
        }
    }

    protected override BExpression VisitParameter(BParameterExpression yParameterExpression)
    {
        CheckForClosure(lambdaStack.Top, yParameterExpression);
        return base.VisitParameter(yParameterExpression);
    }

    private BParameterExpression CheckForClosure(ScopedStack<Scope>.ScopedItem current, BParameterExpression pe, bool setup = false)
    {
        if (current.Item.Contains(pe))
        {
            if (setup)
            {
                return current.Item.Root.GetClosureRepository().Convert(pe);
            }
            return pe;
        }
        var parent = current.Parent;
        if (parent == null)
            return pe;

        var repository = current.Item.Root.GetClosureRepository();
        return repository.Setup(pe, () => CheckForClosure(parent,pe,true));
    }

    public static BExpression Rewrite(BLambdaExpression convert)
    {
        var l = new LambdaRewriter();
        l.RootExpression = convert;
        l.Visit(convert);
        return convert;
    }

    /// <summary>
    /// Rewrites only the root lambda's own body, leaving nested lambdas untouched
    /// for a later enclosing-scope rewrite. Used by the async function pre-rewrite,
    /// which runs before the enclosing scope exists; see <see cref="rewriteNestedLambdas"/>.
    /// </summary>
    public static BExpression RewriteRootOnly(BLambdaExpression convert)
    {
        var l = new LambdaRewriter { rewriteNestedLambdas = false };
        l.RootExpression = convert;
        l.Visit(convert);
        return convert;
    }
}
