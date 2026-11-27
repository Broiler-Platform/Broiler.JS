using Broiler.ECMAScript.Runners;
using Broiler.ECMAScript.Syntax;

namespace Broiler.ECMAScript.Probe;

/// <summary>
/// The corpus digest mode's request and line (SJL-15.2 stage 8; the session's decision D-js-digest-model, and decision
/// D-js-part-g's "the corpus runner's dump mode and the Native AOT probe request the model by stage 8"): the request
/// <see cref="ParseCorpus.Request"/> builds, copied member by member with <see cref="ParseRequest.BuildSemanticModel"/>
/// on, so that the parse's canonical dump holds the model's section; and the line <see cref="CorpusDigest"/> writes for
/// it. <see cref="ParseCorpus.Request"/>'s own request is unchanged, since bundle sjl-13-001's part A runner compiles it.
/// The Frontend tests hold the copy equal to the original in every public member but that one, by reflection.
/// </summary>
internal static class CorpusDigestRequest
{
    /// <summary>
    /// A copy of <paramref name="request"/> in every public member, its source, goal and providers through the
    /// constructor and each init-only member through its own setter, which validates as it validated the original, with
    /// <see cref="ParseRequest.BuildSemanticModel"/> on.
    /// </summary>
    internal static ParseRequest WithModel(ParseRequest request) =>
        new(request.Source, request.Goal, request.UnicodeTables, request.RegularExpressions)
        {
            Switches = request.Switches,
            Context = request.Context,
            Proposals = request.Proposals,
            Extensions = request.Extensions,
            Limits = request.Limits,
            FunctionKind = request.FunctionKind,
            RetainTokens = request.RetainTokens,
            Meter = request.Meter,
            BuildSemanticModel = true,
        };

    /// <summary>One line of the digest: the id, the variant, the outcome and the SHA-256 of the parse's canonical dump.</summary>
    internal static string Line(string id, string variant, ParseResult result) =>
        id + "\t" + variant + "\t" + (result.Outcome == ParseOutcome.Accepted ? "accepted" : "refused") + "\t" + SyntaxDump.Digest(result);
}
