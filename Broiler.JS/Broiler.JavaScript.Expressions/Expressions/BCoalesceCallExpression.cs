using Broiler.JavaScript.ExpressionCompiler.Core;
using System.CodeDom.Compiler;
using System.Reflection;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BCoalesceCallExpression(
    BExpression target,
    MemberInfo test,
    IFastEnumerable<BExpression> testArguments,
    MethodInfo @true,
    IFastEnumerable<BExpression> trueArguments,
    MethodInfo @false,
    IFastEnumerable<BExpression> falseArguments
    ) : BExpression(BExpressionType.CoalesceCall, @true?.ReturnType ?? @false.ReturnType)
{
    public readonly BExpression Target = target;
    public readonly MemberInfo Test = test;
    public readonly IFastEnumerable<BExpression> TestArguments = testArguments;
    public readonly MethodInfo True = @true;
    public readonly IFastEnumerable<BExpression> TrueArguments = trueArguments;
    public readonly MethodInfo False = @false;
    public readonly IFastEnumerable<BExpression> FalseArguments = falseArguments;

    public override void Print(IndentedTextWriter writer)
    {
        
    }
}
