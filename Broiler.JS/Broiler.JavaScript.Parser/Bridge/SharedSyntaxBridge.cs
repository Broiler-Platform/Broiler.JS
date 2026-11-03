#nullable enable

using System;
using Broiler.ECMAScript.Syntax;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime.Unicode;

namespace Broiler.JavaScript.Parser.Bridge;

/// <summary>
/// The bridge (milestone SJL-14.2, window W1; Broiler.VM's JSD-0083, decisions D41 and D44): parses with the shared
/// syntax library, <c>Broiler.ECMAScript.Frontend</c>, and builds this engine's <see cref="AstProgram"/> from the
/// library's tree, token facts and side tables, so that the compiler parses through the library and reads the tree it
/// has always read.
/// </summary>
/// <remarks>
/// <para>
/// <b>Behind a test-only switch</b> (decision D43): the compiler parses through the bridge only when its options'
/// internal switch is on, which tests set and the shell's hidden flag sets; it is off by default, and with it off no
/// product path reaches this class. Window W2 turns it on and removes it.
/// </para>
/// <para>
/// <b>The request</b> carries what the old parser's options carried (the goal and the permission to await at the top
/// level) and what decision D44 adds: eval code's inherited context, the dynamic function's goals and kind, this
/// engine's proposal flags (decorators, <c>accessor</c>, and the two import-phase call forms, decision D17), the
/// <c>m</c> suffix through the lexer extension, the library's supported maximum nesting (decision D47), the first
/// error, this engine's Unicode tables and its regular-expression validator (<see cref="BridgeRegularExpressions"/>,
/// decision D51).
/// </para>
/// <para>
/// <b>The parse thread</b> (decision D47 (a)): the parse and the tree's construction run on a thread whose stack is at
/// least the library's documented <see cref="ParseLimits.StackBytes"/>: on the calling thread when it is a compile
/// worker the engine started with such a stack, and otherwise on a compile worker of at least that stack, whatever
/// size the engine's compile stack is configured to (<c>CompilationStack.RunOnFreshStack(work, minimumStackBytes)</c>).
/// The parse uses no <c>StackSegment</c>.
/// </para>
/// <para>
/// <b>A refusal</b> is the library's first diagnostic, thrown as today's exception type,
/// <see cref="FastParseException"/>, at the token where the diagnostic's span begins, with the text this engine's
/// message table renders (<see cref="BridgeMessages"/>, decision D46).
/// </para>
/// </remarks>
internal static class SharedSyntaxBridge
{
    // Decision D17: what this engine parses today among the proposals' forms.
    private const ProposalFlags Proposals = ProposalFlags.Decorators | ProposalFlags.AutoAccessors
        | ProposalFlags.ImportSourceCalls | ProposalFlags.ImportDeferCalls;

    /// <summary>Parses <paramref name="code"/> under <paramref name="request"/> and returns this engine's tree.</summary>
    /// <exception cref="FastParseException">The library refused the source, or the bridge refused what this engine cannot honour.</exception>
    internal static AstProgram Parse(in StringSpan code, BridgeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Goal is BridgeGoal.FormalParameters or BridgeGoal.FunctionBody)
            throw new ArgumentException("The dynamic function's goals are checked by CheckDynamicFunction, not parsed to a tree.", nameof(request));

