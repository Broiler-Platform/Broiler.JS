using System.Collections.Generic;
using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Patterns;
using Broiler.JavaScript.Ast.Statements;

namespace Broiler.JavaScript.Compiler;

/// <summary>
/// The names a function body references but does not itself bind — what item 1-1's remaining half
/// has to know about a function it has NOT built a tree for
/// (docs/performance-roadmap.md item 1-1).
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the sub-project the item names, and it exists to be priced before anything is built
/// on it.</b> Deferring expression-tree construction is worth 43–75% of compile over a population
/// that is 84–99.7% never invoked, and the one thing standing in the way is that a captured name's
/// index in the enclosing lambda's <c>Box[]</c> is decided by <c>LambdaRewriter</c> <em>from the
/// tree</em> — and a deferred body has no tree. The eager step that makes the array addressable
/// without one is this walk.
/// </para>
/// <para>
/// <b>It is deliberately not the walk that already exists.</b> <c>NestedFunctionScanner</c>
/// collects every identifier a nested function <em>mentions</em>, which is sound for deciding what
/// cannot be scalar-replaced and useless for deciding what to box: it cannot tell
/// <c>function (x) { return x; }</c> — which captures nothing — from <c>function () { return x; }</c>,
/// which captures one binding. Boxing on a mention would box a local that merely shares a spelling
/// with an inner parameter, and every such box costs the enclosing function its numeric tier.
/// </para>
/// <para>
/// <b>Scoping is tracked because the answer depends on it and not because it is tidy.</b> A
/// <c>var</c> is function-scoped and a <c>let</c>/<c>const</c>/class is block-scoped, so
/// <c>{ let x; } x</c> has a free <c>x</c> and <c>{ var x; } x</c> does not. Parameters, catch
/// bindings, a named function expression's own name, and nested function declarations all bind.
/// Getting any of them wrong changes the set, which is why this is measured as itself rather than
/// approximated by an identifier count — <c>--compile-phases</c>' <c>scanMs</c> is that count, and
/// the roadmap records it as a lower bound for exactly this reason.
/// </para>
/// <para>
/// <b>Over-approximation is safe and under-approximation is a miscompile</b>, so every construct
/// this does not understand contributes its names as free. A direct <c>eval</c>, a <c>with</c> or a
/// <c>debugger</c> can reach a binding that is never mentioned at all, so a body containing one is
/// reported as <see cref="Dynamic"/> and must be compiled eagerly rather than deferred.
/// </para>
/// </remarks>
public sealed class FreeNameScan
{
    /// <summary>Names referenced and not bound anywhere inside the function.</summary>
    public readonly HashSet<string> Free = new(System.StringComparer.Ordinal);

    /// <summary>
    /// The body can reach a binding it never names — a direct <c>eval</c>, a <c>with</c>, or
    /// <c>debugger</c>. No free-name set describes it, so the function cannot be deferred.
    /// </summary>
    public bool Dynamic { get; internal set; }

    /// <summary>
    /// Every function in <paramref name="root"/> with its own free-name set, computed in ONE
    /// bottom-up pass.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the shippable shape, and the per-function <see cref="Of"/> is not.</b> Calling
    /// <c>Of</c> once per function re-walks every nested function once per enclosing level, so the
    /// cost is superlinear in nesting depth — measured against body tree construction it runs
    /// 5.8% on Closure and **47.7% on Box2D**, whose 978 functions are deeply nested, against the
    /// 5.4–9.9% the roadmap recorded from an identifier count. Half the prize is not a charge-back
    /// worth paying when the same answer is available for one pass.
    /// </para>
    /// <para>
    /// One pass gives it because free names compose: a function's free set is its own references,
    /// plus its children's free sets, minus what it binds. So a child is walked once and its
    /// result is propagated into the parent as if the parent had referenced those names directly —
    /// which is also exactly what capture means, and why the transitive case needs no special
    /// handling.
    /// </para>
    /// </remarks>
    public static Dictionary<AstFunctionExpression, FreeNameScan> ForProgram(AstNode root)
    {
        var all = new Dictionary<AstFunctionExpression, FreeNameScan>();
        var walker = new Walker(new FreeNameScan()) { All = all };
        walker.Visit(root);
        return all;
    }

