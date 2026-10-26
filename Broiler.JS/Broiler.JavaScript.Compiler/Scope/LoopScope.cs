using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.Compiler;

public class LoopScope(BLabelTarget breakTarget, BLabelTarget continueTarget, bool isSwitch = false, string name = null) : LinkedStackItem<LoopScope>
{
    public readonly BLabelTarget Break = breakTarget;
    public readonly BLabelTarget Continue = continueTarget;
    public readonly string Name = name;
    public readonly bool IsSwitch = isSwitch;
    public BParameterExpression CompletionVariable;

    /// <summary>
    /// True for the label-only scope of an outer label of a label set: a label whose body
    /// is another label of the same loop (<c>L</c> in <c>L: M: while (…)</c>). Only the
    /// innermost label names the loop's own scope, so such a scope has no
    /// <see cref="Continue"/> target of its own.
    /// </summary>
    public bool PrefixesLoop;

    public LoopScope Get(string name)
    {
        var start = this;
        while (start != null && start.Name != name)
            start = start.Parent;
        return start;
    }

    /// <summary>
    /// The target of <c>continue name</c>, or null when no enclosing scope is labelled
    /// <paramref name="name"/>. An outer label of a label set continues the loop that its
    /// label chain directly wraps, which is the scope just inside the chain.
    /// </summary>
    public BLabelTarget GetContinue(string name)
    {
        LoopScope inside = null;
        for (var start = this; start != null; start = start.Parent)
        {
            if (start.Name == name)
                return start.PrefixesLoop ? inside?.Continue : start.Continue;
            if (!start.PrefixesLoop)
                inside = start;
        }
        return null;
    }
}
