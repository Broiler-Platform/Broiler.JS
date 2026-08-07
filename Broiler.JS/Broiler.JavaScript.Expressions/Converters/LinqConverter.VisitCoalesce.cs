using System;
using System.Linq.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Converters;


public partial class LinqConverter
{
    protected override BExpression VisitCoalesce(BinaryExpression node)
    {
        if (node.Method != null)
            throw new NotSupportedException();

        return BExpression.Coalesce(Visit(node.Left), Visit(node.Right));
    }
}
