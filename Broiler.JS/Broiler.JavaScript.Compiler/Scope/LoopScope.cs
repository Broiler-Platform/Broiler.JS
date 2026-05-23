using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.Compiler;

public class LoopScope(YLabelTarget breakTarget, YLabelTarget continueTarget, bool isSwitch = false, string name = null) : LinkedStackItem<LoopScope>
{
    public readonly YLabelTarget Break = breakTarget;
    public readonly YLabelTarget Continue = continueTarget;
    public readonly string Name = name;
    public readonly bool IsSwitch = isSwitch;
    public YParameterExpression CompletionVariable;

    public LoopScope Get(string name)
    {
        var start = this;
        while (start != null && start.Name != name)
            start = start.Parent;
        return start;
    }

    public YParameterExpression FindCompletionVariable()
    {
        var start = this;
        while (start != null)
        {
            if (start.CompletionVariable != null)
                return start.CompletionVariable;
            start = start.Parent;
        }
        return null;
    }
}
