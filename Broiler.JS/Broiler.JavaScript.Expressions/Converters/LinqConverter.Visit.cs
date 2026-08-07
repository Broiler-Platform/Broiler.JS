using System;
using System.Linq.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Converters;


public partial class LinqConverter
{
    private BMemberAssignment Visit(MemberBinding binding)
    {
        return binding switch
        {
            MemberAssignment ma => BExpression.Bind(ma.Member, Visit(ma.Expression)),
            _ => throw new NotSupportedException(),
        };
    }
}
