using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Generator;

public partial class ILCodeGenerator
{
    protected override CodeInfo VisitBooleanConstant(BBooleanConstantExpression node)
    {
        il.EmitConstant(node.Value);
        return true;
    }

    protected override CodeInfo VisitDoubleConstant(BDoubleConstantExpression node)
    {
        il.EmitConstant(node.Value);
        return true;
    }

    protected override CodeInfo VisitFloatConstant(BFloatConstantExpression node)
    {
        il.EmitConstant(node.Value);
        return true;
    }

    protected override CodeInfo VisitInt32Constant(BInt32ConstantExpression node)
    {
        il.EmitConstant(node.Value);
        return true;
    }

    protected override CodeInfo VisitInt64Constant(BInt64ConstantExpression node)
    {
        il.EmitConstant(node.Value);
        return true;
    }

    protected override CodeInfo VisitStringConstant(BStringConstantExpression node)
    {
        il.EmitConstant(node.Value);
        return true;
    }

    protected override CodeInfo VisitUInt32Constant(BUInt32ConstantExpression node)
    {
        il.EmitConstant(node.Value);
        return true;
    }

    protected override CodeInfo VisitUInt64Constant(BUInt64ConstantExpression node)
    {
        il.EmitConstant(node.Value);
        return true;
    }

    protected override CodeInfo VisitByteConstant(BByteConstantExpression node)
    {
        il.EmitConstant(node.Value);
        return true;
    }

    protected override CodeInfo VisitTypeConstant(BTypeConstantExpression node)
    {
        il.EmitConstant(node.Value);
        return true;
    }

    protected override CodeInfo VisitMethodConstant(BMethodConstantExpression node)
    {
        il.EmitConstant(node.Value);
        return true;
    }
}
