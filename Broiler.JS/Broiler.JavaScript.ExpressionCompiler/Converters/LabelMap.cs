using System.Collections.Generic;
using System.Linq.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Converters;

public class LabelMap
{
    private readonly Dictionary<LabelTarget, BLabelTarget> labels = [];

    public BLabelTarget this[LabelTarget label]
    {
        get
        {
            if (labels.TryGetValue(label, out var r))
                return r;

            r = BExpression.Label(label.Name + labels.Count, label.Type);
            labels[label] = r;
            return r;
        }
    }
}
