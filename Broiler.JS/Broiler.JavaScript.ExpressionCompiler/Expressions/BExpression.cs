#nullable enable
using Broiler.JavaScript.ExpressionCompiler.Core;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;


/// <summary>
/// System.Linq.Expressions.Expression is very complex and it allows
/// various complex operations such as += etc.
/// 
/// We need simpler operations to build IL easily without automatically
/// assuming or supporting nullability etc.
/// 
/// Simple IL Generator does not allow += operators etc. It does not 
/// allow Nullable types as well. Expression creator must take care of it.
/// </summary>
public abstract class BExpression(BExpressionType nodeType, Type type)
{
    public readonly BExpressionType NodeType = nodeType;

    public readonly Type Type = type;

    public static BEmptyExpression Empty = new();

    public static BILOffsetExpression ILOffset = new();

    public static BExpression operator +(BExpression left, BExpression right) => Binary(left, BOperator.Add, right);
    public static BExpression operator -(BExpression left, BExpression right) => Binary(left, BOperator.Subtract, right);

    public static BExpression Break(BLabelTarget @break) => new BGoToExpression(@break, null);

    public static BConditionalExpression IfThen(BExpression test, BExpression @true, BExpression? @false = null) => new(test, @true, @false);

    public static BExpression operator +(BExpression left, object right) => Binary(left, BOperator.Add, Constant(right));
    public static BExpression operator -(BExpression left, object right) => Binary(left, BOperator.Subtract, Constant(right));

    public static BExpression operator +(BExpression left, int right) => Binary(left, BOperator.Add, Constant(right));
    public static BExpression operator -(BExpression left, int right) => Binary(left, BOperator.Subtract, Constant(right));


    public static BExpression operator >(BExpression left, BExpression right) => Binary(left, BOperator.Greater, right);
    public static BExpression operator <(BExpression left, BExpression right) => Binary(left, BOperator.Less, right);


    public static BExpression operator >=(BExpression left, object right) => Binary(left, BOperator.GreaterOrEqual, Constant(right));
    public static BExpression operator <=(BExpression left, object right) => Binary(left, BOperator.LessOrEqual, Constant(right));

    public static BExpression Throw(BExpression yNewExpression, Type type) => new BThrowExpression(yNewExpression, type);

    public static BExpression operator >(BExpression left, object right) => Binary(left, BOperator.Greater, Constant(right));
    public static BExpression operator <(BExpression left, object right) => Binary(left, BOperator.Less, Constant(right));


    public static BExpression operator >=(BExpression left, BExpression right) => Binary(left, BOperator.GreaterOrEqual, right);
    public static BExpression operator <=(BExpression left, BExpression right) => Binary(left, BOperator.LessOrEqual, right);

    public abstract void Print(IndentedTextWriter writer);

    public string DebugView => ToString();

    public override string ToString()
    {
        using (var sw = new StringWriter())
        {
            using (var iw = new IndentedTextWriter(sw))
            {
                Print(iw);
                return sw.ToString();
            }
        }
    }

    public static BBinaryExpression Binary(BExpression left, BOperator @operator, BExpression right) => new(left, @operator, right);

    public static BCoalesceExpression Coalesce(BExpression left, BExpression right) => new(left, right);

    /// <summary>
    /// This works in following fashion...
    /// 
    /// var returnValue = if(!target.Member) ? target : target.Call(method, arguments);
    /// 
    /// Here target is not read again, it is only read once and it's value is duplicated.
    /// 
    /// It is equivalent to 
    /// 
    /// var targetValue  = target?.Call(method, arguments);
    /// 
    /// if member is null. For JavaScript, we can introduce null and undefined check using a field or property check
    /// </summary>
    /// <param name="target"></param>
    /// <param name="test"></param>
    /// <param name="testArgs"></param>
    /// <param name="true"></param>
    /// <param name="trueArguments"></param>
    /// <param name="false"></param>
    /// <param name="falseArguments"></param>
    /// <returns></returns>
    public static BCoalesceCallExpression CoalesceCall(
        BExpression target,
        MemberInfo test,
        IFastEnumerable<BExpression> testArgs,
        MethodInfo @true,
        IFastEnumerable<BExpression> trueArguments,
        MethodInfo @false,
        IFastEnumerable<BExpression> falseArguments) => new(
            target,
            test, testArgs,
            @true,
            trueArguments,
            @false,
            falseArguments);



