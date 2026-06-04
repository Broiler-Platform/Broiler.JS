using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;
using System;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    private static readonly System.Reflection.MethodInfo DirectEvalMethod = typeof(DirectEvalSupport)
        .GetMethod(nameof(DirectEvalSupport.Execute), [typeof(Arguments), typeof(JSValue), typeof(JSValue), typeof(CallStackItem), typeof(bool), typeof(bool), typeof(string[]), typeof(JSVariable[]), typeof(string[]), typeof(string[]), typeof(string[]), typeof(bool), typeof(bool), typeof(bool), typeof(JSValue), typeof(bool), typeof(bool)])
        ?? throw new InvalidOperationException("DirectEvalSupport.Execute(Arguments, JSValue, JSValue, CallStackItem, bool, bool, string[], JSVariable[], string[], string[], string[], bool, bool, bool, JSValue, bool, bool) not found");

    protected override YExpression VisitCallExpression(AstCallExpression callExpression)
    {
        var ce = VisitCallExpression(callExpression.Callee, callExpression.Arguments, callExpression.Coalesce);
        return ce;
    }

    protected (IFastEnumerable<YExpression> args, bool hasSpread) VisitArguments(IFastEnumerable<AstExpression> arguments)
    {
        var args = new Sequence<YExpression>(arguments.Count);
        bool hasSpread = false;
        var e = arguments.GetFastEnumerator();

        while (e.MoveNext(out var ae))
        {
            if (ae.Type != FastNodeType.SpreadElement)
            {
                args.Add(Visit(ae));
                continue;
            }

            // spread....
            var sae = (ae as AstSpreadElement)!.Argument;
            args.Add(JSSpreadValueBuilder.New(Visit(sae)));
            hasSpread = true;
        }

        var result = args.Any() ? (args, hasSpread) : (Sequence<YExpression>.Empty, false);
        return result;
    }

    protected YExpression VisitArguments(YExpression? thisArg, IFastEnumerable<AstExpression> arguments, YExpression? newTarget = null)
    {
        var args = new Sequence<YExpression>(arguments.Count);
        bool hasSpread = false;
        var e = arguments.GetFastEnumerator();

        while (e.MoveNext(out var ae))
        {
            if (ae.Type != FastNodeType.SpreadElement)
            {
                args.Add(Visit(ae));
                continue;
            }

            // spread....
            var sae = (ae as AstSpreadElement)!.Argument;
            args.Add(JSSpreadValueBuilder.New(Visit(sae)));
            hasSpread = true;
        }

        if (!args.Any())
        {
            if (thisArg == null)
                return ArgumentsBuilder.Empty();

            return ArgumentsBuilder.NewEmpty(thisArg);
        }

        thisArg ??= JSUndefinedBuilder.Value;
        if (hasSpread)
        {
            var r = ArgumentsBuilder.Spread(thisArg, args);
            return r;
        }

        var result = ArgumentsBuilder.New(thisArg, args);
        return result;
    }

    protected YExpression VisitCallExpression(AstExpression callee, IFastEnumerable<AstExpression> arguments, bool coalesce = false)
    {
        if (!coalesce
            && callee is AstIdentifier identifier
            && identifier.Name.Equals("eval"))
        {
            if (TryGetStaticIdentifierVariable(identifier, out var evalVariable) && evalVariable != null)
                goto skipDirectEval;

            var paramArray = VisitArguments(null, arguments);
            var lexicalBindings = CaptureDirectEvalLexicalBindings();
            var capturedBindings = CaptureDirectEvalBindings();
            var capturedBindingLexicalNames = CaptureDirectEvalBindingLexicalNames();
            var parameterBindings = CaptureDirectEvalParameterBindings();
            var privateNames = CaptureDirectEvalPrivateNames();
            var disallowArgumentsDeclaration = scope.Top.Function != null && !scope.Top.Function.IsArrowFunction;
            var allowSuperProperty = scope.Top.Super != null;
            // A SuperCall is only permitted in a direct eval inside the derived
            // constructor body, not inside a class field initializer (which is
            // compiled within the same constructor scope). See test262
            // class/elements/*-direct-eval-err-contains-supercall.
            var allowSuperCall = allowSuperProperty && scope.Top.MemberInits != null && !inMemberInitializer;
            var useActivationBinding = scope.Top.Function?.IsArrowFunction == true && parameterInitializerDepth > 0;
            var activationOwner = disallowArgumentsDeclaration || useActivationBinding
                ? scope.Top.StackItem
                : YExpression.Constant(null, typeof(CallStackItem));
            // Pass the lexical [[HomeObject]] super reference so super.x inside the
            // eval body resolves against the enclosing method/initializer's super.
            var superValue = scope.Top.Super ?? YExpression.Constant(null, typeof(JSValue));
            // new.target is only legal in the eval body when the direct eval is
            // (transitively, through arrow functions) inside ordinary function
            // code. In global code it is a SyntaxError (PerformEval early error).
            var rejectNewTarget = !EnclosedByOrdinaryFunction(scope.Top);
            return YExpression.Call(null, DirectEvalMethod, paramArray, JSContextBuilder.ResolveIdentifier(KeyOfName(identifier.Name)), scope.Top.ThisExpression, activationOwner, YExpression.Constant(IsStrictMode), YExpression.Constant(disallowArgumentsDeclaration), lexicalBindings, capturedBindings, capturedBindingLexicalNames, parameterBindings, privateNames, YExpression.Constant(allowSuperProperty), YExpression.Constant(allowSuperCall), YExpression.Constant(useActivationBinding), superValue, YExpression.Constant(inMemberInitializer), YExpression.Constant(rejectNewTarget));
        }

    skipDirectEval:

        if (callee.Type == FastNodeType.MemberExpression && callee is AstMemberExpression me)
        {
            YExpression name;

            switch (me.Property.Type)
            {
                case FastNodeType.Identifier:
                    var id = (me.Property as AstIdentifier)!;
                    name = me.Computed ? VisitExpression(id) : KeyOfName(id.Name);
                    break;

                case FastNodeType.Literal:
                    var l = (me.Property as AstLiteral)!;
                    if (l.TokenType == TokenTypes.String)
                        name = KeyOfName(l.Start.CookedText);
                    else if (l.TokenType == TokenTypes.Number)
                        name = GetLiteralPropertyKey(l);
                    else
                        throw new NotImplementedException();
                    break;

                case FastNodeType.MemberExpression:
                    name = VisitMemberExpression(me.Property as AstMemberExpression);
                    break;

                default:
                    name = Visit(me.Property);
                    break;
            }

            bool isSuper = me.Object.Type == FastNodeType.Super;
            var super = isSuper ? scope.Top.Super : null;
            var target = isSuper ? scope.Top.ThisExpression : VisitExpression(me.Object);

            if (isSuper)
            {
                var paramArray = VisitArguments(ArgumentsBuilder.This(scope.Top.ArgumentsExpression), arguments);
                var superMethod = JSValueBuilder.Index(super, name, me.Coalesce);

                return JSFunctionBuilder.InvokeFunction(superMethod, paramArray, me.Coalesce);
            }

            var (args, spread) = VisitArguments(arguments);
            using var te = scope.Top.GetTempVariable(typeof(JSValue));
            using var te2 = scope.Top.GetTempVariable(typeof(JSValue));

            return JSValueBuilder.InvokeMethod(te.Variable, te2.Variable, target, name, args, spread, me.Coalesce || coalesce);
        }
        else
        {
            bool isSuper = callee.Type == FastNodeType.Super;
            var @this = scope.Top.ThisExpression;

            if (isSuper)
            {
                // check if there are pending member inits...
                var paramArray1 = VisitArguments(ArgumentsBuilder.This(scope.Top.ArgumentsExpression), arguments);
                FastFunctionScope top = scope.Top;
                var root = top.RootScope;
                var members = root.MemberInits;
                // super(...) targets the superclass constructor, not the home-object prototype.
                var super = top.SuperConstructor ?? top.Super;

                // we need to set this to null
                // to inform function creator that we have
                // initialized members.. and super has been called...
                if (members?.Any() ?? false)
                {
                    var initList = new Sequence<YExpression>() { JSFunctionBuilder.InvokeSuperConstructor(super, @this, paramArray1) };
                    InitMembers(initList, top);
                    root.MemberInits = null;
                    top.MemberInits = null;

                    return YExpression.Block(initList);
                }
                
                return JSFunctionBuilder.InvokeSuperConstructor(super, @this, paramArray1);
            }

            var paramArray = VisitArguments(null, arguments);
            var target = VisitExpression(callee);
            return JSFunctionBuilder.InvokeFunction(target, paramArray, coalesce);
        }
    }

    // Walks out through arrow-function scopes (which inherit new.target from
    // their enclosing environment) to determine whether the position is inside
    // ordinary function code, where new.target is permitted.
    private static bool EnclosedByOrdinaryFunction(FastFunctionScope scope)
    {
        for (var s = scope; s != null; s = s.Parent)
        {
            if (s.Function == null)
                return false;
            if (!s.Function.IsArrowFunction)
                return true;
        }

        return false;
    }

    private YExpression CaptureDirectEvalBindings()
    {
        var bindings = new Sequence<YExpression>();
        foreach (var variable in scope.Top.GetVisibleVariables())
            bindings.Add(variable.Variable);

        return YExpression.NewArrayInit(typeof(JSVariable), bindings);
    }

    private YExpression CaptureDirectEvalBindingLexicalNames()
    {
        var names = new Sequence<YExpression>();
        foreach (var variable in scope.Top.GetVisibleVariables())
        {
            if (variable.IsLexical)
                names.Add(YExpression.Constant(variable.Name));
        }

        return YExpression.NewArrayInit(typeof(string), names);
    }

    private YExpression CaptureDirectEvalLexicalBindings()
    {
        var bindings = new Sequence<YExpression>();
        foreach (var name in scope.Top.GetDirectEvalLexicalBindingNames())
            bindings.Add(YExpression.Constant(name));

        return YExpression.NewArrayInit(typeof(string), bindings);
    }

    private YExpression CaptureDirectEvalParameterBindings()
    {
        var parameterBindings = scope.Top.CurrentDirectEvalParameterBindings;
        if (parameterBindings == null || parameterBindings.Length == 0)
            return YExpression.Constant(null, typeof(string[]));

        var bindings = new Sequence<YExpression>(parameterBindings.Length);
        foreach (var name in parameterBindings)
            bindings.Add(YExpression.Constant(name));

        return YExpression.NewArrayInit(typeof(string), bindings);
    }

    private YExpression CaptureDirectEvalPrivateNames()
    {
        var privateNames = scope.Top.DirectEvalPrivateNames;
        if (privateNames == null || privateNames.Length == 0)
            return YExpression.Constant(null, typeof(string[]));

        var bindings = new Sequence<YExpression>(privateNames.Length);
        foreach (var name in privateNames)
            bindings.Add(YExpression.Constant(name));

        return YExpression.NewArrayInit(typeof(string), bindings);
    }
}
