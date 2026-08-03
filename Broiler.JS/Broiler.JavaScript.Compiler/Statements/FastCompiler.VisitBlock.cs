using System;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override BExpression VisitBlock(AstBlock block)
    {
        int count = block.Statements.Count;
        if (count == 0)
            return BExpression.Empty;

        var blockList = new Sequence<BExpression>(count);
        var hoistingScope = block.HoistingScope;
        var scope = this.scope.Push(new FastFunctionScope(this.scope.Top));
        var lexicalBindings = CollectTopLevelLexicalBindings(block.Statements);
        // A block-level FunctionDeclaration is lexically block-scoped (its value is
        // hoisted to the top of THIS block via PostInit) in addition to any Annex B var
        // binding it copies out to. Under direct eval that block binding is what makes a
        // call before the textual declaration resolve to the function; without it the
        // name was routed straight to the eval var environment (no block-top hoist), so
        // `eval('{ g(); function g(){} }')` saw `g` undefined (#912). Give such names a
        // block-scoped binding here, exactly as in non-eval compilation.
        var blockFunctionNames = CollectBlockFunctionDeclarationNames(block.Statements);

        // Annex B 3.3 (sloppy mode): consume the function-scope var bindings handed
        // down from CreateFunction for this function body block. Cleared immediately
        // so nested blocks do not inherit them.
        var annexBFunctionNames = pendingAnnexBFunctionNames;
        pendingAnnexBFunctionNames = null;
        if (annexBFunctionNames != null)
        {
            var annexBEn = annexBFunctionNames.GetFastEnumerator();
            while (annexBEn.MoveNext(out var annexBName))
            {
                if (lexicalBindings.Contains(annexBName.Value))
                    continue;

                // Mark as a var (non-lexical) binding so the declaration site's
                // Annex B outer-binding assignment is not blocked by
                // IsAnnexBHoistingBlocked.
                var created = scope.CreateVariable(annexBName, null, true, initialize: true);
                created.IsLexical = false;
            }
        }

        if (hoistingScope != null)
        {
            var en = hoistingScope.GetFastEnumerator();
            while (en.MoveNext(out var v))
            {
                var isLexical = lexicalBindings.Contains(v.Value);

                // A `var arguments` in an ordinary (non-arrow) function shares the
                // function's own `arguments` binding rather than a fresh block-scope
                // var — the binding is initialized to the arguments object, and a
                // later `var arguments = x` (which resolves up to this same binding)
                // overrides it. Without this the declaration created a separate slot
                // while reads kept resolving to the materialized arguments object
                // (test262 language/statements/function/S13_A15_T2). A block-level
                // `function arguments(){}` is the opposite: it is lexically block-scoped
                // and must NOT touch the arguments object (which survives the block per
                // Annex B), so it is excluded here and given an ordinary block binding.
                if (!isLexical
                    && v.Equals("arguments")
                    && !blockFunctionNames.Contains(v.Value)
                    && scope.RootScope.Function is { IsArrowFunction: false } rootFunction)
                {
                    // With a non-simple parameter list the function has a separate parameter
                    // environment (FunctionDeclarationInstantiation §10.2.11): the parameter-env
                    // `arguments` object is what parameter-initializer closures capture, while a
                    // body `var arguments` is a DISTINCT var-environment binding initialised to a
                    // copy of that object — writes to it must NOT be observed through the captured
                    // one (test262 sm/Function/arguments-parameter-shadowing). Create that binding
                    // in this (body-block) scope, seeded from the materialised parameter-env
                    // arguments object; body references and assignments resolve to it ahead of the
                    // function-root binding (TryGetBlockScopedArguments / GetVariable). With a simple
                    // parameter list there is a single environment, so the `var` shares the
                    // function's own arguments binding (S13_A15_T2).
                    if (!HasSimpleParameterList(rootFunction.Params))
                    {
                        var parameterArguments = MaterializeArgumentsBinding();
                        scope.CreateVariable("arguments", parameterArguments.Expression, newScope: true);
                    }
                    else
                    {
                        MaterializeArgumentsBinding();
                    }
                    continue;
                }

                var isEvalRootHoist = isDirectEvalCompilation
                    && scope.Function == null
                    && !IsStrictMode
                    && !isLexical
                    && !IsAnnexBHoistingBlocked(v);

                // An eval block-level FunctionDeclaration needs BOTH bindings: the eval
                // var-environment binding (what references AFTER the block resolve to, and
                // the target the declaration's Annex B copy-out updates — so across several
                // blocks the last declaration wins) AND a block-scoped binding that the
                // function value is hoisted into at the block top (so a reference BEFORE the
                // textual declaration inside the block resolves to the function, #912). The
                // block binding shadows the var binding for in-block references and is
                // mutable/distinct, so a self-reassignment stays block-local.
                if (isEvalRootHoist && blockFunctionNames.Contains(v.Value))
                {
                    GetOrCreateDirectEvalRootVariable(v);
                    var blockFn = scope.CreateVariable(v, null, newScope: true, initialize: true);
                    blockFn.IsLexical = false;
                    continue;
                }

                var hoistToDirectEvalRoot = isEvalRootHoist;
                // The preconditions both raw-local tiers share: the function's locals must be
                // scalar-replaceable at all, and a name a closure, `arguments` or `eval` can
                // reach keeps its cell.
                var canUseRawLocal = !hoistToDirectEvalRoot
                    && scope.CanScalarReplaceLocals
                    && scope.Function != null
                    && !v.Equals("arguments")
                    && !v.Equals("eval")
                    // A closure captures through a cell, so a name any nested function
                    // mentions keeps one; the rest of the function's vars need not.
                    && !scope.CapturedByNestedFunctions.Contains(v.Value);
                // The JSValue tier stays closed to lexical names: a `let`'s temporal dead zone
                // and a `const`'s read-only-ness are properties of its JSVariable cell, and
                // nothing here proves either is unobservable. The numeric tier below is
                // different — its gate proves both.
                var useScalarLocal = canUseRawLocal && !isLexical;
                // A name the analysis proved numeric lives in a raw double. Its hoisted value
                // is 0 rather than undefined, which is only sound because the analysis
                // rejected any name that can be READ before its initializer runs — which is
                // also what discharges a lexical name's temporal dead zone, so `let`/`const`
                // reach this tier even though they do not reach the JSValue one above (their
                // TDZ and const-ness are properties of the cell, and the numeric tier's gate
                // proves the TDZ unreachable and rejects any const that is written).
                //
                // A lexical name qualifies only in the function's OWN body block: `let` in a
                // nested block is a distinct binding per entry, while NumericLocals — which
                // every nested block scope inherits from the function — names only
                // body-top-level declarations. Without this test a nested `{ let v = … }`
                // would take the function's slot (docs/performance-roadmap.md item 3-3).
                var isFunctionBodyBlock = ReferenceEquals(block, scope.Function?.Body);
                var useNumericLocal = canUseRawLocal
                    && (!isLexical || isFunctionBodyBlock)
                    && scope.NumericLocals.Contains(v.Value);
                var variable = hoistToDirectEvalRoot
                    ? GetOrCreateDirectEvalRootVariable(v)
                    : useNumericLocal
                        ? scope.CreateVariable(v, BExpression.Constant(0d), true, typeof(double), initialize: true)
                        : useScalarLocal
                            ? scope.CreateVariable(v, JSUndefinedBuilder.Value, true, typeof(JSValue), initialize: true)
                            : scope.CreateVariable(v, null, true, initialize: isLexical == false);
                variable.IsLexical = isLexical;
                if (hoistToDirectEvalRoot && directEvalBindingNames != null && Array.IndexOf(directEvalBindingNames, v.Value) >= 0)
                    variable.Expression = JSContextBuilder.Index(KeyOfName(v));
            }
        }

        var se = block.Statements.GetFastEnumerator();
        while (se.MoveNext(out var stmt))
        {
            var exp = Visit(stmt);
            if (exp == null)
                continue;

            blockList.Add(CallStackItemBuilder.Step(scope.Context, scope.StackItem, stmt.Start.Start.Line, stmt.Start.Start.Column));
            blockList.Add(exp);
        }

        var result = Scoped(scope, blockList);

        scope.Dispose();
        return result;
    }
}