    //public static BCoalesceCallExpression CoalesceCall(
    //    BExpression target,
    //    MemberInfo test,
    //    MethodInfo @true,
    //    IFastEnumerable<BExpression> arguments)
    //{
    //    return new BCoalesceCallExpression(target, test, Sequence<BExpression>.Empty, @true, arguments);
    //}

    //public static BCoalesceCallExpression CoalesceCall(
    //    BExpression target,
    //    MemberInfo test,
    //    IFastEnumerable<BExpression> testArgs,
    //    MethodInfo @true,
    //    IFastEnumerable<BExpression> arguments)
    //{
    //    return new BCoalesceCallExpression(target, test, testArgs, @true, arguments);
    //}

    public static BDebugInfoExpression DebugInfo(in Position start, in Position end) => new(start, end);

    public static BExpression Add(BExpression left, BExpression right) => new BBinaryExpression(left, BOperator.Add, right);

    public static BExpression Subtract(BExpression left, BExpression right) => new BBinaryExpression(left, BOperator.Subtract, right);

    public static BExpression Multiply(BExpression left, BExpression right) => new BBinaryExpression(left, BOperator.Multipley, right);

    public static BExpression Divide(BExpression left, BExpression right) => new BBinaryExpression(left, BOperator.Divide, right);

    public static BExpression Modulo(BExpression left, BExpression right) => new BBinaryExpression(left, BOperator.Mod, right);
    public static BExpression And(BExpression left, BExpression right) => new BBinaryExpression(left, BOperator.BitwiseAnd, right);
    public static BExpression ExclusiveOr(BExpression left, BExpression right) => new BBinaryExpression(left, BOperator.Xor, right);

    public static BExpression LeftShift(BExpression left, BExpression right) => new BBinaryExpression(left, BOperator.LeftShift, right);
    public static BExpression RightShift(BExpression left, BExpression right) => new BBinaryExpression(left, BOperator.RightShift, right);

    public static BExpression UnsignedRightShift(BExpression left, BExpression right) => new BBinaryExpression(left, BOperator.UnsignedRightShift, right);

    public static BExpression Power(BExpression left, BExpression right)
    {
        //return new BBinaryExpression(left, BOperator.Power, right);
        var m = typeof(Math).GetMethod(nameof(Math.Pow));
        // return BExpression.Binary(Visit(node.Left), BOperator.Power, Visit(node.Right));

        left = left.Type == typeof(double) ? left : Convert(left, typeof(double));
        right = right.Type == typeof(double) ? right : Convert(right, typeof(double));
        return Call(null, m, left, right);
    }

    public static BBoxExpression Box(BExpression target) => new(target);

    public static BInt32ConstantExpression Constant(int value) => BInt32ConstantExpression.For(value);

    public static BUInt32ConstantExpression Constant(uint value) => BUInt32ConstantExpression.For(value);

    public static BInt64ConstantExpression Constant(long value) => new(value);

    public static BUInt64ConstantExpression Constant(ulong value) => new(value);


    public static BBooleanConstantExpression Constant(bool value) => value 
        ? BBooleanConstantExpression.True
        : BBooleanConstantExpression.False;

    public static BExpression Constant(string value) => value == null
        ? new BConstantExpression(null, typeof(string))
        : new BStringConstantExpression(value);

    public static BDoubleConstantExpression Constant(double value) => new(value);

    public static BFloatConstantExpression Constant(float value) => new(value);

    public static BByteConstantExpression Constant(byte value) => new(value);

    public static BTypeConstantExpression Constant(Type value) => new(value);

