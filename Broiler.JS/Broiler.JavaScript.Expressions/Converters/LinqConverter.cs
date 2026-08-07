using System.Collections.Generic;
using System.Linq.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Converters;


public partial class LinqConverter : LinqMap<BExpression>
{
    private readonly Dictionary<ParameterExpression, BParameterExpression> parameters = [];
    private readonly LabelMap labels = new();

    private Core.IFastEnumerable<BParameterExpression> Register(IList<ParameterExpression> plist)
    {
        var list = new Core.Sequence<BParameterExpression>();
        foreach (var p in plist)
        {
            var t = p.IsByRef && !p.Type.IsByRef ? p.Type.MakeByRefType() : p.Type;
            var yp = BExpression.Parameter(t, p.Name);

            parameters[p] = yp;
            list.Add(yp);
        }

        return list;
    }
}
