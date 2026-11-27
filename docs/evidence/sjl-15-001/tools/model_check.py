#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Part F's checklist (bundle sjl-15-001's rule, docs/evidence/sjl-15-001/decision-rule.md): the semantic model against
the milestone's facts and the contract, as bundle sjl-13-001's part H reads its checklist (decision D-js-part-f-tools).

Usage:
    model_check.py --jsd0080 FILE --milestone FILE --public-api FILE --project FILE --rows FILE --semantic-rows FILE
                   --stages FILE [--vm-digests FILE] [--control-absent PREFIX] [--control-engine PREFIX] [--out FILE]

It extends bundle sjl-13-001's docs/evidence/sjl-13-001/tools/contract_check.py without editing it. That script's lines
1 to 3 (the header), 26 to 131 (the imports, the engine-namespace list, the item lists ITEMS, ABSENT_REASONS and
contract_rows) are kept here verbatim but for an added `import hashlib` line among the imports and a second blank line
after contract_rows, and its main's three listings are kept as listings 1b, 2 and 3 below, with additions; its
docstring is replaced by this one. FILE for --jsd0080 and --milestone is Broiler.VM's JSD-0080 and its
milestone file docs/javascript-shared-libraries/milestones/sjl-15-static-semantics.md, as the Broiler.VM commit
docs/evidence/sjl-15-001/tools/broiler-vm-inputs.sha256 names holds them (`git show <commit>:<path>`); --vm-digests is
that file, and when it is given each input's SHA-256 must be the one it lists. The others are
Broiler.ECMAScript.Frontend's PublicAPI.txt, its project file, its Rows.cs and its SemanticRows.cs (the library's
diagnostic producers, one `static readonly LexicalRefusal` entry per catalogue row it reports) and the stage table
Broiler.ECMAScript/docs/frontend/stages.tsv.