    public static BMethodConstantExpression Constant(MethodInfo value) => new(value);

    public static BInt32ConstantExpression Constant(Enum value) => new(System.Convert.ToInt32(value));

    public static BExpression Constant(object value, Type? type = null)
    {
        if (value is BConstantExpression)
            throw new NotSupportedException();
        if (value is string @string)
            return new BStringConstantExpression(@string);
        return new BConstantExpression(value, type ?? value?.GetType() ?? typeof(object));
    }

    public static BExpression MakeIndex(BExpression target, PropertyInfo index, params BExpression[] args) => Index(target, index, args);

    public static BConditionalExpression Conditional(
        BExpression test,
        BExpression @true,
        BExpression @false,
        Type? type = null) => new(test, @true, @false, type);

    public static BAssignExpression Assign(BExpression left, BExpression right, Type? type = null) => new(left, right, type);

    public static BParameterExpression Parameter(Type type, string? name = null) => new(type, name);

    public static BParameterExpression[] Parameters(params Type[] types)
    {
        var pl = new BParameterExpression[types.Length];
        for (int i = 0; i < types.Length; i++)
        {
            pl[i] = new BParameterExpression(types[i], null);
        }
        return pl;
    }

    public static BMemberInitExpression MemberInit(
        BNewExpression exp,
        IFastEnumerable<BBinding> list) => new(exp, list);


    public static BMemberInitExpression MemberInit(
        BNewExpression exp,
        IEnumerable<BBinding> list) => new(exp, list.AsSequence());

    public static BMemberAssignment Bind(MemberInfo field, BExpression value) => new(field, value);

    public static BMemberInitExpression MemberInit(
        BNewExpression exp,
        params BBinding[] list) => new(exp, list.AsSequence());

    //public static BMemberAssignment Bind(MemberInfo field, BExpression value)
    //{
    //    return new BMemberAssignment(field, value);
    //}

    public static BBlockExpression Block(
        IFastEnumerable<BParameterExpression>? variables,
        params BExpression[] expressions) => new(variables, expressions.AsSequence());

    public static BBlockExpression Block(
        IFastEnumerable<BParameterExpression>? variables,
        IFastEnumerable<BExpression> expressions) => new(variables, expressions);

    public static BExpression Block(IFastEnumerable<BExpression> expressions)
    {
        if (expressions.Count == 0)
            return Empty;

        if (expressions.Count == 1)
            return expressions.First();

        return new BBlockExpression(null, expressions);
    }

    public static BBlockExpression Block(params BExpression[] expressions) => new(null, expressions.AsSequence());


    public static BExpression Convert(BExpression exp, Type type, bool cast = false)
    {
        if (BConvertExpression.TryGetConversionMethod(exp.Type, type, out var method))
        {
            if (method == null)
                return new BTypeAsExpression(exp, type);
            return new BConvertExpression(exp, type, method);
        }
        if (exp.Type.IsValueType && type == typeof(object))
            return Box(exp);
        return new BTypeAsExpression(exp, type);
    }

    //public static BConvertExpression Convert(BExpression exp, Type type, MethodInfo method)
    //{
    //    return new BConvertExpression(exp, type, method);
    //}

    public static BExpression Continue(BLabelTarget @break) => new BGoToExpression(@break, null);

    public static BDelegateExpression Delegate(MethodInfo method, Type? type = null) => new(method, type);


    public static BBinaryExpression Equal(BExpression left, BExpression right)
         => Binary(left, BOperator.Equal, right);

    internal static BNewExpression CallNew(
        ConstructorInfo constructor, params BExpression[] args) => new(constructor, args.AsSequence(), true);

    public static BBinaryExpression Or(BExpression left, BExpression right)
        => Binary(left, BOperator.BitwiseOr, right);

    public static BBinaryExpression OrElse(BExpression left, BExpression right)
        => Binary(left, BOperator.BooleanOr, right);

    public static BBinaryExpression NotEqual(BExpression left, BExpression right)
         => Binary(left, BOperator.NotEqual, right);

