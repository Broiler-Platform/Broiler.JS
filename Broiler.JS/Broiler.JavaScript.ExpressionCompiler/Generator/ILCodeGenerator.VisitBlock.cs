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
            if (IsCompilerTemp(p.Name))
            {
                // A pooled compiler scratch temp (#Temp<Type><id>) can be referenced from a
                // SIBLING block after the block that lists it has exited: FlattenVariables
                // hoists variables only out of directly-nested child blocks, not out of blocks
                // buried inside a non-block expression (try/finally, loop, conditional), so
                // such a temp is declared here under this block's transient tvs scope. A
                // tvs-scoped IL local is freed on block exit and REUSED by a later sibling's
                // NewTemp of the same type (ILWriter.TempVariable pooling), so a later
                // reference to the temp would then read/write another variable's slot — the
                // indirect corruption behind the `body-:0,0 — Index was outside the bounds of
                // the array.` runtime crash (#1422/#1425). Declare compiler temps as stable
                // function-lifetime locals so their slot is never reused; value semantics are
                // unchanged (a temp is assigned before each use), and every later read/write
                // resolves to this same local. Non-temp block locals keep block scoping.
                variables.Create(p);
                continue;
            }
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
