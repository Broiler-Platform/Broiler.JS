using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Patterns;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Runtime;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Broiler.JavaScript.Parser;


partial class FastParser
{
    private static int TempVarID = 1;

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
        if (awaitOf && !(inAsyncFunctionBody || (functionDepth == 0 && CoreScript.AllowTopLevelAwait)))
            throw new FastParseException(begin, "for await (... of ...) is only valid in async functions and modules");

        stream.Expect(TokenTypes.BracketStart);

        AstNode? beginNode;

        // desugar let/const in following scope
        bool newScope = false;
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
                        newScope = true;
                        break;

                    case FastKeywords.@const:
                        if (!VariableDeclarationStatement(out declaration, FastVariableKind.Const))
                            throw stream.Unexpected();
                        beginNode = declaration;
                        newScope = true;
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
                newScope = true;
            }
            else if (TryParseForUsingLexicalDeclaration(out declaration))
            {
                // C-style `for (using x = …; …; …)` / `for (await using x = …; …; …)`:
                // a LexicalDeclaration whose `using` / `await using` bindings are disposed
                // once when the loop's lexical environment is torn down (see the cStyleUsing
                // handling below, which keeps the declaration intact instead of desugaring it).
                beginNode = declaration;
                newScope = true;
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
            else if (ExpressionSequence(out test, TokenTypes.SemiColon, true))
            {
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

                if (!ExpressionSequence(out update, TokenTypes.BracketEnd, true))
                    throw stream.Unexpected();

                if (update.Type == FastNodeType.EmptyExpression)
                    update = null;
            }
            else stream.Unexpected();

            // `for await` is only valid with a for-of head. A for-in head with `await` is
            // rejected above; a C-style `for await (;;)` / `for await (init; test; update)`
            // head reaches here with of == false and is a SyntaxError.
            if (awaitOf && !of)
                throw stream.Unexpected();


            // A C-style `for (await using …; …; …)` head is NOT desugared into per-iteration
            // carriers: per spec the LexicalDeclaration is instantiated once in the loop's
            // lexical environment and its resources are disposed once, when that environment
            // is torn down. Keeping the declaration intact as the loop init (and wrapping the
            // whole `for` in a block scope below) lets the scope's disposal machinery dispose
            // them at loop exit — and dispose any already-initialized resource if a later
            // initializer in the BindingList throws. (A plain `using` C-style head is a
            // SyntaxError, rejected during parsing; the for-of / for-in `using` heads keep
            // their per-iteration desugaring.)
            var cStyleUsing = declaration is { Using: true } && !@in && !of;

            AstStatement statement;
            if (stream.CheckAndConsume(TokenTypes.CurlyBracketStart))
            {
                if (!Block(out var block))
                    throw stream.Unexpected();

                if (newScope && declaration != null && !cStyleUsing)
                {
                    (beginNode, statement, update, test) = Desugar(declaration, block.Statements, update, test, cStyle: !@in && !of, block);
                }
                else
                {
                    statement = block;
                }

            }
            else if (NonDeclarativeStatement(out statement))
            {
                if (newScope && declaration != null && !cStyleUsing)
                    (beginNode, statement, update, test) = Desugar(declaration, new Sequence<AstStatement>(1) { statement }, update, test, cStyle: !@in && !of);
            }
            else throw stream.Unexpected();

            IFastEnumerable<StringSpan>? headTdzNames = null;
            if (newScope && declaration != null)
                headTdzNames = GetBindingNames(declaration);

            if (@in)
            {
                node = new AstForInStatement(begin, PreviousToken, beginNode, inTarget, statement)
                {
                    HeadTdzNames = headTdzNames
                };
                scope.GetVariables();

                return true;
            }

            if (of)
            {
                node = new AstForOfStatement(begin, PreviousToken, beginNode, ofTarget, statement, awaitOf)
                {
                    HeadTdzNames = headTdzNames
                };
                scope.GetVariables();

                return true;
            }

            var forStatement = new AstForStatement(begin, PreviousToken, beginNode, test, update, statement);
            // Wrap a C-style `await using` for-loop in a block so its lexical environment
            // (which owns the disposable resources) is established and torn down — disposing
            // the resources — around the entire loop.
            node = cStyleUsing
                ? new AstBlock(begin, PreviousToken, new Sequence<AstStatement>(1) { forStatement })
                : forStatement;
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

        static IFastEnumerable<StringSpan> GetBindingNames(AstVariableDeclaration declaration)
        {
            var names = new Sequence<StringSpan>();
            var en = declaration.Declarators.GetFastEnumerator();
            while (en.MoveNext(out var d))
                CollectBindingNames(d.Identifier, names);
            return names;
        }

        static void CollectBindingNames(AstExpression expression, Sequence<StringSpan> names)
        {
            switch (expression.Type)
            {
                case FastNodeType.Identifier:
                    names.Add((expression as AstIdentifier)!.Name);
                    break;

                case FastNodeType.BinaryExpression:
                    CollectBindingNames((expression as AstBinaryExpression)!.Left, names);
                    break;

                case FastNodeType.SpreadElement:
                    CollectBindingNames((expression as AstSpreadElement)!.Argument, names);
                    break;

                case FastNodeType.ArrayPattern:
                    var elements = (expression as AstArrayPattern)!.Elements.GetFastEnumerator();
                    while (elements.MoveNext(out var element))
                    {
                        if (element != null)
                            CollectBindingNames(element, names);
                    }
                    break;

                case FastNodeType.ObjectPattern:
                    var properties = (expression as AstObjectPattern)!.Properties.GetFastEnumerator();
                    while (properties.MoveNext(out var property))
                        CollectBindingNames(property.Value, names);
                    break;
            }
        }

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
                    break;

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

        // modify the node as well...
        AstExpression AssignTempNames(Sequence<(string id, AstIdentifier temp)> list, Sequence<StringSpan> hoisted, AstExpression e)
        {
            switch (e.Type)
            {
                case FastNodeType.Identifier:
                    var id = e as AstIdentifier;
                    var tempID = Interlocked.Increment(ref TempVarID).ToString();
                    // Preserve the original binding name for NamedEvaluation: a binding such
                    // as `[f = () => {}]` must name the anonymous initializer "f", not the
                    // synthetic temp this rename introduces.
                    var temp = new AstIdentifier(id!.Start, tempID) { InferenceName = id.Name.Value };

                    hoisted.Add(id.Name);
                    list.Add((id.Name.Value!, temp));

                    return temp;

                case FastNodeType.EmptyExpression:
                    return e;

                case FastNodeType.BinaryExpression:
                    var binary = e as AstBinaryExpression;
                    if (binary!.Operator != TokenTypes.Assign)
                        throw new FastParseException(e.Start, $"Unknown token");

                    return new AstBinaryExpression(AssignTempNames(list, hoisted, binary.Left), binary.Operator, binary.Right);

                case FastNodeType.SpreadElement:
                    var spreadElement = e as AstSpreadElement;
                    return new AstSpreadElement(spreadElement!.Start, spreadElement.End, AssignTempNames(list, hoisted, spreadElement.Argument));

                case FastNodeType.ObjectPattern:
                    var pattern = e as AstObjectPattern;
                    var pat = (pattern!.Properties as Sequence<ObjectProperty>)!;

                    for (int i = 0; i < pat.Count; i++)
                    {
                        var property = pat[i];

                        // Object rest (`{...rest}`) stores the rest target in the key as a
                        // SpreadElement, and the compiler reads it from the key — not the
                        // value (the parser shares the same node for both). Rewrite the key
                        // once and keep both sides in sync, otherwise the generated temp
                        // binding for the rest target is never declared and resolving it
                        // throws "<temp> is not defined".
                        if (property.Key != null && property.Key.Type == FastNodeType.SpreadElement)
                        {
                            var renamedRest = AssignTempNames(list, hoisted, property.Key);
                            pat[i] = new ObjectProperty(renamedRest, renamedRest, property.Init, property.Spread, property.Computed);
                            continue;
                        }

                        pat[i] = new ObjectProperty(property.Key, AssignTempNames(list, hoisted, property.Value), property.Init, property.Spread, property.Computed);
                    }

                    return pattern;

                case FastNodeType.ArrayPattern:
                    var arrayPattern = e as AstArrayPattern;
                    var elements = (arrayPattern!.Elements as Sequence<AstExpression>)!;

                    for (int i = 0; i < elements.Count; i++)
                    {
                        var property = elements[i];
                        elements[i] = AssignTempNames(list, hoisted, property);
                    }

                    return arrayPattern;

                default:
                    throw new FastParseException(e.Start, $"Unknown token");
            }
        }

        (AstNode beginNode, AstStatement statement, AstExpression? update, AstExpression? test) Desugar(AstVariableDeclaration declaration, IFastEnumerable<AstStatement> body,
            AstExpression? update, AstExpression? test, bool cStyle, AstBlock? sourceBlock = null)
        {
            var statementList = new Sequence<AstStatement>(body.Count + 1) { null! };
            statementList.AddRange(body);

            // for-of and for-in does not require identifier replacement
            // instead they need single identifier as a temp variable

            // both test/update are null for for-of and for-in

            var requiresReplacement = update != null || test != null;

            var tempDeclarations = new Sequence<VariableDeclarator>();
            var scopedDeclarations = new Sequence<VariableDeclarator>();
            var list = new Sequence<(string id, AstIdentifier temp)>();
            var hoisted = new Sequence<StringSpan>();

            // For a C-style `for (let …; test; update)` head whose bindings are all simple
            // identifiers, keep the original names bound in the loop's own lexical scope (the head's
            // "init" declaration) alongside the synthetic per-iteration carriers. This lets a closure
            // created inside an initializer (e.g. `for (let i = (f = () => i, 0); …)`) capture the
            // loop variable instead of throwing "i is not defined": the closure resolves to this
            // loop-scope binding, which the per-iteration copies never reassign (matching the spec's
            // single loop environment). The carrier is seeded from it, so the initializer's side
            // effects still run exactly once. `const` heads keep the original lowering — a `const`
            // carrier cannot be mutated by an update.
            //
            // This applies even to a head with no test and no update (`for (let x = init; ;)`): an
            // initializer closure must still resolve the loop binding, not the enclosing scope's
            // same-named variable (test262 sm/lexical-environment/for-loop's `for (let outer = (save =
            // function () { return outer; }); ;) break;`, where save() must return the loop `outer`).
            //
            // Restricted to a C-style head (`!@in && !of`): Desugar is shared with for-in / for-of,
            // whose heads also have no test/update but bind the loop variable from each enumerated
            // value (handled by VisitForIn/OfStatement), not from a carrier — they must keep the
            // plain lowering.
            var useLoopEnv = cStyle && declaration.Kind == FastVariableKind.Let;

            var en = declaration.Declarators.GetFastEnumerator();
            while (en.MoveNext(out var d))
            {
                if (useLoopEnv)
                {
                    // Keep the original binding — a simple identifier OR a destructuring pattern
                    // (`{[++q]: r}`, `[a, b]`, …) — bound once in the head's own lexical scope, so a
                    // closure created in a later declarator's initializer (`s = () => r`) resolves the
                    // earlier binding instead of throwing "r is not defined" (test262 sm bug-1216623).
                    // Per spec the loop runs in a single environment whose values are copied per
                    // iteration; the body re-declares each name from a carrier (built below) so closures
                    // in the head capture the original binding while the loop variable still advances.
                    tempDeclarations.Add(new VariableDeclarator(d.Identifier, d.Init));

                    // One per-iteration carrier per bound name, seeded from the original binding. For a
                    // simple identifier this is the single name; for a pattern it is each destructured
                    // name (collected in declaration order so a carrier never precedes its source).
                    var boundNames = new Sequence<StringSpan>();
                    CollectBindingNames(d.Identifier, boundNames);
                    var nameEn = boundNames.GetFastEnumerator();
                    while (nameEn.MoveNext(out var origName))
                    {
                        var tempID = Interlocked.Increment(ref TempVarID).ToString();
                        var temp = new AstIdentifier(d.Identifier.Start, tempID) { InferenceName = origName.Value };

                        tempDeclarations.Add(new VariableDeclarator(temp, new AstIdentifier(d.Identifier.Start, origName.Value)));

                        hoisted.Add(origName);
                        list.Add((origName.Value!, temp));
                    }
                }
                else if (requiresReplacement)
                {
                    var id = AssignTempNames(list, hoisted, d.Identifier);
                    tempDeclarations.Add(new VariableDeclarator(id, d.Init));
                }
                else
                {
                    var tid = Interlocked.Increment(ref TempVarID).ToString();
                    var id = new AstIdentifier(d.Identifier.Start, tid);

                    tempDeclarations.Add(new VariableDeclarator(id, d.Init));
                    scopedDeclarations.Add(new VariableDeclarator(d.Identifier, id));
                }
            }

            var changes = list;

            // The per-iteration body block re-binds each name from its carrier (`let x = <carrier>`).
            // `changes` is empty for a no-replacement non-loop-env head (its scopedDeclarations were
            // populated directly above), so this is a no-op there.
            foreach (var (id, temp) in changes)
                scopedDeclarations.Add(new VariableDeclarator(new AstIdentifier(temp.Start, id), temp));

            // A closure created in the increment must capture the SAME per-iteration environment the
            // following test and body run in: ForBodyEvaluation copies the loop bindings AFTER the
            // body, then evaluates the increment in that fresh environment, which the next test/body
            // then share. Model this by running the increment at the TOP of the per-iteration body
            // block — guarded by a loop-scoped "started" flag so it is skipped on the first pass — and
            // copying each per-iteration binding back into its carrier from a `finally` so the copy
            // happens on `continue`/`break`/`return` too (test262 sm/lexical-environment/for-loop's
            // "incr" closures). Only a loop-env update that actually contains a closure needs this; the
            // common case keeps the cheaper direct carrier mutation plus an end-of-body copy-back.
            var incrementAtTop = useLoopEnv && update != null && ContainsClosure(update);
            AstIdentifier startedFlag = null;
            if (incrementAtTop)
            {
                var flagId = Interlocked.Increment(ref TempVarID).ToString();
                startedFlag = new AstIdentifier(declaration.Start, flagId);
                tempDeclarations.Add(new VariableDeclarator(startedFlag, new AstLiteral(TokenTypes.False, declaration.Start)));

                // Wrap the user body in `try { … } finally { <carrier> = x; … }` so the per-iteration
                // value is copied back to the carrier on every completion path before the next
                // iteration's increment reads it. Rebuild statementList ([0] is the scoped-decl slot).
                var bodyStatements = new Sequence<AstStatement>();
                for (int bi = 1; bi < statementList.Count; bi++)
                    bodyStatements.Add(statementList[bi]);

                var finallyStatements = new Sequence<AstStatement>();
                foreach (var (id, temp) in changes)
                    finallyStatements.Add(new AstExpressionStatement(new AstBinaryExpression(
                        new AstIdentifier(temp.Start, temp.Name.Value), TokenTypes.Assign, new AstIdentifier(temp.Start, id))));

                var tryBody = new AstBlock(declaration.Start, declaration.End, bodyStatements);
                var finallyBody = new AstBlock(declaration.Start, declaration.End, finallyStatements);
                var tryStmt = new AstTryStatement(declaration.Start, declaration.End, tryBody, null, null, finallyBody);
                statementList = new Sequence<AstStatement>(2) { null!, tryStmt };
            }

            if (requiresReplacement)
            {
                if (test != null)
                {
                    if (useLoopEnv)
                    {
                        // Per-iteration test binding: a closure created in the loop test
                        // (`for (let i = 0; a.push(() => i), i < 5; ++i) {}`) must capture
                        // that iteration's `i`, not the shared carrier. Evaluate the test
                        // against the body block's fresh per-iteration binding by injecting
                        // `if (!test) break;` at the top of the body block (right after
                        // `let i = <carrier>`) and dropping the outer loop test. The test
                        // keeps referencing the original name, which now resolves to the
                        // per-iteration binding; its side effects and the final
                        // loop-terminating evaluation still run exactly as before.
                        var notTest = new AstUnaryExpression(test.Start, test, UnaryOperator.Negate);
                        var breakStatement = new AstBreakStatement(test.Start, test.End);
                        statementList.Insert(1, new AstIfStatement(test.Start, test.End, notTest, breakStatement));
                        test = null;
                    }
                    else
                    {
                        test = AstIdentifierReplacer.Replace(test, changes) as AstExpression;
                    }
                }

                if (update != null)
                {
                    if (incrementAtTop)
                    {
                        // `if (started) { <update>; } else { started = true; }`, inserted ABOVE the
                        // test so the increment runs before the test in the shared per-iteration
                        // environment, and is skipped on the first iteration. The update keeps
                        // referencing the original names (which resolve to the per-iteration bindings).
                        var guard = new AstIfStatement(update.Start, update.End,
                            new AstIdentifier(startedFlag.Start, startedFlag.Name.Value),
                            new AstExpressionStatement(update),
                            new AstExpressionStatement(new AstBinaryExpression(
                                new AstIdentifier(startedFlag.Start, startedFlag.Name.Value),
                                TokenTypes.Assign,
                                new AstLiteral(TokenTypes.True, declaration.Start))));
                        statementList.Insert(1, guard);
                        update = null;
                    }
                    else
                    {
                        update = AstIdentifierReplacer.Replace(update, changes) as AstExpression;
                    }
                }
            }

            // NOTE: for a no-closure update the carrier is mutated directly by the (replaced) outer
            // loop update, and the per-iteration body re-binds each name from that carrier — closures
            // in the test/body still capture the correct per-iteration value (test262
            // let-closure-inside-condition). We deliberately do NOT also copy the per-iteration
            // binding back into the carrier at the end of the body: that would change the value seen
            // by the next iteration when the BODY (not the update) mutates the loop variable, which is
            // a separate pre-existing behaviour and not needed by any of the loop-env fixes here
            // (P40/P25's increment-position closures use the increment-at-top `finally` path above).

            // The per-iteration scoped declaration carries the original head's
            // `using` / `await using` disposal markers, so a `for (using x of …)`
            // binding is disposed at the end of each iteration's block scope.
            statementList[0] = new AstVariableDeclaration(declaration.Start, declaration.End, scopedDeclarations, declaration.Kind, declaration.Using, declaration.AwaitUsing);

            // The loop-env lowering keeps the carriers as `let` in the head's own lexical scope
            // (visible to test/update, shadowed in the body); `const` heads keep a `const` carrier;
            // every other head uses a function-scoped `var` carrier as before.
            var tempDeclarationKind = useLoopEnv
                ? FastVariableKind.Let
                : requiresReplacement && declaration.Kind == FastVariableKind.Const
                    ? FastVariableKind.Const
                    : FastVariableKind.Var;
            var r = new AstVariableDeclaration(declaration.Start, declaration.End, tempDeclarations, tempDeclarationKind);
            var last = body.Count == 0 ? declaration : body.Last();
            var block = new AstBlock(r.Start, last.End, statementList);

            // The synthetic per-iteration block replaces the original loop body
            // block, so it must also carry that body's hoisted bindings (e.g. a
            // nested `function` declaration). Otherwise the declaration is not
            // hoisted into this block scope and a closure over the per-iteration
            // loop variable captures an uninitialised slot. Merge those names with
            // the loop's own per-iteration binding names (`hoisted`).
            var combinedHoisting = CombineHoisting(useLoopEnv || requiresReplacement ? hoisted : null, sourceBlock?.HoistingScope);
            if (combinedHoisting != null)
                block.HoistingScope = combinedHoisting;

            if (sourceBlock?.AnnexBFunctionNames != null)
                block.AnnexBFunctionNames = sourceBlock.AnnexBFunctionNames;

            return (r, block, update, test);
        }

        static bool ContainsClosure(AstNode node)
        {
            var detector = new ClosureDetector();
            detector.Visit(node);
            return detector.Found;
        }

        static IFastEnumerable<StringSpan>? CombineHoisting(
            IFastEnumerable<StringSpan>? loopBindings,
            IFastEnumerable<StringSpan>? bodyHoisting)
        {
            var hasLoop = loopBindings != null && loopBindings.Count > 0;
            var hasBody = bodyHoisting != null && bodyHoisting.Count > 0;

            if (!hasLoop && !hasBody)
                return null;
            if (!hasBody)
                return loopBindings;
            if (!hasLoop)
                return bodyHoisting;

            var merged = new Sequence<StringSpan>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            var loopEn = loopBindings!.GetFastEnumerator();
            while (loopEn.MoveNext(out var name))
            {
                if (seen.Add(name.Value))
                    merged.Add(name);
            }

            var bodyEn = bodyHoisting!.GetFastEnumerator();
            while (bodyEn.MoveNext(out var name))
            {
                if (seen.Add(name.Value))
                    merged.Add(name);
            }

            return merged;
        }
    }

    private static bool IsEscapedKeyword(FastToken token, string keyword)
        => token.CookedText == keyword && token.Span.Value != keyword;
}
