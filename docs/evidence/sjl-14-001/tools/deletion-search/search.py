#!/usr/bin/env python3
"""Bundle sjl-14-001's part K: the deletion search (decision-rule.md, part K), committed with window W2's change.

    python3 -I docs/evidence/sjl-14-001/tools/deletion-search/search.py --tree <a clean tree of the after commit> \
        --commit <its forty-digit identifier> --out <file>
    python3 -I docs/evidence/sjl-14-001/tools/deletion-search/search.py --control --tree <a scratch copy of that tree> \
        --commit <the same identifier> --out <file>

Run as bundle sjl-11-001's part E runs its E-1 search: a word search, one name at a time, over Broiler.JS/ and
Broiler.ECMAScript/ outside their docs/ folders, at the after commit; every hit is listed with its file and line. The
population is SJL-14.5's deletion list member by member as dispositions.md lists it ("SJL-14.5's deletion list, member by
member"), item by item below. A name that is a common word of the engine's code (position, line, Next, Push, ...) is
searched in the file that declared it at 33d3093b, where the member was, and the scope is printed with the name; every
other name is searched over both trees. The facade members dispositions.md keeps (the public names of FastParser,
FastScanner, FastTokenStream, FastKeywordMap, CharExtensions, FastScope and FastScopeItem) are listed with their hits;
the part's reading, not this script, says whether each hit is a facade member over the bridge, a relocation decision D51
allows, the other member of the same name dispositions.md names (FastCompiler.EarlyErrors' own ContainsLegacyOctalLiteral,
which stays until SJL-15.5), or a finding. The script judges nothing.

--control is the rule's control: in the scratch copy it declares one deleted member again (DirectEvalSupport's
ContainsStrictReservedWordUsage), runs the search for that name and expects a hit, reverts the file from the commit's own
blob and runs the search again expecting none; it prints both and exits 1 if either expectation fails.
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path

PARSER = "Broiler.JS/Broiler.JavaScript.Parser"
COMPILER = "Broiler.JS/Broiler.JavaScript.Compiler"
ROOTS = ("Broiler.JS", "Broiler.ECMAScript")

PARTIALS = ["ArrayExpression", "AssignmentLeftPattern", "Block", "Class", "Decorator", "DoWhile", "Export", "Expression",
            "ForStatement", "Function", "Identifier", "IfStatement", "Import", "Literal", "NextExpression",
            "NonDeclarativeStatement", "ObjectLiteral", "Parameters", "Program", "PropertyName", "SingleExpression",
            "SingleMemberExpression", "SinglePostfixPrefixExpression", "Statement", "Switch", "Template",
            "VariableDeclaration", "WhileStatement", "WithStatement"]

# (item, names, scope): scope None is both trees; otherwise the file that declared the member at 33d3093b.
ITEMS: list[tuple[str, list[str], str | None]] = [
    ("FastScanner's scanning code: distinctive names", [
        "SaveCursor", "RestoreCursor", "ConsumeAndNext", "CanConsumeNext", "ReadToken", "_ReadToken", "ReadTemplateString",
        "ReadSymbol", "ReadCommentsOrRegExOrSymbol", "SkipMultilineComment", "SkipSingleLineComment", "ReadIdentifierEscape",
        "ReadNumber", "ScanEscaped", "ScanUnicodeCodePointEscape", "HexDigitValue", "OpensStatementHead",
        "IsHtmlOpenCommentStart", "IsHtmlCloseCommentStart", "IsHashbangStart", "UnexpectedCharacter", "NoBigIntWidth",
        "CommitIdentifier", "templateParts", "templateBraceDepth", "templateBraceDepthStack", "templateCookedInvalid",
        "lastSignificantToken", "parenIsStatementHead", "lastCloseParenEndedStatementHead"], None),
    ("FastScanner's scanning code: common names, in its own file", [
        "position", "line", "column", "nextToken", "lastToken", "Peek", "Next", "Consume", "CanConsume", "ReadString",
        "ReadIdentifier", "HexDigit", "IsWhiteSpace", "IsIdentifierStart", "IsIdentifierPart", "Unterminated"],
     PARSER + "/FastScanner.cs"),
    ("FastTokenStream's scanning code, in its own file", ["scanner", "furthest", "LineTerminator", "Unexpected",
                                                          "UnexpectedStatement"], PARSER + "/FastTokenStream.cs"),
    ("FastKeywordMap's table, in its own file", ["switch", "case"], PARSER + "/FastKeywordMap.cs"),
    ("CharExtensions' scanner members, in the parser", ["FromCodePoint", "HexValue", "IsDigitPart", "IsLineTerminator"], PARSER),
    ("The parser's half of FastScope, in its own files", ["Push", "Variables", "annexBFunctionNames", "varDeclaredNames",
                                                          "MarkVarDeclaredName", "HasVarDeclaredName", "AddAnnexBName",
                                                          "GetAnnexBNames", "HasLexicalBinding", "DeclaresVariable",
                                                          "AddVariable", "GetVariables"], PARSER + "/FastScope*.cs"),
    ("RegExpValidator, DecodeBracedUnicodeEscapes and the parenthesis-balance patch",
     ["RegExpValidator", "DecodeBracedUnicodeEscapes"], None),
    ("The division fallback", ["ScanRegEx", "ScanFlags", "divideAndAssign"], None),
    ("The two token re-scans and the direct-eval prefix", ["ContainsLegacyOctalLiteral", "ContainsStrictReservedWordUsage",
                                                           "IsStrictReservedWord", "injectedStrictDirective",
                                                           "TryGetLegacyOctalToken"], None),
    ("The test-only switch", ["ParseThroughBridge", "ParsesThroughBridge", "BridgeByDefault", "BROILER_PARSE_THROUGH_BRIDGE",
                              "BroilerParseThroughBridge", "BroilerBridgeBuild", "parse-through-bridge"], None),
]

# The direct-eval prefix as DirectEvalSupport.cs:127-128 wrote it at 33d3093b, and the parenthesis-balance patch's marker
# as FastScanner.cs:1463 wrote it ("BROILER-PATCH: Validate parentheses balance in regex pattern"), searched as text.
PREFIX = r'"\"use strict\";\n"'
PATCH = "Validate parentheses balance"


# Build output is no source: a tree the search runs over may hold it beside the sources.
BUILD = [":!**/bin/**", ":!**/obj/**"]


def pathspecs(scope: str | None) -> list[str]:
    if scope is not None:
        return [scope, *BUILD]
    return [*ROOTS, *(f":!{root}/docs" for root in ROOTS), *BUILD]


def grep(tree: Path, name: str, scope: str | None, fixed: bool = False) -> list[str]:
    # git grep over the tree's files as they are on disk (--no-index), so that the control's scratch edit is searched.
    arguments = ["git", "grep", "--no-index", "-n", "-I"]
    arguments += ["-F", "-e", name] if fixed else ["-w", "-e", name]
    arguments += ["--", *pathspecs(scope)]
    result = subprocess.run(arguments, cwd=tree, capture_output=True, text=True)
    if result.returncode not in (0, 1):
        raise SystemExit(f"git grep failed: {result.stderr}")
    return sorted(result.stdout.splitlines())


def search(tree: Path, commit: str) -> list[str]:
    lines = [f"# Part K's deletion search at {commit}, over {tree} (a tree of that commit), by search.py.",
             "# A word search (git grep --no-index -w -n -I), one name at a time; each section names its scope.", ""]
    lines.append("== FastParser's partial files: each absent")
    for partial in PARTIALS:
        path = Path(tree, PARSER, f"FastParser.{partial}.cs")
        lines.append(f"{path.relative_to(tree).as_posix()}: {'PRESENT' if path.exists() else 'absent'}")
    lines.append("")
    for item, names, scope in ITEMS:
        lines.append(f"== {item} (scope: {scope or ' and '.join(ROOTS) + ' outside docs/'})")
        for name in names:
            hits = grep(tree, name, scope)
            lines.append(f"-- {name}: {len(hits)} hit(s)")
            lines += [f"   {hit}" for hit in hits]
        lines.append("")
    lines.append(f"== The direct-eval prefix, as text: {PREFIX} (scope: {COMPILER})")
    hits = grep(tree, PREFIX, COMPILER, fixed=True)
    lines.append(f"-- {len(hits)} hit(s)")
    lines += [f"   {hit}" for hit in hits]
    lines.append("")
    lines.append(f"== The parenthesis-balance patch's marker, as text: {PATCH} (scope: {' and '.join(ROOTS)} outside docs/)")
    hits = grep(tree, PATCH, None, fixed=True)
    lines.append(f"-- {len(hits)} hit(s)")
    lines += [f"   {hit}" for hit in hits]
    return lines


def control(tree: Path, commit: str, repository: Path) -> tuple[list[str], bool]:
    target = Path(tree, COMPILER, "DirectEvalSupport.cs")
    name = "ContainsStrictReservedWordUsage"
    lines = [f"# Part K's control at {commit}, in the scratch copy {tree}: {name} declared again in "
             f"{target.relative_to(tree).as_posix()}, searched, reverted from the commit's blob, searched again."]
    original = target.read_bytes()
    text = original.decode("utf-8")
    edited = re.sub(r"\n}\s*$", f"\n\n    private static bool {name}(string text) => false;\n}}\n", text, count=1)
    target.write_bytes(edited.encode("utf-8"))
    planted = grep(tree, name, None)
    lines.append(f"-- with the declaration: {len(planted)} hit(s)")
    lines += [f"   {hit}" for hit in planted]
    blob = subprocess.run(["git", "-C", str(repository), "show", f"{commit}:{COMPILER}/DirectEvalSupport.cs"],
                          capture_output=True, check=True).stdout
    target.write_bytes(blob)
    reverted = grep(tree, name, None)
    same = target.read_bytes() == blob
    lines.append(f"-- reverted (the file equal to the commit's blob: {same}): {len(reverted)} hit(s)")
    lines += [f"   {hit}" for hit in reverted]
    reported = len(planted) > 0 and len(reverted) == 0 and same
    lines.append(f"-- the control is reported: {reported}")
    return lines, reported


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--tree", required=True, help="a tree of the after commit (for --control, a scratch copy)")
    parser.add_argument("--commit", required=True, help="the after commit's forty-digit identifier")
    parser.add_argument("--repository", default=str(Path(__file__).resolve().parents[5]),
                        help="a clone holding the commit, for --control's revert")
    parser.add_argument("--out", required=True)
    parser.add_argument("--control", action="store_true")
    args = parser.parse_args(argv)
    tree = Path(args.tree).resolve()
    if args.control:
        lines, ok = control(tree, args.commit, Path(args.repository))
    else:
        lines, ok = search(tree, args.commit), True
    Path(args.out).write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