    public static BBinaryExpression Greater(BExpression left, BExpression right)
         => Binary(left, BOperator.Greater, right);


    public static BJumpSwitchExpression JumpSwitch(BExpression target, IFastEnumerable<BLabelTarget> cases)
        => new(target, cases);

    public static BLambdaExpression Lambda(
        Type type,
        BExpression body,
        in FunctionName name, BParameterExpression[] parameters) => new(type, name, body, null, parameters);

    public static BBinaryExpression GreaterOrEqual(BExpression left, BExpression right)
         => Binary(left, BOperator.GreaterOrEqual, right);

    public static BBinaryExpression Less(BExpression left, BExpression right)
         => Binary(left, BOperator.Less, right);

    public static BBinaryExpression LessOrEqual(BExpression left, BExpression right)
         => Binary(left, BOperator.LessOrEqual, right);

    public static BCallExpression Call(BExpression? target, MethodInfo method, IFastEnumerable<BExpression> args) => new(target, method, args);


    public static BCallExpression Call(BExpression? target, MethodInfo method, IEnumerable<BExpression> args) => new(target, method, args.AsSequence());
    public static BCallExpression Call(BExpression? target, MethodInfo method, params BExpression[] args) => new(target, method, args.AsSequence());

    public static BNewExpression New(ConstructorInfo constructor, IFastEnumerable<BExpression> args) => new(constructor, args);


    public static BNewExpression New(ConstructorInfo constructor, IEnumerable<BExpression> args) => new(constructor, args.AsSequence());
    public static BNewExpression New(Type type, params BExpression[] args)
    {
        var constructor = type.GetConstructor(args.Select(x => x.Type).ToArray());
        return new BNewExpression(constructor, args.AsSequence());
    }
    public static BNewExpression New(ConstructorInfo constructor, params BExpression[] args) => New(constructor, (IList<BExpression>)args);

    public static BFieldExpression Field(BExpression target, FieldInfo field) => new(target, field);

    public static BFieldExpression Field(BExpression target, string name)
    {
        var field = target.Type.GetUnderlyingTypeIfRef().GetField(name);

        return new BFieldExpression(target, field);
    }

    public static BInvokeExpression Invoke(BExpression target, IFastEnumerable<BExpression> args)
    {
        var t = target.Type;
        var type = t.GetMethod("Invoke").ReturnType;
        return new BInvokeExpression(target, args, type);
    }


    public static BInvokeExpression Invoke(BExpression target, params BExpression[] args)
    {
        var t = target.Type;
        var type = t.GetMethod("Invoke").ReturnType;
        return new BInvokeExpression(target, args.AsSequence(), type);
    }

    public static BParameterExpression Variable(Type type, string? name = null) => new(type, name);

    public static BPropertyExpression Property(BExpression target, PropertyInfo field) => new(target, field);

    public static BNewArrayExpression NewArray(Type type, IFastEnumerable<BExpression> elements) => new(type, elements);


    public static BNewArrayExpression NewArray(Type type, params BExpression[] elements) => new(type, elements.AsSequence());

    public static BNewArrayExpression NewArrayInit(Type type, IEnumerable<BExpression> elements) => new(type, elements.AsSequence());


    public static BNewArrayExpression NewArrayInit(Type type, IFastEnumerable<BExpression> elements) => new(type, elements);

    public static BNewArrayBoundsExpression NewArrayBounds(Type type, BExpression size) => new(type, size);

    public static BMemberElementInit ListBind(MemberInfo member, BElementInit[] elements) => new(member, elements);

    public static BExpression ListInit(BNewExpression newExp, IFastEnumerable<BElementInit> elements)
        => new BListInitExpression(newExp, elements);

    public static BExpression ListInit(BNewExpression newExp, IEnumerable<BElementInit> elements)
        => new BListInitExpression(newExp, elements.AsSequence());

