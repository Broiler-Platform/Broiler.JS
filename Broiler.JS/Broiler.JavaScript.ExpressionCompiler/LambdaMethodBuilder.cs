using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.ClosureSeparator;
using Broiler.JavaScript.ExpressionCompiler.Generator;

namespace Broiler.JavaScript.ExpressionCompiler;

public class LambdaMethodBuilder(MethodBuilder builder) : IMethodBuilder
{
    private readonly TypeBuilder typeBuilder = (TypeBuilder)builder.DeclaringType;

    public BExpression Relay(BExpression @this, IFastEnumerable<BExpression> closures, BLambdaExpression innerLambda)
    {
        LambdaRewriter.Rewrite(innerLambda);
        var derived = (typeBuilder.Module as ModuleBuilder).DefineType(
            ExpressionCompiler.GetUniqueName(innerLambda.Name + ":" + innerLambda.Name.Line),
            TypeAttributes.Public,
            typeof(Closures));

        var (m, il, exp) = innerLambda.CompileToInstnaceMethod(derived, false);


        var cnstr = derived.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, [
            typeof(Box[])
        ]);

        var boxes = BExpression.Parameter(typeof(Box[]));

        var cnstrLambda = BExpression.Lambda(innerLambda.Type, "cnstr",
            BExpression.CallNew(Closures.constructor, BExpression.Null, boxes, BExpression.Null, BExpression.Null),
            [BExpression.Parameter(derived), boxes]);

        var cnstrIL = new ILCodeGenerator(cnstr.GetILGenerator(), null);
        cnstrIL.EmitConstructor(cnstrLambda);

        var dt = innerLambda.Type;

        var cdt = dt.GetConstructors().First(x => x.GetParameters().Length == 2);

        var cd = typeof(MethodInfo).GetMethod(nameof(MethodInfo.CreateDelegate), [typeof(Type), typeof(object)]);

        var derivedType = derived.CreateTypeInfo();
        var ct = derivedType.GetConstructors()[0];

        var im = derivedType.GetMethods().First(x => x.Name == m.Name);

        return BExpression.New(cdt, BExpression.New(ct, closures == null ? BExpression.Null : BExpression.NewArray(typeof(Box), closures)), BExpression.Constant(im));

    }
}
