using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;

namespace Broiler.JavaScript.Parser;


partial class FastParser
{
    bool Function(out AstStatement statement, bool isAsync = false)
    {
        if (!FunctionExpression(out var expression, isAsync, isStatement: true))
        {
            statement = default;
            return false;
        }

        statement = new AstExpressionStatement(expression);
        return true;
    }

    bool FunctionExpression(out AstExpression node, bool isAsync = false, bool isStatement = false)
    {
        bool isRootAsync = this.isAsync;
        // For `async function …` the caller consumed `async` and recorded its token so
        // the reported source text (Function.prototype.toString) begins at `async`.
        var begin = isAsync && pendingAsyncStart != null ? pendingAsyncStart : stream.Current;
        pendingAsyncStart = null;
        node = default;
        stream.Consume();
        var generator = false;

        if (stream.CheckAndConsume(TokenTypes.Multiply))
            generator = true;

        if (Identitifer(out var id))
        {
            // `yield` as the function's BindingIdentifier is a SyntaxError when it is
            // bound in a [+Yield] context. For a function/generator DECLARATION the name
            // is bound in the enclosing scope, so it is illegal when the enclosing body
            // is a generator (`inGeneratorBody` here is still the enclosing context). For
            // a function EXPRESSION (NFE) the name is bound in the function's own scope,
            // so it is illegal only when this function is itself a generator. (Strict
            // mode is handled separately.)
            if (id.Start.Keyword == FastKeywords.yield && (isStatement ? inGeneratorBody : generator))
                throw new FastParseException(id.Start, "yield cannot be used as a function name in this context");

            // BROILER-PATCH: For function declarations, add name to parent scope (hoisted).
            // For function expressions, do NOT add to parent scope (ES3 §13).
            if (isStatement)
            {
                // Annex B 3.3: a function declaration nested in a block/switch also
                // gets a var-scoped binding in the enclosing function/program body.
                // Register the candidate before adding the block-scoped Let binding
                // so the conflict check does not see this declaration's own name.
                RegisterAnnexBFunctionHoisting(id.Name);

                // Annex B.3.4: when this declaration is the sole statement of an
                // `if` clause it is scoped to its own implicit block, so it forms
                // no lexical binding in the enclosing block/switch (only the Annex B
                // var-hoisting registered above). Skip the enclosing-scope binding
                // entirely; the compiler materialises it via
                // VisitRuntimeFunctionDeclaration. Capture and clear the flag before
                // parsing the body so declarations nested inside it are unaffected.
                var isIfClause = nestedFunctionClause;
                nestedFunctionClause = false;

                if (!isIfClause)
                {
                    // A FunctionDeclaration at the top level of a function body,
                    // script, or eval body is var-scoped: it contributes to
                    // VarDeclaredNames (not LexicallyDeclaredNames), so duplicate
                    // function declarations and a same-named `var` are allowed (the
                    // last declaration wins). Only a block/switch-nested declaration
                    // is lexical and conflicts on redeclaration.
                    var top = variableScope.Top;
                    var kind = top.NodeType == FastNodeType.Block
                        && (top.Parent == null || top.Parent.NodeType == FastNodeType.FunctionExpression)
                        ? FastVariableKind.Var
                        : FastVariableKind.Let;
                    top.AddVariable(id.Start, id.Name, kind);
                }
            }
        }

        stream.Expect(TokenTypes.BracketStart);
        var scope = variableScope.Push(begin, FastNodeType.FunctionExpression);

        var previousInGeneratorBody = inGeneratorBody;
        var previousInAsyncFunctionBody = inAsyncFunctionBody;
        try
        {
            functionDepth++;
            // FormalParameters are parsed in THIS function's [Yield]/[Await]
            // context, not the enclosing one. So `yield`/`await` in a parameter
            // default belong to the inner function's grammar: a non-generator
            // function nested in a generator treats `yield` in its parameters as a
            // plain identifier (sloppy mode), not an (illegal) YieldExpression.
            // Setting the context before Parameters also keeps it set across the
            // body's `{`-consume below, which the scanner's one-token lookahead
            // needs for the regex-vs-division decision after the first yield/await.
            inGeneratorBody = generator;
            inAsyncFunctionBody = isAsync;

            if (!Parameters(out var declarators, TokenTypes.BracketEnd, false, FastVariableKind.Var))
                throw stream.Unexpected();

            if (!stream.CheckAndConsume(TokenTypes.CurlyBracketStart))
                throw stream.Unexpected();

            if (!Block(out var body))
                throw stream.Unexpected();

            node = new AstFunctionExpression(begin, PreviousToken, false, isAsync, generator, id, declarators, body, isStatement);
        }
        finally
        {
            inGeneratorBody = previousInGeneratorBody;
            inAsyncFunctionBody = previousInAsyncFunctionBody;
            functionDepth--;
            scope.Dispose();
            this.isAsync = isRootAsync;
        }
        return true;
    }

    // Annex B 3.3: when a FunctionDeclaration appears nested inside a block or
    // switch (not directly at the top level of a function/program body), it also
    // introduces a var-scoped binding in the enclosing function/program body —
    // unless doing so would conflict with a lexical (let/const/class) binding in
    // any intervening scope. We record the candidate name on the body scope; the
    // compiler creates the binding (sloppy mode only) so reads before the
    // declaration resolve to undefined instead of throwing.
    private void RegisterAnnexBFunctionHoisting(in StringSpan name)
    {
        var top = variableScope.Top;

        // Annex B.3.4: when this declaration is the sole statement of an `if`
        // clause it is conceptually wrapped in an implicit block one level below
        // `top`. So even when `top` is itself the body scope (e.g.
        // `if (x) function f(){}` directly in a function/program body) the
        // function still needs an Annex B var binding in that body.
        var isIfClause = nestedFunctionClause;

        // Find the nearest enclosing function-body / program-body scope (the var
        // environment): a Block whose parent is a function or the script root.
        var target = top;
        while (target != null
            && !(target.NodeType == FastNodeType.Block
                && (target.Parent == null || target.Parent.NodeType == FastNodeType.FunctionExpression)))
        {
            target = target.Parent;
        }

        if (target == null)
            return;

        // A plain top-level function declaration (target == top) is already
        // var/globally scoped and needs no Annex B hoisting. An if-clause
        // function whose implicit block IS the body top level is the exception
        // handled above.
        if (target == top && !isIfClause)
            return;

        // Annex B.3.3.1 / B.3.3.2: do not apply Annex B var-hoisting when the
        // name is also a formal parameter of the enclosing function. The
        // parameter binding takes precedence and must not be shadowed by a
        // hoisted function-scope var. (target.Parent, when it is the function
        // scope, holds exactly the formal parameter names.)
        if (target.Parent is { NodeType: FastNodeType.FunctionExpression } funcScope
            && funcScope.DeclaresVariable(name))
            return;

        // Blocked if any scope between the declaration and the body scope
        // (inclusive) already declares the name lexically.
        var s = top;
        while (true)
        {
            if (s.HasLexicalBinding(name))
                return;

            if (s == target)
                break;

            s = s.Parent;
        }

        target.AddAnnexBName(name);
    }
}
