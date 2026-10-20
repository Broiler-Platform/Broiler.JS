namespace Broiler.JavaScript.Parser;

/// <summary>
/// The goal symbol a parse starts from (ECMA-262 section 5.1.4, "The Syntactic Grammar").
/// </summary>
/// <remarks>
/// The parser parses two goals, <see cref="Script"/> and <see cref="Module"/>. The other three are
/// the goals the shared front end's contract names (Broiler.VM's JSD-0080, decisions D19 and D41),
/// reserved at milestone SJL-12: <see cref="FastParserOptions"/> refuses them, so no parse runs
/// under one of them. Eval code is parsed as a <see cref="Script"/>, which is how the specification's
/// PerformEval parses it; the Function constructor assembles its source and parses that as a
/// <see cref="Script"/> too.
/// </remarks>
public enum FastParserGoal
{
    /// <summary>The <i>Script</i> goal: script code, eval code and the Function constructor's source.</summary>
    Script = 0,

    /// <summary>
    /// The <i>Module</i> goal: import and export declarations at the top level, strict code throughout,
    /// <c>await</c> reserved, and HTML-like comments not comments (Annex B.1.1).
    /// </summary>
    Module = 1,

    /// <summary>Reserved: eval code as a goal of its own. Refused at SJL-12.</summary>
    Eval = 2,

    /// <summary>Reserved: the Function constructor's parameter text, parsed alone. Refused at SJL-12.</summary>
    FormalParameters = 3,

    /// <summary>Reserved: the Function constructor's body text, parsed alone. Refused at SJL-12.</summary>
    FunctionBody = 4,
}