    /// <summary>
    /// The free names of <paramref name="function"/> alone. A convenience over
    /// <see cref="ForProgram"/> and not a second implementation — the walk is the same one, so a
    /// defect cannot hide in the difference between them. The independent oracle these are checked
    /// against lives in the tests, where it belongs.
    /// </summary>
    public static FreeNameScan Of(AstFunctionExpression function)
        => ForProgram(function)[function];

    /// <summary>
    /// A scope: the names it binds, and whether a <c>var</c> declared inside it stops here. Only a
    /// function scope stops one; a block scope passes it up, which is what makes
    /// <c>{ var x; } x</c> bound and <c>{ let x; } x</c> free.
    /// </summary>
    private sealed class Scope(bool isFunctionScope)
    {
        public readonly HashSet<string> Bound = new(System.StringComparer.Ordinal);
        public readonly bool IsFunctionScope = isFunctionScope;
    }

    private sealed class Walker(FreeNameScan scan) : AstReduce
    {
        private readonly List<Scope> scopes = [];

        /// <summary>
        /// When set, each function's own free set is recorded here and propagated to its parent,
        /// which is what makes one pass enough.
        /// </summary>
        public Dictionary<AstFunctionExpression, FreeNameScan> All;

        private readonly List<FreeNameScan> functionScans = [];

        /// <summary>
        /// Where each function's own scopes begin in <see cref="scopes"/>. A free name is one this
        /// FUNCTION does not bind, so the search stops at its own base rather than running on into
        /// the enclosing function's scopes — which would report a captured name as bound and drop
        /// it from the set that has to box it.
        /// </summary>
        private readonly List<int> functionScopeBase = [];

        public void EnterFunction(AstFunctionExpression function)
        {
            var own = All == null ? scan : new FreeNameScan();
            functionScans.Add(own);
            functionScopeBase.Add(scopes.Count);
            scopes.Add(new Scope(isFunctionScope: true));

            // A named function expression binds its own name inside itself, which is how
            // `var f = function g () { return g; }` captures nothing.
            if (function.Id != null)
                Bind(function.Id.Name.Value);

            var parameters = function.Params.GetFastEnumerator();
            while (parameters.MoveNext(out var parameter))
            {
                // A parameter's PATTERN binds and its default INITIALIZER references, so the two
                // sides of one declarator go opposite ways.
                BindPattern(parameter.Identifier);
                if (parameter.Init != null)
                    Visit(parameter.Init);
            }

            // `arguments` is bound by every non-arrow function whether or not it is declared.
            if (!function.IsArrowFunction)
                Bind("arguments");

            // Hoisting: a `var` or a function declaration anywhere in the body is bound from
            // function entry, so a reference textually before it is still bound. Collect them
            // before walking, or `f(); function f(){}` would report `f` free.
            new Hoister(this).Visit(function.Body);

            Visit(function.Body);
            scopes.RemoveAt(scopes.Count - 1);
            functionScans.RemoveAt(functionScans.Count - 1);
            functionScopeBase.RemoveAt(functionScopeBase.Count - 1);

            if (All == null)
                return;

            All[function] = own;

            // A child's free names are references the parent has not yet resolved: it may bind
            // them itself, in which case they stop here, or pass them further up. Routing them
            // through the ordinary reference path is what makes capture transitive for free.
            var parent = functionScans.Count == 0 ? scan : functionScans[^1];
            foreach (var name in own.Free)
            {
                if (!IsBound(name))
                    parent.Free.Add(name);
            }

            if (own.Dynamic)
                parent.Dynamic = true;
        }

        internal void Bind(string name)
        {
            if (scopes.Count != 0)
                scopes[^1].Bound.Add(name);
        }

        /// <summary>Binds a <c>var</c> or function declaration at the nearest FUNCTION scope.</summary>
        internal void BindHoisted(string name)
        {
            var floor = functionScopeBase.Count == 0 ? 0 : functionScopeBase[^1];
            for (var i = scopes.Count - 1; i >= floor; i--)
            {
                if (!scopes[i].IsFunctionScope)
                    continue;

                scopes[i].Bound.Add(name);
                return;
            }
        }

