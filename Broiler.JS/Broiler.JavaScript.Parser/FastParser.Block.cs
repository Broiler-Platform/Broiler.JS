
using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.Parser;


partial class FastParser
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void SkipNewLines()
    {
        var type = stream.Current.Type;

        while (type == TokenTypes.LineTerminator)
            type = stream.Consume().Type;
    }

    bool Block(out AstBlock node, bool isProgramRoot = false)
    {
        var begin = stream.Current;
        var list = new Sequence<AstStatement>();
        var scope = variableScope.Push(begin, FastNodeType.Block);

        // Only the Script's root StatementList is the script top level; any nested Block
        // (this method called for a block statement, a function body, a try/catch clause, …)
        // is a `using`-permitting container, so clear the flag while parsing its statements.
        var previousScriptTopLevel = atScriptTopLevel;
        atScriptTopLevel = isProgramRoot;

        // A Script/eval program's StatementList ends at EOF; every other Block is
        // brace-delimited (the caller has consumed the opening `{`) and ends at its
        // matching `}`. Accepting the wrong terminator is a syntax error: a stray `}`
        // at the top level (`eval("}")`) or an unterminated block reaching EOF
        // (`eval("{")`) must both throw, rather than parse as an empty/closed program.
        var terminator = isProgramRoot ? TokenTypes.EOF : TokenTypes.CurlyBracketEnd;

        // The Directive Prologue is the leading run of string-literal ExpressionStatements, and
        // an empty statement or an empty block ENDS it: `function f(){ ; 'use strict'; }` is not
        // strict. Dropping that `;` outright would move the string to the front of the list and
        // make it a directive, so the first one is held back and emitted only if a string
        // literal really does follow it — the one case where it can still be seen. Held rather
        // than kept, because a program that is nothing but empty statements must still come out
        // empty (see IsElidableStatementListItem).
        AstStatement heldPrologueTerminator = null;
        var prologuePossible = true;

        try
        {
            do
            {
                if (stream.CheckAndConsume(terminator))
                    break;

                if (Statement(out var stmt))
                {
                    if (!prologuePossible)
                    {
                        if (!IsElidableStatementListItem(stmt))
                            list.Add(stmt);

                        continue;
                    }

                    if (IsElidableStatementListItem(stmt))
                    {
                        // The first one is the prologue's terminator; a later one adds nothing
                        // to that and is dropped like any other.
                        heldPrologueTerminator ??= stmt;
                        continue;
                    }

                    if (stmt is AstExpressionStatement { Expression: AstLiteral })
                    {
                        // A literal keeps the prologue open — unless something already closed
                        // it, in which case that statement has to be there to say so.
                        if (heldPrologueTerminator != null)
                        {
                            list.Add(heldPrologueTerminator);
                            heldPrologueTerminator = null;
                            prologuePossible = false;
                        }

                        list.Add(stmt);
                        continue;
                    }

                    // Anything else closes the prologue by itself, so the terminator is no
                    // longer worth keeping.
                    heldPrologueTerminator = null;
                    prologuePossible = false;
                    list.Add(stmt);
                    continue;
                }

                if (stream.CheckAndConsumeWithLineTerminator(TokenTypes.SemiColon))
                    continue;

                if (stream.CheckAndConsume(terminator))
                    break;

                // Statement() rewound to its first token when it gave up, so that token is
                // all this frame knows about — and on a minified bundle it is character 1
                // of the file. UnexpectedStatement adds where the parse actually stopped.
                throw stream.UnexpectedStatement();
            } while (true);

            node = new AstBlock(begin, PreviousToken, list)
            {
                HoistingScope = scope.GetVariables(),
                AnnexBFunctionNames = scope.GetAnnexBNames()
            };
        }
        finally
        {
            atScriptTopLevel = previousScriptTopLevel;
            scope.Dispose();
        }

        return true;
    }

    /// <summary>
    /// Whether <paramref name="statement"/> can be dropped from the StatementList it was
    /// parsed into, instead of being carried through the rest of the pipeline.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An empty statement and a block with nothing in it declare nothing, evaluate nothing
    /// and produce the EMPTY completion, which UpdateEmpty leaves the running value alone
    /// for — so a StatementList means the same thing without them. The compiler already
    /// discards both (FastCompiler.VisitBlock / VisitProgram elide a statement that
    /// compiles to nothing), which is what keeps a program of a million <c>{}</c> out of
    /// RyuJIT; dropping them one stage earlier keeps the same program out of the AST.
    /// </para>
    /// <para>
    /// That is what regress-610026 measures: <c>eval</c> of 2^21 <c>{}</c>, where every
    /// unit cost a scope, a statement sequence and a node the compiler was going to throw
    /// away, and the parse ran out of memory before the compiler ever saw it. Elision is
    /// recursive by construction — <c>{{}}</c>'s inner block goes first and leaves the
    /// outer one empty in turn.
    /// </para>
    /// </remarks>
    static bool IsElidableStatementListItem(AstStatement statement)
        => statement switch
        {
            AstBlock block => block.Statements.Count == 0,
            AstExpressionStatement { Expression: AstEmptyExpression } => true,
            _ => false,
        };
}