    [Obsolete("Use Sequence<T>")]
    public static BExpression ListInit(BNewExpression newExp, BElementInit[] elements)
        => new BListInitExpression(newExp, elements.AsSequence());

    public static BElementInit ElementInit(MethodInfo addMethod, params BExpression[] arguments)
        => new(addMethod, arguments);

    public static BLabelTarget Label(Type type, string? name = null) => new(name, type ?? typeof(void));

    public static BLabelTarget Label(string? name = null,
        Type? type = null) => new(name, type ?? typeof(void));

    public static BLabelExpression Label(BLabelTarget target, BExpression? defaultValue = null) => new(target, defaultValue);

    public static BExpression Condition(BExpression yExpression, BExpression def, BExpression target, Type? type = null) => Conditional(yExpression, def, target, type);

    public static BConstantExpression Null = new(null, typeof(object));

    public static BGoToExpression Goto(BLabelTarget target, BExpression? defaultValue = null) => new(target, defaultValue);

    public static BGoToExpression GoTo(BLabelTarget target, BExpression? defaultValue = null) => new(target, defaultValue);

    public static BReturnExpression Return(BLabelTarget target, BExpression? defaultValue = null) => new(target, defaultValue);

    public static BLoopExpression Loop(BExpression body, BLabelTarget @break, BLabelTarget? @continue = null) => new(body, @break, @continue ?? Label("continue", @break.LabelType));

    public static BExpression<T> Lambda<T>(in FunctionName name, BExpression body, params BParameterExpression[] parameters) => new(name, body, null, parameters, null);

    public static BExpression<T> InstanceLambda<T>(
        in FunctionName name,
        BExpression body,
        BParameterExpression @this,
        BParameterExpression[] parameters) => new(name, body, @this, parameters, null);


    public static BLambdaExpression Lambda(
        Type delegateType,
        in FunctionName name,
        BExpression body,
        BParameterExpression[] parameters) => new(delegateType, name, body, null, parameters, body.Type);

    public static BTypeAsExpression TypeAs(BExpression target, Type type) => new(target, type);

    public static BTypeIsExpression TypeIs(BExpression target, Type type) => new(target, type);

    public static BUnboxExpression Unbox(BExpression target, Type type) => new(target, type);

    public static BCatchBody Catch(BParameterExpression parameter, BExpression body) => new(parameter, body);
    public static BCatchBody Catch(BExpression body) => new(null, body);


    public static BTryCatchFinallyExpression TryCatch(
        BExpression @try,
        BCatchBody @catch) => new(@try, @catch, null);

    public static BTryCatchFinallyExpression TryFinally(
        BExpression @try,
        BExpression @finally) => new(@try, null, @finally);

    /// <summary>
    /// Creates a synthetic completion-tracking try/finally that does not block
    /// proper tail calls in its body. See <see cref="BTryCatchFinallyExpression.TailCallTransparent"/>.
    /// </summary>
    public static BTryCatchFinallyExpression TailCallTransparentTryFinally(
        BExpression @try,
        BExpression @finally) => new(@try, null, @finally) { TailCallTransparent = true };

    public static BTryCatchFinallyExpression TryCatchFinally(
        BExpression @try,
        BExpression @finally,
        BCatchBody? catchBody)
    {
        if (catchBody == null && @finally == null)
            throw new ArgumentNullException($"Both finally and catch cannot be null");
        return new BTryCatchFinallyExpression(@try, catchBody, @finally);
    }

    public static BTryCatchFinallyExpression TryCatchFinally(
        BExpression @try,
        BCatchBody? catchBody,
        BExpression? @finally = null)
    {
        if (catchBody == null && @finally == null)
            throw new ArgumentNullException($"Both finally and catch cannot be null");
        return new BTryCatchFinallyExpression(@try, catchBody, @finally);
    }

    public static BArrayIndexExpression ArrayIndex(BExpression target, BExpression index) => new(target, index);

    public static BArrayLengthExpression ArrayLength(BExpression target) => new(target);

