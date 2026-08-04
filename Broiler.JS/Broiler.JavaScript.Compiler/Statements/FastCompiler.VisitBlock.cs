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
                // scalar-replaceable at all, and `arguments` / `eval` keep their cell.
                var canUseRawLocal = !hoistToDirectEvalRoot
                    && scope.CanScalarReplaceLocals
                    && scope.Function != null
                    && !v.Equals("arguments")
                    && !v.Equals("eval");
                var isCaptured = scope.CapturedByNestedFunctions.Contains(v.Value);
                // The JSValue tier stays closed to lexical names: a `let`'s temporal dead zone
                // and a `const`'s read-only-ness are properties of its JSVariable cell, and
                // nothing here proves either is unobservable. The numeric tier below is
                // different — its gate proves both.
                //
                // It also stays closed to a captured name. The sharing itself would survive —
                // a captured CLR local becomes a `Box<T>` that every closure over it reads
                // through — but a JSValue tier local carries no cell, and a cell is what a
                // direct `delete` of an eval-introduced binding, an EvalShadowVariable and a
                // JSVariable's own TDZ state are. Item 3-7 widens the NUMERIC tier only,
                // where the analysis has already proved each of those unreachable.
                var useScalarLocal = canUseRawLocal && !isCaptured && !isLexical;
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
                // Item 3-7: a captured name may take the numeric tier, because a captured raw
                // local is a `Box<double>` and that IS the shared cell a closure needs — one
                // allocation instead of the `Box<JSVariable>` plus `JSVariable` pair, and no box
                // per write. What it may not do is inherit the hoisting argument: a function
                // declaration at body top level exists before the body runs, so its mention of
                // the name can execute before the name's initializer even though it is textually
                // after it, and a raw double hoisted to 0 would answer 0 where `undefined` is
                // the right answer. That conjunct is a correctness rule, so it holds on both
                // settings of the switch.
                var capturedRejectsNumeric = isCaptured
                    && (!CapturedNumericLocals.Enabled
                        || scope.CapturedByHoistedFunctions.Contains(v.Value));
                var useNumericLocal = canUseRawLocal
                    && !capturedRejectsNumeric
                    && (!isLexical || isFunctionBodyBlock)
                    && scope.NumericLocals.Contains(v.Value);
                // Item 3-6: why this name did or did not reach the numeric tier, attributed to
                // the FIRST conjunct it fails. Item 3-5 measured the coverage at 5.0% of scalar
                // locals and could not say which condition costs it; this is that count, and
                // 3-6's own instruction is to take it before designing anything.
                CompilerSpecializationDiagnostics.RecordNumericLocalDecision(
                    useNumericLocal ? NumericLocalRejection.Accepted
                    : hoistToDirectEvalRoot ? NumericLocalRejection.DirectEvalRoot
                    : !scope.CanScalarReplaceLocals ? NumericLocalRejection.FunctionNotScalarReplaceable
                    : scope.Function == null ? NumericLocalRejection.NotInAFunction
                    : v.Equals("arguments") || v.Equals("eval") ? NumericLocalRejection.ArgumentsOrEval
                    // Split so the OFF arm reports which half of the captured names a widening
                    // could ever reach — the hoisted half can never be reached, because that
                    // conjunct is correctness. In the ON arm capture alone stops rejecting, so
                    // `capturedRejectsNumeric` is false for the other half and it falls through
                    // to whatever really refuses it.
                    : isCaptured && scope.CapturedByHoistedFunctions.Contains(v.Value) ? NumericLocalRejection.CapturedByHoistedFunction
                    : capturedRejectsNumeric ? NumericLocalRejection.CapturedByNestedFunction
                    : isLexical && !isFunctionBodyBlock ? NumericLocalRejection.LexicalOutsideFunctionBody
                    : NumericLocalRejection.NotProvenNumeric);

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
