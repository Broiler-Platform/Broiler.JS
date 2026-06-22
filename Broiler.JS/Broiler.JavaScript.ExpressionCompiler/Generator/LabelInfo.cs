using System.Collections.Generic;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Generator;

public class LabelInfo(ILWriter il)
{
    private Dictionary<BLabelTarget, ILWriterLabel> labels = [];

    public ILWriterLabel this[BLabelTarget target] => Create(target);



    private ILWriterLabel Create(BLabelTarget target)
    {
        if (labels.TryGetValue(target, out var l))
            return l;
        l = il.DefineLabel(target.Name);
        labels[target] = l;
        return l;
    }

    public ILWriterLabel Create(BLabelTarget target, ILTryBlock tryBlock, bool throwIfFail = true)
    {
        if (labels.TryGetValue(target, out var l))
        {
            if (throwIfFail)
                throw new System.InvalidOperationException();
            return l;
        }
        l = il.DefineLabel(target.Name, tryBlock);
        labels[target] = l;
        return l;
    }

}
