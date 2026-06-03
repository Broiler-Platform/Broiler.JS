#nullable enable
using System.CodeDom.Compiler;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class YCatchBody(YParameterExpression? parameter, YExpression body)
{
    public readonly YParameterExpression? Parameter = parameter;
    public readonly YExpression Body = body;
}

public class YTryCatchFinallyExpression(
    YExpression @try,
    YCatchBody? @catch,
    YExpression? @finally) : YExpression(YExpressionType.TryCatchFinally, @try.Type)
{
    public readonly YExpression Try = @try;
    public new readonly YCatchBody? Catch = @catch;
    public readonly YExpression? Finally = @finally;

    /// <summary>
    /// When true this try/finally is a synthetic completion-tracking wrapper
    /// (emitted for if/switch/loop statement completion values) whose finally has
    /// no observable ordering relative to a return. Such wrappers must not block
    /// proper tail calls in their body: a tail-call sentinel returned from inside
    /// still runs the finally and is resolved by the caller, so semantics are
    /// preserved. Real try/finally (user code, function stack-pop) leave this
    /// false so tail calls remain blocked.
    /// </summary>
    public bool TailCallTransparent { get; init; }

    public override void Print(IndentedTextWriter writer)
    {
        writer.WriteLine("try {");
        writer.Indent++;
        Try.Print(writer);
        writer.Indent--;
        if (Catch != null)
        {
            if (Catch.Parameter != null) {
                writer.WriteLine($"}} catch({Catch.Parameter.Name}) {{");
            }
            else
            {
                writer.WriteLine("} catch {");
            }
            writer.Indent++;
            Catch.Body.Print(writer);
            writer.Indent--;
        }
        if(Finally != null)
        {
            writer.WriteLine("} finally {");
            writer.Indent++;
            Finally.Print(writer);
            writer.Indent--;
        }
        writer.WriteLine("}");
    }
}