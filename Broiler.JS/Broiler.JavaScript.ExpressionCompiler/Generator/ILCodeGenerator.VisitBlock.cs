using System.Reflection.Emit;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Generator;

public partial class ILCodeGenerator
{
    protected override CodeInfo VisitBlock(BBlockExpression yBlockExpression)
    {
        using var tvs = tempVariables.Push();
        foreach(var p in yBlockExpression.FlattenVariables)
        {
            if (variables.TryGetValue(p, out _))
                continue;
            variables.Create(p, tvs);
        }
        var expressions = yBlockExpression.FlattenExpressions;
        foreach(var (exp, last) in expressions)
        {
            VisitSave(exp, last);
        }
        return true;
    }

    private CodeInfo VisitSave(BExpression exp, bool save)
    {
        if(exp.NodeType == BExpressionType.Assign)
        {
            if (!save)
            {
                return VisitAssign(exp as BAssignExpression, -1);
            }
        }
        Visit(exp);
        if (!save)
        {
            if (exp.Type != typeof(void))
            {
                il.Emit(OpCodes.Pop);
            }
        }
        return true;
    }
}
