using System;
using System.Reflection;
using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions;

/// <summary>
/// Emits a speculative fast path with its generic fallback <b>in the same method</b>, so a
/// failed guard is a branch rather than a restart (docs/performance-roadmap.md item 4-3b).
/// </summary>
/// <remarks>
/// <para>
/// This is the transfer item 4-3's design spike identified as the one that makes speculation
/// legal <em>after effects have begun</em>, which is what items 4-2 and 4-4 need and what
/// restart cannot give them. Because both forms are compiled into one method the CLR locals are
/// shared, so there is no state to transfer and nothing to get wrong; because nothing is
/// re-entered, effects already performed are never repeated; and because no
/// <c>CallFrameStack</c> slot changes hands, the three frame invariants item 4-3a has to
/// preserve are not engaged at all.
/// </para>
/// <para>
/// <b>The contract a caller must satisfy.</b> The facility guarantees the transfer; it cannot
/// guarantee that the two forms mean the same thing.
/// </para>
/// <list type="number">
/// <item><b>The guard must be free of observable effects.</b> It runs on every execution and its
/// result decides nothing else; a guard that mutated state would do so whether or not the
/// speculation held.</item>
/// <item><b>The two forms must be observationally equivalent whenever the guard holds</b> — same
/// value, same effects, same order. The guard is what makes that a narrow claim rather than a
/// broad one.</item>
/// <item><b>Neither form may assume it is the only one emitted.</b> The generic path can never
/// be dropped; that is the standing cost of this design, along with the code size of carrying
/// both.</item>
/// </list>
/// <para>
/// <b>What the facility does guarantee</b>, and the reason a caller should not hand-roll the
/// conditional: <paramref name="subject"/> is evaluated <b>exactly once</b>, into a temporary
/// that the guard and both arms share. Hand-rolled, the obvious spelling evaluates it in the
/// guard and again in whichever arm runs — so a receiver with an effect (<c>f().x</c>) would
/// run <c>f()</c> twice, which is a wrong answer that only shows up on effectful receivers.
/// </para>
/// </remarks>
public static class SpeculationBuilder
{
    private static readonly MethodInfo AllowsMethod =
        typeof(Speculation).GetMethod(nameof(Speculation.Allows), [typeof(int)])
        ?? throw new InvalidOperationException("Speculation.Allows(int) not found");

    private static readonly MethodInfo OnGuardMissedMethod =
        typeof(Speculation).GetMethod(nameof(Speculation.OnGuardMissed), [typeof(int)])
        ?? throw new InvalidOperationException("Speculation.OnGuardMissed(int) not found");

    /// <summary>
    /// Builds <c>guard ? specialized : (record, generic)</c> over a single evaluation of
    /// <paramref name="subject"/>.
    /// </summary>
    /// <param name="site">
    /// A site index from <see cref="Speculation.Allocate"/>. Its poison state short-circuits the
    /// guard, so a site whose guard keeps failing stops paying for it.
    /// </param>
    /// <param name="subject">
    /// The value both the guard and the two forms operate on. Evaluated once; see the remarks.
    /// </param>
    /// <param name="guard">Given the evaluated subject, a <c>bool</c> the speculation rests on.</param>
    /// <param name="specialized">The form taken when the guard holds.</param>
    /// <param name="generic">The form taken otherwise, and the one that must always be correct.</param>
    /// <param name="resultType">The type both forms yield.</param>
    public static Expression Guarded(
        int site,
        Expression subject,
        Func<Expression, Expression> guard,
        Func<Expression, Expression> specialized,
        Func<Expression, Expression> generic,
        Type resultType)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(guard);
        ArgumentNullException.ThrowIfNull(specialized);
        ArgumentNullException.ThrowIfNull(generic);
        ArgumentNullException.ThrowIfNull(resultType);

        // A site the allocator refused (the table is bounded) speculates on nothing. Emitting
        // the generic form alone is the safe answer and costs the guard nothing.
        if (site < 0)
            return generic(subject);

        var subjectTemp = Expression.Parameter(subject.Type, "#specsubject");
        var taken = Expression.Parameter(typeof(bool), "#spectaken");
        var siteConstant = Expression.Constant(site);

        // The poison check comes FIRST and is spelled as a conditional rather than a boolean
        // `and`, so a poisoned site short-circuits without evaluating the guard at all — the
        // whole point of poisoning is to stop paying for a speculation that stopped holding.
        var guardWithPoisonCheck = Expression.Condition(
            Expression.Call(null, AllowsMethod, siteConstant),
            guard(subjectTemp),
            Expression.Constant(false),
            typeof(bool));

        // OnGuardMissed returns bool purely so it can sit in expression position; the value is
        // discarded by the block, which yields its last expression.
        var fallback = Expression.Block(
            (IFastEnumerable<BParameterExpression>)null,
            Expression.Call(null, OnGuardMissedMethod, siteConstant),
            generic(subjectTemp));

        return Expression.Block(
            new Sequence<BParameterExpression> { subjectTemp, taken },
            Expression.Assign(subjectTemp, subject),
            Expression.Assign(taken, guardWithPoisonCheck),
            Expression.Condition(taken, specialized(subjectTemp), fallback, resultType));
    }
}