        var span = code;
        return OnParseThread(() =>
        {
            var tokens = new BridgeTokens(span);
            var result = SyntaxParser.Parse(Request(span, request));
            // A script's top-level await is refused as the library refuses it, at the token where its diagnostic
            // begins and with that diagnostic's text: the old parser never read it as an await expression, so the text
            // the shell's top-level-await retry reads ("Unexpected await") never reached it from a script, and the
            // bridge gives that retry nothing the old parser did not.
            if (result.Outcome != ParseOutcome.Accepted || result.Tree is null || result.Declarations is null)
                throw Refusal(result, span, request, tokens);

            var program = new BridgeTreeBuilder(span, tokens, result.Declarations, request).Build(result.Tree);
            tokens.Link();
            return program;
        });
    }

    /// <summary>
    /// CreateDynamicFunction's two parses (ECMA-262 clause 20.2.1.1.1, steps 18 to 20): the parameter text under the
    /// FormalParameters goal and the body text under the FunctionBody goal, each alone, with the function's kind.
    /// </summary>
    /// <exception cref="FastParseException">Either piece is refused.</exception>
    internal static void CheckDynamicFunction(string parameters, string body, BridgeFunctionKind kind)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(body);
        OnParseThread(() =>
        {
            Check(parameters, BridgeGoal.FormalParameters);
            Check(body, BridgeGoal.FunctionBody);
            return true;
        });

        void Check(string text, BridgeGoal goal)
        {
            var span = new StringSpan(text);
            var request = new BridgeRequest { Goal = goal, FunctionKind = kind };
            var result = SyntaxParser.Parse(Request(span, request));
            if (result.Outcome != ParseOutcome.Accepted)
                throw Refusal(result, span, request, new BridgeTokens(span));
        }
    }

    /// <summary>The library's request for <paramref name="request"/> over <paramref name="code"/>.</summary>
    internal static ParseRequest Request(in StringSpan code, BridgeRequest request)
    {
        var goal = request.Goal switch
        {
            BridgeGoal.Script => ParseGoal.Script,
            BridgeGoal.Module => ParseGoal.Module,
            BridgeGoal.Eval => ParseGoal.Eval,
            BridgeGoal.FormalParameters => ParseGoal.FormalParameters,
            BridgeGoal.FunctionBody => ParseGoal.FunctionBody,
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Goal, "Not a goal."),
        };

        var switches = ParseSwitches.For(goal) with
        {
            ForcedStrict = request.ForcedStrict,
            TopLevelAwait = goal is ParseGoal.Script or ParseGoal.Module && request.AllowTopLevelAwait,
        };

        var source = new SourceText(code.Value ?? string.Empty, string.Empty, code.Offset);
        return new ParseRequest(source, goal, JSUnicodeTables.Instance, BridgeRegularExpressions.Instance)
        {
            Switches = switches,
            Context = goal == ParseGoal.Eval
                ? new InheritedContext
                {
                    InFunction = request.InFunction,
                    InMethod = request.InMethod,
                    InDerivedConstructor = request.InDerivedConstructor,
                    InClassFieldInitializer = request.InClassFieldInitializer,
                    VisiblePrivateNames = request.VisiblePrivateNames,
                }
                : null,
            Proposals = Proposals,
            Extensions = LexerExtensions.DecimalSuffix,
            Limits = ParseLimits.Default,
            FunctionKind = goal is ParseGoal.FormalParameters or ParseGoal.FunctionBody
                ? request.FunctionKind switch
                {
                    BridgeFunctionKind.Generator => DynamicFunctionKind.Generator,
                    BridgeFunctionKind.Async => DynamicFunctionKind.Async,
                    BridgeFunctionKind.AsyncGenerator => DynamicFunctionKind.AsyncGenerator,
                    _ => DynamicFunctionKind.Normal,
                }
                : DynamicFunctionKind.Normal,
        };
    }

    private static FastParseException Refusal(ParseResult result, in StringSpan code, BridgeRequest request, BridgeTokens tokens)
    {
        if (result.Diagnostics.Count == 0)
            return new FastParseException(tokens.Eof, "The shared syntax library gave no answer for the source (" + result.Outcome + ")");

        var diagnostic = result.Diagnostics[0];
        var text = code.Value ?? string.Empty;
        var message = BridgeMessages.Render(diagnostic, text, code.Offset, request.Goal == BridgeGoal.Module);
        return new FastParseException(tokens.At(diagnostic.Span.Offset), message);
    }

    // Decision D47 (a): the library's documented stack, the hop skipped on a compile worker that has it. The hop's
    // worker has at least that stack whatever CompilationStack.SizeBytes is configured to.
    private static T OnParseThread<T>(Func<T> parse)
        => CompilationStack.CurrentWorkerStackBytes >= ParseLimits.StackBytes
            ? parse()
            : CompilationStack.RunOnFreshStack(parse, ParseLimits.StackBytes);
}
