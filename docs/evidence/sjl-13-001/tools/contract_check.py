#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Part H's record-consistency check (docs/evidence/sjl-13-001/decision-rule.md): the library against the contract.

Usage:
    contract_check.py --jsd0080 FILE --public-api FILE --project FILE --rows FILE --stages FILE [--out FILE]

FILE for --jsd0080 is Broiler.VM's JSD-0080 as the commit the bundle names holds it (`git show <commit>:<path>`); the
others are Broiler.ECMAScript.Frontend's PublicAPI.txt, its project file, its Rows.cs (the library's diagnostic
producers, one entry per catalogue row it reports) and the stage table Broiler.ECMAScript/docs/frontend/stages.tsv.

It lists:
1. for each row of JSD-0080's input and output tables ("### Input", "### Output") and each bullet of its "### Limits" and
   "### Providers" sections, every item the row names and the public member of PublicAPI.txt that carries it, or
   "absent"; a row or bullet this script has no item list for is listed "unmapped". The caller's lexical names are
   listed absent with decision D16's reason (JSD-0083: no caller's lexical names at SJL-13 or SJL-14);
2. every line of PublicAPI.txt, and every reference of the project file, that names a type or package of Broiler.VM
   or Broiler.JS (the engine-namespace list: "Broiler.VM.", "Broiler.JavaScript."); none is expected;
3. every row the stage table assigns `engine` for which Rows.cs declares a producer; none is expected.

