using System.Collections.Generic;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    // Resolves <paramref name="name"/> to an EvalShadowVariable binding when the
    // current position is inside a sloppy function whose parameter list contains a
    // direct eval (<see cref="evalShadowBoundary"/>) AND the name resolves to a
    // binding OUTSIDE that function. The shadow forwards to the outer binding until
    // the eval introduces the var, and — being an ordinary function-scope binding —
    // is captured by closures so they observe the eval-introduced value even after
    // the function returns. Returns false (use ordinary resolution) for names bound
    // inside the boundary function or names with no outer binding to forward to.
    private bool TryResolveEvalShadow(in StringSpan name, out FastFunctionScope.VariableScope shadow)
    {
        shadow = null;
        var boundary = evalShadowBoundary;
        if (boundary == null)
            return false;

        if (name.Equals("this") || name.Equals("arguments") || name.Equals("eval")
            || name.Equals("undefined") || name.Equals("super"))
            return false;

        // A `with` object environment changes resolution dynamically; do not shadow.
        if (withBoundaries.Count != 0)
            return false;

        var withinBoundary = true;
        FastFunctionScope.VariableScope outer = null;
        var outerIsGlobal = false;
        // The eval-boundary function scopes the reference crosses to reach `outer`,
        // collected innermost-first. A binding introduced by an eval in the inner
        // boundary must be able to shadow one introduced by an eval in an enclosing
        // boundary, which must in turn shadow the real outer binding — so a shadow is
        // created at EACH crossed boundary and chained (inner forwards to next-outer).
        var crossedBoundaries = new List<FastFunctionScope>();
        for (var s = scope.Top; s != null; s = s.Parent)
        {
            if (s.TryGetOwnVariable(name, out var v) && v.Variable != null)
            {
                if (v.IsEvalShadow)
                {
                    // An existing shadow at a crossed boundary already forwards correctly;
                    // reuse it as the outer binding for any (inner) boundaries still to chain.
                    outer = v;
                    outerIsGlobal = false;
                    break;
                }

                if (withinBoundary)
                    return false; // an ordinary binding declared inside the boundary function

                outer = v; // a binding in an enclosing scope: shadow it
                // Only a `var`/function global (a global-object-backed binding) is read
                // and written through JSVariable.GlobalValue. A top-level `let`/`const`
                // is a LEXICAL binding that is NOT a property of the global object — it is
                // its own JSVariable storage like a function local — so it must use
                // GetValue/SetValue. Treating it as global made the shadow read the
                // (absent) global-object property and observe undefined.
                outerIsGlobal = s.Function == null && !v.IsLexical;
                break;
            }

            // Collect eval-boundary scopes the reference crosses. The innermost boundary
            // (evalShadowBoundary) is recorded here too; `withinBoundary` flips when we
            // pass it so a binding owned inside it falls back to ordinary resolution.
            if (s.IsEvalShadowBoundary)
                crossedBoundaries.Add(s);

            if (ReferenceEquals(s, boundary))
                withinBoundary = false;
        }

        if (outer == null)
            return false; // undeclared name: nothing to forward to

        // Build the shadow chain outermost→innermost: the outermost crossed boundary
        // forwards to the real outer binding; each inner boundary forwards to the
        // shadow of the next-outer boundary. Reuse a boundary's existing shadow.
        var nextOuterVariable = outer.Variable;
        var nextOuterIsGlobal = outer.IsEvalShadow ? false : outerIsGlobal;
        FastFunctionScope.VariableScope created = null;
        for (var i = crossedBoundaries.Count - 1; i >= 0; i--)
        {
            var b = crossedBoundaries[i];
            if (b.TryGetOwnVariable(name, out var existing) && existing.IsEvalShadow)
            {
                created = existing;
            }
            else
            {
                created = b.CreateEvalShadow(name, nextOuterVariable, nextOuterIsGlobal);
            }

            nextOuterVariable = created.Variable;
            // Chained shadows forward to another JSVariable via GetValue/SetValue, never
            // through a global-object property.
            nextOuterIsGlobal = false;
        }

        if (created != null)
            shadow = created;                 // innermost shadow of the freshly built chain
        else if (outer.IsEvalShadow)
            shadow = outer;                   // existing shadow, no inner boundary to chain
        else
            // Defensive: no crossed boundary (shouldn't happen once the binding is found
            // outside the active boundary) — single shadow on the active boundary.
            shadow = boundary.CreateEvalShadow(name, outer.Variable, outerIsGlobal);
        return true;
    }

    // Whether <paramref name="functionDeclaration"/> has a direct `eval(...)` call
    // somewhere in a parameter initializer (not nested inside another function).
    // Such a call can introduce parameter-environment vars that must shadow outer
    // bindings for the body and the closures created in the parameter list/body.
    private static bool ParametersContainDirectEval(AstFunctionExpression functionDeclaration)
    {
        var detector = new ParameterDirectEvalDetector();
        var parameters = functionDeclaration.Params.GetFastEnumerator();
        while (parameters.MoveNext(out var parameter))
        {
            if (parameter.Identifier != null)
                detector.Visit(parameter.Identifier);
            if (parameter.Init != null)
                detector.Visit(parameter.Init);

            if (detector.Found)
                return true;
        }

        return false;
    }

    // Whether <paramref name="functionDeclaration"/> has a direct `eval(...)` call in
    // its body (not nested inside another function/class, which have their own var
    // environments). A sloppy direct eval can introduce a `var`/`function` into THIS
    // function's variable environment that must be observed by closures created in the
    // body (test262 staging sm/regress-554955, sm/eval/exhaustive-*) — so free names
    // that resolve to an enclosing scope are routed through EvalShadowVariable bindings
    // that the eval reuses as its var storage.
    private static bool BodyContainsDirectEval(AstFunctionExpression functionDeclaration)
    {
        if (functionDeclaration.Body == null)
            return false;

        var detector = new ParameterDirectEvalDetector();
        detector.Visit(functionDeclaration.Body);
        return detector.Found;
    }

    // Finds a direct `eval(...)` call in an expression, without descending into
    // nested functions or classes (which establish their own scopes).
    private sealed class ParameterDirectEvalDetector : Broiler.JavaScript.Ast.AstReduce
    {
        public bool Found { get; private set; }

        // AstReduce leaves these compact containers to specialized rewriters.
        // Direct-eval analysis must inspect them so `var x = eval(...)`, pattern
        // defaults, and switch-clause evals establish the correct dynamic boundary.
        protected override VariableDeclarator VisitVariableDeclarator(VariableDeclarator declarator)
        {
            Visit(declarator.Identifier);
            if (declarator.Init != null)
                Visit(declarator.Init);
            return declarator;
        }

        protected override ObjectProperty VisitObjectProperty(ObjectProperty property)
        {
            if (property.Key != null)
                Visit(property.Key);
            if (property.Value != null)
                Visit(property.Value);
            if (property.Init != null)
                Visit(property.Init);
            return property;
        }

        protected override Case VisitCase(Case @case)
        {
            if (@case.Test != null)
                Visit(@case.Test);
            var statements = @case.Statements.GetFastEnumerator();
            while (statements.MoveNext(out var statement))
                Visit(statement);
            return @case;
        }

        protected override AstNode VisitCallExpression(AstCallExpression callExpression)
        {
            if (callExpression.Callee is AstIdentifier callee && callee.Name.Equals("eval"))
            {
                Found = true;
                return callExpression;
            }

            return base.VisitCallExpression(callExpression);
        }

        protected override AstNode VisitFunctionExpression(AstFunctionExpression functionExpression)
        {
            // Arrow functions inherit the enclosing function's `arguments` binding (they
            // have none of their own), so a direct eval inside an arrow needs the outer
            // function's arguments materialised. Ordinary nested functions have their own
            // arguments and so do not bubble the "contains eval" signal up.
            if (!functionExpression.IsArrowFunction)
                return functionExpression;

            // Visit the arrow's body and parameter initializers to keep searching for a
            // direct eval. (Visiting the arrow's own AstFunctionExpression node via base
            // would recreate it; we just need the side-effect on `Found`.)
            if (functionExpression.Body != null)
                Visit(functionExpression.Body);
            var pe = functionExpression.Params.GetFastEnumerator();
            while (pe.MoveNext(out var p) && !Found)
            {
                if (p.Init != null) Visit(p.Init);
            }
            return functionExpression;
        }

        protected override AstNode VisitClassStatement(AstClassExpression classStatement)
            => classStatement;
    }

    protected override BExpression VisitIdentifier(AstIdentifier identifier) => VisitIdentifier(identifier, true);

    private static bool IsScopeInsideWithBoundary(FastFunctionScope declarationScope, FastFunctionScope boundary)
    {
        if (ReferenceEquals(declarationScope, boundary))
            return false;

        for (var current = declarationScope.Parent; current != null; current = current.Parent)
        {
            if (ReferenceEquals(current, boundary))
                return true;
        }

        return false;
    }

    private bool TryGetStaticIdentifierVariable(AstIdentifier identifier, out FastFunctionScope.VariableScope variable)
    {
        variable = null;
        if (withBoundaries.Count == 0)
        {
            variable = scope.Top.GetVariable(identifier.Name, true);
            return true;
        }

        var boundary = withBoundaries.Peek();
        for (var current = scope.Top; current != null; current = current.Parent)
        {
            if (!current.TryGetOwnVariable(identifier.Name, out var ownVariable))
                continue;

            if (IsScopeInsideWithBoundary(current, boundary))
            {
                variable = ownVariable;
                return true;
            }

            return false;
        }

        return false;
    }

    private BExpression VisitIdentifierReference(AstIdentifier identifier)
    {
        if (identifier.Name.Equals("arguments")
            && scope.Top.Function?.IsArrowFunction == true)
        {
            if (parameterInitializerDepth > 0)
                return JSContextBuilder.Index(KeyOfName(identifier.Name));

            if (TryGetStaticIdentifierVariable(identifier, out var arrowVariable) && arrowVariable != null)
                return arrowVariable.Expression;

            return JSContextBuilder.Index(KeyOfName(identifier.Name));
        }

        if (TryGetStaticIdentifierVariable(identifier, out var variable) && variable != null)
            return variable.Expression;

        return JSContextBuilder.Index(KeyOfName(identifier.Name));
    }

    private BExpression VisitIdentifier(AstIdentifier identifier, bool throwIfMissing)
    {
        if (identifier.Name.Equals("undefined"))
            return JSUndefinedBuilder.Value;

        if (identifier.Name.Equals("this"))
        {
            var thisExpression = scope.Top.ThisExpression;
            if (scope.Top.RootScope.MemberInits != null)
                return JSValueExtensionsBuilder.Coalesce(thisExpression, JSExceptionBuilder.Throw("Must call super constructor before accessing 'this'"));

            return thisExpression;
        }

        if (identifier.Name.Equals("arguments"))
        {
            // `arguments` may not appear in a class field initializer (it is forbidden
            // by ClassFieldDefinition's ContainsArguments early error). inMemberInitializer
            // is inherited by arrow functions in the initializer but reset by a nested
            // ordinary function (which has its own arguments object), so it marks exactly
            // the positions that are an error. Direct eval validates its own body.
            if (inMemberInitializer && !isDirectEvalCompilation)
                throw new FastParseException(identifier.Start, "'arguments' is not allowed in a class field initializer");

            // A lexical binding named `arguments` declared in an INNER block scope (a
            // `let`/`const`/`class` or a block-level function declaration) shadows the
            // function's arguments object within that block, so `arguments` there must
            // resolve to that binding — not the materialised arguments object. (A
            // `var arguments` instead lives in the function root and shares the arguments
            // binding, handled by MaterializeArgumentsBinding below.) Skipped under an
            // active `with`, whose object may dynamically provide `arguments`; that path
            // keeps the existing with-aware handling. (test262 annexB block-decl-func-skip-
            // arguments, language/.../arguments lexical-shadow cases.)
            if (withBoundaries.Count == 0 && TryGetBlockScopedArguments(out var lexicalArguments))
                return lexicalArguments.Expression;

            // A direct eval body has no `arguments` of its own — references resolve to the
            // ENCLOSING function's `arguments` binding via the scope chain at runtime
            // (test262 sm/extensions/function-caller-skips-eval-frames probes
            // `arguments.callee` inside an eval, which only works when it sees the outer
            // function's mapped arguments object — not a freshly materialised empty one).
            // The non-eval scope here would otherwise materialise an arguments binding in
            // the eval's program scope (which has no function / parameters), giving an
            // empty unmapped arguments object whose `callee` is the strict-mode poison.
            //
            // This applies ONLY to references lexically inside the eval program body
            // itself (RootScope.Function == null). An ordinary function DECLARED within
            // the eval (`eval('(function f(){ return arguments[0]; })(7)')`) has its own
            // arguments object — its RootScope is that function (Function != null) — and
            // must materialise it like any function, not leak to the enclosing frame's
            // arguments via dynamic resolution (test262 sm/strict/10.6 arguments index
            // writability/configurability under eval).
            if (isDirectEvalCompilation && scope.Top.RootScope.Function == null)
                return throwIfMissing
                    ? JSContextBuilder.ResolveIdentifier(KeyOfName(identifier.Name))
                    : JSContextBuilder.ResolveIdentifierOrUndefined(KeyOfName(identifier.Name));

            if (scope.Top.Function?.IsArrowFunction == true
            )
            {
                // An arrow in a parameter initializer (`function g(h = () => arguments)`)
                // refers to the enclosing function's `arguments` exactly like an arrow in
                // the body. Fall through to the shared materialisation path (which targets
                // the arrow's RootScope — the enclosing function) instead of emitting a
                // runtime identifier lookup that finds no such binding and throws
                // "arguments is not defined".
                if (TryGetStaticIdentifierVariable(identifier, out var arrowVariable) && arrowVariable != null)
                    return arrowVariable.Expression;

                // An arrow has no `arguments` of its own — it refers to the enclosing
                // function's. An arrow's RootScope IS that enclosing function's scope, so
                // fall through to materialise the binding there (the function may have
                // referenced `arguments` only from inside this arrow, so it does not exist
                // yet). At program scope there is no enclosing function, so `arguments`
                // stays an unbound free reference.
                if (scope.Top.RootScope.Function == null)
                    return throwIfMissing
                        ? JSContextBuilder.ResolveIdentifier(KeyOfName(identifier.Name))
                        : JSContextBuilder.Index(KeyOfName(identifier.Name));
            }

            // Inside a `with`, an unqualified `arguments` first resolves against the
            // with-object — whose own `arguments` property shadows the function's
            // arguments binding (`with ({ arguments: 42 }) { arguments }` is 42) — and
            // only falls back to that binding when the with-object does not provide it.
            // This mirrors the `delete arguments` with-aware path.
            if (withBoundaries.Count > 0)
            {
                var argumentsKey = KeyOfName(identifier.Name);
                var argumentsBinding = MaterializeArgumentsBinding().Expression;
                using var withObjectTemp = scope.Top.GetTempVariable(typeof(JSObject));
                var hasWithObject = BExpression.NotEqual(withObjectTemp.Expression, BExpression.Constant(null, typeof(JSObject)));
                return BExpression.Block(
                    new Sequence<BParameterExpression> { withObjectTemp.Variable },
                    BExpression.Assign(withObjectTemp.Expression, JSContextBuilder.ResolveWithObject(argumentsKey)),
                    BExpression.Condition(
                        hasWithObject,
                        JSValueBuilder.Index(withObjectTemp.Expression, argumentsKey),
                        argumentsBinding,
                        typeof(JSValue)));
            }

            return MaterializeArgumentsBinding().Expression;
        }

        if (TryResolveEvalShadow(identifier.Name, out var shadow))
            return shadow.Expression;

        if (TryGetStaticIdentifierVariable(identifier, out var variable) && variable != null)
            // A throwing read prefers the binding's dedicated read expression when it has one
            // (an eval-introduced global var, whose read must throw once deleted); `typeof` and the
            // write path keep the plain Expression.
            return throwIfMissing && variable.ReadExpression != null ? variable.ReadExpression : variable.Expression;

        return throwIfMissing
            // A strict reference resolving through an active `with` scope (a strict function nested
            // in a sloppy `with`) throws if the binding was deleted by @@unscopables; a sloppy one
            // yields undefined. Either form throws for a genuinely undeclared name.
            ? (IsStrictMode
                ? JSContextBuilder.ResolveIdentifierStrict(KeyOfName(identifier.Name))
                : JSContextBuilder.ResolveIdentifier(KeyOfName(identifier.Name)))
            : JSContextBuilder.ResolveIdentifierOrUndefined(KeyOfName(identifier.Name));
    }

    // Finds a lexical `arguments` binding declared in an inner block scope (a
    // `let`/`const`/`class` or a block-level function declaration) that shadows the
    // function's arguments object. Walks the block (and arrow) scopes from the current
    // position up to — but NOT including — the function root that owns the arguments
    // object (the unique scope where <c>s == s.RootScope</c>): a `var arguments` lives in
    // that root and must keep sharing the arguments binding via MaterializeArgumentsBinding.
    private bool TryGetBlockScopedArguments(out FastFunctionScope.VariableScope variable)
    {
        variable = null;
        for (var s = scope.Top; s != null && !ReferenceEquals(s, s.RootScope); s = s.Parent)
        {
            if (s.TryGetOwnVariable("arguments", out var v) && v.Variable != null)
            {
                variable = v;
                return true;
            }
        }

        return false;
    }

    // Creates (or returns) the ordinary function's own `arguments` binding in the
    // function (root) scope, initialized to the arguments object — mapped to the
    // parameters in sloppy mode with a simple parameter list, unmapped otherwise.
    // Both an `arguments` reference and a `var arguments` declaration resolve here so
    // they share one binding (test262 S13_A15_T2: `var arguments = x` overrides it).
    internal FastFunctionScope.VariableScope MaterializeArgumentsBinding()
    {
        var functionScope = scope.Top.RootScope;

        // Already materialized (an earlier reference or the var-hoisting pass).
        if (functionScope.TryGetOwnVariable("arguments", out var existing))
            return existing;

        var argumentsObject = JSArgumentsBuilder.New(functionScope.ArgumentsExpression);
        if (!IsStrictMode
            && functionScope.Function != null
            && HasSimpleParameterList(functionScope.Function.Params))
        {
            var parameters = new List<VariableDeclarator>();
            var parameterEnumerator = functionScope.Function.Params.GetFastEnumerator();
            while (parameterEnumerator.MoveNext(out var parameter))
                parameters.Add(parameter);

            var parameterCount = parameters.Count;
            var mappedParameters = new BExpression[parameterCount];
            var seenNames = new HashSet<string>();

            for (var i = parameterCount - 1; i >= 0; i--)
            {
                if (parameters[i].Identifier is not AstIdentifier parameterIdentifier)
                {
                    mappedParameters[i] = BExpression.Constant(null, typeof(JSVariable));
                    continue;
                }

                var parameterName = parameterIdentifier.Name.Value;
                if (!seenNames.Add(parameterName))
                {
                    mappedParameters[i] = BExpression.Constant(null, typeof(JSVariable));
                    continue;
                }

                mappedParameters[i] = functionScope.GetVariable(parameterIdentifier.Name).Variable;
            }

            argumentsObject = JSArgumentsBuilder.NewMapped(functionScope.ArgumentsExpression, BExpression.NewArrayInit(typeof(JSVariable), mappedParameters));
        }

        return functionScope.CreateVariable("arguments", argumentsObject);
    }
}
