using Broiler.JavaScript.ExpressionCompiler.Core;
using System.Collections.Generic;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BBlockBuilder
{

    private Sequence<BExpression> expressions = [];
    private Sequence<BParameterExpression> variables = [];

    public BBlockBuilder()
    {

    }

    public void AddVariable(BParameterExpression pe) => variables.Add(pe);

    public Sequence<BExpression> ConvertToVariables(IFastEnumerable<BExpression> inputs, BExpressionMapVisitor visitor)
    {
        var newInputs = new Sequence<BExpression>(inputs.Count);
        var en = inputs.GetFastEnumerator();
        while(en.MoveNext(out var input))
        {
            newInputs.Add(ConvertToVariable(visitor.Visit(input)));
        }
        return newInputs;
    }


    public BExpression ConvertToVariable(BExpression init)
    {
        if (init.NodeType == BExpressionType.Parameter)
            return init;
        BParameterExpression pe;
        // break init if it is block..
        if (init.NodeType == BExpressionType.Block)
        {
            var block = init as BBlockExpression;
            variables.AddRange(block.FlattenVariables);
            foreach (var (e, last) in block.FlattenExpressions)
            {
                if (last)
                {
                    if (e.NodeType == BExpressionType.Parameter)
                    {
                        AddExpression(e);
                        return e as BParameterExpression;
                    }
                    pe = BExpression.Parameter(e.Type);
                    variables.Add(pe);
                    AddExpression(BExpression.Assign(pe, e));
                    return pe;
                }
                AddExpression(e);
            }
        }
        pe = BExpression.Parameter(init.Type);
        variables.Add(pe);
        AddExpression(BExpression.Assign(pe, init));
        return pe;
    }

    public BBlockBuilder AddExpressionRange(IEnumerable<BExpression> exps)
    {
        foreach (var e in exps)
            AddExpression(e);
        return this;
    }


    public BBlockBuilder AddExpression(BExpression exp)
    {
        switch(exp.NodeType)
        {
            case BExpressionType.Block:
                var block = (exp as BBlockExpression)!;
                variables.AddRange(block.Variables);
                {
                    var en = block.Expressions.GetFastEnumerator();
                    while(en.MoveNext(out var e))
                    {
                        AddExpression(e);
                    }
                }
                return this;
            case BExpressionType.Return:
                var @return = (exp as BReturnExpression)!;
                if(@return.Default?.NodeType == BExpressionType.Block)
                {
                    block = (@return.Default as BBlockExpression)!;
                    var en = block.Enumerate();
                    while(en.MoveNext(out var e, out var isLast))
                    {
                        if (isLast)
                        {
                            return AddExpression(@return.Update(@return.Target, e));
                        }
                        AddExpression(e);
                    }
                    return this;
                }
                break;
        }
        expressions.Add(exp);
        return this;
    }

    public BExpression Build()
    {
        if (expressions.Count == 0)
            return BExpression.Empty;

        if (variables.Count == 0 && expressions.Count == 1)
            return expressions.First();

        return new BBlockExpression(variables, expressions);
    }

}