        internal void BindPattern(AstNode target)
        {
            switch (target)
            {
                case null:
                    return;

                case AstIdentifier identifier:
                    Bind(identifier.Name.Value);
                    return;

                case AstArrayPattern array:
                    var elements = array.Elements.GetFastEnumerator();
                    while (elements.MoveNext(out var element))
                        BindPattern(element);
                    return;

                case AstObjectPattern pattern:
                    var properties = pattern.Properties.GetFastEnumerator();
                    while (properties.MoveNext(out var property))
                    {
                        // A computed key is an expression, not a binding: `{ [k]: v } = o` reads k.
                        if (property.Computed && property.Key != null)
                            Visit(property.Key);

                        BindPattern(property.Value ?? property.Key);
                        if (property.Init != null)
                            Visit(property.Init);
                    }

                    return;

                case AstBinaryExpression { Operator: TokenTypes.Assign } withDefault:
                    // `function (a = expr)`: the target binds, the default references.
                    BindPattern(withDefault.Left);
                    Visit(withDefault.Right);
                    return;

                case AstUnaryExpression spread:
                    BindPattern(spread.Argument);
                    return;

                default:
                    // Anything unrecognised in binding position: walk it as a reference, which
                    // can only add names to the free set.
                    Visit(target);
                    return;
            }
        }

        private bool IsBound(string name)
        {
            var floor = functionScopeBase.Count == 0 ? 0 : functionScopeBase[^1];
            for (var i = scopes.Count - 1; i >= floor; i--)
            {
                if (scopes[i].Bound.Contains(name))
                    return true;
            }

            return false;
        }

        private FreeNameScan Current => functionScans.Count == 0 ? scan : functionScans[^1];

        protected override AstNode VisitIdentifier(AstIdentifier identifier)
        {
            var name = identifier.Name.Value;
            if (!IsBound(name))
                Current.Free.Add(name);

            return identifier;
        }

        protected override AstNode VisitFunctionExpression(AstFunctionExpression function)
        {
            // A nested function's own free names are free HERE too unless this function binds
            // them — capture is transitive, and a body two levels down reaching a name one level
            // up is the shape RelayRewriteTests exists for.
            var declarationName = function.IsStatement ? function.Id?.Name.Value : null;
            if (declarationName != null)
                BindHoisted(declarationName);

            EnterFunction(function);
            return function;
        }

        protected override AstNode VisitBlock(AstBlock block)
        {
            scopes.Add(new Scope(isFunctionScope: false));
            var statements = block.Statements.GetFastEnumerator();
            while (statements.MoveNext(out var statement))
                Visit(statement);

            scopes.RemoveAt(scopes.Count - 1);
            return block;
        }

        protected override AstNode VisitVariableDeclaration(AstVariableDeclaration declaration)
        {
            var declarators = declaration.Declarators.GetFastEnumerator();
            while (declarators.MoveNext(out var declarator))
            {
                // `var` was already bound by the hoist pass at the function scope; `let`, `const`
                // and `using` bind here, in the block being walked.
                if (declaration.Kind != FastVariableKind.Var)
                    BindPattern(declarator.Identifier);

                if (declarator.Init != null)
                    Visit(declarator.Init);
            }

            return declaration;
        }

        protected override AstNode VisitTryStatement(AstTryStatement statement)
        {
            Visit(statement.Block);

            if (statement.Catch != null)
            {
                scopes.Add(new Scope(isFunctionScope: false));
                if (statement.CatchParam != null)
                    BindPattern(statement.CatchParam);

                Visit(statement.Catch);
                scopes.RemoveAt(scopes.Count - 1);
            }

            if (statement.Finally != null)
                Visit(statement.Finally);

            return statement;
        }

