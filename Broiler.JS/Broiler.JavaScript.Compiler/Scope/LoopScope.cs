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

    public LoopScope Get(string name)
    {
        var start = this;
        while (start != null && start.Name != name)
            start = start.Parent;
        return start;
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
