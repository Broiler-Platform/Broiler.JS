#nullable enable
using Broiler.JavaScript.ExpressionCompiler.Core;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BBlockExpression: BExpression
{
    public readonly IFastEnumerable<BParameterExpression> Variables;
    public readonly IFastEnumerable<BExpression> Expressions;

    public BBlockExpression(IFastEnumerable<BParameterExpression>? variables,
        IFastEnumerable<BExpression> expressions)
        :base(BExpressionType.Block, expressions.Last().Type)
    {
        Variables = variables ?? Sequence<BParameterExpression>.Empty;
        if (Variables.Any(v => v == null))
            throw new ArgumentNullException();
        Expressions = expressions;
    }

    public override void Print(IndentedTextWriter writer)
    {
        writer.WriteLine("{");
        writer.Indent++;
        {
            var en = Variables.GetFastEnumerator();
            while(en.MoveNext(out var v))
                writer.WriteLine($"{v.Type.GetFriendlyName()} {v.Name};");
        }
        {
            var en = Expressions.GetFastEnumerator();
            while(en.MoveNext(out var exp))
            {
                exp.Print(writer);
                writer.WriteLine(";");
            }
        }
        writer.Indent--;
        writer.WriteLine("}");
    }

    public IEnumerable<BParameterExpression> FlattenVariables
    {
        get
        {
            var ve = Variables.GetFastEnumerator();
            while(ve.MoveNext(out var v))
                yield return v;
            var ee = Expressions.GetFastEnumerator();
            while(ee.MoveNext(out var s))
            {
                if(s.NodeType == BExpressionType.Block && s is BBlockExpression b)
                {
                    foreach (var v in b.FlattenVariables)
                        yield return v;
                }
            }
        }
    }

    public IEnumerable<(BExpression expression, bool isLast)> FlattenExpressions
    {
        get
        {
            var l = Expressions.Count - 1;
            var en = Expressions.GetFastEnumerator();
            while (en.MoveNext(out var e, out var i))
            {
                bool last = i == l;
                // var e = Expressions[i];
                if (e.NodeType == BExpressionType.Block && e is BBlockExpression b) {
                    foreach (var (item, isLast) in b.FlattenExpressions)
                        yield return (item, isLast && last);
                    continue;
                }

                yield return (e, last);
            }
        }
    }

    public Enumerator Enumerate() => new(Expressions);

    public ref struct Enumerator(IFastEnumerable<BExpression> expressions)
    {
        private IFastEnumerator<BExpression> expressions = expressions.GetFastEnumerator();
        private int last = expressions.Count - 1;

        public readonly bool MoveNext(out BExpression? exp, out bool isLast)
        {
            if(expressions.MoveNext(out exp, out var index))
            {
                isLast = index == last;
                return true;
            }

            isLast = false;
            exp = default;
            return false;
        }
    }
}