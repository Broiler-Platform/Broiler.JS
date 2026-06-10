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
        .GetMethod(nameof(DirectEvalSupport.Execute), [typeof(Arguments), typeof(JSValue), typeof(JSValue), typeof(CallStackItem), typeof(bool), typeof(bool), typeof(string[]), typeof(JSVariable[]), typeof(string[]), typeof(string[]), typeof(string[]), typeof(bool), typeof(bool), typeof(bool), typeof(JSValue), typeof(bool), typeof(bool), typeof(bool)])
        ?? throw new InvalidOperationException("DirectEvalSupport.Execute(Arguments, JSValue, JSValue, CallStackItem, bool, bool, string[], JSVariable[], string[], string[], string[], bool, bool, bool, JSValue, bool, bool, bool) not found");

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
            // A class field initializer counts as ordinary function code even when
            // the class has no explicit constructor (so scope.Top is not itself a
            // function scope), so new.target is permitted there too.
            var rejectNewTarget = !inMemberInitializer && !EnclosedByOrdinaryFunction(scope.Top);
            return YExpression.Call(null, DirectEvalMethod, paramArray, JSContextBuilder.ResolveIdentifier(KeyOfName(identifier.Name)), scope.Top.ThisExpression, activationOwner, YExpression.Constant(IsStrictMode), YExpression.Constant(disallowArgumentsDeclaration), lexicalBindings, capturedBindings, capturedBindingLexicalNames, parameterBindings, privateNames, YExpression.Constant(allowSuperProperty), YExpression.Constant(allowSuperCall), YExpression.Constant(useActivationBinding), superValue, YExpression.Constant(inMemberInitializer), YExpression.Constant(rejectNewTarget));
        }

    skipDirectEval:

        if (callee.Type == FastNodeType.MemberExpression && callee is AstMemberExpression me)
        {
            YExpression name;
            var isPrivateMethodKey = false;

            switch (me.Property.Type)
            {
                case FastNodeType.Identifier:
                    var id = (me.Property as AstIdentifier)!;
                    if (!me.Computed && id.Name.Length > 0 && id.Name.Value[0] == '#')
                    {
                        name = KeyOfPrivateName(id.Name);
                        isPrivateMethodKey = true;
                    }
                    else
                    {
                        name = me.Computed ? VisitExpression(id) : KeyOfName(id.Name);
                    }
                    break;

                case FastNodeType.Literal:
                    var l = (me.Property as AstLiteral)!;
                    if (l.TokenType == TokenTypes.String)
                        name = KeyOfName(l.Start.CookedText);
                    else if (l.TokenType == TokenTypes.Number)
                        name = GetLiteralPropertyKey(l);
                    else
                        // null / bigint / regexp / etc. literal key: evaluate the
                        // literal and coerce it to a property key at runtime
                        // (mirrors VisitMemberExpression's read path).
                        name = VisitLiteral(l);
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
                // The super method runs with the caller's `this`. Use the lexical
                // this binding (ThisExpression) rather than the current activation's
                // Arguments.This: inside an arrow function the arrow has its own
                // Arguments whose This is not the enclosing method's receiver, but
                // ThisExpression resolves to the captured lexical this in both cases.
                var paramArray = VisitArguments(target, arguments);
                var superMethod = JSValueBuilder.Index(super, name, me.Coalesce);

                return JSFunctionBuilder.InvokeFunction(superMethod, paramArray, me.Coalesce);
            }

            var (args, spread) = VisitArguments(arguments);
            using var te = scope.Top.GetTempVariable(typeof(JSValue));
            using var te2 = scope.Top.GetTempVariable(typeof(JSValue));

            // A private name resolves to a per-evaluation key captured from the class
            // scope. InvokeMethod takes the key as an `in KeyString` (by-address)
            // argument, and a captured closure variable cannot be loaded by address;
            // copy it into a method-local temp (which can) first.
            if (isPrivateMethodKey)
            {
                using var keyTemp = scope.Top.GetTempVariable(typeof(KeyString));
                return YExpression.Block(new YExpression[]
                {
                    YExpression.Assign(keyTemp.Variable, name),
                    JSValueBuilder.InvokeMethod(te.Variable, te2.Variable, target, keyTemp.Variable, args, spread, me.Coalesce || coalesce),
                });
            }

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

                // super(...) performs BindThisValue: the derived constructor's
                // `this` binding may be initialized only once, so a second
                // super() call (or a super() after `this` is already bound) is a
                // ReferenceError. The superclass [[Construct]] runs on every call
                // (it precedes the bind), and the bind throws when the binding is
                // already initialized. `@this` is the `this` binding's value read
                // (a property over the underlying JSVariable parameter); bind that
                // parameter directly when available, else fall back to a plain
                // assignment for non-JSVariable `this` representations.
                var thisBinding = (@this as YPropertyExpression)?.Target;
                YExpression BindSuperResult() => thisBinding != null
                    ? JSVariableBuilder.BindThis(thisBinding, JSFunctionBuilder.ConstructSuper(super, paramArray1))
                    : JSFunctionBuilder.InvokeSuperConstructor(super, @this, paramArray1);

                // we need to set this to null
                // to inform function creator that we have
                // initialized members.. and super has been called...
                if (members?.Any() ?? false)
                {
                    var initList = new Sequence<YExpression>() { BindSuperResult() };
                    InitMembers(initList, top);
                    root.MemberInits = null;
                    top.MemberInits = null;

                    return YExpression.Block(initList);
                }

                return BindSuperResult();
            }

            // Calling a name resolved through a `with` object binds that object as
            // the call's `this` (the reference's WithBaseObject). Only applies to a
            // name that is not a binding declared inside the with body — those are
            // ordinary locals with an undefined `this`.
            if (callee.Type == FastNodeType.Identifier
                && withBoundaries.Count != 0
                && callee is AstIdentifier withCallee
                && !withCallee.Name.Equals("eval")
                && !(TryGetStaticIdentifierVariable(withCallee, out var withStatic) && withStatic != null)
                && !TryResolveEvalShadow(withCallee.Name, out _))
            {
                var key = KeyOfName(withCallee.Name);
                using var withObjTemp = scope.Top.GetTempVariable(typeof(JSObject));
                using var withTargetTemp = scope.Top.GetTempVariable(typeof(JSValue));

                var hasWithObject = YExpression.NotEqual(withObjTemp.Expression, YExpression.Constant(null, typeof(JSObject)));
                var withThis = YExpression.Condition(
                    hasWithObject,
                    YExpression.Convert(withObjTemp.Expression, typeof(JSValue)),
                    JSUndefinedBuilder.Value,
                    typeof(JSValue));

                var withArgs = VisitArguments(withThis, arguments);

                return YExpression.Block(
                    new Sequence<YParameterExpression> { withObjTemp.Variable, withTargetTemp.Variable },
                    YExpression.Assign(withObjTemp.Expression, JSContextBuilder.ResolveWithObject(key)),
                    YExpression.Assign(
                        withTargetTemp.Expression,
                        YExpression.Condition(
                            hasWithObject,
                            JSValueBuilder.Index(withObjTemp.Expression, key),
                            JSContextBuilder.ResolveIdentifier(key),
                            typeof(JSValue))),
                    JSFunctionBuilder.InvokeFunction(withTargetTemp.Expression, withArgs, coalesce));
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

    // All in-scope bindings, overlaid so they remain resolvable inside a `with`
    // body even though the object environment is consulted first.
    private YExpression CaptureWithFallbackBindings()
    {
        var bindings = new Sequence<YExpression>();
        foreach (var variable in scope.Top.GetVisibleVariables())
            bindings.Add(variable.Variable);

        return YExpression.NewArrayInit(typeof(JSVariable), bindings);
    }

    // The function-owned subset whose writes must stay local. A program-level
    // global `var` is resolvable through the global environment and kept in sync
    // with its property by the normal dual-binding path, so it is not isolated.
    private YExpression CaptureWithFallbackShadowedBindings()
    {
        var bindings = new Sequence<YExpression>();
        foreach (var variable in scope.Top.GetWithFallbackVariables())
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
        // In non-strict mode a simple catch parameter does not conflict with a
        // `var` of the same name in the direct eval body (Annex B.3.4).
        foreach (var name in scope.Top.GetDirectEvalLexicalBindingNames(excludeSimpleCatchBindings: !IsStrictMode))
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