It lists:
1. for each fact the milestone lists (its "The facts are:" bullets, which the rule's part F names), every item
   of the fact and the public member of PublicAPI.txt that carries it, or "absent"; a fact this script has no item list
   for is listed "unmapped". All of a fact's items are looked for: a missing member is a problem whatever the others;
2. (listed as 1b) contract_check.py's first listing, JSD-0080's input and output rows and its limits and providers,
   with two changes: "the caller's lexical names" is looked for at stage 5c's members, InheritedContext.CallerScopes,
   CallerScope, SemanticModel.DirectEvalCalls (an item of its own) and SemanticModel.CallerScopesOf, instead of being
   listed absent with decision D16's reason; and the output row "Semantic model" also looks for the model's entry and
   its members;
3. (listed as 2) contract_check.py's second listing, the public API lines and project references naming an engine;
4. (listed as 3) contract_check.py's third listing, the rows the stage table assigns `engine` with a producer, the
   producers read from Rows.cs and SemanticRows.cs.

It prints one outcome line for each listing ("outcome", the listing, "pass" or "fail"), and, when --control-absent or
--control-engine is given (bundle sjl-13-001's part H's controls, run over a scratch archive), one outcome line for each
control: "reported" when the listing shows the member given absent, or the engine line given, "not-reported" otherwise.
It exits 0 when every listing passes and no input digest differs, 1 otherwise; a control run therefore exits 1 when its
control is reported. Its item lists say where each fact and contract item is carried; they decide nothing of the outcome
but which member to look for. Which outcome lines count toward part F is a reading the bundle's README records, not this
script's.
"""
import argparse
import hashlib
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


# --- Additions for bundle sjl-15-001's part F (decision D-js-part-f-tools). ---

# The milestone's facts and the public members that carry each. Every item is looked for (all of them).
FACTS = {
    "scopes and their kinds": [
        ("the model", S + "ParseResult.get_Model"), ("scopes in source order", M + "SemanticModel.get_Scopes"),
        ("the scope of any node", M + "SemanticModel.ScopeOf("), ("a scope's parent", M + "SemanticModel.ParentOf("),
        ("a scope's kind", M + "Scope.get_Kind"), ("a scope's node", M + "Scope.get_Node"),
        ("program: a Script's", M + "ScopeKind.Script"), ("program: a Module's (module)", M + "ScopeKind.Module"),
        ("function", M + "ScopeKind.Function"), ("block", M + "ScopeKind.Block"), ("with", M + "ScopeKind.With"),
        ("eval (eval code's program, decision D-js-kinds)", M + "FunctionKind.EvalCode"),
        ("class body", M + "ScopeKind.ClassBody"), ("the separate parameter scope", M + "ScopeKind.Parameters")],
    "declarations and conflicts": [
        ("each binding identifier's declaration", M + "SemanticModel.DeclarationOf("),
        ("a scope's declarations", M + "Scope.get_Declarations"), ("a declaration's name", M + "Declaration.get_Name"),
        ("a declaration's kind", M + "Declaration.get_Kind"), ("a declaration's node", M + "Declaration.get_Node"),
        ("its mutability", M + "Declaration.get_IsMutable"), ("var", M + "BindingKind.Var"), ("let", M + "BindingKind.Let"),
        ("const", M + "BindingKind.Const"), ("class", M + "BindingKind.Class"), ("function", M + "BindingKind.Function"),
        ("parameter", M + "BindingKind.Parameter"), ("catch", M + "BindingKind.CatchParameter"),
        ("import", M + "BindingKind.Import"),
        ("every conflict (the syntax rows, in the result's diagnostics)", S + "ParseResult.get_Diagnostics")],
    "Annex B.3.3 aliases": [
        ("a block function's alias", M + "SemanticModel.AnnexBAlias("), ("the alias's kind", M + "BindingKind.BlockFunctionAlias"),
        ("a block function", M + "Declaration.get_IsBlockFunction"),
        ("a reference's answer through the alias", M + "SemanticModel.Resolve(" + S + "Identifier)")],
    "per-function flags": [
        ("the functions' facts", M + "SemanticModel.get_Functions"), ("a node's facts", M + "SemanticModel.FactsOf("),
        ("strict", M + "FunctionFacts.get_IsStrict"), ("simple parameters", M + "FunctionFacts.get_HasSimpleParameterList"),
        ("parameter expressions", M + "FunctionFacts.get_HasParameterExpressions"),
        ("a separate variable environment", M + "FunctionFacts.get_HasSeparateVariableEnvironment"),
        ("the environment facts", M + "SemanticModel.EnvironmentOf("), ("the arguments binding", M + "EnvironmentFacts.get_Arguments"),
        ("arguments needed", M + "EnvironmentFacts.get_ArgumentsNeeded"), ("arguments mapped", M + "EnvironmentFacts.get_ArgumentsMapped"),
        ("direct eval in the body", M + "EnvironmentFacts.get_DirectEvalInBody"),
        ("direct eval in the parameters", M + "EnvironmentFacts.get_DirectEvalInParameters"),
        ("with", M + "EnvironmentFacts.get_ContainsWith"), ("the function's kind", M + "FunctionFacts.get_Kind"),
        ("async", M + "FunctionFacts.get_IsAsync"), ("generator", M + "FunctionFacts.get_IsGenerator")],
    "reference resolution with dynamic markers": [
        ("a reference's resolution", M + "SemanticModel.Resolve(" + S + "Identifier)"),
        ("a binding identifier's resolution", M + "SemanticModel.Resolve(" + S + "BindingIdentifier)"),
        ("its kind", M + "Resolution.get_Kind"), ("static, to a scope", M + "Resolution.get_Scope"),
        ("static, to a declaration", M + "Resolution.get_Declaration"), ("static", M + "ResolutionKind.Static"),
        ("dynamic", M + "ResolutionKind.Dynamic"), ("global", M + "ResolutionKind.Global"),
        ("inherited (eval code)", M + "ResolutionKind.Inherited"), ("the dynamic marker's reason", M + "Resolution.get_Reason"),
        ("with", M + "DynamicReason.With"), ("sloppy direct eval", M + "DynamicReason.SloppyDirectEval"),
        ("the scopes that may bind it at run time", M + "Resolution.get_DynamicScopes"),
        ("its answer without them", M + "Resolution.get_Fallback")],
    "labels and jump targets": [
        ("a break's target", M + "SemanticModel.TargetOf(" + S + "BreakStatement)"),
        ("a continue's target", M + "SemanticModel.TargetOf(" + S + "ContinueStatement)"),
        ("the target statement", M + "LabelTarget.get_Statement"), ("its labels", M + "LabelTarget.get_LabelSet")],
    "private-name environments": [
        ("a class's environment", M + "SemanticModel.PrivateEnvironmentOf("), ("its class", M + "PrivateEnvironment.get_Class"),
        ("its names", M + "PrivateEnvironment.get_Names"), ("its outer environment", M + "PrivateEnvironment.get_Outer"),
        ("a private name's resolution", M + "SemanticModel.Resolve(" + S + "PrivateIdentifier)"),
        ("a binding's name", M + "PrivateNameBinding.get_Name"), ("its kind", M + "PrivateNameBinding.get_Kind"),
        ("static", M + "PrivateNameBinding.get_IsStatic"), ("its elements", M + "PrivateNameBinding.get_Elements"),
        ("its environment", M + "PrivateNameBinding.get_Environment")],
    "module records": [
        ("the module record", M + "SemanticModel.get_Module"), ("requests", M + "ModuleRecord.get_RequestedModules"),
        ("a request's specifier", M + "ModuleRequest.get_Specifier"), ("its phase", M + "ModuleRequest.get_Phase"),
        ("its statements", M + "ModuleRequest.get_Statements"), ("with attributes", M + "ModuleRequest.get_Attributes"),
        ("an attribute's key", M + "ModuleRequestAttribute.get_Key"), ("its value", M + "ModuleRequestAttribute.get_Value"),
        ("top-level await", M + "ModuleRecord.get_HasTopLevelAwait"), ("import entries", M + "ModuleRecord.get_ImportEntries"),
        ("an import's request", M + "ImportEntry.get_ModuleRequest"), ("its import name", M + "ImportEntry.get_ImportName"),
        ("its kind", M + "ImportEntry.get_ImportKind"), ("its local name", M + "ImportEntry.get_LocalName"),
        ("its node", M + "ImportEntry.get_Node"), ("its declaration", M + "ImportEntry.get_Declaration"),
        ("local export entries", M + "ModuleRecord.get_LocalExportEntries"),
        ("indirect export entries", M + "ModuleRecord.get_IndirectExportEntries"),
        ("star export entries", M + "ModuleRecord.get_StarExportEntries"), ("an export's name", M + "ExportEntry.get_ExportName"),
        ("its request", M + "ExportEntry.get_ModuleRequest"), ("its import name", M + "ExportEntry.get_ImportName"),
        ("its kind", M + "ExportEntry.get_ImportKind"), ("its local name", M + "ExportEntry.get_LocalName"),
        ("its node", M + "ExportEntry.get_Node"), ("its declaration", M + "ExportEntry.get_Declaration"),
        ("their errors (the module rows, in the result's diagnostics)", S + "ParseResult.get_Diagnostics")],
    "named-evaluation names": [
        ("a definition's name", M + "SemanticModel.NamedEvaluationName("),
        ("a definition's computed key", M + "SemanticModel.NamedEvaluationKey(")],
    "free names and captures": [
        ("the free names", M + "SemanticModel.FreeNames("), ("the captures", M + "SemanticModel.Captured("),
        ("the dynamic reach", M + "EnvironmentFacts.get_DirectEvalOrWithWithin"),
        ("the self-name use", M + "EnvironmentFacts.get_FunctionNameReferenced")],
    "every early error": [
        ("the request member that asks for them", S + "ParseRequest.get_BuildSemanticModel"),
        ("in the result's diagnostics, in a deterministic order", S + "ParseResult.get_Diagnostics"),
        ("the semantics phase", S + "DiagnosticPhase.Semantics"), ("a stable code", S + "SyntaxDiagnostic.get_Code")],
}

# Contract_check.py's item lists with this bundle's two changes (its listing 1, here 1b).
ITEMS_SJL15 = dict(ITEMS)
ITEMS_SJL15[("Input", "Inherited evaluation context")] = [
    item for item in ITEMS[("Input", "Inherited evaluation context")] if item[0] != "the caller's lexical names"] + [
    ("the caller's lexical names (stage 5c's records)", S + "InheritedContext.get_CallerScopes"),
    ("the caller's lexical names: a record", S + "CallerScope "),
    ("the caller's lexical names: a record's names", S + "CallerScope.get_Names"),
    ("the caller's lexical names: a Catch clause's record", S + "CallerScope.get_IsCatch"),
    ("the caller's lexical names: the calls that receive them", M + "SemanticModel.get_DirectEvalCalls"),
    ("the caller's lexical names: a call's records", M + "SemanticModel.CallerScopesOf(")]
ITEMS_SJL15[("Output", "Semantic model")] = ITEMS[("Output", "Semantic model")] + [
    ("the semantic model", S + "ParseResult.get_Model"), ("the request member", S + "ParseRequest.get_BuildSemanticModel"),
    ("the model's type", M + "SemanticModel ")]

VM_INPUTS = {
    "jsd0080": "src/Broiler.VM.Profile.JavaScript/docs/decisions/0080-the-shared-frontend-contract-and-the-in-place-preparation.md",
    "milestone": "docs/javascript-shared-libraries/milestones/sjl-15-static-semantics.md",
}


def milestone_facts(text):
    """The facts the milestone lists: the bullets after its line "The facts are:", each without its closing mark."""
    facts, inside = [], False
    for line in text.split("\n"):
        if line.strip() == "The facts are:":
            inside = True
            continue
        if inside:
            match = re.match(r"^  - (.+?)[;.]?$", line)
            if not match:
                break
            facts.append(match.group(1))
    return facts


def carrier_of(api, member):
    return next((line for line in api if line.startswith(member)), None)


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    for name in ("--jsd0080", "--milestone", "--public-api", "--project", "--rows", "--semantic-rows", "--stages"):
        parser.add_argument(name, required=True)
    parser.add_argument("--vm-digests")
    parser.add_argument("--control-absent")
    parser.add_argument("--control-engine")
    parser.add_argument("--out")
    args = parser.parse_args()

    with open(args.jsd0080, "rb") as handle:
        jsd_bytes = handle.read()
    with open(args.milestone, "rb") as handle:
        milestone_bytes = handle.read()
    jsd = jsd_bytes.decode("utf-8")
    milestone = milestone_bytes.decode("utf-8")
    with open(args.public_api, encoding="utf-8") as handle:
        api = [line for line in handle.read().split("\n") if line and not line.startswith("#")]
    with open(args.project, encoding="utf-8") as handle:
        project = handle.read()
    with open(args.rows, encoding="utf-8") as handle:
        rows_source = handle.read()
    with open(args.semantic_rows, encoding="utf-8") as handle:
        semantic_rows_source = handle.read()
    with open(args.stages, encoding="utf-8") as handle:
        stages = [line.split("\t") for line in handle.read().split("\n")[1:] if line]

    out, problems = [], 0
    outcome, absent = {}, set()

    out.append("# 0. The Broiler.VM inputs")
    digests = {"jsd0080": hashlib.sha256(jsd_bytes).hexdigest(), "milestone": hashlib.sha256(milestone_bytes).hexdigest()}
    if args.vm_digests:
        with open(args.vm_digests, encoding="utf-8") as handle:
            listed = [line.split("  ", 1) for line in handle.read().split("\n") if line and not line.startswith("#")]
        for name, digest in digests.items():
            match = [entry for entry in listed if len(entry) == 2 and entry[1].endswith(":" + VM_INPUTS[name])]
            if len(match) == 1 and match[0][0] == digest:
                out.append("input\t%s\t%s\tas listed" % (name, digest))
            else:
                out.append("input\t%s\t%s\tproblem: not the digest %s lists" % (name, digest, args.vm_digests))
                problems += 1
    else:
        out.extend("input\t%s\t%s\tnot checked" % (name, digest) for name, digest in digests.items())

    out.append("# 1. Each fact the milestone lists, and the public members that carry it")
    found_facts, item_1 = milestone_facts(milestone), 0
    if not found_facts:
        out.append("problem\tno fact of the milestone was read")
        item_1 += 1
    for fact in found_facts:
        items = FACTS.get(fact)
        if items is None:
            out.append("%s\t-\tunmapped" % fact)
            item_1 += 1
            continue
        for item, member in items:
            carrier = carrier_of(api, member)
            if carrier is None:
                out.append("%s\t%s\tabsent" % (fact, item))
                absent.add(member)
                item_1 += 1
            else:
                out.append("%s\t%s\t%s" % (fact, item, carrier.split(" ")[0]))
    outcome["1"] = item_1
    problems += item_1

    out.append("# 1b. Each row and item of JSD-0080's contract, and the public member that carries it")
    found_rows, item_1b = contract_rows(jsd), 0
    if not found_rows:
        out.append("problem\tno row of JSD-0080's contract was read")
        item_1b += 1
    for section, row in found_rows:
        items = ITEMS_SJL15.get((section, row))
        if items is None:
            out.append("%s\t%s\t-\tunmapped" % (section, row))
            item_1b += 1
            continue
        for item, member in items:
            if member is None:
                out.append("%s\t%s\t%s\tabsent\t%s" % (section, row, item, ABSENT_REASONS[item]))
                continue
            carrier = carrier_of(api, member)
            if carrier is None:
                out.append("%s\t%s\t%s\tabsent" % (section, row, item))
                absent.add(member)
                item_1b += 1
            else:
                out.append("%s\t%s\t%s\t%s" % (section, row, item, carrier.split(" ")[0]))
    outcome["1b"] = item_1b
    problems += item_1b

    out.append("# 2. Public API lines and project references naming an engine (none is expected)")
    engine = [line for line in api if any(ns in line for ns in ENGINE_NAMESPACES)]
    engine += ["project: " + m for m in re.findall(r'Include="([^"]*)"', project) if any(ns.rstrip(".") in m for ns in ENGINE_NAMESPACES)]
    out.extend("engine\t" + line for line in engine)
    out.append("engine-lines\t%d" % len(engine))
    outcome["2"] = len(engine)
    problems += len(engine)

    out.append("# 3. Rows the stage table assigns engine, with a producer in the library, Rows.cs or SemanticRows.cs (none is expected)")
    producers = set(re.findall(r"static readonly LexicalRefusal (\w+)\s*=", rows_source))
    producers |= set(re.findall(r"static readonly LexicalRefusal (\w+)\s*=", semantic_rows_source))
    engine_rows = [row[0] for row in stages if row[2] == "engine"]
    produced = [row for row in engine_rows if row in producers]
    out.extend("engine-row-producer\t" + row for row in produced)
    out.append("engine-rows\t%d\tproducers\t%d\twith-a-producer\t%d" % (len(engine_rows), len(producers), len(produced)))
    outcome["3"] = len(produced)
    problems += len(produced)

    out.append("# The outcomes")
    for listing in ("1", "1b", "2", "3"):
        out.append("outcome\t%s\t%s" % (listing, "pass" if outcome[listing] == 0 else "fail"))
    if args.control_absent:
        reported = args.control_absent in absent
        out.append("outcome\tcontrol-absent\t%s\t%s" % (args.control_absent, "reported" if reported else "not-reported"))
    if args.control_engine:
        reported = any(line.startswith("engine\t" + args.control_engine) for line in out)
        out.append("outcome\tcontrol-engine\t%s\t%s" % (args.control_engine, "reported" if reported else "not-reported"))

    out.append("problems\t%d" % problems)
    text = "\n".join(out) + "\n"
    if args.out:
        with open(args.out, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(text)
    sys.stdout.write(text)
    return 0 if problems == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
