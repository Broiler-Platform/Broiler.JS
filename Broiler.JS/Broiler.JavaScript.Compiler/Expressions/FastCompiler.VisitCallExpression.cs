using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;
using System;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    private static readonly System.Reflection.MethodInfo DirectEvalMethod = typeof(DirectEvalSupport)
        .GetMethod(nameof(DirectEvalSupport.Execute), [typeof(Arguments), typeof(JSValue), typeof(JSValue), typeof(CallStackItem), typeof(bool), typeof(bool), typeof(string[]), typeof(JSVariable[]), typeof(JSVariable[]), typeof(string[]), typeof(string[]), typeof(string[]), typeof(bool), typeof(bool), typeof(bool), typeof(JSValue), typeof(bool), typeof(bool), typeof(JSValue), typeof(JSVariable), typeof(string[]), typeof(JSValue), typeof(bool)])
        ?? throw new InvalidOperationException("DirectEvalSupport.Execute(Arguments, JSValue, JSValue, CallStackItem, bool, bool, string[], JSVariable[], JSVariable[], string[], string[], string[], bool, bool, bool, JSValue, bool, bool, JSValue, JSVariable, string[], JSValue, bool) not found");

    protected override BExpression VisitCallExpression(AstCallExpression callExpression)
    {
        var ce = VisitCallExpression(callExpression.Callee, callExpression.Arguments, callExpression.Coalesce, callExpression.InOptionalChain);
        return ce;
    }

    protected (IFastEnumerable<BExpression> args, bool hasSpread) VisitArguments(IFastEnumerable<AstExpression> arguments)
    {
        var args = new Sequence<BExpression>(arguments.Count);
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

        var result = args.Any() ? (args, hasSpread) : (Sequence<BExpression>.Empty, false);
        return result;
    }

    protected BExpression VisitArguments(BExpression? thisArg, IFastEnumerable<AstExpression> arguments, BExpression? newTarget = null)
    {
        var args = new Sequence<BExpression>(arguments.Count);
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

    protected BExpression VisitCallExpression(AstExpression callee, IFastEnumerable<AstExpression> arguments, bool coalesce = false, bool inChain = false)
    {
        if (!coalesce
            && callee is AstIdentifier identifier
            && identifier.Name.Equals("eval"))
        {
            // `eval(...)` is a *direct* eval candidate whenever the callee is the
            // IdentifierReference "eval" — regardless of whether that name resolves to a
            // local binding (a parameter, `var eval = …`, or a `with`-object property) or
            // the global. Whether it is actually a direct eval is decided at RUNTIME by
            // DirectEvalSupport.Execute, which compares the resolved callee to %eval% and
            // falls back to an ordinary call when it differs. So a shadowed `eval` that
            // still holds the real %eval% (test262 sm/global/eval-02 directArg/directVar)
            // must run as a direct eval, evaluating its source in the caller's scope —
            // hence resolve the callee through the static binding when there is one rather
            // than skipping the direct-eval path entirely.
            BExpression evalCallee = (TryGetStaticIdentifierVariable(identifier, out var evalVariable) && evalVariable != null)
                ? evalVariable.Expression
                : JSContextBuilder.ResolveIdentifier(KeyOfName(identifier.Name));

            var paramArray = VisitArguments(null, arguments);
            var lexicalBindings = CaptureDirectEvalLexicalBindings();
            var capturedBindings = CaptureDirectEvalBindings();
            var shadowedBindings = CaptureDirectEvalShadowedBindings();
            var capturedBindingLexicalNames = CaptureDirectEvalBindingLexicalNames();
            var parameterBindings = CaptureDirectEvalParameterBindings();
            var privateNames = CaptureDirectEvalPrivateNames();
            var disallowArgumentsDeclaration = scope.Top.Function != null && !scope.Top.Function.IsArrowFunction;
            var allowSuperProperty = scope.Top.Super != null;
            // A SuperCall is only permitted in a direct eval inside the derived
            // constructor body, not inside a class field initializer (which is
            // compiled within the same constructor scope). See test262
            // class/elements/*-direct-eval-err-contains-supercall.
            // super() is permitted in the eval when the eval sits (transitively through
            // arrow functions) in a derived constructor before super(): the pending
            // member inits live on the root (constructor) scope, not an enclosing arrow's
            // own scope, so consult RootScope — matching the non-eval super() lowering.
            // When we are ourselves compiling a direct-eval body that was already granted
            // super-call capability by its outer context (the constructor or another
            // eval that wrapped it), forward that capability to a NESTED eval so that
            // `eval("eval('super()')")` resolves super() at the innermost site
            // (test262 sm/class/derivedConstructorArrowEvalNestedSuperCall).
            var nestedEvalInheritsSuperCall = isDirectEvalCompilation
                && JSEngine.Current is JSContext nestedCtx
                && nestedCtx.HasDirectEvalSuperCall;
            var allowSuperCall = allowSuperProperty
                && !inMemberInitializer
                && (scope.Top.RootScope.MemberInits != null || nestedEvalInheritsSuperCall);
            var useActivationBinding = scope.Top.Function?.IsArrowFunction == true && parameterInitializerDepth > 0;
            var activationOwner = disallowArgumentsDeclaration || useActivationBinding
                ? scope.Top.StackItem
                : BExpression.Constant(null, typeof(CallStackItem));
            // Pass the lexical [[HomeObject]] super reference so super.x inside the
            // eval body resolves against the enclosing method/initializer's super.
            var superValue = scope.Top.Super ?? BExpression.Constant(null, typeof(JSValue));
            // new.target is only legal in the eval body when the direct eval is
            // (transitively, through arrow functions) inside ordinary function
            // code. In global code it is a SyntaxError (PerformEval early error).
            // A class field initializer counts as ordinary function code even when
            // the class has no explicit constructor (so scope.Top is not itself a
            // function scope), so new.target is permitted there too.
            // When we are ourselves compiling a direct-eval body (a NESTED eval such
            // as `eval('eval("new.target")')`) the local scope chain no longer carries
            // the enclosing ordinary function, so inherit the outer eval's legality
            // decision rather than recomputing it — matching how VisitMeta defers to
            // DirectEvalSupport for new.target placed directly in an eval body.
            var rejectNewTarget = isDirectEvalCompilation
                ? (JSEngine.Current as JSContext)?.DirectEvalRejectsNewTarget ?? true
                : !inMemberInitializer && !EnclosedByOrdinaryFunction(scope.Top);

            // When a `super(...)` call is permitted in the eval body (a derived
            // constructor before super()), share the constructor's `this` binding and
            // superclass constructor so the eval's super() runs the superclass
            // [[Construct]] and initializes that same binding. The `this` value is then
            // read lazily through the binding instead of eagerly here, where it would
            // throw "Cannot access 'this' before initialization" merely for hosting the
            // super() call. Falls back to the prior behaviour when `this` is not a
            // JSVariable-backed binding.
            var thisArg = scope.Top.ThisExpression;
            BExpression directEvalSuperConstructor = BExpression.Constant(null, typeof(JSValue));
            BExpression directEvalThisBinding = BExpression.Constant(null, typeof(JSVariable));
            if (allowSuperCall && (scope.Top.ThisExpression as BPropertyExpression)?.Target is { } thisBindingTarget
                && thisBindingTarget.Type == typeof(JSVariable))
            {
                directEvalThisBinding = thisBindingTarget;
                directEvalSuperConstructor = scope.Top.SuperConstructor ?? scope.Top.Super ?? directEvalSuperConstructor;
                thisArg = BExpression.Constant(null, typeof(JSValue));
            }

            var evalVarEnvNames = CaptureEvalVarEnvNames();
            // The caller's new.target, threaded so `new.target` in the eval body
            // (PerformEval shares the calling context's [[NewTarget]]) resolves to it.
            // Inside an enclosing direct-eval compilation read the already-threaded
            // value so a nested eval inherits it.
            var directEvalNewTarget = scope.Top.NewTargetExpression
                ?? (isDirectEvalCompilation ? JSContextBuilder.DirectEvalNewTarget : JSContextBuilder.NewTarget());
            return BExpression.Call(null, DirectEvalMethod, paramArray, evalCallee, thisArg, activationOwner, BExpression.Constant(IsStrictMode), BExpression.Constant(disallowArgumentsDeclaration), lexicalBindings, capturedBindings, shadowedBindings, capturedBindingLexicalNames, parameterBindings, privateNames, BExpression.Constant(allowSuperProperty), BExpression.Constant(allowSuperCall), BExpression.Constant(useActivationBinding), superValue, BExpression.Constant(inMemberInitializer), BExpression.Constant(rejectNewTarget), directEvalSuperConstructor, directEvalThisBinding, evalVarEnvNames, directEvalNewTarget);
        }

        // A parenthesized optional chain closes the chain at the parens (per spec ECMAScript
        // §13.3.5 Optional Chains), but the inner MemberExpression still carries a Reference
        // whose base is the `this` value for the surrounding call: `(a?.b)()` must invoke
        // `a.b` with `this = a`, just as `(a.b)()` does. Unwrap the AstOptionalChain wrapper
        // here so the member-call path threads the correct receiver, and re-apply the chain
        // boundary's sentinel→undefined conversion to the call result. Without this, the
        // outer call falls into the no-this branch below and the function runs with the
        // surrounding `this` (test262 optional-chaining/optional-call-preserves-this).
        var chainBoundary = false;
        if (callee is AstOptionalChain wrapped && wrapped.Expression is AstMemberExpression)
        {
            callee = wrapped.Expression;
            chainBoundary = true;
        }

        if (callee.Type == FastNodeType.MemberExpression && callee is AstMemberExpression me)
        {
            BExpression name;
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
            BExpression invocation;
            if (isPrivateMethodKey)
            {
                using var keyTemp = scope.Top.GetTempVariable(typeof(KeyString));
                invocation = BExpression.Block(
                [
                    BExpression.Assign(keyTemp.Variable, name),
                    JSValueBuilder.InvokeMethod(te.Variable, te2.Variable, target, keyTemp.Variable, args, spread, me.Coalesce, coalesce, inChain || me.InOptionalChain),
                ]);
            }
            else
            {
                invocation = JSValueBuilder.InvokeMethod(te.Variable, te2.Variable, target, name, args, spread, me.Coalesce, coalesce, inChain || me.InOptionalChain);
            }

            // A parenthesized optional chain closes its chain at the parens, so the
            // outer call's result must collapse any in-flight skip sentinel back to
            // `undefined`.
            return chainBoundary ? JSValueBuilder.UnwrapOptionalChain(invocation) : invocation;
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

                // A super call is only legal inside a DERIVED class constructor (or an arrow
                // nested in one), which is exactly where SuperConstructor is bound. A base
                // class constructor (no super-constructor binding) or any method/accessor/
                // object method/plain function — which still has a home-object Super for
                // super.x but no super-constructor — is an early SyntaxError. (Direct eval
                // resolves and validates its own super placement, so it is exempt here.)
                if (top.SuperConstructor == null && !isDirectEvalCompilation)
                    throw new FastParseException(callee.Start, "'super' keyword is only valid inside a derived class constructor");

                // super(...) performs BindThisValue: the derived constructor's
                // `this` binding may be initialized only once, so a second
                // super() call (or a super() after `this` is already bound) is a
                // ReferenceError. The superclass [[Construct]] runs on every call
                // (it precedes the bind), and the bind throws when the binding is
                // already initialized. `@this` is the `this` binding's value read
                // (a property over the underlying JSVariable parameter); bind that
                // parameter directly when available, else fall back to a plain
                // assignment for non-JSVariable `this` representations.
                var thisBinding = (@this as BPropertyExpression)?.Target;
                // Thread the lexically-captured new.target (inherited correctly across
                // arrow functions) so a super() nested in an arrow allocates the instance
                // with the most-derived prototype. The arrow's own call-stack item carries
                // no new target, so the runtime fallback would otherwise be undefined.
                var superNewTarget = scope.Top.NewTargetExpression ?? JSContextBuilder.NewTarget();
                BExpression BindSuperResult() => thisBinding != null
                    ? JSVariableBuilder.BindThis(thisBinding, JSFunctionBuilder.ConstructSuper(super, superNewTarget, paramArray1))
                    : JSFunctionBuilder.InvokeSuperConstructor(super, superNewTarget, @this, paramArray1);

                // we need to set this to null
                // to inform function creator that we have
                // initialized members.. and super has been called...
                if (members?.Any() ?? false)
                {
                    var initList = new Sequence<BExpression>() { BindSuperResult() };
                    InitMembers(initList, top);
                    root.MemberInits = null;
                    top.MemberInits = null;

                    return BExpression.Block(initList);
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

                var hasWithObject = BExpression.NotEqual(withObjTemp.Expression, BExpression.Constant(null, typeof(JSObject)));
                var withThis = BExpression.Condition(
                    hasWithObject,
                    BExpression.Convert(withObjTemp.Expression, typeof(JSValue)),
                    JSUndefinedBuilder.Value,
                    typeof(JSValue));

                var withArgs = VisitArguments(withThis, arguments);

                return BExpression.Block(
                    new Sequence<BParameterExpression> { withObjTemp.Variable, withTargetTemp.Variable },
                    BExpression.Assign(withObjTemp.Expression, JSContextBuilder.ResolveWithObject(key)),
                    BExpression.Assign(
                        withTargetTemp.Expression,
                        BExpression.Condition(
                            hasWithObject,
                            // §9.1.1.2.6 GetBindingValue re-probes HasProperty (step 2) before
                            // the Get (step 4): HasBinding (ResolveWithObject) and GetBindingValue
                            // are distinct abstract operations, so a `with` object proxy observes
                            // a second `has` after the @@unscopables `get`.
                            JSContextBuilder.GetWithObjectBindingValue(withObjTemp.Expression, key, IsStrictMode),
                            // The with scopes were already walked by ResolveWithObject above
                            // (hasWithObject is false here), so resolve the remaining scopes
                            // without re-probing the binding object — a second `has` trap on
                            // the with object would violate HasBinding's single-probe contract.
                            JSContextBuilder.ResolveIdentifierWithoutWithScopes(key),
                            typeof(JSValue))),
                    JSFunctionBuilder.InvokeFunction(withTargetTemp.Expression, withArgs, coalesce));
            }

            var paramArray = VisitArguments(null, arguments);
            var target = VisitExpression(callee);
            return JSFunctionBuilder.InvokeFunction(target, paramArray, coalesce, inChain);
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

    private BExpression CaptureDirectEvalBindings()
    {
        var bindings = new Sequence<BExpression>();
        foreach (var variable in scope.Top.GetVisibleVariables())
            bindings.Add(variable.CaptureExpression);

        return BExpression.NewArrayInit(typeof(JSVariable), bindings);
    }

    // The function-owned subset of the direct-eval captured bindings whose writes
    // must stay local. A function-local binding genuinely shadows a same-named
    // program-level global, so an assignment to it inside the eval body must not
    // leak to that global or its global-object property. A program-level global
    // `var` is resolvable through the global environment and kept in sync with its
    // property by the normal dual-binding path, so it is not isolated. Mirrors the
    // `with`-fallback shadowed subset.
    private BExpression CaptureDirectEvalShadowedBindings()
    {
        var bindings = new Sequence<BExpression>();
        foreach (var variable in scope.Top.GetWithFallbackVariables())
            bindings.Add(variable.Variable);

        return BExpression.NewArrayInit(typeof(JSVariable), bindings);
    }

    // All in-scope bindings, overlaid so they remain resolvable inside a `with`
    // body even though the object environment is consulted first.
    private BExpression CaptureWithFallbackBindings()
    {
        var bindings = new Sequence<BExpression>();
        foreach (var variable in scope.Top.GetVisibleVariables())
            bindings.Add(variable.CaptureExpression);

        return BExpression.NewArrayInit(typeof(JSVariable), bindings);
    }

    // The function-owned subset whose writes must stay local. A program-level
    // global `var` is resolvable through the global environment and kept in sync
    // with its property by the normal dual-binding path, so it is not isolated.
    private BExpression CaptureWithFallbackShadowedBindings()
    {
        var bindings = new Sequence<BExpression>();
        foreach (var variable in scope.Top.GetWithFallbackVariables())
            bindings.Add(variable.Variable);

        return BExpression.NewArrayInit(typeof(JSVariable), bindings);
    }

    private BExpression CaptureDirectEvalBindingLexicalNames()
    {
        var names = new Sequence<BExpression>();
        foreach (var variable in scope.Top.GetVisibleVariables())
        {
            if (variable.IsLexical)
                names.Add(BExpression.Constant(variable.Name));
        }

        return BExpression.NewArrayInit(typeof(string), names);
    }

    // The immediate calling function's var-environment binding names: a sloppy direct eval's
    // `var X` reuses such a binding (it already exists in that var environment) instead of creating
    // a separate overlay, so reads/writes inside the eval reach the function's own binding.
    private BExpression CaptureEvalVarEnvNames()
    {
        var names = new Sequence<BExpression>();
        foreach (var name in scope.Top.GetImmediateVarEnvNames())
            names.Add(BExpression.Constant(name));

        return BExpression.NewArrayInit(typeof(string), names);
    }

    private BExpression CaptureDirectEvalLexicalBindings()
    {
        var bindings = new Sequence<BExpression>();
        // In non-strict mode a simple catch parameter does not conflict with a
        // `var` of the same name in the direct eval body (Annex B.3.4).
        foreach (var name in scope.Top.GetDirectEvalLexicalBindingNames(excludeSimpleCatchBindings: !IsStrictMode))
            bindings.Add(BExpression.Constant(name));

        return BExpression.NewArrayInit(typeof(string), bindings);
    }

    private BExpression CaptureDirectEvalParameterBindings()
    {
        var parameterBindings = scope.Top.CurrentDirectEvalParameterBindings;
        if (parameterBindings == null || parameterBindings.Length == 0)
            return BExpression.Constant(null, typeof(string[]));

        var bindings = new Sequence<BExpression>(parameterBindings.Length);
        foreach (var name in parameterBindings)
            bindings.Add(BExpression.Constant(name));

        return BExpression.NewArrayInit(typeof(string), bindings);
    }

    private BExpression CaptureDirectEvalPrivateNames()
    {
        var privateNames = scope.Top.DirectEvalPrivateNames;
        if (privateNames == null || privateNames.Length == 0)
            return BExpression.Constant(null, typeof(string[]));

        var bindings = new Sequence<BExpression>(privateNames.Length);
        foreach (var name in privateNames)
            bindings.Add(BExpression.Constant(name));

        return BExpression.NewArrayInit(typeof(string), bindings);
    }
}
