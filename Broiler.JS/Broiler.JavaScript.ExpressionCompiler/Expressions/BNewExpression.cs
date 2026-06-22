using Broiler.JavaScript.ExpressionCompiler.Core;
using System;
using System.CodeDom.Compiler;
using System.Linq;
using System.Reflection;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BNewExpression: BExpression
{
    public readonly ConstructorInfo constructor;
    public readonly IFastEnumerable<BExpression> args;

    /// <summary>
    /// Base class constructors must be called a a 'call' instruction and not 'new'
    /// </summary>
    public readonly bool AsCall;

    public BNewExpression(ConstructorInfo constructor, IFastEnumerable<BExpression> args, bool asCall = false)
        : base(BExpressionType.New, constructor.DeclaringType)
    {
        this.constructor = constructor;
        this.args = args;
        if (args.Any(x => x == null))
            throw new ArgumentNullException();
        AsCall = asCall;
    }

    public BNewExpression Update(ConstructorInfo constructor, IFastEnumerable<BExpression> args) => new(constructor, args, AsCall);

    public override void Print(IndentedTextWriter writer)
    {
        if (AsCall)
        {
            writer.Write($"call {constructor.DeclaringType.GetFriendlyName()}(");
        }
        else
        {
            writer.Write($"new {constructor.DeclaringType.GetFriendlyName()}(");
        }
        writer.PrintCSV(args);
        writer.Write(")");
    }
}