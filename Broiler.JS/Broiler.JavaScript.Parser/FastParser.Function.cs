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
        var begin = stream.Current;
        node = default;
        stream.Consume();
        var generator = false;

        if (stream.CheckAndConsume(TokenTypes.Multiply))
            generator = true;

        if (Identitifer(out var id))
        {
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

        if (!Parameters(out var declarators, TokenTypes.BracketEnd, false, FastVariableKind.Var))
            throw stream.Unexpected();

        if (!stream.CheckAndConsume(TokenTypes.CurlyBracketStart))
            throw stream.Unexpected();

        try
        {
            functionDepth++;
            var previousInGeneratorBody = inGeneratorBody;
            var previousInAsyncFunctionBody = inAsyncFunctionBody;
            inGeneratorBody = generator;
            inAsyncFunctionBody = isAsync;
            try
            {
                if (!Block(out var body))
                    throw stream.Unexpected();

                node = new AstFunctionExpression(begin, PreviousToken, false, isAsync, generator, id, declarators, body, isStatement);
            }
            finally
            {
                inGeneratorBody = previousInGeneratorBody;
                inAsyncFunctionBody = previousInAsyncFunctionBody;
                functionDepth--;
            }
        }
        finally
        {
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

        // Find the nearest enclosing function-body / program-body scope (the var
        // environment): a Block whose parent is a function or the script root.
        var target = top;
        while (target != null
            && !(target.NodeType == FastNodeType.Block
                && (target.Parent == null || target.Parent.NodeType == FastNodeType.FunctionExpression)))
        {
            target = target.Parent;
        }

        if (target == null || target == top)
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
