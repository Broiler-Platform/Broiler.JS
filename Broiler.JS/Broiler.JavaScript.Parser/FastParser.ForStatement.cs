using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Patterns;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
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

            if (current.IsKeyword)
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
                // case of automatic semicolon insertion
                if (test.End.Type == TokenTypes.BracketEnd)
                    throw stream.Unexpected();

                if (test.Type == FastNodeType.EmptyExpression)
                    test = null;

                if (!ExpressionSequence(out update, TokenTypes.BracketEnd, true))
                    throw stream.Unexpected();

                if (update.Type == FastNodeType.EmptyExpression)
                    update = null;
            }
            else stream.Unexpected();


            AstStatement statement;
            if (stream.CheckAndConsume(TokenTypes.CurlyBracketStart))
            {
                if (!Block(out var block))
                    throw stream.Unexpected();

                if (newScope && declaration != null)
                {
                    (beginNode, statement, update, test) = Desugar(declaration, block.Statements, update, test, block);
                }
                else
                {
                    statement = block;
                }

            }
            else if (NonDeclarativeStatement(out statement))
            {
                if (newScope && declaration != null)
                    (beginNode, statement, update, test) = Desugar(declaration, new Sequence<AstStatement>(1) { statement }, update, test);
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

            node = new AstForStatement(begin, PreviousToken, beginNode, test, update, statement);
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
            var en = declaration.Declarators.GetFastEnumerator();
            while (en.MoveNext(out var d))
            {
                count++;
                if (d.Init != null)
                    hasInit = true;
            }

            // for-in/for-of must have exactly one binding
            if (count != 1)
                throw new FastParseException(declaration.Start, "Invalid left-hand side in for-in/for-of loop");

            // Initializer is always forbidden in for-of; forbidden for let/const in for-in
            if (isOf && hasInit)
                throw new FastParseException(declaration.Start, "for-of loop variable declaration may not have an initializer");

            if (!isOf && hasInit && declaration.Kind != FastVariableKind.Var)
                throw new FastParseException(declaration.Start, "for-in loop variable declaration may not have an initializer");
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
            AstExpression? update, AstExpression? test, AstBlock? sourceBlock = null)
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

            var en = declaration.Declarators.GetFastEnumerator();
            while (en.MoveNext(out var d))
            {
                if (requiresReplacement)
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

            if (requiresReplacement)
            {
                foreach (var (id, temp) in changes)
                    scopedDeclarations.Add(new VariableDeclarator(new AstIdentifier(temp.Start, id), temp));

                if (update != null)
                    update = AstIdentifierReplacer.Replace(update, changes) as AstExpression;

                if (test != null)
                    test = AstIdentifierReplacer.Replace(test, changes) as AstExpression;
            }

            statementList[0] = new AstVariableDeclaration(declaration.Start, declaration.End, scopedDeclarations, declaration.Kind);

            var tempDeclarationKind = requiresReplacement && declaration.Kind == FastVariableKind.Const
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
            var combinedHoisting = CombineHoisting(requiresReplacement ? hoisted : null, sourceBlock?.HoistingScope);
            if (combinedHoisting != null)
                block.HoistingScope = combinedHoisting;

            if (sourceBlock?.AnnexBFunctionNames != null)
                block.AnnexBFunctionNames = sourceBlock.AnnexBFunctionNames;

            return (r, block, update, test);
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
