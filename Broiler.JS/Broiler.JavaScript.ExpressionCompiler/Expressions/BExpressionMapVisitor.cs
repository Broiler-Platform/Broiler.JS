using Broiler.JavaScript.ExpressionCompiler.Core;
using System;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;

public class BExpressionMapVisitor : BExpressionVisitor<BExpression>
{


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Modified<T>(T node, out T r)
        where T : BExpression
    {
        r = Visit(node) as T;
        return r != node;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Modified<T1, T2>(T1 node1, T2 node2, out T1 r1, out T2 r2)
        where T1 : BExpression
        where T2 : BExpression
    {
        r1 = Visit(node1) as T1;
        r2 = Visit(node2) as T2;
        return r1 != node1 || r2 != node2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Modified<T1, T2, T3>(T1 node1, T2 node2, T3 node3, out T1 r1, out T2 r2, out T3 r3)
        where T1 : BExpression
        where T2 : BExpression
        where T3 : BExpression
    {
        r1 = Visit(node1) as T1;
        r2 = Visit(node2) as T2;
        r3 = Visit(node3) as T3;
        return r1 != node1 || r2 != node2 || r3 != node3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Modified<T1, T2, T3, T4>(T1 node1, T2 node2, T3 node3, T4 node4, out T1 r1, out T2 r2, out T3 r3, out T4 r4)
        where T1 : BExpression
        where T2 : BExpression
        where T3 : BExpression
        where T4 : BExpression
    {
        r1 = Visit(node1) as T1;
        r2 = Visit(node2) as T2;
        r3 = Visit(node3) as T3;
        r4 = Visit(node4) as T4;
        return r1 != node1 || r2 != node2 || r3 != node3 || r4 != node4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Modified<T>(IFastEnumerable<T> statements, out IFastEnumerable<T> list)
        where T : BExpression
    {
        list = statements;
        if (statements.Count == 0)
        {
            return false;
        }
        // we will create new sequence only if any expression has been modified
        // this will prevent allocations
        Sequence<T> r = null;
        var en = statements.GetFastEnumerator();
        while(en.MoveNext(out var item))
        {
            var visited = Visit(item) as T ?? throw new ArgumentNullException();
            if (visited == item)
            {
                r?.Add(item);
                continue;
            }
            if (r == null)
            {
                r = new Sequence<T>(statements.Count);
                var ec = statements.GetFastEnumerator();
                while(ec.MoveNext(out var previous))
                {
                    if (previous == item)
                        break;
                    r.Add(previous);
                }
            }
            r.Add(visited);
        }
        if (r == null)
        {
            return false;
        }
        list = r;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Modified<T>(IFastEnumerable<T> statements, Func<T, T> visitor, out IFastEnumerable<T> list)
    {
        list = statements;
        if (statements.Count == 0)
        {
            return false;
        }
        // we will create new sequence only if any expression has been modified
        // this will prevent allocations
        Sequence<T> r = null;
        var en = statements.GetFastEnumerator();
        while (en.MoveNext(out var item, out var index))
        {
            var visitedItem = visitor(item);
            if (visitedItem.Equals(item))
            {
                r?.Add(item);
                continue;
            }
            if (r == null)
            {
                r = new Sequence<T>(statements.Count);
                var ec = statements.GetFastEnumerator();
                while (ec.MoveNext(out var previous, out var i))
                {
                    if (index == i)
                        break;
                    r.Add(previous);
                }
            }
            r.Add(visitedItem);
        }
        if (r == null)
        {
            return false;
        }
        list = r;
        return true;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Modified<T>(T[] statements, out T[] list)
        where T : BExpression
    {
        list = statements;
        if (statements.Length == 0)
        {
            return false;
        }
        T[] r = null;
        for (int i = 0; i < statements.Length; i++)
        {
            ref var item = ref statements[i];
            var visited = Visit(item) as T ?? throw new ArgumentNullException();
            if (visited == item)
            {
                if(r != null)
                {
                    r[i] = visited;
                }
                continue;
            }
            if(r == null)
            {
                r = new T[statements.Length];
                for (int j = 0; j < i; j++)
                {
                    r[j] = statements[j];
                }
            }
            r[i] = visited;
        }
        if (r == null)
        {
            return false;
        }
        list = r;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Modified<T>(T[] statements, Func<T, T> visitor, out T[] list)
    {
        list = statements;
        if (statements.Length == 0)
        {
            return false;
        }
        T[] r = null;
        for (int i = 0; i < statements.Length; i++)
        {
            ref var item = ref statements[i];
            var visited = visitor(item);
            if (visited.Equals(item))
            {
                if(r != null)
                {
                    r[i] = visited;
                }
                continue;
            }
            if (r == null)
            {
                r = new T[statements.Length];
                for (int j = 0; j < i; j++)
                {
                    r[j] = statements[j];
                }
            }
            r[i] = visited;
        }
        if (r == null)
        {
            return false;
        }
        list = r;
        return true;
    }

    protected override BExpression VisitArrayLength(BArrayLengthExpression arrayLengthExpression)
    {
        if (Modified(arrayLengthExpression.Target, out var target))
            return new BArrayLengthExpression(target);
        return arrayLengthExpression;
    }
    protected override BExpression VisitBinary(BBinaryExpression yBinaryExpression)
    {
        if (Modified(yBinaryExpression.Left, yBinaryExpression.Right, out var left, out var right))
            return new BBinaryExpression(left, yBinaryExpression.Operator, right);
        return yBinaryExpression;
    }

    protected override BExpression VisitBlock(BBlockExpression yBlockExpression)
    {
        // var pm = Modified(yBlockExpression.Variables, out var variables);
        var sm = Modified(yBlockExpression.Expressions, out var expressions);
        if (sm)
            return new BBlockExpression(yBlockExpression.Variables, expressions);
        return yBlockExpression;
    }

    protected override BExpression VisitBox(BBoxExpression node)
    {
        if (Modified(node.Target, out var target))
            return new BBoxExpression(target);
        return node;
    }

    protected override BExpression VisitCall(BCallExpression yCallExpression)
    {
        var tm = Modified(yCallExpression.Target, out var target);
        var am = Modified(yCallExpression.Arguments, out var arguments);
        if (tm || am)
            return new BCallExpression(target, yCallExpression.Method, arguments);
        return yCallExpression;
    }

    protected override BExpression VisitConditional(BConditionalExpression yConditionalExpression)
    {
        if (Modified(
            yConditionalExpression.test, yConditionalExpression.@true, yConditionalExpression.@false,
            out var test, out var @true, out var @false))
            return new BConditionalExpression(test, @true, @false);
        return yConditionalExpression;
    }

    protected override BExpression VisitConvert(BConvertExpression convertExpression)
    {
        if (Modified(convertExpression.Target, out var target))
            return BExpression.Convert(target, convertExpression.Type);
        return convertExpression;
    }

    protected override BExpression VisitCoalesce(BCoalesceExpression yCoalesceExpression)
    {
        if (Modified(yCoalesceExpression.Left, yCoalesceExpression.Right, out var left, out var right))
            return new BCoalesceExpression(left, right);
        return yCoalesceExpression;
    }

    protected override BExpression VisitCoalesceCall(BCoalesceCallExpression node)
    {
        var tm = Modified(node.Target, out var target);
        var testArgsM = Modified(node.TestArguments, out var testArgs);
        var trueArgsM = Modified(node.TrueArguments, out var trueArgs);
        var falseArgsM = Modified(node.FalseArguments, out var falseArgs);
        if (tm || testArgsM || trueArgsM || falseArgsM)
            return new BCoalesceCallExpression(target,
                node.Test,
                testArgs,
                node.True,
                trueArgs,
                node.False,
                falseArgs);
        return node;
    }

    protected override BExpression VisitConstant(BConstantExpression yConstantExpression) => yConstantExpression;

    protected override BExpression VisitDebugInfo(BDebugInfoExpression node) => node;

    protected override BExpression VisitDelegate(BDelegateExpression yDelegateExpression) => yDelegateExpression;

    protected override BExpression VisitEmpty(BEmptyExpression exp) => exp;

    protected override BExpression VisitField(BFieldExpression yFieldExpression)
    {
        if (Modified(yFieldExpression.Target, out var target))
            return new BFieldExpression(target, yFieldExpression.FieldInfo);
        return yFieldExpression;
    }

    protected override BExpression VisitGoto(BGoToExpression yGoToExpression)
    {
        if (Modified(yGoToExpression.Default, out var @default))
            return new BGoToExpression(yGoToExpression.Target, @default);
        return yGoToExpression;
    }

    protected override BExpression VisitILOffset(BILOffsetExpression node) => node;

    protected override BExpression VisitInvoke(BInvokeExpression invokeExpression)
    {
        var tm = Modified(invokeExpression.Target, out var target);
        var am = Modified(invokeExpression.Arguments, out var args);
        if (tm || am)
            return new BInvokeExpression(target, args, invokeExpression.Type);
        return invokeExpression;
    }

    protected override BExpression VisitLabel(BLabelExpression yLabelExpression)
    {
        if (Modified(yLabelExpression.Default, out var @default))
            return new BLabelExpression(yLabelExpression.Target, @default);
        return yLabelExpression;
    }

    protected override BExpression VisitLambda(BLambdaExpression yLambdaExpression)
    {
        var pm = Modified(yLambdaExpression.Parameters, out var parameters);
        var bm = Modified(yLambdaExpression.Body, out var body);
        var tm = Modified(yLambdaExpression.This, out var @this);
        if (pm || bm || tm)
            return new BLambdaExpression(yLambdaExpression.Type, yLambdaExpression.Name, body, @this, parameters, yLambdaExpression.ReturnType, yLambdaExpression.Repository);
        return yLambdaExpression;

    }

    protected override BExpression VisitLoop(BLoopExpression yLoopExpression)
    {
        if (Modified(yLoopExpression.Body, out var body))
            return new BLoopExpression(body, yLoopExpression.Break, yLoopExpression.Continue);
        return yLoopExpression;
    }

    protected override BExpression VisitMemberInit(BMemberInitExpression memberInitExpression)
    {
        var ne = Modified(memberInitExpression.Target, out var target);
        var be = Modified(memberInitExpression.Bindings, VisitMemberBinding, out var bindings);
        if (ne || be)
            return new BMemberInitExpression(target, bindings);
        return memberInitExpression;
    }

    protected virtual BBinding VisitMemberBinding(BBinding b)
    {
        switch (b.BindingType)
        {
            case BindingType.MemberAssignment:
                return VisitMemberAssignment(b as BMemberAssignment);
            case BindingType.MemberListInit:
                return VisitMemberListBinding(b as BMemberElementInit);
            case BindingType.ElementInit:
                return VisitElementInit(b as BElementInit);
        }
        throw new NotImplementedException();
    }

    protected virtual BMemberElementInit VisitMemberListBinding(BMemberElementInit a)
    {
        if(Modified(a.Elements, VisitElementInit, out var ea))
        {
            return new BMemberElementInit(a.Member, ea);
        }
        return a;
    }


    protected virtual  BMemberAssignment VisitMemberAssignment(BMemberAssignment a)
    {
        if (Modified(a.Value, out var v))
            return new BMemberAssignment(a.Member, v);
        return a;
    }

    protected override BExpression VisitNew(BNewExpression yNewExpression)
    {
        var am = Modified(yNewExpression.args, out var args);
        if (am)
            return new BNewExpression(yNewExpression.constructor, args);
        return yNewExpression;

    }

    protected override BExpression VisitNewArray(BNewArrayExpression yNewArrayExpression)
    {
        var am = Modified(yNewArrayExpression.Elements, out var elements);
        if (am)
            return new BNewArrayExpression(yNewArrayExpression.ElementType, elements);
        return yNewArrayExpression;
    }

    protected override BExpression VisitParameter(BParameterExpression yParameterExpression) => yParameterExpression;

    protected override BExpression VisitProperty(BPropertyExpression yPropertyExpression)
    {
        if (Modified(yPropertyExpression.Target, out var target))
            return new BPropertyExpression(target, yPropertyExpression.PropertyInfo);
        return yPropertyExpression;
    }

    //protected override BExpression VisitRelay(BRelayExpression relayExpression)
    //{
    //    var cm = Modified(relayExpression.Closures, out var closures);
    //    var lm = Modified(relayExpression.InnerLambda, out var lambda);
    //        if(cm || lm ) 
    //        return new BRelayExpression(closures, lambda);
    //    return relayExpression;
    //}

    protected override BExpression VisitReturn(BReturnExpression yReturnExpression)
    {
        if (Modified(yReturnExpression.Default, out var @default))
            return new BReturnExpression(yReturnExpression.Target, @default);
        return yReturnExpression;
    }

    protected override BExpression VisitSwitch(BSwitchExpression node)
    {
        var tOrdModified = Modified(node.Target, node.Default, out var target, out  var @default);
        var casesModified = Modified(node.Cases, VisitSwitchCase, out var cases);
        if(tOrdModified || casesModified)
        {
            return new BSwitchExpression(target, node.CompareMethod, @default, cases);
        }
        return node;
    }

    protected virtual BSwitchCaseExpression VisitSwitchCase(BSwitchCaseExpression @case)
    {
        var tvm = Modified(@case.TestValues, out var tv);
        var bm = Modified(@case.Body, out var body);
        if (tvm || bm)
            return new BSwitchCaseExpression(body, tv);
        return @case;
    }

    protected override BExpression VisitAssign(BAssignExpression yAssignExpression)
    {
        if (Modified(yAssignExpression.Left, yAssignExpression.Right, out var left, out var right))
            return new BAssignExpression(left, right, right?.Type);
        return yAssignExpression;
    }

    protected override BExpression VisitTypeIs(BTypeIsExpression yTypeIsExpression)
    {
        if (Modified(yTypeIsExpression.Target, out var target))
            return new BTypeIsExpression(target, yTypeIsExpression.TypeOperand);
        return yTypeIsExpression;
    }

    protected override BExpression VisitTypeAs(BTypeAsExpression yTypeAsExpression)
    {
        if (Modified(yTypeAsExpression.Target, out var target))
            return new BTypeAsExpression(target, yTypeAsExpression.Type);
        return yTypeAsExpression;
    }

    protected override BExpression VisitUnbox(BUnboxExpression node)
    {
        if (Modified(node.Target, out var target))
            return new BUnboxExpression(target, node.Type);
        return node;
    }

    protected override BExpression VisitIndex(BIndexExpression yIndexExpression)
    {
        var tm = Modified(yIndexExpression.Target, out var target);
        var am = Modified(yIndexExpression.Arguments, out var arguments);
        if (tm || am)
            return new BIndexExpression(target, yIndexExpression.Property, arguments);
        return yIndexExpression;
    }

    protected override BExpression VisitArrayIndex(BArrayIndexExpression yArrayIndexExpression)
    {
        if (Modified(yArrayIndexExpression.Target, yArrayIndexExpression.Index, out var target, out var index))
            return new BArrayIndexExpression(target, index);
        return yArrayIndexExpression;
    }

    protected override BExpression VisitNewArrayBounds(BNewArrayBoundsExpression yNewArrayBoundsExpression)
    {
        if (Modified(yNewArrayBoundsExpression.Size, out var size))
            return new BNewArrayBoundsExpression(yNewArrayBoundsExpression.ElementType, size);
        return yNewArrayBoundsExpression;
    }

    protected override BExpression VisitUnary(BUnaryExpression yUnaryExpression)
    {
        if (Modified(yUnaryExpression.Target, out var target))
            return new BUnaryExpression(target, yUnaryExpression.Operator);
        return yUnaryExpression;
    }

    protected override BExpression VisitThrow(BThrowExpression throwExpression)
    {
        if (Modified(throwExpression.Expression, out var exp))
            return new BThrowExpression(exp);
        return throwExpression;
    }

    protected override BExpression VisitTryCatchFinally(BTryCatchFinallyExpression tryCatchFinallyExpression)
    {
        var tf = Modified(tryCatchFinallyExpression.Try, tryCatchFinallyExpression.Finally,
            out var @try, out var @finally);
        BCatchBody @catch = tryCatchFinallyExpression.Catch;
        bool cf = false;
        if(tryCatchFinallyExpression.Catch != null)
        {
            cf = Modified(tryCatchFinallyExpression.Catch.Body, out var cb);
            if (cf)
                @catch = new BCatchBody(tryCatchFinallyExpression.Catch.Parameter, cb);
            
        }
        if(cf || tf)
            return BExpression.TryCatchFinally(@try, @catch, @finally);
        return tryCatchFinallyExpression;
    }

    protected override BExpression VisitYield(BYieldExpression node)
    {
        if (Modified(node.Argument, out var arg))
            return new BYieldExpression(arg, node.DelegateYield, node.IsAwait);
        return node;
    }

    protected override BExpression VisitJumpSwitch(BJumpSwitchExpression node)
    {
        if (Modified(node.Target, out var target))
            return new BJumpSwitchExpression(target, node.Cases);
        return node;
    }

    protected override BExpression VisitListInit(BListInitExpression node)
    {
        var nm = Modified(node.NewExpression, out var newExp);
        var mm = Modified(node.Members, VisitElementInit, out var members);
        if(nm || mm)
        {
            return new BListInitExpression(newExp, members);
        }
        return node;
    }

    protected virtual BElementInit VisitElementInit(BElementInit e)
    {
        if(Modified(e.Arguments, out var args))
        {
            return new BElementInit(e.AddMethod, args);
        }
        return e;
    }

    protected override BExpression VisitStringConstant(BStringConstantExpression node) => node;

    protected override BExpression VisitBooleanConstant(BBooleanConstantExpression node) => node;

    protected override BExpression VisitFloatConstant(BFloatConstantExpression node) => node;

    protected override BExpression VisitDoubleConstant(BDoubleConstantExpression node) => node;

    protected override BExpression VisitUInt64Constant(BUInt64ConstantExpression node) => node;

    protected override BExpression VisitInt64Constant(BInt64ConstantExpression node) => node;

    protected override BExpression VisitUInt32Constant(BUInt32ConstantExpression node) => node;

    protected override BExpression VisitInt32Constant(BInt32ConstantExpression node) => node;

    protected override BExpression VisitByteConstant(BByteConstantExpression node) => node;

    protected override BExpression VisitTypeConstant(BTypeConstantExpression node) => node;

    protected override BExpression VisitMethodConstant(BMethodConstantExpression node) => node;

    protected override BExpression VisitAddressOf(BAddressOfExpression node)
    {
        if (Modified(node.Target, out var target))
            return new BAddressOfExpression(target);
        return node;
    }
}
