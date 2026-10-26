using System;
using System.Collections.Generic;
using System.Globalization;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Patterns;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Compiler;

/// <summary>
/// The compiler's for-loop pass: it rewrites each <c>for</c>, <c>for-in</c> and <c>for-of</c> loop whose
/// head declares <c>let</c>, <c>const</c>, <c>using</c> or <c>await using</c> bindings into the shape the
/// compiler lowers, with per-iteration carriers, the head's temporal-dead-zone names and the block a
/// C-style <c>using</c> loop is disposed by. It runs over the parser's tree, before
/// <see cref="SyntaxValidation"/> reads it, wherever the compiler parses a program: in
/// <see cref="FastCompiler"/> and in <see cref="DirectEvalSupport"/>.
/// </summary>
/// <remarks>
/// <para>
/// The parser did this as it parsed until milestone SJL-12.5, item 5 (Broiler.VM's JSD-0080, decision
/// D22), which leaves the parser a tree without desugaring. The pass emits the shapes the parser
/// emitted, node for node; only the temporaries' names differ, the statements it adds are marked
/// (below), and a labelled C-style <c>using</c> loop's labels are inside the block the loop is wrapped
/// in, where the parser left them outside it.
/// </para>
/// <para>
/// <b>Completion values.</b> A loop completes with ForBodyEvaluation's V: the last value other than empty
/// that an iteration of its body completed with, or the value a <c>break</c> carries, by UpdateEmpty. The
/// statements the pass adds to the body - the per-iteration declaration, the test and the guarded update it
/// moves in, the copy back into the carriers and the <c>try</c> that runs it - are not the body's, so each
/// is marked <see cref="AstStatement.IsLoopBookkeeping"/>, and the compiler keeps them out of V. The
/// parser's shapes, which the pass emitted unmarked until then, were not kept out: the test moved into the
/// body completed as an <c>if</c> without an <c>else</c> does, with undefined, so a loop its test ended was
/// undefined (<c>for (let i = 0; i &lt; 3; i++) { i; }</c> for 2), and a body that completed empty took the
/// value of the per-iteration declaration (<c>1; for (let x of [1, 2]) { }</c> was 2 for undefined).
/// </para>
/// <para>
/// <b>Temporaries' names</b> (decision D22) are <c>&lt;d&gt;:&lt;n&gt;</c>: <c>d</c> is the direct-eval
/// nesting depth the compilation inherits, and <c>n</c> counts the temporaries this pass names, from 1.
/// A name with a colon is never an identifier, so no program can name, declare or shadow a temporary.
/// The names depend on nothing but the tree and the depth, so two passes over one tree name alike,
/// whatever ran before or beside them.
/// </para>
/// <para>
/// <c>d</c> is a compile-time depth. Direct eval code is at one more than the depth the code that calls
/// <c>eval</c> was compiled at: <see cref="FastCompiler"/> passes its own depth + 1 as a constant of every
/// direct eval call it emits, and <see cref="DirectEvalSupport"/> validates and compiles the eval code at
/// that depth. Every other compilation - a script, a module, an indirect eval, a <c>Function</c>
/// constructor's body, even one compiled while a direct eval runs - is at 0, since no direct eval
/// encloses it. Along a chain of direct evals, then, each eval code's <c>d</c> is greater than that of the
/// code that calls it and of every compilation enclosing that, so the names differ from an enclosing
/// compilation's carriers: a <c>var</c> carrier a sloppy direct eval declares in its caller's variable
/// environment never shares a name with the caller's own, however late the caller runs - a function
/// compiled in eval code and called after that eval returned among them. Two direct evals from the same
/// code are both at its depth + 1 and may name alike; neither encloses the other. The depth is not read
/// from the number of evals running when the code is compiled, which a function called after its eval
/// returned would undercount. The parser's names until item 5 were per-parse numbers from 2, which a
/// direct eval's carriers shared with its caller's; the compilation's identifier is not used, because it
/// comes from a process-wide counter.
/// </para>
/// <para>
/// <b>What it reads from the parser.</b> A loop's head and body as written, and the one fact a search of
/// the tree cannot recover safely, <c>ContainsContinue</c>: whether a <c>continue</c> was parsed inside
/// the body, which decides whether a <c>let</c> loop's copy back into its carriers runs in a
/// <c>finally</c>. The parser counts the continue statements it parses, so the answer has no false
/// negative; a search of the tree would have to reach every statement position, a <c>switch</c> case's
/// statements among them, and a <c>continue</c> it missed would make the copy-back skippable and the
/// loop infinite.
/// </para>
/// <para>
/// <b>Order.</b> The parser rewrote a loop once its head and body were parsed, so a loop nested in
/// another's head or body was rewritten first, and loops in source order otherwise. The pass keeps that
/// order: it walks the tree in source order and prepares a loop after everything inside it. The
/// temporaries are numbered in that order, and of two loops the rewrite refuses, the one the parser
/// refused first is the one the pass refuses.
/// </para>
/// <para>
/// <b>In place.</b> A rewritten loop is a new node, so the statement list that holds it is updated in
/// place, and a statement that holds it directly (an <c>if</c>, a loop, a label, a <c>with</c>, a
/// <c>try</c>) is rebuilt with its other members as they were. Every statement list the parser builds is
/// a <see cref="Sequence{T}"/>. Expressions are never rebuilt: a loop inside one is inside a function's
/// or a class's body, whose statement lists are updated in place. Each statement list is prepared once,
/// even where the tree reaches it twice.
/// </para>
/// <para>
/// <b>Cost.</b> The pass is one more walk of every program the compiler compiles, whether or not it
/// holds a lexical <c>for</c> head, and of the code of an eval, direct or indirect, up to twice: once for
/// <see cref="DirectEvalSupport"/>'s validation and once for its compile. The parser's rewrite cost no
/// walk of its own. That cost is not measured, and nothing here claims a speed.
/// </para>
/// </remarks>
internal static class ForLoopPreparation
{
    /// <summary>
    /// Prepares every loop of <paramref name="program"/> in place and returns the prepared program.
    /// </summary>
    /// <param name="program">The parser's tree, which this changes.</param>
    /// <param name="directEvalDepth">
    /// The direct-eval nesting depth the compilation inherits, the <c>d</c> of the temporaries' names: 0
    /// for a program, and for direct eval code one more than the depth the code that calls <c>eval</c> was
    /// compiled at.
    /// </param>
    /// <exception cref="FastParseException">
    /// A head's binding pattern holds a target the rewrite cannot rename ("Unknown token"), as the parser
    /// refused it before the rewrite moved here.
    /// </exception>
    internal static AstProgram Prepare(AstProgram program, int directEvalDepth)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentOutOfRangeException.ThrowIfNegative(directEvalDepth);

