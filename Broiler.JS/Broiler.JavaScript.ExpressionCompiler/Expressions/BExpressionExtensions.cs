using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public static class BExpressionExtensions
{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Is<T>(this BExpression exp, BExpressionType type, out T value)
    {
        if(exp.NodeType == type && exp is T texp)
        {
            value = texp;
            return true;
        }
        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsConstant(this BExpression exp, out BConstantExpression c)
        => Is(exp, BExpressionType.Constant, out c);

    public static void PrintCSV<T>(this IndentedTextWriter writer, IEnumerable<T> items)
        where T: BExpression
    {
        bool first = true;
        foreach(var item in items)
        {
            if (!first)
                writer.Write(", ");
            first = false;
            item.Print(writer);
        }
    }

}
