#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Extract the early-error catalogue's rows from ECMA-262's source (milestone SJL-12.3).

The catalogue is Broiler.ECMAScript/docs/frontend/early-errors.tsv; early-errors.md beside it defines its
columns, its families, the row-identity policy and the code and message-identifier schemes. This tool
writes every column that identifies a row (the id, the anchor, the productions, the rule text and its
digest, and the shared code) and leaves the columns the fill reads from the engines `unfilled`.

THE INPUTS. None is committed in this repository (conformance/vectors/sources/ecma262.pin keeps the
same practice); each is named here by revision and SHA-256, and the tool refuses a file whose digest
differs.

  edition  ES2026, tc39/ecma262 at 0248456c758431e4bb8e5d26333ff1865123c9cd (the tag es2026), 2978793
           bytes, sha256 ce7bc30174061fd8d212270b81cf6511661180c1e174f6911d10ced0581527b0
           https://raw.githubusercontent.com/tc39/ecma262/0248456c758431e4bb8e5d26333ff1865123c9cd/spec.html
           Broiler.VM archives the same file as
           src/Broiler.VM.Profile.JavaScript/docs/specification/ecma-262-es2026-spec.html (its rule N14).
  later    tc39/ecma262 main at 4feb0272b799ea795c5eadee9b482aea0c10ce45 (main's head on 2026-10-19),
           3090376 bytes, sha256 1f7935840bf249dbbfb32cc4d083114cea48bab4c513b7d7bed00387a3ebe89e
           https://raw.githubusercontent.com/tc39/ecma262/4feb0272b799ea795c5eadee9b482aea0c10ce45/spec.html
           Read only for the explicit-resource-management rules that main has merged (family EP); the
           edition's rows come from the edition alone.
  errata   es2026-errata, tc39/ecma262 at d89c03f2db8a597bc915b363a6518d0cc8acdbc0 (2026-07-28),
           2979557 bytes, sha256 032ecc740356b38f6846338d8c44536291a134a6ce60e651f935befb239cfc1a
           https://raw.githubusercontent.com/tc39/ecma262/d89c03f2db8a597bc915b363a6518d0cc8acdbc0/spec.html
           Read only by `diff`. Result on 2026-10-19: 213 rules on each side, no rule added, removed or
           changed (the errata change no early-error rule of the edition).
  shadowrealm  tc39/proposal-shadowrealm at 9ff2a01f1ec68a427f6f37ce710aa7cba8150678 (its main on 2026-10-20,
           and the revision Broiler.VM's JavaScriptProfile pins for its ShadowRealm surface), 28416 bytes,
           sha256 1e20aea8b997471e0c157e3d0d2df5faebe4070d68739dfc5bbaad8ea48d1fe5
           https://raw.githubusercontent.com/tc39/proposal-shadowrealm/9ff2a01f1ec68a427f6f37ce710aa7cba8150678/spec.html
           Not read when the tool runs: EP_STEPS below holds the four SyntaxError steps of its
           PerformShadowRealmEval as step_rule renders them from that file with each tab read as two
           spaces (the file indents with tabs). Checked so on 2026-10-20.

HOW TO RE-RUN (from the repository root; Python 3.10 or later, standard library only):

  curl -sS -o /tmp/es2026.html https://raw.githubusercontent.com/tc39/ecma262/0248456c758431e4bb8e5d26333ff1865123c9cd/spec.html
  curl -sS -o /tmp/main.html https://raw.githubusercontent.com/tc39/ecma262/4feb0272b799ea795c5eadee9b482aea0c10ce45/spec.html
  curl -sS -o /tmp/errata.html https://raw.githubusercontent.com/tc39/ecma262/d89c03f2db8a597bc915b363a6518d0cc8acdbc0/spec.html

  # every identifying column of the committed catalogue equals the extraction (exit status 1 if not)
  python3 -I scripts/frontend/ee_extract.py check --edition /tmp/es2026.html --later /tmp/main.html \
      --catalogue Broiler.ECMAScript/docs/frontend/early-errors.tsv
  # the skeleton as first committed: every fill column `unfilled`
  python3 -I scripts/frontend/ee_extract.py write --edition /tmp/es2026.html --later /tmp/main.html --out skeleton.tsv
  # the rules another revision adds, removes or changes against the edition (exit status 1 if any)
  python3 -I scripts/frontend/ee_extract.py diff --edition /tmp/es2026.html --other /tmp/errata.html --other-pin errata

`write` at the catalogue's first commit gives that commit's early-errors.tsv byte for byte. After the fill,
`check` is the test: the fill changes only the fill columns, and a row is added only here, by adding it to
the tables below.
"""

from __future__ import annotations

import argparse
import hashlib
import re
import sys
from html.parser import HTMLParser

PINS = {
    "edition": ("es2026@0248456c", "ce7bc30174061fd8d212270b81cf6511661180c1e174f6911d10ced0581527b0"),
    "later": ("ecma262@4feb0272", "1f7935840bf249dbbfb32cc4d083114cea48bab4c513b7d7bed00387a3ebe89e"),
    "errata": ("es2026-errata@d89c03f2", "032ecc740356b38f6846338d8c44536291a134a6ce60e651f935befb239cfc1a"),
}

COLUMNS = [
    # identifying columns: written here, compared by `check`
    "id", "family", "anchor", "section", "productions", "ordinal", "text_from", "rule_sha10", "annexb",
    "rule_text", "shared_code",
    # fill columns: read from the engines (early-errors.md, "Columns")
    "message_id", "vm_slice", "vm_wide_stage", "vm_wide_site", "vm_code", "js_stage", "js_site",
    "vm_admits", "js_admits", "test262", "status", "notes",
]
IDENTITY = COLUMNS[: COLUMNS.index("shared_code") + 1]
UNFILLED = "unfilled"

SECTION_TAGS = {"emu-clause", "emu-annex", "emu-intro"}
VOID = {"br", "img", "meta", "link", "input", "hr", "col", "wbr", "source", "area", "base", "param", "track", "embed"}


# --- reading ecmarkup ------------------------------------------------------------------------------------------

class Node:
    __slots__ = ("tag", "attrs", "children", "parent")

    def __init__(self, tag, attrs, parent):
        self.tag, self.attrs, self.children, self.parent = tag, dict(attrs), [], parent

    def text(self):
        """The element's text. A cross-reference is kept as [xref#target]; a superscript as ^n, so that
        2<sup>32</sup> reads 2^32 rather than 232."""
        if self.tag == "emu-xref":
            return "[xref" + self.attrs.get("href", "") + "]"
        inner = "".join(c if isinstance(c, str) else c.text() for c in self.children)
        return "^" + inner if self.tag == "sup" else inner

    def elements(self):
        return [c for c in self.children if isinstance(c, Node)]


class TreeBuilder(HTMLParser):
    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.root = Node("#root", {}, None)
        self.cur = self.root

    def handle_starttag(self, tag, attrs):
        node = Node(tag, attrs, self.cur)
        self.cur.children.append(node)
        if tag not in VOID:
            self.cur = node

    def handle_startendtag(self, tag, attrs):
        self.cur.children.append(Node(tag, attrs, self.cur))

    def handle_endtag(self, tag):
        if tag in VOID:
            return
        node = self.cur
        while node is not None and node.tag != tag:
            node = node.parent
        if node is not None and node.parent is not None:
            self.cur = node.parent

    def handle_data(self, data):
        self.cur.children.append(data)


def norm(s):
    return re.sub(r"\s+", " ", s).strip()


def heading(section):
    for c in section.elements():
        if c.tag == "h1":
            return norm(c.text())
    return ""


class Spec:
    """One spec.html: its tree, its section numbers and its sections by id."""

    def __init__(self, path, pin):
        with open(path, "rb") as f:
            data = f.read()
        label, digest = PINS[pin]
        actual = hashlib.sha256(data).hexdigest()
        if actual != digest:
            sys.exit(f"{path}: sha256 {actual}, but the {pin} pin ({label}) is {digest}")
        self.label = label
        builder = TreeBuilder()
        builder.feed(data.decode("utf-8"))
        self.root = builder.root
        self.numbers = {}
        self._number(self.root, "", [0], [0])
        self.sections = list(self._sections(self.root))
        self.by_id = {s.attrs.get("id", ""): s for s in self.sections}

    def _number(self, node, prefix, top, annex):
        """Clauses are numbered 1, 2, ... at the top and annexes lettered A, B, ...; below the top,
        clauses and annexes share one count. Introductions are not numbered."""
        index = 0
        for c in node.elements():
            if c.tag in ("emu-clause", "emu-annex"):
                if prefix == "":
                    if c.tag == "emu-annex":
                        label = chr(ord("A") + annex[0])
                        annex[0] += 1
                    else:
                        top[0] += 1
                        label = str(top[0])
                else:
                    index += 1
                    label = prefix + "." + str(index)
                self.numbers[id(c)] = label
                self._number(c, label, top, annex)
            elif c.tag == "emu-intro":
                if prefix != "":
                    self._number(c, prefix, top, annex)
            else:
                self._number(c, prefix, top, annex)

    def _sections(self, node):
        for c in node.elements():
            if c.tag in SECTION_TAGS:
                yield c
            yield from self._sections(c)

    def number(self, section):
        return self.numbers.get(id(section), "?")


# --- the rules of "Static Semantics: Early Errors" clauses -----------------------------------------------------

LHS = re.compile(r"[A-Za-z_\[\], ?+~]+ :{1,3}")
WITH_LHS = re.compile(r"[A-Za-z_]+(\[[^\]]*\])? :{1,3} ")


def grammar_lines(grammar):
    """An emu-grammar block as one line per right-hand side, each with its left-hand side."""
    lines, lhs = [], None
    for x in (norm(line) for line in grammar.text().splitlines()):
        if not x:
            continue
        if LHS.fullmatch(x):
            lhs = x
            continue
        lines.append(lhs + " " + x if lhs and not WITH_LHS.match(x) else x)
    return lines


def rules_of(clause):
    """Yield (productions, list item) for each top-level item of each list in an Early Errors clause.
    A grammar block after a list starts a new set of productions; consecutive grammar blocks add up."""
    grammar, after_list = [], False
    for c in clause.elements():
        if c.tag == "emu-grammar":
            if after_list:
                grammar, after_list = [], False
            grammar.extend(grammar_lines(c))
        elif c.tag == "ul":
            for li in c.elements():
                if li.tag == "li":
                    yield list(grammar), li
            after_list = True


ANNEXB = re.compile(r"\[xref#(sec-[a-z0-9-]*(?:annexb|web-legacy|catch-blocks|legacy|labelled-function|forin|"
                    r"html-like|ifstatement)[a-z0-9-]*)\]")


def annexb_of(text, own_clause_in_annex_b):
    if own_clause_in_annex_b:
        return "annex-b"
    refs = sorted(set(ANNEXB.findall(text)))
    return ";".join(refs) if refs else "-"


def early_error_rules(spec):
    """Every rule of every "Static Semantics: Early Errors" clause, in document order."""
    out = []
    for section in spec.sections:
        if "Early Errors" not in heading(section):
            continue
        clause_id = section.attrs.get("id", "")
        number = spec.number(section)
        last, ordinal = None, 0
        for grammar, li in rules_of(section):
            key = tuple(grammar)
            ordinal = ordinal + 1 if key == last else 1
            last = key
            text = norm(li.text())
            out.append({
                "anchor": f"{spec.label}#{clause_id}",
                "clause": clause_id,
                "section": number,
                "productions": " ;; ".join(grammar) if grammar else "-",
                "ordinal": str(ordinal),
                "text_from": "rule",
                "rule_text": text,
                "annexb": annexb_of(text, number.startswith("B")),
            })
    return out


def check_sentences(spec, rules):
    """Every "Syntax Error" sentence of an Early Errors clause lies inside an extracted rule."""
    def own_text(node):
        parts = []
        for c in node.children:
            if isinstance(c, str):
                parts.append(c)
            elif c.tag not in SECTION_TAGS and c.tag != "emu-note":
                parts.append(c.text())
        return "".join(parts)
    phrase = re.compile(r"Syntax Error")
    in_clauses = sum(len(phrase.findall(own_text(s))) for s in spec.sections if "Early Errors" in heading(s))
    in_rules = sum(len(phrase.findall(r["rule_text"])) for r in rules)
    if in_clauses != in_rules:
        sys.exit(f"{spec.label}: {in_clauses} 'Syntax Error' sentences in Early Errors clauses, {in_rules} in rules")


# --- rules outside "Early Errors" clauses: algorithm steps and Annex B prose ------------------------------------

def step_labels(alg):
    """(label path, text) for each step of an emu-alg, the label path being the rendered step number
    such as 11.e: decimal, then lower-case letters, then lower-case roman numerals, repeating."""
    def roman(n):
        out = ""
        for value, digits in ((10, "x"), (9, "ix"), (5, "v"), (4, "iv"), (1, "i")):
            while n >= value:
                out, n = out + digits, n - value
        return out

    def render(depth, n):
        kind = depth % 3
        return str(n) if kind == 0 else (chr(ord("a") + n - 1) if kind == 1 else roman(n))

    lines = [m for m in (re.match(r"^(\s*)1\. (.*)$", line) for line in alg.text().split("\n")) if m]
    base = min((len(m.group(1)) for m in lines), default=0)
    counters, steps = [], []
    for m in lines:
        depth = (len(m.group(1)) - base) // 2
        del counters[depth + 1:]
        while len(counters) <= depth:
            counters.append(0)
        counters[depth] += 1
        label = ".".join(render(d, n) for d, n in enumerate(counters))
        text = norm(re.sub(r"^\[[^\]]*\]\s*", "", m.group(2)))
        steps.append((label, depth, text))
    return steps


def step_rule(spec, clause_id, phrase):
    """The one step of the clause's algorithms whose text contains the phrase, with its enclosing steps:
    "11. Perform ... 11.e. If ..."."""
    section = spec.by_id[clause_id]
    found = []

    def algs(node):
        for c in node.elements():
            if c.tag in SECTION_TAGS:
                continue
            if c.tag == "emu-alg":
                yield c
            else:
                yield from algs(c)

    for alg in algs(section):
        steps = step_labels(alg)
        for i, (label, depth, text) in enumerate(steps):
            if phrase in text:
                chain, want = [], depth
                for j in range(i, -1, -1):
                    if steps[j][1] == want:
                        chain.append(f"{steps[j][0]}. {steps[j][2]}")
                        want -= 1
                        if want < 0:
                            break
                found.append((label, " ".join(reversed(chain))))
    if len(found) != 1:
        sys.exit(f"{spec.label}#{clause_id}: {len(found)} steps contain {phrase!r}")
    return found[0]


def prose_rule(spec, clause_id, phrase, with_list):
    """The paragraph of the clause that contains the phrase (and, with_list, the list after it), with the
    productions of a grammar block directly before it."""
    section = spec.by_id[clause_id]
    elements = [c for c in section.elements() if c.tag not in SECTION_TAGS]
    hits = [i for i, c in enumerate(elements) if c.tag == "p" and phrase in norm(c.text())]
    if len(hits) != 1:
        sys.exit(f"{spec.label}#{clause_id}: {len(hits)} paragraphs contain {phrase!r}")
    i = hits[0]
    text = norm(elements[i].text())
    if with_list:
        items = [norm(li.text()) for li in elements[i + 1].elements() if li.tag == "li"]
        text += " " + " ; ".join(items)
    productions = "-"
    if i > 0 and elements[i - 1].tag == "emu-grammar":
        productions = " ;; ".join(grammar_lines(elements[i - 1]))
    paragraph = sum(1 for c in elements[: i + 1] if c.tag == "p")
    return productions, f"p{paragraph}", text


# Family EA: the SyntaxError steps of eval and of dynamic functions (decision D30), in document order.
EA_STEPS = [
    ("sec-performeval", "If _script_ is a List of errors"),
    ("sec-performeval", "If _inFunction_ is *false* and _body_ Contains |NewTarget|"),
    ("sec-performeval", "If _inMethod_ is *false* and _body_ Contains |SuperProperty|"),
    ("sec-performeval", "If _inDerivedConstructor_ is *false* and _body_ Contains |SuperCall|"),
    ("sec-performeval", "If _inClassFieldInitializer_ is *true* and ContainsArguments"),
    ("sec-evaldeclarationinstantiation", "If HasLexicalDeclaration(_varEnv_, _name_) is *true*, throw"),
    ("sec-evaldeclarationinstantiation", "If _thisEnv_ is not the Environment Record for a |Catch| clause, throw"),
    ("sec-evaldeclarationinstantiation", "Throw a *SyntaxError* exception."),
    ("sec-evaldeclarationinstantiation", "If AllPrivateIdentifiersValid of _body_"),
    ("sec-createdynamicfunction", "If _parameters_ is a List of errors"),
    ("sec-createdynamicfunction", "If _body_ is a List of errors"),
    ("sec-createdynamicfunction", "If _expr_ is a List of errors"),
]

# Family EG: Annex B's grammar-only features and the relaxations it lets a parser make, one row per feature,
# in document order: (clause, phrase of the paragraph that states it, whether the list after it belongs).
EG_PROSE = [
    ("sec-html-like-comments", "this extension is not allowed when parsing source text using the goal symbol", False),
    ("sec-regular-expressions-patterns", "none of these extensions change the syntax of Unicode patterns", False),
    ("sec-labelled-function-declarations", "That rule is then modified to suppress the Syntax Error", False),
    ("sec-block-level-function-declarations-web-legacy-compatibility-semantics",
     "This feature involves special semantics at the following points", True),
    ("sec-functiondeclarations-in-ifstatement-statement-clauses", "This production only applies when parsing non-strict code", False),
    ("sec-variablestatements-in-catch-blocks", "In this feature, the |Block| of a |Catch| clause may contain", False),
    ("sec-initializers-in-forin-statement-heads", "This production only applies when parsing non-strict code", False),
    ("sec-runtime-errors-for-function-call-assignment-targets", "instead of producing an early error", False),
]

# Family EP, explicit resource management: the rules main has merged, which the edition does not have.
EP_MERGED_COUNT = 8

# Family EP, proposals main does not hold. The rule text is the catalogue's own (text_from "catalogue"): the
# construct, the proposal's production for it, and what a parse without the proposal does.
EP_WRITTEN = [
    ("72", "tc39/proposal-decorators@24882c7b", "-",
     "Decorators: `@` followed by a member expression, a call expression or a parenthesised expression, before a "
     "class declaration, a class expression or a class element (test262 feature `decorators`; ccaac100 holds "
     "positive syntax tests only). Without the proposal `@` begins no token, and the source text does not parse."),
    ("72", "tc39/proposal-decorators@24882c7b", "-",
     "Auto-accessors: a class element `accessor` ClassElementName Initializer?, with no line terminator after "
     "`accessor` (test262 feature `decorators`). Without the proposal `accessor` is a field name, and `accessor x` "
     "on one line does not parse."),
    ("73", "tc39/proposal-source-phase-imports@360899f1",
     "ImportDeclaration : `import` `source` ImportedBinding FromClause `;`",
     "A source-phase import declaration (test262 feature `source-phase-imports`). Without the proposal "
     "`import source x from \"m\"` does not parse."),
    ("73", "tc39/proposal-source-phase-imports@360899f1",
     "ImportCall : `import` `.` `source` `(` AssignmentExpression[+In, ?Yield, ?Await] `)`",
     "A source-phase dynamic import: exactly one argument, and never an assignment target (test262 feature "
     "`source-phase-imports`, test/language/expressions/dynamic-import/syntax/invalid and "
     "test/language/expressions/assignmenttargettype). Without the proposal `import.source` does not parse."),
    ("73", "tc39/proposal-defer-import-eval@b9eb1429",
     "ImportDeclaration : `import` `defer` NameSpaceImport FromClause WithClause? `;`",
     "A deferred namespace import; only the namespace form is admitted, so `import defer x from` and "
     "`import defer { x } from` are syntax errors under the proposal (test262 feature `import-defer`, "
     "test/language/import/import-defer/syntax). Without the proposal `import defer * as ns from \"m\"` does not "
     "parse."),
    ("73", "tc39/proposal-defer-import-eval@b9eb1429",
     "ImportCall : `import` `.` `defer` ImportCallArguments[?Yield, ?Await]",
     "A deferred dynamic import, with the arguments of `import(...)`, and never an assignment target (test262 "
     "feature `import-defer`, test/language/expressions/dynamic-import/syntax/invalid). Without the proposal "
     "`import.defer` does not parse."),
]

# Family EP, ShadowRealm: the SyntaxError steps of PerformShadowRealmEval, which ShadowRealm.prototype.evaluate
# calls (test262 feature `ShadowRealm`), in document order, as EA_STEPS holds PerformEval's. Added on 2026-10-20,
# after review of the completeness check, each taking the family's next id and block 74's next code. The texts
# are the proposal's steps as step_rule renders them (the header says how they were checked); the tool does not
# read the proposal.
SHADOWREALM_EVAL = "tc39/proposal-shadowrealm@9ff2a01f#sec-performshadowrealmeval"
SHADOWREALM_STEP_2 = ("2. Perform the following substeps in an implementation-defined order, possibly interleaving "
                      "parsing and error detection: ")
EP_STEPS = [
    ("74", SHADOWREALM_EVAL, "2.b",
     SHADOWREALM_STEP_2 + "2.b. If _script_ is a List of errors, throw a *SyntaxError* exception."),
    ("74", SHADOWREALM_EVAL, "2.e",
     SHADOWREALM_STEP_2 + "2.e. If _body_ Contains |NewTarget| is *true*, throw a *SyntaxError* exception."),
    ("74", SHADOWREALM_EVAL, "2.f",
     SHADOWREALM_STEP_2 + "2.f. If _body_ Contains |SuperProperty| is *true*, throw a *SyntaxError* exception."),
    ("74", SHADOWREALM_EVAL, "2.g",
     SHADOWREALM_STEP_2 + "2.g. If _body_ Contains |SuperCall| is *true*, throw a *SyntaxError* exception."),
]

# Family EX: refusals no early-error rule of the edition makes (early-errors.md, "Families"). Block 91 is
# source text that matches no production, which the specification reports only as a failure to parse and
# whose classification is each engine's own; 92 the engines' limits; 93 the engines' policies.
EX_WRITTEN = [
    ("91", "Grammar: a code point that begins no input element of the goal's lexical grammar."),
    ("91", "Grammar: a MultiLineComment with no closing `*/` before the end of the source text."),
    ("91", "Grammar: a numeric literal that matches no NumericLiteral production, or a NumericLiteral directly "
           "followed by an IdentifierStart or a DecimalDigit (12.9.3)."),
    ("91", "Grammar: a StringLiteral with no closing quote before a LineTerminator or the end of the source text."),
    ("91", "Grammar: an escape sequence in a StringLiteral that the lexical grammar does not define: an "
           "incomplete `\\x` or `\\u` escape, or a `\\u{...}` escape above 0x10FFFF."),
    ("91", "Grammar: a RegularExpressionLiteral with no closing `/` before a LineTerminator or the end of the "
           "source text."),
    ("91", "Grammar: a template literal with no closing backquote before the end of the source text."),
    ("91", "Grammar: a token that no production admits where it stands, including the end of the source text."),
    ("91", "Grammar: a token that the production being parsed requires is missing."),
    ("91", "Grammar: an ImportDeclaration or ExportDeclaration in source text parsed with the goal symbol Script."),
    ("92", "Limit: syntactic nesting deeper than the parse request's nesting bound (the contract's nesting "
           "diagnostic, JSD-0080 decision D27)."),
    ("92", "Limit: a tree deeper than the front end's tree-depth constant."),
    ("92", "Limit: eval code whose enclosing scope chain is deeper than the engine's eval scope-depth ceiling."),
    ("92", "Limit: a function with more locals than the artifact format admits."),
    ("92", "Limit: a unit with more constants than the artifact format admits."),
    ("92", "Limit: an operand stack deeper than the artifact format admits."),
    ("93", "Policy: a construct the declared feature manifest does not admit (Broiler.VM's manifests)."),
    ("93", "Policy: an assignment to a `const` binding, refused when the program is compiled; the specification "
           "throws a TypeError when the assignment is evaluated."),
    ("93", "Policy: a name with no binding and no global object to resolve it on, refused when the program is "
           "compiled; the specification throws a ReferenceError when such a reference is read, or is written in "
           "strict mode code."),
    ("93", "Policy: two modules presented under one module key in one composition."),
    ("93", "Policy: an import attribute the host cannot honour, refused when the source is parsed or compiled; "
           "the specification fails the module's loading instead, with a SyntaxError for a key the host does not "
           "support (InnerModuleLoading) and as the host decides for a `type` value it cannot load."),
    ("93", "Policy: a JSON module whose source text is not JSON (the host's ParseJSONModule)."),
    ("93", "Policy: `await` at the top level of a module when the host has not enabled top-level await "
           "(Broiler.JS's switch)."),
    ("93", "Policy: a construct the engine parses but does not compile (a meta property, or a form the "
           "compiler has no translation for)."),
    # Added by the completeness check (task 3.11, 2026-10-19), each taking the family's next id and its block's
    # next code: refusal sites the engines have that no row above explains.
    ("91", "Grammar: a unicode escape sequence in an IdentifierName that the lexical grammar does not define: an "
           "incomplete `\\u` escape, a `\\u{...}` escape with no digits or a digit that is not hexadecimal, or a "
           "`\\u{...}` escape above 0x10FFFF."),
    ("92", "Limit: a regular-expression pattern nested deeper than the engine's bound on groups and character "
           "classes."),
    ("92", "Limit: a JSON module nested deeper than the engine's bound."),
    ("92", "Limit: a BigInt literal wider than the artifact format admits."),
    ("93", "Policy: a module specifier that is not well-formed Unicode (it holds a lone surrogate), refused when "
           "the source is parsed; the specification admits any StringLiteral as a ModuleSpecifier and leaves the "
           "specifier to the host."),
    ("93", "Policy: a compile request the engine cannot serve: an output form the declared feature manifest does "
           "not define, a backend the build does not carry, or a backend's refusal of the unit. No property of "
           "the source text is involved."),
    ("93", "Policy: a dynamic function (the Function, GeneratorFunction, AsyncFunction or AsyncGeneratorFunction "
           "constructor) whose source text holds a private name, refused as AllPrivateIdentifiersValid refuses one "
           "in a Script; the edition's CreateDynamicFunction has no step that refuses it, and test262 expects a "
           "SyntaxError."),
]


def sha10(text):
    return hashlib.sha256(text.encode("utf-8")).hexdigest()[:10]


def block_of(row):
    """The shared code's block: the ECMA-262 clause of the row's anchor, 81 and 83 for Annex B.1 and B.3,
    71 to 74 for the proposals, 91 to 93 for the engine-only refusals (early-errors.md, "Shared codes")."""
    if row["family"] in ("EP", "EX"):
        return row["block"]
    section = row["section"]
    if section.startswith("B."):
        return "8" + section.split(".")[1]
    return f"{int(section.split('.')[0]):02d}"


def build(edition, later):
    rows = []
    rules = early_error_rules(edition)
    check_sentences(edition, rules)
    for n, r in enumerate(rules, 1):
        rows.append(dict(r, id=f"EE{n:04d}", family="EE"))

    for n, (clause_id, phrase) in enumerate(EA_STEPS, 1):
        label, text = step_rule(edition, clause_id, phrase)
        rows.append({"id": f"EA{n:04d}", "family": "EA", "anchor": f"{edition.label}#{clause_id}", "clause": clause_id,
                     "section": edition.number(edition.by_id[clause_id]), "productions": "-", "ordinal": label,
                     "text_from": "step", "rule_text": text, "annexb": annexb_of(text, False)})

    for n, (clause_id, phrase, with_list) in enumerate(EG_PROSE, 1):
        productions, ordinal, text = prose_rule(edition, clause_id, phrase, with_list)
        rows.append({"id": f"EG{n:04d}", "family": "EG", "anchor": f"{edition.label}#{clause_id}", "clause": clause_id,
                     "section": edition.number(edition.by_id[clause_id]), "productions": productions,
                     "ordinal": ordinal, "text_from": "prose", "rule_text": text, "annexb": "annex-b"})

    known = {(r["clause"], r["productions"], sha10(r["rule_text"])) for r in rules}
    merged = [r for r in early_error_rules(later)
              if (r["clause"], r["productions"], sha10(r["rule_text"])) not in known
              and ("Using" in r["productions"] or "ContainsUsing" in r["rule_text"])]
    if len(merged) != EP_MERGED_COUNT:
        sys.exit(f"{later.label}: {len(merged)} explicit-resource-management rules, expected {EP_MERGED_COUNT}")
    ep = [dict(r, block="71") for r in merged]
    ep += [{"block": b, "anchor": a, "clause": "-", "section": "-", "productions": p, "ordinal": "-",
            "text_from": "catalogue", "rule_text": t, "annexb": "-"} for b, a, p, t in EP_WRITTEN]
    ep += [{"block": b, "anchor": a, "clause": a.split("#")[-1], "section": "-", "productions": "-", "ordinal": o,
            "text_from": "step", "rule_text": t, "annexb": "-"} for b, a, o, t in EP_STEPS]
    for n, r in enumerate(ep, 1):
        rows.append(dict(r, id=f"EP{n:04d}", family="EP"))

    for n, (b, text) in enumerate(EX_WRITTEN, 1):
        rows.append({"id": f"EX{n:04d}", "family": "EX", "block": b, "anchor": "-", "clause": "-", "section": "-",
                     "productions": "-", "ordinal": "-", "text_from": "catalogue", "rule_text": text, "annexb": "-"})

    used = {}
    keys = set()
    for row in rows:
        row["rule_sha10"] = sha10(row["rule_text"])
        block = block_of(row)
        used[block] = used.get(block, 0) + 1
        if used[block] > 99:
            sys.exit(f"block {block} is full")
        row["shared_code"] = f"JSE{block}{used[block]:02d}"
        key = (row["anchor"].split("#")[-1], row["productions"], row["rule_sha10"])
        if key in keys:
            sys.exit(f"{row['id']}: content key {key} is not unique")
        keys.add(key)
        for column in COLUMNS[COLUMNS.index("message_id"):]:
            if column in ("vm_admits", "js_admits"):
                row[column] = UNFILLED if row["family"] in ("EG", "EP") else "-"
            elif column == "notes":
                row[column] = "-"
            else:
                row[column] = UNFILLED
    return rows


def render(rows):
    def cell(value):
        value = str(value)
        if "\t" in value or "\n" in value or "\r" in value:
            raise ValueError(f"a cell holds a tab or a line break: {value[:60]!r}")
        return value
    lines = ["\t".join(COLUMNS)]
    lines += ["\t".join(cell(row[c]) for c in COLUMNS) for row in rows]
    return "\n".join(lines) + "\n"


def read_catalogue(path):
    with open(path, encoding="utf-8", newline="") as f:
        lines = f.read().split("\n")
    if lines[-1] == "":
        lines.pop()
    header = lines[0].split("\t")
    return header, [dict(zip(header, line.split("\t"))) for line in lines[1:]]


# --- commands -----------------------------------------------------------------------------------------------

def command_write(args):
    rows = build(Spec(args.edition, "edition"), Spec(args.later, "later"))
    with open(args.out, "w", encoding="utf-8", newline="\n") as f:
        f.write(render(rows))
    families = {}
    for row in rows:
        families[row["family"]] = families.get(row["family"], 0) + 1
    print(f"{len(rows)} rows: " + ", ".join(f"{k} {v}" for k, v in families.items()))


def command_check(args):
    rows = build(Spec(args.edition, "edition"), Spec(args.later, "later"))
    header, committed = read_catalogue(args.catalogue)
    problems = []
    if header != COLUMNS:
        problems.append(f"columns differ: {header}")
    if len(committed) != len(rows):
        problems.append(f"{len(committed)} rows committed, {len(rows)} extracted")
    for want, have in zip(rows, committed):
        for column in IDENTITY:
            if str(want[column]) != have.get(column):
                problems.append(f"{want['id']} {column}: committed {have.get(column)!r}, extracted {want[column]!r}")
    for p in problems:
        print(p)
    print(f"{len(rows)} rows extracted; {len(problems)} differences in the identifying columns")
    return 1 if problems else 0


def command_diff(args):
    base = Spec(args.edition, "edition")
    other = Spec(args.other, args.other_pin)
    a, b = early_error_rules(base), early_error_rules(other)
    check_sentences(other, b)

    def key(r):
        return (r["clause"], r["productions"], r["ordinal"], r["rule_text"])
    sections = {key(r): r["section"] for r in a}
    kb = {key(r) for r in b}
    removed = [r for r in a if key(r) not in kb]
    added = [r for r in b if key(r) not in sections]
    moved = sum(1 for r in b if key(r) in sections and r["section"] != sections[key(r)])
    print(f"{base.label}: {len(a)} rules; {other.label}: {len(b)} rules; "
          f"{len(removed)} only in {base.label}, {len(added)} only in {other.label}, {moved} renumbered")
    for r in removed:
        print(f"- {r['section']} {r['clause']} | {r['productions']} | {r['ordinal']} | {sha10(r['rule_text'])} | {r['rule_text']}")
    for r in added:
        print(f"+ {r['section']} {r['clause']} | {r['productions']} | {r['ordinal']} | {sha10(r['rule_text'])} | {r['rule_text']}")
    return 1 if removed or added else 0


def main(argv):
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    sub = parser.add_subparsers(dest="command", required=True)
    w = sub.add_parser("write", help="write the catalogue's skeleton")
    w.add_argument("--edition", required=True)
    w.add_argument("--later", required=True)
    w.add_argument("--out", required=True)
    c = sub.add_parser("check", help="compare a catalogue's identifying columns with the extraction")
    c.add_argument("--edition", required=True)
    c.add_argument("--later", required=True)
    c.add_argument("--catalogue", required=True)
    d = sub.add_parser("diff", help="the early-error rules another revision adds, removes or changes")
    d.add_argument("--edition", required=True)
    d.add_argument("--other", required=True)
    d.add_argument("--other-pin", choices=["errata", "later"], required=True)
    args = parser.parse_args(argv)
    if args.command == "write":
        command_write(args)
        return 0
    if args.command == "check":
        return command_check(args)
    return command_diff(args)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
