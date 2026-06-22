using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BYieldExpression(BExpression arg, bool @delegate, bool isAwait = false) : BExpression(BExpressionType.Yield, arg.Type)
{
    public readonly BExpression Argument = arg;
    public readonly bool DelegateYield = @delegate;

    // True when this suspension is an `await` (explicit, or the per-iteration await
    // of `for await`) rather than a user `yield`. In a plain async function every
    // suspension is an await; in a sync generator every suspension is a user yield;
    // only an async *generator* contains both, and its driver must resume internally
    // for awaits while surfacing user yields to the consumer.
    public readonly bool IsAwait = isAwait;

    public override void Print(IndentedTextWriter writer)
    {
        writer.Write("yield ");
        if (DelegateYield)
        {
            writer.Write("*");
        }
        Argument.Print(writer);
    }
}