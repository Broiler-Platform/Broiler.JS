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

    // A LabelledStatement whose item is not an iteration statement (`L: { … }`): only
    // `break L` targets it, so the unlabeled break/continue lookups below skip it.
    internal bool IsLabelOnly { get; init; }

    // A label-only scope whose item is another label of the same loop (`L` in
    // `L: M: while (…)`): an outer label of the loop's label set. Only the innermost
    // label names the loop's own scope, so this one has no Continue target of its own.
    internal bool PrefixesLoop { get; init; }

    public LoopScope Get(string name)
    {
        var start = this;
        while (start != null && start.Name != name)
            start = start.Parent;
        return start;
    }

    // The target of `continue name`, or null when no enclosing scope is labelled `name`.
    // An outer label of a label set continues the loop its label chain directly wraps,
    // which is the scope just inside the chain (ECMA-262 sec-continue-statement).
    internal BLabelTarget GetContinue(string name)
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

    // The target of an unlabeled `break`: the innermost IterationStatement or
    // SwitchStatement (ECMA-262 sec-break-statement).
    internal LoopScope GetUnlabeledBreak()
    {
        var start = this;
        while (start != null && start.IsLabelOnly)
            start = start.Parent;
        return start;
    }

    // The target of an unlabeled `continue`: the innermost IterationStatement
    // (ECMA-262 sec-continue-statement).
    internal LoopScope GetUnlabeledContinue()
    {
        var start = this;
        while (start != null && (start.IsLabelOnly || start.IsSwitch))
            start = start.Parent;
        return start;
    }
}
