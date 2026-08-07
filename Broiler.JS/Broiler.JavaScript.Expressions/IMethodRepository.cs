using System;
using System.Reflection;
using Broiler.JavaScript.ExpressionCompiler.ClosureSeparator;

namespace Broiler.JavaScript.ExpressionCompiler;

/// <summary>
/// The store of compiled inner-lambda sites a <see cref="Closures"/> instance creates delegates
/// from.
/// </summary>
/// <remarks>
/// Declared here rather than beside its emitting implementation because <see cref="Closures"/>
/// holds one, and <see cref="Closures"/> is part of the expression model. <see cref="RegisterNew"/>
/// takes a <see cref="MethodInfo"/> rather than the <c>DynamicMethod</c> the IL back end actually
/// passes: a derived type satisfies it, and it keeps <c>System.Reflection.Emit</c> out of this
/// assembly, which is the line this split draws.
/// </remarks>
public interface IMethodRepository
{
    object Create(Box[] boxes, ulong id);

    ulong RegisterNew(MethodInfo d, string il, string exp, Type type);
}
