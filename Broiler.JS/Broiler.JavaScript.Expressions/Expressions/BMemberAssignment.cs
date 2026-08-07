using Broiler.JavaScript.ExpressionCompiler.Core;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public enum BindingType
{
    MemberAssignment,
    MemberListInit,
    ElementInit
}

public class BBinding(MemberInfo member, BindingType bindingType)
{
    public readonly MemberInfo Member = member;
    public readonly BindingType BindingType = bindingType;
}

public class BElementInit: BBinding
{
    public readonly MethodInfo AddMethod;
    public readonly IFastEnumerable<BExpression> Arguments;

    public BElementInit(MethodInfo addMethod, IFastEnumerable<BExpression> arguments)
        : base(addMethod, BindingType.ElementInit)
    {
        AddMethod = addMethod;
        Arguments = arguments;
    }


    public BElementInit(MethodInfo addMethod, params BExpression[] arguments)
        : base(addMethod, BindingType.ElementInit)
    {
        AddMethod = addMethod;
        Arguments = arguments.AsSequence();
    }
}

public class BMemberElementInit : BBinding
{
    public readonly BElementInit[] Elements;

    public BMemberElementInit(MemberInfo member, IEnumerable<BElementInit> inits)
        : base(member, BindingType.MemberListInit) => Elements = inits.ToArray();


    public BMemberElementInit(MemberInfo member, BElementInit[] inits)
        : base(member, BindingType.MemberListInit) => Elements = inits;
}

public class BMemberAssignment(MemberInfo field, BExpression value) : BBinding(field, BindingType.MemberAssignment)
{
    public BExpression Value = value;
}