        new Preparer(directEvalDepth).Visit(program);
        return program;
    }

    /// <summary>
    /// The name of the <paramref name="number"/>th temporary a pass at <paramref name="directEvalDepth"/>
    /// names: <c>&lt;d&gt;:&lt;n&gt;</c>, in the invariant culture.
    /// </summary>
    internal static string TemporaryName(int directEvalDepth, int number)
        => directEvalDepth.ToString(CultureInfo.InvariantCulture) + ":" + number.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// One pass over one program: the walk, and the temporaries' counter, which is this pass's alone.
    /// </summary>
    private sealed class Preparer(int directEvalDepth) : AstMapVisitor<AstNode>
    {
        private readonly HashSet<object> preparedLists = new(System.Collections.Generic.ReferenceEqualityComparer.Instance);

        // The blocks this pass wraps a C-style `using` loop in, its labels included (VisitLabeledStatement).
        private readonly HashSet<AstBlock> usingLoopBlocks = new(System.Collections.Generic.ReferenceEqualityComparer.Instance);

        // The number of temporaries this pass has named.
        private int temporaries;

        private string NewTempName() => TemporaryName(directEvalDepth, ++temporaries);

        // ---- statement lists: updated in place ----------------------------------------------------

        private void PrepareStatements(IFastEnumerable<AstStatement> statements)
        {
            if (statements == null || statements.Count == 0 || !preparedLists.Add(statements))
                return;

            if (statements is not Sequence<AstStatement> list)
            {
                // Every list the parser builds is a Sequence; any other one is only walked, and a loop in
                // it that the pass would have to replace is an error, never a silent skip.
                var en = statements.GetFastEnumerator();
                while (en.MoveNext(out var item))
                {
                    if (!ReferenceEquals(Statement(item), item))
                        throw new InvalidOperationException("The for-loop pass cannot replace a statement in a " + statements.GetType().Name + ".");
                }

                return;
            }

            for (var i = 0; i < list.Count; i++)
            {
                var item = list[i];
                var prepared = Statement(item);
                if (!ReferenceEquals(prepared, item))
                    list[i] = prepared;
            }
        }

        private AstStatement Statement(AstStatement statement)
            => statement == null ? null : (AstStatement)Visit(statement);

        // A child that holds no statement of its own position: an expression, a pattern, a declaration.
        // It is walked for the functions and classes inside it, and it is never replaced.
        private void Walk(AstNode node)
        {
            if (node != null && !ReferenceEquals(Visit(node), node))
                throw new InvalidOperationException("The for-loop pass replaced a " + node.Type + ", which it never replaces.");
        }

        private void Walk<T>(IFastEnumerable<T> nodes)
            where T : AstNode
        {
            if (nodes == null)
                return;

            var en = nodes.GetFastEnumerator();
            while (en.MoveNext(out var node))
                Walk(node);
        }

        private void Walk(IFastEnumerable<VariableDeclarator> declarators)
        {
            if (declarators == null)
                return;

            var en = declarators.GetFastEnumerator();
            while (en.MoveNext(out var declarator))
            {
                Walk(declarator.Identifier);
                Walk(declarator.Init);
            }
        }

        private static T Rebuilt<T>(T rebuilt, AstNode original)
            where T : AstNode
        {
            rebuilt.WasParenthesized = original.WasParenthesized;
            return rebuilt;
        }

        protected override AstNode VisitProgram(AstProgram program)
        {
            PrepareStatements(program.Statements);
            return program;
        }

        protected override AstNode VisitBlock(AstBlock block)
        {
            PrepareStatements(block.Statements);
            return block;
        }

        protected override AstNode VisitSwitchStatement(AstSwitchStatement switchStatement)
        {
            Walk(switchStatement.Target);
            var cases = switchStatement.Cases.GetFastEnumerator();
            while (cases.MoveNext(out var @case))
            {
                Walk(@case.Test);
                PrepareStatements(@case.Statements);
            }

            return switchStatement;
        }

        // ---- the loops --------------------------------------------------------------------------

        protected override AstNode VisitForStatement(AstForStatement forStatement, string label = null)
        {
            Walk(forStatement.Init);
            Walk(forStatement.Test);
            Walk(forStatement.Update);

            // Whether the body was written as a block is read before the body is prepared: a body
            // that is itself a C-style `using` loop becomes a block when it is prepared.
            var sourceBlock = forStatement.Body as AstBlock;
            var body = Statement(forStatement.Body);

            if (forStatement.Init is AstVariableDeclaration { Kind: FastVariableKind.Let or FastVariableKind.Const } declaration)
            {
                if (declaration.Using)
                {
                    // A C-style `for (await using …; …; …)` head is NOT desugared into per-iteration
                    // carriers: per spec the LexicalDeclaration is instantiated once in the loop's
                    // lexical environment and its resources are disposed once, when that environment
                    // is torn down. Keeping the declaration intact as the loop init and wrapping the
                    // whole `for` in a block scope lets the scope's disposal machinery dispose them at
                    // loop exit — and dispose any already-initialized resource if a later initializer
                    // in the BindingList throws. (A plain `using` C-style head is a SyntaxError,
                    // rejected during parsing; the for-of `using` heads keep their per-iteration
                    // desugaring.)
                    var loop = ReferenceEquals(body, forStatement.Body)
                        ? forStatement
                        : Rebuilt(new AstForStatement(forStatement.Start, forStatement.End, forStatement.Init, forStatement.Test, forStatement.Update, body)
                        {
                            ContainsContinue = forStatement.ContainsContinue
                        }, forStatement);
                    var usingLoopBlock = new AstBlock(forStatement.Start, forStatement.End, new Sequence<AstStatement>(1) { loop });
                    usingLoopBlocks.Add(usingLoopBlock);
                    return usingLoopBlock;
                }

                var (init, statement, update, test) = sourceBlock != null
                    ? Desugar(declaration, sourceBlock.Statements, forStatement.Update, forStatement.Test,
                        cStyle: true, forStatement.ContainsContinue, sourceBlock)
                    : Desugar(declaration, new Sequence<AstStatement>(1) { body }, forStatement.Update, forStatement.Test,
                        cStyle: true, forStatement.ContainsContinue);
                return Rebuilt(new AstForStatement(forStatement.Start, forStatement.End, init, test, update, statement)
                {
                    ContainsContinue = forStatement.ContainsContinue
                }, forStatement);
            }

            if (ReferenceEquals(body, forStatement.Body))
                return forStatement;

            return Rebuilt(new AstForStatement(forStatement.Start, forStatement.End, forStatement.Init, forStatement.Test, forStatement.Update, body)
            {
                ContainsContinue = forStatement.ContainsContinue
            }, forStatement);
        }

        protected override AstNode VisitForInStatement(AstForInStatement forInStatement, string label = null)
        {
            Walk(forInStatement.Init);
            Walk(forInStatement.Target);

            var sourceBlock = forInStatement.Body as AstBlock;
            var body = Statement(forInStatement.Body);

            if (forInStatement.Init is AstVariableDeclaration { Kind: FastVariableKind.Let or FastVariableKind.Const } declaration)
            {
                var (init, statement, _, _) = Head(declaration, sourceBlock, body, forInStatement.ContainsContinue);
                return Rebuilt(new AstForInStatement(forInStatement.Start, forInStatement.End, init, forInStatement.Target, statement)
                {
                    HeadTdzNames = GetBindingNames(declaration),
                    ContainsContinue = forInStatement.ContainsContinue
                }, forInStatement);
            }

            if (ReferenceEquals(body, forInStatement.Body))
                return forInStatement;

            return Rebuilt(new AstForInStatement(forInStatement.Start, forInStatement.End, forInStatement.Init, forInStatement.Target, body)
            {
                HeadTdzNames = forInStatement.HeadTdzNames,
                ContainsContinue = forInStatement.ContainsContinue
            }, forInStatement);
        }

        protected override AstNode VisitForOfStatement(AstForOfStatement forOfStatement, string label = null)
        {
            Walk(forOfStatement.Init);
            Walk(forOfStatement.Target);

            var sourceBlock = forOfStatement.Body as AstBlock;
            var body = Statement(forOfStatement.Body);

            if (forOfStatement.Init is AstVariableDeclaration { Kind: FastVariableKind.Let or FastVariableKind.Const } declaration)
            {
                var (init, statement, _, _) = Head(declaration, sourceBlock, body, forOfStatement.ContainsContinue);
                return Rebuilt(new AstForOfStatement(forOfStatement.Start, forOfStatement.End, init, forOfStatement.Target, statement, forOfStatement.IsAwait)
                {
                    HeadTdzNames = GetBindingNames(declaration),
                    ContainsContinue = forOfStatement.ContainsContinue
                }, forOfStatement);
            }

            if (ReferenceEquals(body, forOfStatement.Body))
                return forOfStatement;

            return Rebuilt(new AstForOfStatement(forOfStatement.Start, forOfStatement.End, forOfStatement.Init, forOfStatement.Target, body, forOfStatement.IsAwait)
            {
                HeadTdzNames = forOfStatement.HeadTdzNames,
                ContainsContinue = forOfStatement.ContainsContinue
            }, forOfStatement);
        }

        // A for-in or for-of head: no test and no update.
        private (AstNode beginNode, AstStatement statement, AstExpression update, AstExpression test) Head(
            AstVariableDeclaration declaration, AstBlock sourceBlock, AstStatement body, bool containsContinue)
            => sourceBlock != null
                ? Desugar(declaration, sourceBlock.Statements, null, null, cStyle: false, containsContinue, sourceBlock)
                : Desugar(declaration, new Sequence<AstStatement>(1) { body }, null, null, cStyle: false, containsContinue);

        // ---- the statements that hold a statement directly: rebuilt when it is replaced ----------

        protected override AstNode VisitIfStatement(AstIfStatement ifStatement)
        {
            Walk(ifStatement.Test);
            var @true = Statement(ifStatement.True);
            var @false = Statement(ifStatement.False);
            if (ReferenceEquals(@true, ifStatement.True) && ReferenceEquals(@false, ifStatement.False))
                return ifStatement;

            return Rebuilt(new AstIfStatement(ifStatement.Start, ifStatement.End, ifStatement.Test, @true, @false), ifStatement);
        }

        protected override AstNode VisitWhileStatement(AstWhileStatement whileStatement, string label = null)
        {
            Walk(whileStatement.Test);
            var body = Statement(whileStatement.Body);
            if (ReferenceEquals(body, whileStatement.Body))
                return whileStatement;

            return Rebuilt(new AstWhileStatement(whileStatement.Start, whileStatement.End, whileStatement.Test, body), whileStatement);
        }

        protected override AstNode VisitDoWhileStatement(AstDoWhileStatement doWhileStatement, string label = null)
        {
            // `do Statement while (Expression)`: the body comes first in the source.
            var body = Statement(doWhileStatement.Body);
            Walk(doWhileStatement.Test);
            if (ReferenceEquals(body, doWhileStatement.Body))
                return doWhileStatement;

            return Rebuilt(new AstDoWhileStatement(doWhileStatement.Start, doWhileStatement.End, doWhileStatement.Test, body), doWhileStatement);
        }

        protected override AstNode VisitLabeledStatement(AstLabeledStatement labeledStatement)
        {
            var body = Statement(labeledStatement.Body);
            if (ReferenceEquals(body, labeledStatement.Body))
                return labeledStatement;

            // The label of a C-style `using` loop goes inside the block the loop is wrapped in, so it
            // still prefixes the loop, directly or through the loop's other labels: `L: { for … }`
            // would label the block, and `continue L` would have no target. Each label of a chain
            // (`A: B: for …`) moves in, the nearest first, and the block is rebuilt to start at it.
            if (body is AstBlock block && usingLoopBlocks.Contains(block))
            {
                var labeled = Rebuilt(new AstLabeledStatement(labeledStatement.Label, block.Statements[0]), labeledStatement);
                var labeledBlock = new AstBlock(labeledStatement.Start, block.End, new Sequence<AstStatement>(1) { labeled });
                usingLoopBlocks.Add(labeledBlock);
                return labeledBlock;
            }

            return Rebuilt(new AstLabeledStatement(labeledStatement.Label, body), labeledStatement);
        }

        protected override AstNode VisitWithStatement(AstWithStatement withStatement)
        {
            Walk(withStatement.Object);
            var body = Statement(withStatement.Body);
            if (ReferenceEquals(body, withStatement.Body))
                return withStatement;

            return Rebuilt(new AstWithStatement(withStatement.Start, withStatement.End, withStatement.Object, body), withStatement);
        }

        protected override AstNode VisitTryStatement(AstTryStatement tryStatement)
        {
            var block = Statement(tryStatement.Block);
            Walk(tryStatement.CatchParam);
            var @catch = Statement(tryStatement.Catch);
            var @finally = Statement(tryStatement.Finally);
            if (ReferenceEquals(block, tryStatement.Block) && ReferenceEquals(@catch, tryStatement.Catch) && ReferenceEquals(@finally, tryStatement.Finally))
                return tryStatement;

            return Rebuilt(new AstTryStatement(tryStatement.Start, tryStatement.End, block, tryStatement.CatchParam, @catch, @finally), tryStatement);
        }

        // ---- every other node: walked for the bodies inside it, never replaced --------------------

        protected override AstNode VisitExpressionStatement(AstExpressionStatement expressionStatement)
        {
            Walk(expressionStatement.Expression);
            return expressionStatement;
        }

        protected override AstNode VisitVariableDeclaration(AstVariableDeclaration variableDeclaration)
        {
            Walk(variableDeclaration.Declarators);
            return variableDeclaration;
        }

        protected override AstNode VisitReturnStatement(AstReturnStatement returnStatement)
        {
            Walk(returnStatement.Argument);
            return returnStatement;
        }

        protected override AstNode VisitThrowStatement(AstThrowStatement throwStatement)
        {
            Walk(throwStatement.Argument);
            return throwStatement;
        }

        protected override AstNode VisitExportStatement(AstExportStatement astExportStatement)
        {
            Walk(astExportStatement.Declaration);
            Walk(astExportStatement.Source);
            return astExportStatement;
        }

        protected override AstNode VisitImportStatement(AstImportStatement astImportStatement) => astImportStatement;

        protected override AstNode VisitBreakStatement(AstBreakStatement breakStatement) => breakStatement;

        protected override AstNode VisitContinueStatement(AstContinueStatement continueStatement) => continueStatement;

        protected override AstNode VisitDebuggerStatement(AstDebuggerStatement debuggerStatement) => debuggerStatement;

        protected override AstNode VisitFunctionExpression(AstFunctionExpression functionExpression)
        {
            Walk(functionExpression.Id);
            Walk(functionExpression.Params);
            Walk(functionExpression.Body);
            return functionExpression;
        }

        protected override AstNode VisitClassStatement(AstClassExpression classStatement)
        {
            Walk(classStatement.Identifier);
            Walk(classStatement.Base);
            Walk(classStatement.Members);
            return classStatement;
        }

        protected override AstNode VisitClassProperty(AstClassProperty property)
        {
            Walk(property.Key);
            if (!ReferenceEquals(property.Init, property.Key))
                Walk(property.Init);
            return property;
        }

        protected override AstNode VisitObjectLiteral(AstObjectLiteral objectLiteral)
        {
            Walk(objectLiteral.Properties);
            return objectLiteral;
        }

        protected override AstNode VisitObjectPattern(AstObjectPattern objectPattern)
        {
            var properties = objectPattern.Properties.GetFastEnumerator();
            while (properties.MoveNext(out var property))
            {
                Walk(property.Key);
                if (!ReferenceEquals(property.Value, property.Key))
                    Walk(property.Value);
                Walk(property.Init);
            }

            return objectPattern;
        }

        protected override AstNode VisitArrayPattern(AstArrayPattern arrayPattern)
        {
            Walk(arrayPattern.Elements);
            return arrayPattern;
        }

        protected override AstNode VisitArrayExpression(AstArrayExpression arrayExpression)
        {
            Walk(arrayExpression.Elements);
            return arrayExpression;
        }

        protected override AstNode VisitBinaryExpression(AstBinaryExpression binaryExpression)
        {
            Walk(binaryExpression.Left);
            Walk(binaryExpression.Right);
            return binaryExpression;
        }

        protected override AstNode VisitUnaryExpression(AstUnaryExpression unaryExpression)
        {
            Walk(unaryExpression.Argument);
            return unaryExpression;
        }

        protected override AstNode VisitConditionalExpression(AstConditionalExpression conditionalExpression)
        {
            Walk(conditionalExpression.Test);
            Walk(conditionalExpression.True);
            Walk(conditionalExpression.False);
            return conditionalExpression;
        }

        protected override AstNode VisitSequenceExpression(AstSequenceExpression sequenceExpression)
        {
            Walk(sequenceExpression.Expressions);
            return sequenceExpression;
        }

        protected override AstNode VisitCallExpression(AstCallExpression callExpression)
        {
            Walk(callExpression.Callee);
            Walk(callExpression.Arguments);
            return callExpression;
        }

        protected override AstNode VisitNewExpression(AstNewExpression newExpression)
        {
            Walk(newExpression.Callee);
            Walk(newExpression.Arguments);
            return newExpression;
        }

        protected override AstNode VisitMemberExpression(AstMemberExpression memberExpression)
        {
            Walk(memberExpression.Object);
            Walk(memberExpression.Property);
            return memberExpression;
        }

        protected override AstNode VisitOptionalChain(AstOptionalChain node)
        {
            Walk(node.Expression);
            return node;
        }

        protected override AstNode VisitImportCall(AstImportCall node)
        {
            Walk(node.Source);
            Walk(node.Options);
            return node;
        }

        protected override AstNode VisitTemplateExpression(AstTemplateExpression templateExpression)
        {
            Walk(templateExpression.Parts);
            return templateExpression;
        }

        protected override AstNode VisitTaggedTemplateExpression(AstTaggedTemplateExpression astTaggedTemplateExpression)
        {
            Walk(astTaggedTemplateExpression.Tag);
            Walk(astTaggedTemplateExpression.Arguments);
            return astTaggedTemplateExpression;
        }

        protected override AstNode VisitAwaitExpression(AstAwaitExpression node)
        {
            Walk(node.Argument);
            return node;
        }

        protected override AstNode VisitYieldExpression(AstYieldExpression yieldExpression)
        {
            Walk(yieldExpression.Argument);
            return yieldExpression;
        }

        protected override AstNode VisitSpreadElement(AstSpreadElement spreadElement)
        {
            Walk(spreadElement.Argument);
            return spreadElement;
        }

        protected override AstNode VisitIdentifier(AstIdentifier identifier) => identifier;

        protected override AstNode VisitLiteral(AstLiteral literal) => literal;

        protected override AstNode VisitEmptyExpression(AstEmptyExpression emptyExpression) => emptyExpression;

        protected override AstNode VisitMeta(AstMeta astMeta) => astMeta;

        protected override AstNode VisitSuper(AstSuper super) => super;

        // ---- the rewrite, as the parser did it ---------------------------------------------------

        // modify the node as well...
        private AstExpression AssignTempNames(Sequence<(string id, AstIdentifier temp)> list, Sequence<StringSpan> hoisted, AstExpression e)
        {
            switch (e.Type)
            {
                case FastNodeType.Identifier:
                    var id = e as AstIdentifier;
                    var tempID = NewTempName();
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

        private (AstNode beginNode, AstStatement statement, AstExpression update, AstExpression test) Desugar(AstVariableDeclaration declaration, IFastEnumerable<AstStatement> body,
            AstExpression update, AstExpression test, bool cStyle, bool bodyContainsContinue, AstBlock sourceBlock = null)
        {
            // The head's bindings and the body's belong to DIFFERENT scopes: the loop's
            // per-iteration environment holds the head's, and a Block body opens its own
            // environment nested inside it, so a body-level `let`/`const`/`class`/`function`
            // may shadow a head binding of the same name. Splicing the body's statements in
            // beside the synthetic head declaration put both in ONE block, which made the two
            // declarations collide:
            //   for (const x of xs) { const x = 5; }   threw "Cannot assign to read only
            //                                          variable" — the body initializer was
            //                                          assigning the head's binding
            //   for (let i = 0; i < 2; i++) { let i = 9; }   ran once instead of twice — the
            //                                          body's 9 was copied back into the
            //                                          loop carrier as the next `i`
            // Source written by hand rarely shadows a loop variable with its own name, but a
            // minifier renames the head binding and the body binding independently and lands
            // on one short name for both. Nest only when a name actually collides: the
            // ordinary loop body keeps its single flat block scope, and with it the shape the
            // per-iteration lowering below was tuned around.
            //
            // A `let` head also MOVES the test (and a closure-carrying update) into that block —
            // see the injection below — so for it the body shares a scope with the head's own
            // expressions too, and a body declaration shadows every name they read, not only the
            // loop variable's:
            //   const n = 3; for (let r = 1; r <= n; ++r) { const n = 1; }
            // read the body's `n` in the test and threw "Cannot access 'n' before initialization"
            // on the first evaluation, one iteration before that binding exists. Which names the
            // test and update read is what decides it, so collect them alongside the head's.
            var headExpressionNames = cStyle && declaration.Kind == FastVariableKind.Let
                ? IdentifierNameCollector.Collect(test, update)
                : null;
            var nestBody = sourceBlock != null
                && BodyShadowsHeadBinding(declaration, body, headExpressionNames);

            var statementList = new Sequence<AstStatement>(nestBody ? 2 : body.Count + 1) { null! };
            if (nestBody)
                statementList.Add(sourceBlock!);
            else
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
            // value (lowered by FastCompiler's VisitForInStatement and VisitForOfStatement, in
            // Statements/FastCompiler.VisitFor.cs), not from a carrier — they must keep the plain
            // lowering.
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
                        var tempID = NewTempName();
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
                    var tid = NewTempName();
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
            // copying each per-iteration binding back into its carrier (see below) before the next
            // iteration's increment reads it (test262 sm/lexical-environment/for-loop's "incr"
            // closures). Only a loop-env update that actually contains a closure needs this.
            var incrementAtTop = useLoopEnv && update != null && ContainsClosure(update);
            AstIdentifier startedFlag = null;
            if (incrementAtTop)
            {
                var flagId = NewTempName();
                startedFlag = new AstIdentifier(declaration.Start, flagId);
                tempDeclarations.Add(new VariableDeclarator(startedFlag, new AstLiteral(TokenTypes.False, declaration.Start)));
            }

            // ForBodyEvaluation performs CreatePerIterationEnvironment AFTER the body and BEFORE the
            // increment, seeding the next iteration from the values the body last saw. So a mutation the
            // BODY makes to a loop binding has to reach the next iteration: copy each per-iteration
            // binding back into its carrier at the end of the body. Without the copy an empty increment
            // left nothing to advance the carrier and the loop re-read the seed value forever —
            // `for (let x = 0; x < 10;) { x++; }` never terminated (test262
            // language/statements/continue/no-label-continue and its six siblings).
            //
            // `continue` jumps past the end of the body straight to the increment, so a body that can
            // `continue` takes the copy in a `finally`; one that cannot only needs it as a trailing
            // statement, which keeps the hot `for (let i = 0; i < n; i++)` shape free of a per-iteration
            // try/finally frame (worth ~25% on a tight counting loop). The loop's ContainsContinue, which
            // the parser recorded from its count of the continue statements it parsed, is what decides,
            // because it cannot report a false negative. `break` and `return` need no copy either way:
            // they leave the loop, and the carrier is never read again.
            if (useLoopEnv && changes.Count > 0)
            {
                var copyBackStatements = new Sequence<AstStatement>();
                foreach (var (id, temp) in changes)
                    copyBackStatements.Add(new AstExpressionStatement(new AstBinaryExpression(
                        new AstIdentifier(temp.Start, temp.Name.Value), TokenTypes.Assign, new AstIdentifier(temp.Start, id)))
                    {
                        IsLoopBookkeeping = true
                    });

                if (incrementAtTop || bodyContainsContinue)
                {
                    // Wrap the user body in `try { … } finally { <carrier> = x; … }`. Rebuild
                    // statementList ([0] is the scoped-decl slot).
                    var bodyStatements = new Sequence<AstStatement>();
                    for (int bi = 1; bi < statementList.Count; bi++)
                        bodyStatements.Add(statementList[bi]);

                    var tryBody = new AstBlock(declaration.Start, declaration.End, bodyStatements);
                    var finallyBody = new AstBlock(declaration.Start, declaration.End, copyBackStatements);
                    statementList = [null!, new AstTryStatement(declaration.Start, declaration.End, tryBody, null, null, finallyBody)
                    {
                        IsLoopBookkeeping = true
                    }];
                }
                else
                {
                    statementList.AddRange(copyBackStatements);
                }
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
                        statementList.Insert(1, new AstIfStatement(test.Start, test.End, notTest, breakStatement)
                        {
                            IsLoopBookkeeping = true
                        });
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
                                new AstLiteral(TokenTypes.True, declaration.Start))))
                        {
                            IsLoopBookkeeping = true
                        };
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
            // let-closure-inside-condition). The end-of-body copy above runs first, so the update reads
            // the value the body left behind, matching ForBodyEvaluation's
            // CreatePerIterationEnvironment-then-increment order.

            // The per-iteration scoped declaration carries the original head's
            // `using` / `await using` disposal markers, so a `for (using x of …)`
            // binding is disposed at the end of each iteration's block scope.
            statementList[0] = new AstVariableDeclaration(declaration.Start, declaration.End, scopedDeclarations, declaration.Kind, declaration.Using, declaration.AwaitUsing)
            {
                IsLoopBookkeeping = true
            };

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
            // When the body was nested rather than spliced it is still its own block, and it
            // keeps carrying its own hoisting scope and Annex B names — re-hoisting them into
            // the synthetic outer block would bind them one scope too high.
            var combinedHoisting = CombineHoisting(
                useLoopEnv || requiresReplacement ? hoisted : null,
                nestBody ? null : sourceBlock?.HoistingScope);
            if (combinedHoisting != null)
                block.HoistingScope = combinedHoisting;

            if (!nestBody && sourceBlock?.AnnexBFunctionNames != null)
                block.AnnexBFunctionNames = sourceBlock.AnnexBFunctionNames;

            return (r, block, update, test);
        }

        private static IFastEnumerable<StringSpan> GetBindingNames(AstVariableDeclaration declaration)
        {
            var names = new Sequence<StringSpan>();
            var en = declaration.Declarators.GetFastEnumerator();
            while (en.MoveNext(out var d))
                CollectBindingNames(d.Identifier, names);
            return names;
        }

        /// <summary>
        /// True when a statement at the top level of the loop body declares a name the head
        /// also binds — or, for a head whose test and update are evaluated in the same block,
        /// a name one of them reads — so the body must keep its own scope instead of being
        /// spliced into the per-iteration block (see the call site).
        /// </summary>
        /// <remarks>
        /// Only the body's OWN declarations can collide: anything inside a nested block, loop
        /// or function already has a scope of its own. A `var` cannot collide either — it is
        /// hoisted to the enclosing function, and a `var` sharing a name with a lexical head
        /// binding is an early error the validator reports.
        /// </remarks>
        private static bool BodyShadowsHeadBinding(
            AstVariableDeclaration declaration,
            IFastEnumerable<AstStatement> body,
            HashSet<string> headExpressionNames = null)
        {
            var headNames = new Sequence<StringSpan>();
            var headEn = declaration.Declarators.GetFastEnumerator();
            while (headEn.MoveNext(out var headDeclarator))
                CollectBindingNames(headDeclarator.Identifier, headNames);

            var hasHeadExpressionNames = headExpressionNames is { Count: > 0 };
            if (headNames.Count == 0 && !hasHeadExpressionNames)
                return false;

            bool IsHeadName(StringSpan name)
            {
                foreach (var headName in headNames)
                {
                    if (string.Equals(headName.Value, name.Value, StringComparison.Ordinal))
                        return true;
                }

                return hasHeadExpressionNames
                    && name.Value != null
                    && headExpressionNames!.Contains(name.Value);
            }

            var statements = body.GetFastEnumerator();
            while (statements.MoveNext(out var statement))
            {
                switch (statement)
                {
                    case AstVariableDeclaration { Kind: not FastVariableKind.Var } lexical:
                        var bodyNames = new Sequence<StringSpan>();
                        var declarators = lexical.Declarators.GetFastEnumerator();
                        while (declarators.MoveNext(out var declarator))
                            CollectBindingNames(declarator.Identifier, bodyNames);

                        foreach (var bodyName in bodyNames)
                        {
                            if (IsHeadName(bodyName))
                                return true;
                        }
                        break;

                    case AstExpressionStatement
                    {
                        Expression: AstClassExpression { IsDeclaration: true, Identifier: { } classIdentifier }
                    }:
                        if (IsHeadName(classIdentifier.Name))
                            return true;
                        break;

                    case AstExpressionStatement
                    {
                        Expression: AstFunctionExpression { IsStatement: true, Id: { } functionIdentifier }
                    }:
                        if (IsHeadName(functionIdentifier.Name))
                            return true;
                        break;
                }
            }

            return false;
        }

        private static void CollectBindingNames(AstExpression expression, Sequence<StringSpan> names)
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

        private static bool ContainsClosure(AstNode node)
        {
            var detector = new ClosureDetector();
            detector.Visit(node);
            return detector.Found;
        }

        private static IFastEnumerable<StringSpan> CombineHoisting(
            IFastEnumerable<StringSpan> loopBindings,
            IFastEnumerable<StringSpan> bodyHoisting)
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
}