It exits 0 when every item has a member but the caller's lexical names, nothing names an engine, and no engine row has
a producer; 1 otherwise. Its item lists say where each contract item is carried; they decide nothing of the outcome but
which member to look for.
"""
import argparse
import re
import sys

ENGINE_NAMESPACES = ("Broiler.VM.", "Broiler.JavaScript.")
S = "Broiler.ECMAScript.Syntax."
M = "Broiler.ECMAScript.Semantics."

# The items each row or bullet of JSD-0080 names, and the public member that carries each (a prefix of a PublicAPI.txt
# line). An item whose member is None is absent by decision, with the reason given.
ITEMS = {
    ("Input", "Source"): [
        ("text", S + "SourceText.get_Text"), ("source name", S + "SourceText.get_Name"),
        ("base offset", S + "SourceText.get_BaseOffset"), ("base line", S + "SourceText.get_BaseLine"),
        ("base column", S + "SourceText.get_BaseColumn"), ("the request's source", S + "ParseRequest.get_Source")],
    ("Input", "Goal"): [
        ("Script", S + "ParseGoal.Script"), ("Module", S + "ParseGoal.Module"), ("Eval", S + "ParseGoal.Eval"),
        ("FormalParameters", S + "ParseGoal.FormalParameters"), ("FunctionBody", S + "ParseGoal.FunctionBody"),
        ("the request's goal", S + "ParseRequest.get_Goal")],
    ("Input", "Switches"): [
        ("forced strictness", S + "ParseSwitches.get_ForcedStrict"), ("top-level await", S + "ParseSwitches.get_TopLevelAwait"),
        ("Annex B", S + "ParseSwitches.get_AnnexB"), ("HTML-like comments", S + "ParseSwitches.get_HtmlLikeComments"),
        ("the request's switches", S + "ParseRequest.get_Switches")],
    ("Input", "Inherited evaluation context"): [
        ("Strict (as forced strictness, decision D16)", S + "ParseSwitches.get_ForcedStrict"),
        ("InFunction", S + "InheritedContext.get_InFunction"), ("InMethod", S + "InheritedContext.get_InMethod"),
        ("InDerivedConstructor", S + "InheritedContext.get_InDerivedConstructor"),
        ("InClassFieldInitializer", S + "InheritedContext.get_InClassFieldInitializer"),
        ("InClassBody (as the visible private names' absence, decision D16)", S + "InheritedContext.get_VisiblePrivateNames"),
        ("the visible private names", S + "InheritedContext.get_VisiblePrivateNames"),
        ("the caller's lexical names", None), ("the request's context", S + "ParseRequest.get_Context")],
    ("Input", "Proposal flags"): [
        ("decorators", S + "ProposalFlags.Decorators"), ("accessor", S + "ProposalFlags.AutoAccessors"),
        ("import phases: import source declarations", S + "ProposalFlags.ImportSourceDeclarations"),
        ("import phases: import defer declarations", S + "ProposalFlags.ImportDeferDeclarations"),
        ("import phases: import.source calls", S + "ProposalFlags.ImportSourceCalls"),
        ("import phases: import.defer calls", S + "ProposalFlags.ImportDeferCalls"),
        ("off by default", S + "ProposalFlags.None"), ("the request's proposals", S + "ParseRequest.get_Proposals")],
    ("Input", "Lexer extensions"): [
        ("the m suffix", S + "LexerExtensions.DecimalSuffix"), ("the request's extensions", S + "ParseRequest.get_Extensions")],
    ("Input", "Limits"): [("the request's limits", S + "ParseRequest.get_Limits")],
    ("Input", "Providers"): [
        ("the Unicode provider", S + "ParseRequest.get_UnicodeTables"),
        ("the regular-expression provider", S + "ParseRequest.get_RegularExpressions")],
    ("Output", "Tree"): [
        ("immutable nodes", S + "SyntaxNode "), ("the root", S + "ParseResult.get_Tree"),
        ("full spans: offset", S + "SourceSpan.get_Offset"), ("full spans: length", S + "SourceSpan.get_Length"),
        ("full spans: line", S + "SourceSpan.get_Line"), ("full spans: column", S + "SourceSpan.get_Column"),
        ("a node's span", S + "SyntaxNode.get_Span"),
        ("directive raw text", S + "ExpressionStatement.get_Directive"),
        ("legacy-octal flags", S + "NumericLiteralFlags.LegacyOctal"),
        ("escape flags", S + "StringLiteralFlags.LegacyOctalEscape"),
        ("cooked template chunks", S + "TemplateElement.get_Cooked"), ("raw template chunks", S + "TemplateElement.get_Raw"),
        ("regular-expression pattern", S + "RegularExpressionLiteral.get_Pattern"),
        ("regular-expression flags", S + "RegularExpressionLiteral.get_Flags"),
        ("BigInt literal text", S + "NumericLiteral.get_Text"), ("the BigInt flag", S + "NumericLiteralFlags.BigInt"),
        ("parenthesisation", S + "ParenthesizedExpression "), ("trailing commas", S + "ArrayLiteral.get_TrailingComma"),
        ("side facts in identity-keyed tables", S + "ParseResult.get_Declarations")],
    ("Output", "Diagnostics"): [
        ("a stable code", S + "SyntaxDiagnostic.get_Code"), ("a phase", S + "SyntaxDiagnostic.get_Phase"),
        ("tokenizer, parser, semantics", S + "DiagnosticPhase.Tokenizer"), ("a span", S + "SyntaxDiagnostic.get_Span"),
        ("a source name", S + "SyntaxDiagnostic.get_SourceName"), ("a message identifier", S + "SyntaxDiagnostic.get_MessageId"),
        ("its arguments", S + "SyntaxDiagnostic.get_Arguments"), ("in a deterministic order", S + "ParseResult.get_Diagnostics")],
    ("Output", "Semantic model"): [
        ("the declarations walk's side tables (SJL-13; the model is SJL-15's)", M + "DeclarationTables "),
        ("each scope's declarations", M + "Scope.get_Declarations"), ("each function's facts", M + "FunctionFacts.get_IsStrict")],
    ("Limits", "The nesting depth is a bounded parameter"): [
        ("the nesting depth", S + "ParseLimits.get_NestingDepth"),
        ("its supported maximum", S + "ParseLimits.MaximumSupportedNestingDepth")],
    ("Limits", "The tree depth is a constant of the library"): [
        ("the tree depth", S + "ParseLimits.MaximumTreeDepth"), ("the stack it requires", S + "ParseLimits.StackBytes")],
    ("Limits", "In both engines, every input deeper than the bound is refused"): [
        ("a refusal, never a lost process", S + "ParseOutcome.Refused"), ("as a diagnostic", S + "SyntaxDiagnostic.get_Code")],
    ("Limits", "A diagnostic cap"): [("the cap", S + "ParseLimits.get_DiagnosticCap")],
    ("Limits", "A first-error or all-errors policy"): [
        ("first error", S + "ErrorReporting.FirstError"), ("all errors", S + "ErrorReporting.AllErrors"),
        ("the policy", S + "ParseLimits.get_Reporting")],
    ("Providers", "Unicode lexical classes"): [("Broiler.Unicode's tables", S + "ParseRequest.get_UnicodeTables")],
    ("Providers", "Regular-expression syntax validation"): [
        ("the seam", S + "IRegularExpressionValidator "), ("its verdict", S + "RegularExpressionVerdict ")],
}

ABSENT_REASONS = {
    "the caller's lexical names": "decision D16 (JSD-0083): no caller's lexical names at SJL-13 or SJL-14; the conflict "
    "they serve is EvalDeclarationInstantiation's, a run-time SyntaxError each engine raises, until SJL-15's model",
}


def contract_rows(text):
    """The rows of the input and output tables and the bullets of the limits and providers sections, by section."""
    rows, section = [], None
    for line in text.split("\n"):
        heading = re.match(r"^### (\w+)", line)
        if line.startswith("## ") or line.startswith("### "):
            section = heading.group(1) if heading else None
            continue
        if section in ("Input", "Output"):
            match = re.match(r"^\| \*\*(.+?)\*\* \|", line)
            if match:
                rows.append((section, match.group(1)))
        elif section in ("Limits", "Providers"):
            match = re.match(r"^- \*\*(.+?)\*\*", line)
            if match:
                rows.append((section, match.group(1).rstrip(".")))
    return rows


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    for name in ("--jsd0080", "--public-api", "--project", "--rows", "--stages"):
        parser.add_argument(name, required=True)
    parser.add_argument("--out")
    args = parser.parse_args()

    with open(args.jsd0080, encoding="utf-8") as handle:
        jsd = handle.read()
    with open(args.public_api, encoding="utf-8") as handle:
        api = [line for line in handle.read().split("\n") if line and not line.startswith("#")]
    with open(args.project, encoding="utf-8") as handle:
        project = handle.read()
    with open(args.rows, encoding="utf-8") as handle:
        rows_source = handle.read()
    with open(args.stages, encoding="utf-8") as handle:
        stages = [line.split("\t") for line in handle.read().split("\n")[1:] if line]

    out, problems = [], 0
    out.append("# 1. Each row and item of JSD-0080's contract, and the public member that carries it")
    found_rows = contract_rows(jsd)
    if not found_rows:
        out.append("problem\tno row of JSD-0080's contract was read")
        problems += 1
    for section, row in found_rows:
        items = ITEMS.get((section, row))
        if items is None:
            out.append("%s\t%s\t-\tunmapped" % (section, row))
            problems += 1
            continue
        for item, member in items:
            if member is None:
                out.append("%s\t%s\t%s\tabsent\t%s" % (section, row, item, ABSENT_REASONS[item]))
                continue
            carrier = next((line for line in api if line.startswith(member)), None)
            if carrier is None:
                out.append("%s\t%s\t%s\tabsent" % (section, row, item))
                problems += 1
            else:
                out.append("%s\t%s\t%s\t%s" % (section, row, item, carrier.split(" ")[0]))

    out.append("# 2. Public API lines and project references naming an engine (none is expected)")
    engine = [line for line in api if any(ns in line for ns in ENGINE_NAMESPACES)]
    engine += ["project: " + m for m in re.findall(r'Include="([^"]*)"', project) if any(ns.rstrip(".") in m for ns in ENGINE_NAMESPACES)]
    out.extend("engine\t" + line for line in engine)
    out.append("engine-lines\t%d" % len(engine))
    problems += len(engine)

    out.append("# 3. Rows the stage table assigns engine, with a producer in the library (none is expected)")
    producers = set(re.findall(r"static readonly LexicalRefusal (\w+)\s*=", rows_source))
    engine_rows = [row[0] for row in stages if row[2] == "engine"]
    produced = [row for row in engine_rows if row in producers]
    out.extend("engine-row-producer\t" + row for row in produced)
    out.append("engine-rows\t%d\tproducers\t%d\twith-a-producer\t%d" % (len(engine_rows), len(producers), len(produced)))
    problems += len(produced)

    out.append("problems\t%d" % problems)
    text = "\n".join(out) + "\n"
    if args.out:
        with open(args.out, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(text)
    sys.stdout.write(text)
    return 0 if problems == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