        protected override AstNode VisitObjectLiteral(AstObjectLiteral literal)
        {
            // Overridden because the base walks a literal's properties GENERICALLY — unlike
            // VisitObjectPattern, which routes through VisitObjectProperty explicitly — so the
            // override below never sees them and every `{ a: 1 }` key reads as a reference to `a`.
            // Caught by a fixture rather than by inspection, which is the whole reason the
            // property-name case has one.
            var properties = literal.Properties.GetFastEnumerator();
            while (properties.MoveNext(out var node))
            {
                if (node is not AstClassProperty property)
                {
                    Visit(node);
                    continue;
                }

                // A shorthand `{ a }` has no Init and IS a reference to `a`; a `{ a: v }` key is
                // not. That is the one place a property name and a reference share a node.
                if (property.Computed || property.Init == null)
                    Visit(property.Key);

                if (property.Init != null)
                    Visit(property.Init);
            }

            return literal;
        }

        protected override AstNode VisitMemberExpression(AstMemberExpression member)
        {
            Visit(member.Object);

            // `o.a` does not read a binding called `a`; `o[a]` does. Without this every property
            // name in the program joins the free set, and the deferral would box a binding for
            // each one — which is the failure mode the "over-approximation is safe" rule hides,
            // because it is safe and ruinous at the same time.
            if (member.Computed)
                Visit(member.Property);

            return member;
        }

        protected override AstNode VisitWithStatement(AstWithStatement statement)
        {
            Current.Dynamic = true;
            return base.VisitWithStatement(statement);
        }

        protected override AstNode VisitCallExpression(AstCallExpression call)
        {
            if (call.Callee is AstIdentifier { } callee && callee.Name.Equals("eval"))
                Current.Dynamic = true;

            return base.VisitCallExpression(call);
        }

        // The containers AstReduce leaves to specialized rewriters. Missing one is not a missed
        // optimization but a miscompile, so they are overridden here for the same reason
        // NestedFunctionScanner overrides them.
        protected override VariableDeclarator VisitVariableDeclarator(VariableDeclarator declarator)
        {
            Visit(declarator.Identifier);
            if (declarator.Init != null)
                Visit(declarator.Init);
            return declarator;
        }

        protected override ObjectProperty VisitObjectProperty(ObjectProperty property)
        {
            // A non-computed key is a property name, not a reference: `{ a: 1 }` does not read `a`.
            if (property.Computed && property.Key != null)
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
    }

    /// <summary>
    /// Binds the <c>var</c>s and function declarations of one function body before it is walked,
    /// without descending into nested functions — whose own <c>var</c>s belong to them.
    /// </summary>
    private sealed class Hoister(Walker walker) : AstReduce
    {
        protected override AstNode VisitFunctionExpression(AstFunctionExpression function)
        {
            // The declaration's NAME hoists into this body; its own vars do not.
            if (function.IsStatement && function.Id != null)
                walker.BindHoisted(function.Id.Name.Value);

            return function;
        }

        protected override AstNode VisitVariableDeclaration(AstVariableDeclaration declaration)
        {
            if (declaration.Kind != FastVariableKind.Var)
                return declaration;

            var declarators = declaration.Declarators.GetFastEnumerator();
            while (declarators.MoveNext(out var declarator))
                HoistPattern(declarator.Identifier);

            return declaration;
        }

        private void HoistPattern(AstNode target)
        {
            switch (target)
            {
                case AstIdentifier identifier:
                    walker.BindHoisted(identifier.Name.Value);
                    return;

                case AstArrayPattern array:
                    var elements = array.Elements.GetFastEnumerator();
                    while (elements.MoveNext(out var element))
                        HoistPattern(element);
                    return;

                case AstObjectPattern pattern:
                    var properties = pattern.Properties.GetFastEnumerator();
                    while (properties.MoveNext(out var property))
                        HoistPattern(property.Value ?? property.Key);
                    return;

                case AstBinaryExpression { Operator: TokenTypes.Assign } withDefault:
                    HoistPattern(withDefault.Left);
                    return;

                case AstUnaryExpression spread:
                    HoistPattern(spread.Argument);
                    return;
            }
        }

        protected override Case VisitCase(Case @case)
        {
            var statements = @case.Statements.GetFastEnumerator();
            while (statements.MoveNext(out var statement))
                Visit(statement);
            return @case;
        }
    }
}
