using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Broiler.ECMAScript.Runners;
using Broiler.ECMAScript.Syntax;

namespace Broiler.ECMAScript.Probe;

/// <summary>
/// The parse corpus as the probe's frontend modes read it (stage SJL-13.2e): <c>conformance/parse/corpus.manifest</c>'s rows,
/// each input's text - a test262 input read from a checkout of the pinned revision and held to the manifest's SHA-256, any
/// other from <c>conformance/parse/</c> - and the requests of the home rule's two configurations, V (Broiler.VM's) and J
/// (Broiler.JS's), for each variant, and the eval requests under the manifest's flag sets E0 to E7.
/// </summary>
internal sealed class ParseCorpus
{
    /// <summary>The strict prologue the manifest names, prepended for the <c>strict</c> variant.</summary>
    internal const string StrictPrologue = "\"use strict\";\n";

    private readonly string corpusRoot;
    private readonly string? test262;
    private readonly string? test262Json;
    private readonly Dictionary<string, int> evalFlags = new(StringComparer.Ordinal);

    /// <summary>One row of the manifest.</summary>
    internal readonly record struct Row(string Id, string Origin, string Path, string Sha256, string Goal, string[] Variants, bool EvalFamily);

    private ParseCorpus(string corpusRoot, string? test262, string? test262Json, List<Row> rows, string digest)
    {
        this.corpusRoot = corpusRoot;
        this.test262 = test262;
        this.test262Json = test262Json;
        Rows = rows;
        Digest = digest;
    }

    /// <summary>The rows, in the manifest's order.</summary>
    internal IReadOnlyList<Row> Rows { get; }

    /// <summary>The manifest's SHA-256, lowercase hexadecimal.</summary>
    internal string Digest { get; }

    /// <summary>The flag sets E0 to E7, by name.</summary>
    internal IReadOnlyDictionary<string, int> EvalFlags => evalFlags;

    /// <summary>Reads <c>corpus.manifest</c> in <paramref name="corpusRoot"/>.</summary>
    internal static ParseCorpus Load(string corpusRoot, string? test262, string? test262Json)
    {
        var bytes = File.ReadAllBytes(Path.Combine(corpusRoot, "corpus.manifest"));
        var rows = new List<Row>();
        var corpus = new ParseCorpus(corpusRoot, test262, test262Json, rows, Convert.ToHexStringLower(SHA256.HashData(bytes)));
        foreach (var line in Encoding.UTF8.GetString(bytes).Split('\n'))
        {
            if (line.Length == 0)
                continue;
            if (line.StartsWith("#% eval-flags\t", StringComparison.Ordinal))
            {
                var fields = line.Split('\t');
                corpus.evalFlags[fields[1]] = int.Parse(fields[2], System.Globalization.NumberStyles.None, System.Globalization.NumberFormatInfo.InvariantInfo);
                continue;
            }

            if (line[0] == '#' || line.StartsWith("id\t", StringComparison.Ordinal))
                continue;
            var c = line.Split('\t');
            rows.Add(new Row(c[0], c[1], c[2], c[3], c[4], c[5].Split(','), c[7] == "yes"));
        }

        return corpus;
    }

    /// <summary>The input's text, its byte-order mark dropped; null when its file is absent or differs from the manifest's SHA-256.</summary>
    internal string? Text(Row row, out string? problem)
    {
        problem = null;
        string path;
        if (row.Origin == "test262")
        {
            if (test262 is null)
            {
                problem = "no test262 checkout was named";
                return null;
            }

            path = Path.Combine(test262, row.Path);
            if (!File.Exists(path) && test262Json is not null)
                path = Path.Combine(test262Json, row.Path);
        }
        else
        {
            path = Path.Combine(corpusRoot, row.Path);
        }

        if (!File.Exists(path))
        {
            problem = "absent: " + path;
            return null;
        }

        var bytes = File.ReadAllBytes(path);
        if (!string.Equals(Convert.ToHexStringLower(SHA256.HashData(bytes)), row.Sha256, StringComparison.Ordinal))
        {
            problem = "differs from the manifest's SHA-256: " + path;
            return null;
        }

        var text = new UTF8Encoding(false, false).GetString(bytes);
        return text.Length > 0 && text[0] == '\uFEFF' ? text[1..] : text;
    }

    /// <summary>
    /// The request of a variant (<c>strict</c>, <c>sloppy</c>, <c>raw</c>, <c>module</c>, <c>json</c>, or an eval flag set
    /// <c>E0</c> to <c>E7</c>) in configuration <c>V</c> or <c>J</c> (the home rule's section "The two configurations"):
    /// V with no proposal, no lexer extension and Broiler.VM's default nesting; J with Broiler.JS's admitted proposals, the
    /// <c>m</c> suffix and the library's supported maximum; an eval request in V.
    /// </summary>
    internal ParseRequest Request(string text, string variant, string configuration, IRegularExpressionValidator validator)
    {
        if (variant.Length == 2 && variant[0] == 'E')
        {
            var flags = evalFlags[variant];
            return new ParseRequest(new SourceText(text), ParseGoal.Eval, UnicodeDataHome.Tables, validator)
            {
                Switches = ParseSwitches.For(ParseGoal.Eval) with { ForcedStrict = (flags & 1) != 0 },
                Context = new InheritedContext
                {
                    InFunction = (flags & 2) != 0,
                    InMethod = (flags & 4) != 0,
                    InDerivedConstructor = (flags & 8) != 0,
                    InClassFieldInitializer = (flags & 16) != 0,
                },
                Limits = new ParseLimits { NestingDepth = new DepthLimit(VmDefaultNesting) },
            };
        }

        var goal = variant == "module" ? ParseGoal.Module : ParseGoal.Script;
        var source = variant == "strict" ? StrictPrologue + text : text;
        var j = configuration == "J";
        return new ParseRequest(new SourceText(source), goal, UnicodeDataHome.Tables, validator)
        {
            Switches = ParseSwitches.For(goal),
            Proposals = j ? ProposalFlags.Decorators | ProposalFlags.AutoAccessors | ProposalFlags.ImportSourceCalls | ProposalFlags.ImportDeferCalls : ProposalFlags.None,
            Extensions = j ? LexerExtensions.DecimalSuffix : LexerExtensions.None,
            Limits = new ParseLimits { NestingDepth = new DepthLimit(j ? ParseLimits.MaximumSupportedNestingDepth : VmDefaultNesting) },
        };
    }

    /// <summary>Broiler.VM's default nesting parameter, <c>SliceParseOptions.DefaultMaximumNestingDepth</c>, configuration V's.</summary>
    internal const int VmDefaultNesting = 64;
}