    public static BIndexExpression Index(BExpression target, PropertyInfo propertyInfo, IFastEnumerable<BExpression> args) => new(target, propertyInfo, args);


    public static BIndexExpression Index(BExpression target, PropertyInfo propertyInfo, params BExpression[] args) => new(target, propertyInfo, args.AsSequence());

    public static BIndexExpression Index(BExpression target, IFastEnumerable<BExpression> args)
    {
        var types = args.Select(x => x.Type).ToArray();
        PropertyInfo propertyInfo =
            target.Type.GetType()
                .GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.GetProperty)
                .FirstOrDefault(x => x.GetIndexParameters().Select(p => p.ParameterType).SequenceEqual(types));
        if (propertyInfo == null)
        {
            throw new NotSupportedException($"Index[{string.Join(",", types.Select(n => n.Name))}] not found on {target.Type.GetFriendlyName()}");
        }
        return new BIndexExpression(target, propertyInfo, args);
    }

    public static BIndexExpression Index(BExpression target, params BExpression[] args)
    {
        var types = args.Select(x => x.Type).ToArray();
        PropertyInfo propertyInfo =
            target.Type.GetType()
                .GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.GetProperty)
                .FirstOrDefault(x => x.GetIndexParameters().Select(p => p.ParameterType).SequenceEqual(types));
        if (propertyInfo == null)
        {
            throw new NotSupportedException($"Index[{string.Join(",", types.Select(n => n.Name))}] not found on {target.Type.GetFriendlyName()}");
        }
        return new BIndexExpression(target, propertyInfo, args.AsSequence());
    }

    public static BUnaryExpression Not(BExpression exp) => new(exp, BUnaryOperator.Not);

    public static BUnaryExpression Negative(BExpression exp) => new(exp, BUnaryOperator.Negative);

    public static BUnaryExpression OnesComplement(BExpression exp) => new(exp, BUnaryOperator.OnesComplement);
    public static BUnaryExpression Negate(BExpression exp) => new(exp, BUnaryOperator.Negative);
    public static BExpression UnaryPlus(BExpression exp) => exp;
    public static BTypeIsExpression TypeEqual(BExpression exp, Type type) => new(exp, type);

    public static BThrowExpression Throw(BExpression exp) => new(exp);
    internal static BLambdaExpression InlineLambda(
        Type delegateType,
        in FunctionName name,
        BExpression body,
        BParameterExpression @this,
        BParameterExpression[] parameters,
        BExpression? repository,
        Type returnType) => new(delegateType, name, body, @this, parameters, returnType, repository);

    internal static BLambdaExpression InlineLambda(
        Type delegateType,
        in FunctionName name,
        BExpression body,
        List<BParameterExpression> parameters,
        BExpression? repository) => new(delegateType, name, body, null, parameters.ToArray(), null, repository);

    //internal static BRelayExpression Relay(
    //    IFastEnumerable<BExpression> box,
    //    BLambdaExpression inner)
    //{
    //    return new BRelayExpression(box, inner);
    //}

    public static BSwitchExpression Switch(
        BExpression target,
        params BSwitchCaseExpression[] cases) => new(target, null, null, cases);

    public static BSwitchExpression Switch(
        BExpression target,
        BExpression? defaultBody,
        params BSwitchCaseExpression[] cases) => new(target, null, defaultBody, cases);

    public static BSwitchExpression Switch(
        BExpression target,
        BExpression? defaultBody,
        MethodInfo method,
        IEnumerable<BSwitchCaseExpression> cases) => new(target, method, defaultBody, cases.ToArray());


    public static BSwitchCaseExpression SwitchCase(BExpression body, params BExpression[] testValues) => new(body, testValues);

    public static BSwitchCaseExpression SwitchCase(BExpression body, IEnumerable<BExpression> testValues) => new(body, testValues.ToArray());


    public static BYieldExpression Yield(BExpression arg, bool @delegate = false) => new(arg, @delegate);

    public static BYieldExpression Await(BExpression arg) => new(arg, false, isAwait: true);
}
