using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Parser;


partial class FastParser
{
    /// <summary>
    /// Number of <c>continue</c> statements parsed so far by this parser instance, incremented in
    /// exactly one place (FastParser.Statement.cs's <c>Continue</c>). A for-statement snapshots it
    /// around its body parse: an unchanged count proves the body holds no <c>continue</c> at all, in
    /// any statement position, and the loop records the answer as its <c>ContainsContinue</c>. The
    /// compiler's for-loop pass reads it to let the `for (let …)` lowering skip a per-iteration
    /// try/finally. An AST search would be the obvious alternative, but a single missed
    /// <c>continue</c> there turns a working loop into an infinite one, and the shared AST walkers do
    /// not reach every statement position (a <c>switch</c> case's statements, for one).
    /// </summary>
    /// <remarks>
    /// The parser builds every loop as written. Rewriting a <c>let</c>, <c>const</c> or <c>using</c>
    /// head into per-iteration carriers is the compiler's preparation pass, <c>ForLoopPreparation</c>
    /// (milestone SJL-12.5, item 5; Broiler.VM's JSD-0080, decision D22), which also names the
    /// temporaries, so the parser holds no counter for them.
    /// </remarks>
    private int ParsedContinueCount;

    /// <summary>
    /// For ( in
    /// For ( of
    /// For await ( // not supported yet...
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    bool ForStatement(out AstStatement node)
    {
        var begin = stream.Current;
        stream.Consume();

        var awaitOf = stream.CheckAndConsume(FastKeywords.await);

        // `for await` is only valid where an AwaitExpression is — inside an async function body,
        // or at the top level of a module / TLA-enabled eval. In an ordinary script or a
        // non-async function it is a SyntaxError (test262 sm/AsyncGenerators/for-await-of-error);
        // without this guard the `await` reaches the compiler as an unsupported top-level await
        // and surfaces as an internal error rather than a SyntaxError.
        if (awaitOf && !(inAsyncFunctionBody || (functionDepth == 0 && allowTopLevelAwait)))
            throw new FastParseException(begin, "for await (... of ...) is only valid in async functions and modules");

        // A top-level `for await` awaits, so the program is async exactly as a top-level
        // AwaitExpression makes it (see AwaitExpression, which sets the same flag). The flag is
        // what routes the program through the generator rewrite; without it the program was
        // compiled straight to IL and the await this loop emits reached the IL generator as a
        // raw yield node, which it cannot emit (NotImplementedException in VisitYield).
        if (awaitOf && functionDepth == 0)
            isAsync = true;

        stream.Expect(TokenTypes.BracketStart);

        AstNode? beginNode;

        AstVariableDeclaration? declaration = null;
        var scope = variableScope.Push(begin, FastNodeType.ForStatement);

        try
        {
            var @in = false;
            var of = false;

            // Line terminators are insignificant inside the for-head parens.
            // Skip any that follow "(" so a declaration keyword starting on the
            // next line (e.g. `for (\n let [x] in obj)`) is still recognised as
            // a keyword rather than mis-parsed as an identifier expression.
            stream.SkipNewLines();
            var current = stream.Current;

            if (current.IsKeyword
                && (current.Keyword is FastKeywords.var or FastKeywords.@const
                    // `let` heads a ForDeclaration only when a BindingList follows; a bare
                    // `let` (e.g. `for (let; ;)`, `for (let = 1; ;)`, `for (let in obj)`) is
                    // an IdentifierReference and flows through the expression path below.
                    || (current.Keyword is FastKeywords.let && LetBeginsLexicalDeclaration())))
            {
                // Disable `in`/`of` as binary operators while parsing the
                // variable declaration so that `for (var x = 3 in obj)` is
                // parsed as a for-in loop with a binding initializer, not
                // as `for (var x = (3 in obj); …)`.
                considerInOfAsOperators = false;
                switch (current.Keyword)
                {
                    case FastKeywords.let:
                        if (!VariableDeclarationStatement(out declaration, FastVariableKind.Let))
                            throw stream.Unexpected();
                        beginNode = declaration;
                        break;

                    case FastKeywords.@const:
                        if (!VariableDeclarationStatement(out declaration, FastVariableKind.Const))
                            throw stream.Unexpected();
                        beginNode = declaration;
                        break;

                    case FastKeywords.var:
                        if (!VariableDeclarationStatement(out declaration))
                            throw stream.Unexpected();
                        beginNode = declaration;
                        break;

                    default:
                        throw stream.Unexpected();
                }
                considerInOfAsOperators = true;
            }
            else if (TryParseForUsingDeclaration(out declaration))
            {
                // `for (using x of …)` / `for (await using x of …)`: a lexical,
                // disposed-per-iteration binding (only valid in a for-of head).
                beginNode = declaration;
            }
            else if (TryParseForUsingLexicalDeclaration(out declaration))
            {
                // C-style `for (using x = …; …; …)` / `for (await using x = …; …; …)`:
                // a LexicalDeclaration whose `using` / `await using` bindings are disposed
                // once when the loop's lexical environment is torn down (the compiler's
                // for-loop pass keeps the declaration intact instead of desugaring it, and
                // wraps the loop in the block whose scope disposes them).
                beginNode = declaration;
            }
            else if (current.Keyword == FastKeywords.async && TryParseAsyncForOfTarget(out var asyncTarget))
            {
                beginNode = asyncTarget;
            }
            else if (ExpressionList(out var expressions))
            {
                beginNode = expressions;
            }
            else throw stream.Unexpected();

            // Line terminators are insignificant inside the for-head parens, so a
            // newline between the head binding and the `in`/`of` keyword must not
            // hide it (e.g. `for (let a\n of obj)`). Skip any here so the
            // contextual `of`/`in` keyword is recognised rather than parsed as an
            // identifier expression.
            stream.SkipNewLines();

            AstExpression? inTarget = null;
            AstExpression? ofTarget = null;
            AstExpression? test = null;
            AstExpression? update = null;

            if (IsEscapedKeyword(stream.Current, "in") || IsEscapedKeyword(stream.Current, "of"))
                throw new FastParseException(stream.Current, "Keyword must not contain escaped characters");

            if (stream.CheckAndConsume(TokenTypes.In))
            {
                if (awaitOf)
                    throw stream.Unexpected();

                // Validate for-in binding restrictions
                if (declaration != null)
                {
                    ValidateForInOfDeclaration(declaration, isOf: false);
                }
                else
                {
                    beginNode = ReinterpretForHeadTarget(beginNode);
                }

                @in = true;

                // for-in's right-hand side is an `Expression` (the full comma
                // production), so `for (x in a, b)` is valid; ExpressionSequence
                // also consumes the closing ")". (for-of, below, is restricted to
                // a single AssignmentExpression and must NOT accept a comma.)
                if (!ExpressionSequence(out inTarget, TokenTypes.BracketEnd))
                    throw stream.Unexpected();
            }
            else if (IsOfKeyword(stream.Current))
            {
                stream.Consume();

                // Validate for-of binding restrictions
                if (declaration != null)
                {
                    ValidateForInOfDeclaration(declaration, isOf: true);
                }
                else
                {
                    beginNode = ReinterpretForHeadTarget(beginNode);
                }

                of = true;

                if (!Expression(out ofTarget))
                    throw stream.Unexpected();

                stream.Expect(TokenTypes.BracketEnd);
            }
            else if (ExpressionSequence(out test, out var testTerminator, TokenTypes.SemiColon, true))
            {
                // `for ( Init_opt ; Test_opt ; Update_opt )` — the head has exactly two `;`,
                // and each omitted clause is omitted, not absent. ExpressionSequence reports an
                // omitted clause and a missing one alike as an AstEmptyExpression, so the
                // terminator it actually consumed is the only thing that separates them: a head
                // carrying a single `;` (`for (;)`, `for (a;)`, `for (;a)`) used to parse as
                // though the update clause had simply been left out — i.e. as `for (;;)`, an
                // infinite loop — instead of raising the SyntaxError it is. WPT's
                // window.onerror compile-error tests (html/webappapis/scripting/processing-model-2)
                // are literally `for(;) {}`, and every one of them hung the WPT runner until it
                // hit the per-test timeout rather than failing to compile.
                if (testTerminator != TokenTypes.SemiColon)
                    throw stream.Unexpected();

                // A C-style for-head declaration (`for (const x = 0; …)`) is an
                // ordinary LexicalDeclaration/VariableDeclaration: a `const` binding
                // or a destructuring pattern must carry an initializer. (for-in/of
                // ForBindings are exempt and validated above.)
                if (declaration != null)
                    ValidateDeclaratorInitializers(declaration);

                // NOTE: do not reject a test clause whose AST happens to end in a
                // `)` token (`for (; (a, b); …)`, `for (; x && (a, b); …)`). A
                // parenthesised expression / sequence is a valid test, and its
                // End token is the closing `)` — that is not a malformed head.

                if (test.Type == FastNodeType.EmptyExpression)
                    test = null;

                if (!ExpressionSequence(out update, out var updateTerminator, TokenTypes.BracketEnd, true))
                    throw stream.Unexpected();

                // The update clause closes the head, so it must end on the `)` — not on a third
                // `;` (`for (;;;)`) and not by running out of tokens (`for (;;`).
                if (updateTerminator != TokenTypes.BracketEnd)
                    throw stream.Unexpected();

                if (update.Type == FastNodeType.EmptyExpression)
                    update = null;
            }
            // `stream.Unexpected()` builds the exception, it does not raise it; leaving the
            // `throw` off let a head no branch above could parse fall through into the body
            // parse with a null init/test/update.
            else throw stream.Unexpected();

            // `for await` is only valid with a for-of head. A for-in head with `await` is
            // rejected above; a C-style `for await (;;)` / `for await (init; test; update)`
            // head reaches here with of == false and is a SyntaxError.
            if (awaitOf && !of)
                throw stream.Unexpected();

            // Snapshot before the body is parsed, and compared after it, to learn whether the body
            // can `continue`; the loop records the answer. The head's test/update are already parsed,
            // so a `continue` buried in a closure there cannot pollute the count — and nested loops
            // each capture their own snapshot.
            var continuesBeforeBody = ParsedContinueCount;

            // The loop is built as written: the head's declaration, test and update, and the body.
            // The compiler's for-loop pass (ForLoopPreparation) rewrites a let/const/using head into
            // per-iteration carriers, names the head's TDZ bindings, and wraps a C-style `using`
            // loop in the block that disposes its resources, before SyntaxValidation reads the tree.
            AstStatement statement;
            if (stream.CheckAndConsume(TokenTypes.CurlyBracketStart))
            {
                if (!Block(out var block))
                    throw stream.Unexpected();

                statement = block;
            }
            else if (NonDeclarativeStatement(out statement))
            {
                // A LabelledStatement whose item is a FunctionDeclaration is never a
                // legal loop body (`for (…) label: function f(){}`), in either strict
                // or sloppy mode. SyntaxValidation catches this for a non-lexical head,
                // but a let/const/using for-in/for-of/C-style head is rewritten by the
                // compiler's for-loop pass into a synthetic per-iteration block before
                // validation, hiding the raw labelled statement from the validator — so
                // reject it here, on the body as written.
                ThrowIfLabelledFunctionLoopBody(statement);
            }
            else throw stream.Unexpected();

            var containsContinue = ParsedContinueCount != continuesBeforeBody;

            if (@in)
            {
                node = new AstForInStatement(begin, PreviousToken, beginNode, inTarget, statement)
                {
                    ContainsContinue = containsContinue
                };
                scope.GetVariables();

                return true;
            }

            if (of)
            {
                node = new AstForOfStatement(begin, PreviousToken, beginNode, ofTarget, statement, awaitOf)
                {
                    ContainsContinue = containsContinue
                };
                scope.GetVariables();

                return true;
            }

            node = new AstForStatement(begin, PreviousToken, beginNode, test, update, statement)
            {
                ContainsContinue = containsContinue
            };
            scope.GetVariables();
        }
        finally
        {
            scope.Dispose();
        }

        return true;

        static void ValidateForInOfDeclaration(AstVariableDeclaration declaration, bool isOf)
        {
            int count = 0;
            bool hasInit = false;
            AstExpression binding = null;
            var en = declaration.Declarators.GetFastEnumerator();
            while (en.MoveNext(out var d))
            {
                count++;
                binding = d.Identifier;
                if (d.Init != null)
                    hasInit = true;
            }

            // A `using` / `await using` declaration is a valid for-of ForDeclaration but never a
            // for-in one (the for-in grammar admits only var/let/const) — `for (using x in obj)` is
            // a SyntaxError. (A C-style `for (using x = …; …; …)` head never reaches here.)
            if (!isOf && declaration.Using)
                throw new FastParseException(declaration.Start, "'using' declarations are not allowed in a for-in loop head");

            // for-in/for-of must have exactly one binding
            if (count != 1)
                throw new FastParseException(declaration.Start, "Invalid left-hand side in for-in/for-of loop");

            // Initializer is always forbidden in for-of; forbidden for let/const in for-in
            if (isOf && hasInit)
                throw new FastParseException(declaration.Start, "for-of loop variable declaration may not have an initializer");

            if (!isOf && hasInit && declaration.Kind != FastVariableKind.Var)
                throw new FastParseException(declaration.Start, "for-in loop variable declaration may not have an initializer");

            // Annex B 3.5 only tolerates a for-in `var` initializer for a *simple*
            // BindingIdentifier (`for (var x = init in obj)`). A BindingPattern with an
            // initializer (`for (var [a] = 0 in obj)`) is never permitted — it is an
            // early SyntaxError in both strict and non-strict code. (The strict-mode
            // rejection of the identifier form is enforced by SyntaxValidation, which
            // knows the strictness.)
            if (!isOf && hasInit && declaration.Kind == FastVariableKind.Var
                && binding != null && binding.Type != FastNodeType.Identifier)
                throw new FastParseException(declaration.Start, "for-in loop variable declaration may not have an initializer");
        }

        // `async of` at the head of a for-of loop is ambiguous with an async arrow
        // (`async of => …`). The grammar forbids it as the LHS of a sync for-of
        // ([lookahead ∉ { async of }]) but permits it in `for await`, where `async`
        // is the loop target and `of` the for-of keyword. The async-arrow lookahead
        // would otherwise consume `async of` as arrow parameters and fail, so resolve
        // it here. Only the bare `async of` form (not `async of => …`) is intercepted;
        // a real async arrow keeps `=>` after `of` and is left to ExpressionList.
        bool TryParseAsyncForOfTarget(out AstNode target)
        {
            target = null;
            var asyncToken = stream.Current;
            stream.Consume();
            if (!IsOfKeyword(stream.Current))
            {
                stream.Reset(asyncToken);
                return false;
            }

            // `async of => …` is an async arrow function whose single parameter is
            // named `of` — i.e. the init expression of a C-style `for`, not a for-of
            // head. Peek past `of` for the `=>`: when present, leave the whole
            // expression to ExpressionList rather than treating `async` as a for-of
            // target (which would wrongly reject `for (async of => {}; ; )`).
            var ofToken = stream.Current;
            stream.Consume();
            var followedByArrow = stream.Current.Type == TokenTypes.Lambda;
            stream.Reset(ofToken);

            if (followedByArrow)
            {
                stream.Reset(asyncToken);
                return false;
            }

            if (!awaitOf)
                throw new FastParseException(asyncToken, "'async' is not allowed as the left-hand side of a for-of loop");

            target = new AstIdentifier(asyncToken);
            return true;
        }

        // A `using` ForBinding is only a declaration at the head of a for-of loop. It is
        // recognised when `using` (optionally preceded by `await`) is followed (no
        // LineTerminator) by a BindingIdentifier that is not the contextual `of` keyword and
        // the `of` keyword follows the single binding — so `for (using of x)` /
        // `for (using in x)` / `for (using; ;)` keep `using` as an ordinary
        // IdentifierReference, and an initializer / for-in head (`for (using x = …)` /
        // `for (using x in …)`) is left to fail as the SyntaxError it is. The
        // `await using x of …` form is an async-disposed for-of binding: each iteration's
        // resource is async-disposed at the end of its iteration block.
        bool TryParseForUsingDeclaration(out AstVariableDeclaration declaration)
        {
            declaration = null;
            var start = stream.Current;
            var isAwait = false;

            if (start.Keyword == FastKeywords.await)
            {
                // `await using x of …`: only a using ForBinding when `using` follows; else
                // leave `await` to the expression path.
                if (stream.Next.Keyword != FastKeywords.@using)
                    return false;

                isAwait = true;
                stream.Consume(); // await
            }
            else if (start.Keyword != FastKeywords.@using)
            {
                return false;
            }

            stream.Consume(); // using

            var bindingToken = stream.Current;
            // A plain `using` followed by `of` is never a declaration — `for (using of …)`
            // keeps `using` as an IdentifierReference and `of` as the for-of keyword (so
            // `for (using of of x)` is `using` of `of[…]`). But `await using` unambiguously
            // begins a declaration, so the contextual `of` is a valid BindingIdentifier
            // there: `for (await using of of x)` binds `of` and iterates `x`.
            if (bindingToken.Type != TokenTypes.Identifier || (!isAwait && IsOfKeyword(bindingToken)))
            {
                stream.Reset(start);
                return false;
            }

            stream.Consume(); // BindingIdentifier
            stream.SkipNewLines();
            if (!IsOfKeyword(stream.Current))
            {
                stream.Reset(start);
                return false;
            }

            // Outside every function an `await using` makes the program async, as a top-level
            // AwaitExpression does (see Using).
            if (isAwait && functionDepth == 0)
                isAsync = true;

            var declarator = new VariableDeclarator(new AstIdentifier(bindingToken), null);
            declaration = new AstVariableDeclaration(start, PreviousToken, declarator,
                FastVariableKind.Const, @using: true, await: isAwait);
            return true;
        }

        // The C-style / for-in head for a `using` family declaration. A `using` / `await using`
        // LexicalDeclaration is permitted in a C-style for head (`for (using x = …; …; …)` /
        // `for (await using x = …; …; …)`), where its resources are disposed when the loop's
        // lexical environment is torn down. A bare `using` ForBinding at the head of a for-of loop
        // is handled by TryParseForUsingDeclaration above; a for-in `for (using x in …)` head is a
        // SyntaxError (using is not a valid for-in ForDeclaration) — it is rejected below when the
        // declarator parse, run with the [~In] grammar, fails to reach the head's `;`. Recognised
        // when `using` (optionally preceded by `await`) is followed — with no intervening
        // LineTerminator — by a BindingIdentifier the for-of path did not claim. `for (using of x)` /
        // `for (using; ;)` / `for (using in x)` keep `using` as an ordinary IdentifierReference and
        // flow through the expression path instead.
        bool TryParseForUsingLexicalDeclaration(out AstVariableDeclaration declaration)
        {
            declaration = null;
            var start = stream.Current;
            var isAwait = false;

            if (start.Keyword == FastKeywords.await)
            {
                if (stream.Next.Keyword != FastKeywords.@using)
                    return false;

                isAwait = true;
                stream.Consume(); // await
            }
            else if (start.Keyword != FastKeywords.@using)
            {
                return false;
            }

            stream.Consume(); // using

            var bindingToken = stream.Current;
            if (bindingToken.Type != TokenTypes.Identifier)
            {
                stream.Reset(start);
                return false;
            }

            // `using of …` (no initializer): `using` is an IdentifierReference and the for-of
            // path owns it (`for (using of x)`).
            if (IsOfKeyword(bindingToken) && stream.Next.Type != TokenTypes.Assign)
            {
                stream.Reset(start);
                return false;
            }

            // `in`/`of` are not operators inside the LexicalDeclaration's initializers
            // (the head uses the [~In] grammar), so `for (using x = a in b; …)` keeps
            // `in` for the for-head rather than folding it into the initializer expression.
            // A for-in head `for (using x in …)` reaches here too; with `in` non-operator and
            // each `using` binding requiring an initializer, the declarator parse fails to land on
            // the head's `;` and surfaces a SyntaxError below — using is not a for-in ForDeclaration.
            considerInOfAsOperators = false;
            if (!Parameters(out var declarators, TokenTypes.SemiColon, false, FastVariableKind.Const))
                throw stream.Unexpected();
            considerInOfAsOperators = true;

            if (isAwait && functionDepth == 0)
                isAsync = true;

            declaration = new AstVariableDeclaration(start, PreviousToken, declarators,
                FastVariableKind.Const, @using: true, await: isAwait);
            return true;
        }

        static bool IsOfKeyword(FastToken token)
            => token.ContextualKeyword == FastKeywords.of
                || token.CookedText == "of";

        // A non-declaration for-in/for-of LHS is parsed as an expression. When it
        // is an object/array literal it is actually a destructuring assignment
        // target (a CoverInitializedName like `{ x = 1 }` is only legal here), so
        // reinterpret it as a pattern at parse time — mirroring the assignment
        // expression path (NextExpression). Other targets (identifiers, member
        // expressions, and even invalid `f()` references handled at runtime per
        // Annex B) are left untouched.
        static AstNode ReinterpretForHeadTarget(AstNode lhs)
            => lhs is AstExpression { Type: FastNodeType.ObjectLiteral or FastNodeType.ArrayExpression } expr
                ? expr.ToPattern()
                : lhs;

        bool ExpressionList(out AstExpression? node)
        {
            var list = new Sequence<AstExpression>();
            var token = stream.Current;

            node = null;
            considerInOfAsOperators = false;

            while (true)
            {
                if (stream.CheckAndConsume(TokenTypes.SemiColon))
                    break;

                if (!Expression(out node))
                    throw stream.Unexpected();

                var c = stream.Current;

                if (c.Type == TokenTypes.In || IsOfKeyword(c))
                    break;

                if (stream.CheckAndConsume(TokenTypes.SemiColon))
                {
                    // The expression before the head's `;` closes a comma list, so it
                    // belongs in `list` like every earlier one. Leaving it only in `node`
                    // dropped it: the AstSequenceExpression built below REPLACES `node`,
                    // so `for (i = 0, len = a.length; …)` compiled to just `i = 0` and
                    // never assigned `len` (Octane's Typescript suite walks a stale index
                    // and dies on `type.implementsTypeLinks[i].ast`). Only when a comma was
                    // actually seen — a lone head expression stays an ordinary expression.
                    if (list.Any())
                        list.Add(node);
                    break;
                }

                if (stream.CheckAndConsume(TokenTypes.Comma))
                {
                    list.Add(node);
                    continue;
                }
            }

            if (list.Any())
                node = new AstSequenceExpression(token, list.Last().End, list);

            considerInOfAsOperators = true;
            return true;
        }

        // Rejects a LabelledStatement whose (possibly multiply-labelled) item is a
        // FunctionDeclaration when it appears as a loop body. `label: function f(){}`
        // is a legal statement at the top level of a script, block, or function body,
        // but never as the body of a for/for-in/for-of loop, in either mode.
        static void ThrowIfLabelledFunctionLoopBody(AstStatement body)
        {
            if (body is not AstLabeledStatement)
                return;

            var current = body;
            while (current is AstLabeledStatement labeled)
                current = labeled.Body;

            if (current is AstExpressionStatement { Expression: AstFunctionExpression { IsStatement: true } func })
                throw new FastParseException(func.Start, "In strict mode code, functions can only be declared at top level or inside a block");
        }
    }

    private static bool IsEscapedKeyword(FastToken token, string keyword)
        => token.CookedText == keyword && token.Span.Value != keyword;
}
