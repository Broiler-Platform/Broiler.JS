#nullable enable

using System;
using System.Collections.Generic;
using Broiler.Unicode.Algorithms;

namespace Broiler.JavaScript.Parser;

/// <summary>
/// What one parse is asked to parse: its goal symbol, whether <c>await</c> may stand at its top level,
/// and the provider its lexical classes come from. Immutable; a scanner, a token stream and a parser
/// each take it when they are made, so nothing a parse depends on comes from the thread or the
/// asynchronous flow it runs on.
/// </summary>
/// <remarks>
/// <para>
/// These are the in-place options of the shared JavaScript libraries programme's milestone SJL-12.5,
/// item 1 (Broiler.VM's JSD-0080, decision D19). Before them the goal and the top-level-<c>await</c>
/// permission reached the parser through ambient scopes of the runtime's <c>CoreScript</c>, and so
/// reached every compile that ran inside them.
/// </para>
/// <para>
/// A token stream is made with its options because the scanner reads its first two tokens in its
/// constructor, and the goal decides how it reads them: HTML-like comments are comments only in a
/// script. A parser takes its stream's options, and refuses a stream made with other options than
/// the ones it is given.
/// </para>
/// <para>
/// Equality is by value: two options are equal when their goals and permissions are equal and they
/// name the same provider.
/// </para>
/// </remarks>
public sealed record FastParserOptions
{
    private readonly FastParserGoal goal;
    private readonly IUnicodeTables? unicodeTables;
    private readonly EcmaScriptLexical? lexical;

    /// <summary>A script without top-level <c>await</c>, over the engine's own Unicode tables.</summary>
    /// <remarks>
    /// What the constructors without options parse, and what eval code, the Function constructor's
    /// source and every other compile made while a program runs are parsed as.
    /// </remarks>
    public static FastParserOptions Script { get; } = new();

    /// <summary>A module, which may <c>await</c> at its top level, over the engine's own Unicode tables.</summary>
    /// <remarks>What <see cref="FastParser.ParseModule(in Broiler.JavaScript.Ast.Misc.StringSpan)"/> parses.</remarks>
    public static FastParserOptions Module { get; } = new() { Goal = FastParserGoal.Module, AllowTopLevelAwait = true };

    /// <summary>The goal symbol: <see cref="FastParserGoal.Script"/>, the default, or <see cref="FastParserGoal.Module"/>.</summary>
    /// <exception cref="NotSupportedException">The goal is one of the three reserved at milestone SJL-12.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a goal.</exception>
    public FastParserGoal Goal
    {
        get => goal;
        init => goal = value switch
        {
            FastParserGoal.Script or FastParserGoal.Module => value,
            FastParserGoal.Eval or FastParserGoal.FormalParameters or FastParserGoal.FunctionBody => throw new NotSupportedException(
                "The goal " + value + " is reserved: at milestone SJL-12 the parser parses the Script and Module goals only (Broiler.VM's JSD-0080, decision D19). "
                + "Eval code is parsed as a Script."),
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Not a goal symbol."),
        };
    }

    /// <summary>
    /// Whether an <c>await</c> outside every function is an AwaitExpression and a top-level
    /// <c>for await</c> is a statement. False by default; <see cref="Module"/> sets it, and so does
    /// the engine's <c>EvalWithTopLevelAwaitAsync</c> for the program it is given, and for nothing
    /// that program compiles while it runs.
    /// </summary>
    public bool AllowTopLevelAwait { get; init; }

    /// <summary>
    /// The provider the scanner's lexical classes come from: IdentifierStartChar, IdentifierPartChar
    /// and WhiteSpace, as Broiler.Unicode's <see cref="EcmaScriptLexical"/> reads them from these
    /// tables. Null, the default, is the engine's own tables, the Unicode version its pin names.
    /// </summary>
    /// <remarks>
    /// The provider is read through when it is set. A regular-expression literal's early errors do
    /// not come through it: their validation is the engine's own, behind the contract's
    /// regular-expression seam, until milestone SJL-11.
    /// </remarks>
    /// <exception cref="ArgumentException">The tables are not of the version Broiler.Unicode reads, or lack a table the lexical classes need.</exception>
    public IUnicodeTables? UnicodeTables
    {
        get => unicodeTables;
        init
        {
            lexical = value is null ? null : new EcmaScriptLexical(new UnicodeProperties(value));
            unicodeTables = value;
        }
    }

    /// <summary>The lexical classes over <see cref="UnicodeTables"/>, built once; null for the engine's own tables.</summary>
    internal EcmaScriptLexical? Lexical => lexical;

    /// <summary>Whether this is the Module goal.</summary>
    internal bool IsModule => goal == FastParserGoal.Module;

    /// <summary>
    /// The options the runtime's ambient goal and top-level-<c>await</c> scopes describe, which is what
    /// the constructors without options parse until every compile site passes its own.
    /// </summary>
    internal static FastParserOptions FromAmbientScopes()
    {
        var module = Runtime.CoreScript.IsModuleGoal;
        var topLevelAwait = Runtime.CoreScript.AllowTopLevelAwait;
        return (module, topLevelAwait) switch
        {
            (false, false) => Script,
            (true, true) => Module,
            _ => new FastParserOptions { Goal = module ? FastParserGoal.Module : FastParserGoal.Script, AllowTopLevelAwait = topLevelAwait },
        };
    }

    /// <inheritdoc />
    public bool Equals(FastParserOptions? other) =>
        other is not null
        && (ReferenceEquals(this, other)
            || (goal == other.goal
                && AllowTopLevelAwait == other.AllowTopLevelAwait
                && EqualityComparer<IUnicodeTables?>.Default.Equals(unicodeTables, other.unicodeTables)));

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(goal, AllowTopLevelAwait, unicodeTables);
}
