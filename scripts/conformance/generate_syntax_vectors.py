#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Write the syntax area of the conformance corpus (corpus revision 29, milestone SJL-13, work package SJL-13.2).

    python3 -I scripts/conformance/generate_syntax_vectors.py [--out conformance/vectors/syntax]
    python3 -I scripts/conformance/generate_syntax_vectors.py --check-node <Node v26.12.0>
        [--spec <ECMA-262 spec.html at 0248456c>] [--test262 <test262 checkout at ccaac100>]

THE AREA (decision D25 of Broiler.VM's JSD-0083, schema 1 unchanged). A vector pins one source text's parse: `op` is
`ParseScript`, the source parsed as a Script, or `PerformEval`, the source parsed as the eval code of a top-level
direct eval in a fresh realm; `in` holds the source as one `str:` value, which the library-level runner parses; and
`script` holds the same source as the one-line Script the engine-level runners evaluate, or, under `PerformEval`, a
top-level direct `eval` of it. `out` is `error: SyntaxError` for a refusal, or the completion value of an acceptance,
which completes without throwing. A source that holds a line terminator or a code unit outside printable ASCII is a
`PerformEval` vector, since a `script` is one line of ASCII; a source an eval code step decides (EA0001, EA0002) is one
too. Six files, one per class of row: lexical (the lexical grammar, its early errors and the plan's lexical vectors),
expressions (ECMA-262 clause 13), statements (clause 14 and the explicit-resource-management rules), functions and
classes (clause 15), scripts and eval (clauses 16 and 19 and the grammar rows) and annex-b (Annex B's B.3 extensions).

THE ROWS. Every row of the stage table (Broiler.ECMAScript/docs/frontend/stages.tsv) whose stage is `syntax` and whose
input a Script or a top-level direct eval can carry has one accepted and one refused vector, named in the vectors'
notes, beside more where a correction of the correspondence is carried (``check_rows``). The rows only a module goal,
a switch or a parameter reaches, or whose half no source of schema 1 asserts, are listed in ``UNCOVERED`` with the
reason, so the count of rows covered is checkable: the parse corpus judges them (the home's part A in configurations V
and J; Broiler.VM's part B).

THE EXPECTED VALUES are the specification's, ECMA-262 ES2026 at 0248456c (sources/ecma262.pin), each vector citing the
clause that decides it and, for a refusal, the test262 tests at ccaac100 that assert it; an acceptance's completion
value follows from the evaluation semantics, and its sources are chosen so that the value is plain. No expected value
is read from an engine or from the reference. The rules ECMA-262 main merged after the pinned edition (EP0001 to EP0008,
explicit resource management) have no clause in the pinned text: their vectors cite the test262 tests that assert them
alone, and a half no test asserts has no vector (``UNCOVERED``). Every vector is checked against an independent
reference, the Node.js v26.12.0 bundle sjl-9-001's reference.txt names, by --check-node (syntaxcheck.mjs): a refused
vector must be a SyntaxError there at parse time and an accepted one must complete there with the vector's `out`;
where the reference disagrees with the specification the vector follows the specification and ``NODE_DISAGREES``
names it, and the check fails on any disagreement it does not name or any it names that does not occur.

THE CLASSES. `normative` unless the specification leaves the outcome to the host. Annex B's extensions are required
of a web-browser host and optional for any other (Annex B's introduction and B.3's), so a vector whose outcome holds
only with one is `impl-defined`, with each engine's `per-engine` expectation from its record: both engines' requests
turn Annex B and HTML-like comments on for scripts (Broiler.VM's JSD-0083, decision D17 and its correspondence's row
R08; this repository's docs/roadmap/SharedLibraries.md, the frontend's syntax half, "The request"). Broiler.JS's `m`
suffix admits a source the specification refuses: its vectors carry a `divergence.broiler-js` field from this
engine's record (SJL-D-17, and the correspondence's row T18), read from syntax-divergences.txt like every other
classification of the area: each engine's first run on the area is classified in the revision after the one that
adds it, in scripts/conformance/syntax-divergences.txt, which this generator reads where it exists.

The generator is deterministic and reads nothing but this file, the stage table and syntax-divergences.txt: the same
inputs write the same bytes.
"""

from __future__ import annotations

import argparse
import csv
from dataclasses import dataclass, field
import json
import math
from pathlib import Path
import re
import struct
import subprocess
import sys
import tempfile
from typing import Sequence

SCRIPTS = Path(__file__).resolve().parent
REPOSITORY = SCRIPTS.parents[1]
CORPUS = REPOSITORY / "conformance" / "vectors"
DEFAULT_OUT = CORPUS / "syntax"
STAGES = REPOSITORY / "Broiler.ECMAScript" / "docs" / "frontend" / "stages.tsv"
DIVERGENCES = SCRIPTS / "syntax-divergences.txt"
CHECKER = SCRIPTS / "syntaxcheck.mjs"
SINCE = 29              # the corpus revision that added the area
CORPUS_REVISION = 29    # the corpus revision this generator writes: REVISIONS.md's last row
GENERATOR = "generate_syntax_vectors.py"
REFERENCE_VERSION = "v26.12.0"

SPEC = "spec:es2026@0248456c#"
SE = "error: SyntaxError"

FILES = {
    "lexical": "the lexical grammar (ECMA-262 clause 12): white space, line terminators, comments with the HTML-like "
               "comments of B.1.1 and the hashbang, identifiers and their escapes, numeric literals with separators "
               "and their legacy forms, string literals, templates and their cooking, regular-expression delimiting "
               "after every punctuator class, and automatic semicolon insertion",
    "expressions": "expressions (clause 13): identifiers and reserved words, object literals, templates, the cover "
                   "grammars, update, delete and assignment targets, and destructuring assignment",
    "statements": "statements and declarations (clause 14), and the explicit-resource-management rules ECMA-262 main "
                  "merged after the pinned edition",
    "functions-and-classes": "functions and classes (clause 15): parameters, bodies, arrows, methods, generators, "
                             "async functions and class elements",
    "scripts": "scripts and eval code (clauses 16 and 19), and the grammar's own refusals",
    "annex-b": "Annex B's syntactic extensions (B.3): labelled function declarations, block-level functions, function "
               "declarations in if clauses, var in catch blocks, for-in initialisers and calls as assignment targets",
}

# ---- Records the impl-defined vectors and the divergence fields cite ------------------------------------------------

ANNEX_B = ("Annex B is required of a web-browser host and optional for any other, so the outcome is the host's: both "
           "engines' requests turn it on for scripts (Broiler.VM's JSD-0083, decision D17, and its bundle sjl-13-001's "
           "correspondence, row R08, same; Broiler.JS's docs/roadmap/SharedLibraries.md, the frontend's syntax half, "
           "the request's switches), so each engine's expectation is the extension's outcome")
HTML_COMMENTS = ("HTML-like comments are Annex B's (B.1.1), on for scripts in both engines' requests (Broiler.VM's "
                 "JSD-0083, decision D17, rows T4 and R07; Broiler.JS's docs/roadmap/SharedLibraries.md, the "
                 "frontend's syntax half, the request's switches), so each engine's expectation is the comment's")


# ---- Typed values and scripts ------------------------------------------------------------------------------------


class GeneratorError(RuntimeError):
    """The generator cannot write the area, and says why."""


@dataclass(frozen=True)
class Big:
    """A BigInt outcome."""
    value: int


def utf16_units(text: str) -> list[int]:
    data = text.encode("utf-16-le", errors="surrogatepass")
    return [data[i] | (data[i + 1] << 8) for i in range(0, len(data), 2)]


def quote(text: str) -> str:
    """A String in the schema's notation: printable ASCII but `"` and `\\`, and \\uXXXX for every other code unit."""
    out = ['"']
    for unit in utf16_units(text):
        if unit == 0x22:
            out.append('\\"')
        elif unit == 0x5C:
            out.append("\\\\")
        elif 0x20 <= unit <= 0x7E:
            out.append(chr(unit))
        else:
            out.append(f"\\u{unit:04X}")
    out.append('"')
    return "".join(out)


def js(text: str) -> str:
    """A JavaScript string literal, ASCII only: every code unit outside printable ASCII as \\uXXXX."""
    out = ['"']
    for unit in utf16_units(text):
        if unit == 0x22:
            out.append('\\"')
        elif unit == 0x5C:
            out.append("\\\\")
        elif 0x20 <= unit <= 0x7E:
            out.append(chr(unit))
        else:
            out.append(f"\\u{unit:04x}")
    out.append('"')
    return "".join(out)


def typed(value) -> str:
    """A Python value as the schema's typed value (section 3)."""
    if value is None:
        return "undefined"
    if isinstance(value, bool):
        return "bool:true" if value else "bool:false"
    if isinstance(value, Big):
        return f"bigint:{value.value}"
    if isinstance(value, (int, float)):
        raw = struct.unpack(">Q", struct.pack(">d", float(value)))[0]
        if math.isnan(float(value)):
            raw = 0x7FF8000000000000
        return f"f64:0x{raw:016X}"
    if isinstance(value, str):
        return "str:" + quote(value)
    raise GeneratorError(f"no typed value for {value!r}")


def V(value) -> str:
    """An acceptance's outcome: the completion value."""
    return "value: " + typed(value)


NAN = float("nan")


def printable(text: str) -> bool:
    return all(0x20 <= ord(c) <= 0x7E for c in text)


# ---- Cases --------------------------------------------------------------------------------------------------------


@dataclass
class Case:
    """One vector."""
    file: str
    group: str
    op: str
    source: str
    script: str
    out: str
    rows: tuple[str, ...]
    corr: tuple[str, ...]
    clauses: tuple[str, ...]
    tests: tuple[str, ...]
    note: str
    cls: str = "normative"
    per_engine: dict[str, str] = field(default_factory=dict)

    @property
    def refused(self) -> bool:
        return self.out == SE


CASES: list[Case] = []


def add(file: str, group: str, source: str, out: str, *, rows: Sequence[str] = (), corr: Sequence[str] = (),
        clauses: Sequence[str] = (), tests: Sequence[str] = (), note: str = "", op: str | None = None,
        script: str | None = None, impl: str | None = None) -> None:
    """One vector. `op` is PerformEval where the source needs a line terminator or a code unit outside printable
    ASCII, ParseScript otherwise, unless given; `script` is the source itself under ParseScript and a top-level direct
    eval of it under PerformEval, unless given; `impl` makes it impl-defined, each engine's expectation being `out`,
    with the reason given."""
    if file not in FILES:
        raise GeneratorError(f"no file {file}")
    if op is None:
        op = "ParseScript" if printable(source) else "PerformEval"
    if script is None:
        script = source if op == "ParseScript" else "eval(" + js(source) + ")"
    if op == "ParseScript" and script != source:
        raise GeneratorError(f"{group}: a ParseScript vector's script is its source")
    if not printable(script):
        raise GeneratorError(f"{group}: a script is one line of printable ASCII: {script!r}")
    if not clauses and not tests:
        raise GeneratorError(f"{group}: {source!r} has no source of its expected value")
    note_parts = []
    if rows:
        note_parts.append(("catalogue row " if len(rows) == 1 else "catalogue rows ") + ", ".join(rows))
    if corr:
        note_parts.append(("correspondence row " if len(corr) == 1 else "correspondence rows ") + ", ".join(corr))
    head = "; ".join(note_parts)
    verdict = "refused" if out == SE else "accepted"
    text = f"{head}: {verdict}, {note}" if head else f"{verdict}, {note}"
    case = Case(file, group, op, source, script, out, tuple(rows), tuple(corr), tuple(clauses), tuple(tests),
                text.rstrip(", "))
    if impl is not None:
        case.cls = "impl-defined"
        case.per_engine = {"broiler-js": out, "broiler-vm": out}
        case.note += "; " + impl
    CASES.append(case)


def C(*ids: str) -> tuple[str, ...]:
    return tuple(ids)


def T(*paths: str) -> tuple[str, ...]:
    return tuple(paths)


# Clause ids of the pinned edition used more than once.
IDS_EE = "sec-identifier-names-static-semantics-early-errors"
IDS_ID = "sec-identifiers-static-semantics-early-errors"
NUM = "sec-literals-numeric-literals"
NUM_EE = "sec-numeric-literals-early-errors"
STR = "sec-literals-string-literals"
STR_EE = "sec-string-literals-early-errors"
TEMPLATE_EE = "sec-static-semantics-template-early-errors"
TEMPLATE_LEX = "sec-template-literal-lexical-components"
REGEXP = "sec-literals-regular-expression-literals"
LEX = "sec-ecmascript-language-lexical-grammar"
ASI = "sec-rules-of-automatic-semicolon-insertion"
STRICT = "sec-strict-mode-code"
DIRECTIVE = "sec-directive-prologues-and-the-use-strict-directive"


def lexical() -> None:
    f = "lexical"
    # EE0001, EE0002: an escaped identifier code point.
    add(f, "ee0001", "var \\u0061b = 1; ab", V(1), rows=("EE0001",), clauses=C(IDS_EE, "sec-identifier-names"),
        note="an escaped ID_Start code point begins an identifier")
    add(f, "ee0001", "var \\u0030a;", SE, rows=("EE0001",), clauses=C(IDS_EE),
        tests=T("test/language/identifiers/vertical-tilde-start-escaped.js",
                "test/language/identifiers/start-zwj-escaped.js"),
        note="an escaped digit is no IdentifierStartChar")
    add(f, "ee0002", "var a\\u0030 = 2; a0", V(2), rows=("EE0002",), clauses=C(IDS_EE, "sec-identifier-names"),
        note="an escaped digit continues an identifier")
    add(f, "ee0002", "var a\\u002D;", SE, rows=("EE0002",), clauses=C(IDS_EE),
        tests=T("test/language/identifiers/vertical-tilde-continue-escaped.js"),
        note="an escaped hyphen-minus is no IdentifierPartChar")
    # EE0003, EE0004: legacy octal and non-octal decimal forms in strict code.
    add(f, "ee0003", "010", V(8), rows=("EE0003",), clauses=C(NUM, NUM_EE),
        tests=T("test/language/literals/numeric/legacy-octal-integer.js"),
        note="a LegacyOctalIntegerLiteral in sloppy code is octal")
    add(f, "ee0003", "\"use strict\"; 010", SE, rows=("EE0003",), clauses=C(NUM_EE, DIRECTIVE),
        tests=T("test/language/literals/numeric/legacy-octal-integer-strict.js"),
        note="a LegacyOctalIntegerLiteral in strict code")
    add(f, "ee0004", "var s = \"\\01\"; s", V("\x01"), rows=("EE0004",), clauses=C(STR, STR_EE),
        note="a LegacyOctalEscapeSequence in sloppy code is octal")
    add(f, "ee0004", "\"use strict\"; var s = \"\\01\";", SE, rows=("EE0004",), clauses=C(STR_EE),
        tests=T("test/language/literals/string/legacy-octal-escape-sequence-strict.js"),
        note="a LegacyOctalEscapeSequence in strict code")
    add(f, "ee0004", "var s = \"\\8\"; s", V("8"), rows=("EE0004",), clauses=C(STR, STR_EE),
        note="a NonOctalDecimalEscapeSequence in sloppy code is its digit")
    add(f, "ee0004", "\"use strict\"; \"\\8\";", SE, rows=("EE0004",), clauses=C(STR_EE),
        tests=T("test/language/literals/string/legacy-non-octal-escape-sequence-8-strict-explicit-pragma.js"),
        note="a NonOctalDecimalEscapeSequence in strict code")
    add(f, "ee0004", "function f() { \"\\01\"; \"use strict\"; }", SE, rows=("EE0004",), clauses=C(STR_EE, DIRECTIVE),
        tests=T("test/language/literals/string/legacy-octal-escape-sequence-prologue-strict.js"),
        note="a legacy octal escape in a directive before a later Use Strict Directive of the same prologue")
    # T16: strict legacy octal and non-octal decimal literals where a key or a fraction holds them.
    add(f, "t16", "\"use strict\"; ({ 010: 1 })", SE, rows=("EE0003",), corr=("T16",), clauses=C(NUM_EE),
        tests=T("test/language/literals/numeric/legacy-octal-integer-strict.js"),
        note="a legacy octal numeric property key in strict code (a correction of Broiler.VM that step 1 makes)")
    add(f, "t16", "class C { 010() {} }", SE, rows=("EE0003",), corr=("T16",), clauses=C(NUM_EE, STRICT),
        tests=T("test/language/literals/numeric/legacy-octal-integer-strict.js"),
        note="a legacy octal method name in a class body, which is strict code (a correction of both engines)")
    add(f, "t16", "function f() { \"use strict\"; return { 010: 1 }; }", SE, rows=("EE0003",), corr=("T16",),
        clauses=C(NUM_EE, DIRECTIVE), tests=T("test/language/literals/numeric/legacy-octal-integer-strict.js"),
        note="a legacy octal numeric property key in a function made strict by its own directive")
    add(f, "t16", "\"use strict\"; 08", SE, rows=("EE0003",), corr=("T16",), clauses=C(NUM_EE),
        tests=T("test/language/literals/numeric/non-octal-decimal-integer-strict.js"),
        note="a NonOctalDecimalIntegerLiteral in strict code")
    add(f, "t16", "\"use strict\"; 08.5", SE, rows=("EE0003",), corr=("T16",), clauses=C(NUM_EE, NUM),
        tests=T("test/language/literals/numeric/non-octal-decimal-integer-strict.js"),
        note="a NonOctalDecimalIntegerLiteral with a fraction in strict code")
    # EX0001: a code point that begins no input element.
    add(f, "ex0001", "var\u00a0a = 1; a", V(1), rows=("EX0001",), clauses=C("sec-white-space", LEX),
        note="U+00A0 is WhiteSpace")
    add(f, "ex0001", "var\u180ea;", SE, rows=("EX0001",), clauses=C("sec-white-space", LEX),
        tests=T("test/language/white-space/mongolian-vowel-separator.js"),
        note="U+180E is no WhiteSpace and begins no input element")
    add(f, "ex0001", "1 # 2", SE, rows=("EX0001",), clauses=C(LEX, "sec-names-and-keywords"),
        note="a number sign followed by no IdentifierName begins no input element")
    # EX0002 to EX0007: unterminated and malformed tokens.
    add(f, "ex0002", "/* a */ 1", V(1), rows=("EX0002",), clauses=C("sec-comments"), note="a closed MultiLineComment")
    add(f, "ex0002", "/* a", SE, rows=("EX0002",), clauses=C("sec-comments"),
        tests=T("test/language/comments/S7.4_A2_T2.js"), note="a MultiLineComment with no closing */")
    add(f, "ex0002", "/* a */ */", SE, rows=("EX0002",), clauses=C("sec-comments"),
        note="a */ outside a comment is no token a production admits there")
    add(f, "ex0003", "0x1F", V(31), rows=("EX0003",), clauses=C(NUM), note="a HexIntegerLiteral")
    add(f, "ex0003", "3in []", SE, rows=("EX0003",), clauses=C(NUM),
        tests=T("test/language/literals/numeric/numeric-followed-by-ident.js"),
        note="a NumericLiteral directly followed by an IdentifierStart")
    add(f, "m-suffix", "var d = 1m; 0", SE, rows=("EX0003",), corr=("T18",), clauses=C(NUM),
        tests=T("test/language/literals/numeric/numeric-followed-by-ident.js"),
        note="a NumericLiteral directly followed by the IdentifierStart m, which Broiler.JS's extension reads as a "
             "Decimal (SJL-D-17)")
    add(f, "m-suffix", "var d = 1.5m; 0", SE, rows=("EX0003",), corr=("T18",), clauses=C(NUM),
        tests=T("test/language/literals/numeric/numeric-followed-by-ident.js"),
        note="a fractional NumericLiteral directly followed by m, which Broiler.JS's extension reads as a Decimal "
             "(SJL-D-17)")
    add(f, "ex0004", "\"a\" + 'b'", V("ab"), rows=("EX0004",), clauses=C(STR), note="two closed string literals")
    add(f, "ex0004", "\"a", SE, rows=("EX0004",), clauses=C(STR),
        tests=T("test/language/literals/string/S7.8.4_A1.1_T2.js"),
        note="a StringLiteral with no closing quote before the end of the source text")
    add(f, "ex0005", "\"\\x41\\u0042\\u{43}\"", V("ABC"), rows=("EX0005",), clauses=C(STR, "sec-static-semantics-sv"),
        note="hexadecimal, Unicode and braced Unicode escapes")
    add(f, "ex0005", "\"\\u1\"", SE, rows=("EX0005",), clauses=C(STR),
        tests=T("test/language/literals/string/S7.8.4_A7.2_T1.js"), note="an incomplete \\u escape")
    add(f, "ex0005", "\"\\u{110000}\"", SE, rows=("EX0005",), clauses=C(STR, "sec-string-literals-early-errors"),
        tests=T("test/language/literals/string/unicode-escape-nls-err-double.js"),
        note="a braced escape above U+10FFFF")
    add(f, "ex0006", "/a/.source", V("a"), rows=("EX0006",), clauses=C(REGEXP), note="a closed RegularExpressionLiteral")
    add(f, "ex0006", "/a", SE, rows=("EX0006",), clauses=C(REGEXP),
        tests=T("test/language/literals/regexp/S7.8.5_A1.2_T1.js"),
        note="a RegularExpressionLiteral with no closing / before the end of the source text")
    add(f, "ex0007", "`a`", V("a"), rows=("EX0007",), clauses=C(TEMPLATE_LEX), note="a closed template")
    add(f, "ex0007", "`a", SE, rows=("EX0007",), clauses=C(TEMPLATE_LEX),
        note="a template with no closing backquote before the end of the source text")
    # EX0025 and T12: a Unicode escape an IdentifierName does not define.
    add(f, "ex0025", "var \\u{62} = 1; b", V(1), rows=("EX0025",), clauses=C("sec-identifier-names"),
        note="a braced escape in an IdentifierName")
    add(f, "ex0025", "var \\u{00_76} = 1;", SE, rows=("EX0025",), corr=("T12",), clauses=C("sec-identifier-names"),
        tests=T("test/language/identifiers/unicode-escape-nls-err.js"),
        note="a numeric separator among a braced escape's digits")
    add(f, "ex0025", "var a\\u{};", SE, rows=("EX0025",), clauses=C("sec-identifier-names"),
        note="a braced escape with no digits")
    # T12 and T13: an escape's hexadecimal digits.
    add(f, "escapes", "var s = \"\\x_1\";", SE, rows=("EX0005",), corr=("T12",), clauses=C(STR),
        note="an underscore among a \\x escape's digits")
    add(f, "escapes", "var s = \"\\u{0_41}\";", SE, rows=("EX0005",), corr=("T12",), clauses=C(STR),
        note="a numeric separator among a braced escape's digits")
    add(f, "escapes", "var s = \"\\x 41\";", SE, rows=("EX0005",), corr=("T13",), clauses=C(STR),
        note="white space inside a \\x escape's digits (a correction of Broiler.VM that step 1 makes)")
    add(f, "escapes", "var s = \"\\x1 \";", SE, rows=("EX0005",), corr=("T13",), clauses=C(STR),
        note="white space after a \\x escape's one digit (a correction of Broiler.VM that step 1 makes)")
    add(f, "escapes", "var s = \"\\u 0041\";", SE, rows=("EX0005",), corr=("T13",), clauses=C(STR),
        note="white space inside a \\u escape's digits (a correction of Broiler.VM that step 1 makes)")
    add(f, "escapes", "var a\\u 0062;", SE, rows=("EX0025",), corr=("T13",), clauses=C("sec-identifier-names"),
        note="white space inside an identifier's \\u escape (a correction of Broiler.VM that step 1 makes)")
    add(f, "escapes", "var s = '\\x\n1';", SE, rows=("EX0005",), corr=("T13",), clauses=C(STR),
        note="a line feed inside a \\x escape (a correction of Broiler.VM that step 1 makes)")
    add(f, "escapes", "\"\\x41\" + \"\\u0041\" + \"\\u{000041}\"", V("AAA"), rows=("EX0005",),
        clauses=C(STR, "sec-static-semantics-sv"), note="each escape with its exact digits, leading zeros in a "
        "braced escape")
    lexical_identifiers()
    lexical_numeric()
    lexical_templates()
    lexical_lines_and_comments()
    lexical_regexp()
    lexical_asi()


def lexical_identifiers() -> None:
    f = "lexical"
    ids = "sec-identifier-names"
    add(f, "identifiers", "var \\u{61}\\u0062 = 1; ab", V(1), clauses=C(ids), note="an identifier spelt by two escapes")
    add(f, "identifiers", "var \\u{0000000061} = 2; a", V(2), clauses=C(ids),
        note="a braced escape with leading zeros")
    add(f, "identifiers", "var a\\u200C = 3; a\\u200C", V(3), clauses=C(ids, IDS_EE),
        note="ZWNJ continues an identifier")
    add(f, "identifiers", "var \\u{10400} = 4; \\u{10400}", V(4), clauses=C(ids, IDS_EE),
        note="a supplementary ID_Start code point by a braced escape")
    add(f, "identifiers", "var $\\u{24} = 5; $$", V(5), clauses=C(ids, IDS_EE), note="an escaped dollar sign")
    add(f, "identifiers", "var _\\u005F = 6; __", V(6), clauses=C(ids, IDS_EE), note="an escaped low line")
    add(f, "identifiers", "({ \\u0069f: 7 }).if", V(7), rows=("EE0014",), clauses=C(ids, IDS_ID),
        note="an escaped reserved word is an IdentifierName as a property name")
    add(f, "identifiers", "var \\u0069f;", SE, rows=("EE0014",), clauses=C(IDS_ID),
        tests=T("test/language/identifiers/val-if-via-escape-hex4.js"), note="an escaped reserved word as a binding")
    add(f, "identifiers", "var \\u{110000};", SE, clauses=C(ids), note="a braced escape above U+10FFFF")
    add(f, "identifiers", "var \\uD801\\uDC00;", SE, rows=("EE0001",), clauses=C(IDS_EE),
        note="two escaped surrogates, each judged alone, are no IdentifierStartChar")
    add(f, "identifiers", "var a\\u{1D7CE} = 8; a\\u{1D7CE}", V(8), rows=("EE0002",), clauses=C(IDS_EE),
        note="a supplementary ID_Continue digit continues an identifier")
    add(f, "identifiers", "var \\u{1D7CE}a;", SE, rows=("EE0001",), clauses=C(IDS_EE),
        note="a supplementary digit is no IdentifierStartChar")
    add(f, "identifiers", "\\u0061sync function f() {}", SE, clauses=C(ids, "sec-keywords-and-reserved-words"),
        note="an escaped async is an identifier and no keyword, so a declaration follows it on its line")
    add(f, "identifiers", "for (var x \\u006ff []) ;", SE, clauses=C(ids, "sec-keywords-and-reserved-words"),
        note="an escaped of is no keyword of a for-of head")
    add(f, "identifiers", "class C { #x = 1; static g(o) { return o.#x; } } C.g(new C)", V(1),
        clauses=C("sec-names-and-keywords"), note="a private name is one token, # with its IdentifierName")
    add(f, "identifiers", "class C { # x; }", SE, corr=("T21",), clauses=C("sec-names-and-keywords"),
        tests=T("test/language/statements/class/elements/syntax/early-errors/grammar-privatename-whitespace-error-field.js"),
        note="white space between # and the name")


def lexical_numeric() -> None:
    f, g = "lexical", "numeric-literals"
    sep = "test/language/literals/numeric/numeric-separators/numeric-separator-literal-"
    accepted = [
        ("1_000", 1000, "nzd-nsl-dd.js", "a separator between decimal digits"),
        ("0x1_F", 31, "hil-hd-nsl-hd.js", "a separator between hexadecimal digits"),
        ("0b1_0", 2, "bil-bd-nsl-bd.js", "a separator between binary digits"),
        ("0o1_7", 15, "oil-od-nsl-od.js", "a separator between octal digits"),
        ("1_0.0_1", 10.01, "dds-dot-dd-nsl-dd-ep-dd.js", "separators in the integer and the fraction"),
        (".0_1", 0.01, "dot-dd-nsl-dd-ep.js", "a separator in a fraction with no integer part"),
        ("1e1_0", 1e10, None, "a separator in an exponent"),
        ("1.0e+1_0", 1e10, "dd-dot-dd-ep-sign-plus-dd-nsl-dd.js", "a separator in a signed exponent"),
    ]
    for text, value, test, note in accepted:
        add(f, g, text, V(value), clauses=C(NUM), tests=T(sep + test) if test else (), note=note)
    refused = [
        ("1__0", "dd-nsl-dds-dunder-err.js", "two adjacent separators"),
        ("10_", "dds-nsl-err.js", "a trailing separator"),
        ("0_1", "nzd-nsl-dds-leading-zero-err.js", "a separator after a leading zero"),
        ("0x_1", "hil-nsl-hd-err.js", "a separator after the radix prefix"),
        ("0x1_", "hil-hd-nsl-hd-err.js", "a trailing separator in a hexadecimal literal"),
        ("1._5", "dil-dot-nsl-err.js", "a separator after the decimal point"),
        ("1_.5", "dds-nsl-err.js", "a separator before the decimal point"),
        ("1e_1", "dot-nsl-ep-err.js", "a separator after the exponent indicator"),
        ("07_0", "lol-07-err.js", "a separator in a LegacyOctalIntegerLiteral"),
        ("08_0", "nonoctal-08-err.js", "a separator in a NonOctalDecimalIntegerLiteral"),
        ("0_8", "nonoctal-0_8-err.js", "a separator after the leading zero of a non-octal decimal"),
    ]
    for text, test, note in refused:
        add(f, g, text, SE, clauses=C(NUM), tests=T(sep + test), note=note)
    add(f, g, "07", V(7), rows=("EE0003",), clauses=C(NUM), tests=T("test/language/literals/numeric/legacy-octal-integer.js"),
        note="a LegacyOctalIntegerLiteral in sloppy code")
    add(f, g, "08", V(8), rows=("EE0003",), clauses=C(NUM),
        tests=T("test/language/literals/numeric/non-octal-decimal-integer.js"),
        note="a NonOctalDecimalIntegerLiteral in sloppy code is decimal")
    add(f, g, "09.5", V(9.5), rows=("EE0003",), clauses=C(NUM),
        note="a NonOctalDecimalIntegerLiteral may take a fraction")
    add(f, g, "08e1", V(80), rows=("EE0003",), clauses=C(NUM), note="a NonOctalDecimalIntegerLiteral may take an exponent")
    add(f, g, "07.5", SE, corr=("T17",), clauses=C(NUM), tests=T("test/language/literals/numeric/S7.8.3_A6.1_T1.js"),
        note="a LegacyOctalIntegerLiteral takes no fraction, so a second numeric literal follows it directly")
    add(f, g, "3$", SE, corr=("T17",), clauses=C(NUM), note="a NumericLiteral directly followed by the IdentifierStart $")
    add(f, g, "0b2", SE, clauses=C(NUM), tests=T("test/language/literals/numeric/binary-invalid-digit.js"),
        note="a digit no binary literal has")
    add(f, g, "0o8", SE, clauses=C(NUM), tests=T("test/language/literals/numeric/octal-invalid-digit.js"),
        note="a digit no octal literal has")
    add(f, g, "0xg", SE, clauses=C(NUM), note="a hexadecimal prefix with no digit")
    add(f, g, "5..toString()", V("5"), clauses=C(NUM), note="a literal with a decimal point and no fraction")
    add(f, g, "1_0n", V(Big(10)), clauses=C(NUM), note="a BigInt literal with a separator")
    add(f, g, "0x1_0n", V(Big(16)), clauses=C(NUM), note="a hexadecimal BigInt literal")
    add(f, g, "07n", SE, clauses=C(NUM), note="a LegacyOctalIntegerLiteral takes no n")
    add(f, g, "08n", SE, clauses=C(NUM), tests=T("test/language/literals/bigint/non-octal-like-invalid-08n.js"),
        note="a NonOctalDecimalIntegerLiteral takes no n")
    add(f, g, "1.5n", SE, clauses=C(NUM), tests=T("test/language/literals/bigint/mv-is-not-integer-dil-dot-dds.js"),
        note="a fractional literal takes no n")
    add(f, g, "1e3n", SE, clauses=C(NUM), tests=T("test/language/literals/bigint/exponent-part.js"),
        note="a literal with an exponent takes no n")


def lexical_templates() -> None:
    f = "lexical"
    raw0 = "((s) => s.raw[0])"
    raw1 = "((s) => s.raw[1])"
    add(f, "ee0020", raw0 + "`\\xg`", V("\\xg"), rows=("EE0020",), clauses=C(TEMPLATE_EE, "sec-static-semantics-trv"),
        note="a NotEscapeSequence in a tagged template's NoSubstitutionTemplate, its raw text kept")
    add(f, "ee0020", "`\\xg`", SE, rows=("EE0020",), clauses=C(TEMPLATE_EE),
        tests=T("test/language/expressions/template-literal/invalid-hexidecimal-character-escape-sequence-truncated-1.js"),
        note="a NotEscapeSequence in an untagged NoSubstitutionTemplate")
    add(f, "ee0022", raw0 + "`\\u{${1}`", V("\\u{"), rows=("EE0022",), clauses=C(TEMPLATE_EE, "sec-static-semantics-trv"),
        note="a NotEscapeSequence in a tagged template's TemplateHead")
    add(f, "ee0022", "`\\u{${1}`", SE, rows=("EE0022",), clauses=C(TEMPLATE_EE),
        tests=T("test/language/expressions/template-literal/invalid-unicode-escape-sequence-8.js"),
        note="a NotEscapeSequence in an untagged TemplateHead")
    add(f, "ee0023", raw1 + "`${1}\\xg`", V("\\xg"), rows=("EE0023",), clauses=C(TEMPLATE_EE, "sec-static-semantics-trv"),
        note="a NotEscapeSequence in a tagged template's TemplateTail")
    add(f, "ee0023", "`${1}\\xg`", SE, rows=("EE0023",), clauses=C(TEMPLATE_EE),
        note="a NotEscapeSequence in an untagged TemplateTail")
    add(f, "ee0024", raw1 + "`${1}\\xg${2}`", V("\\xg"), rows=("EE0024",),
        clauses=C(TEMPLATE_EE, "sec-static-semantics-trv"), note="a NotEscapeSequence in a tagged template's TemplateMiddle")
    add(f, "ee0024", "`${1}\\xg${2}`", SE, rows=("EE0024",), clauses=C(TEMPLATE_EE),
        note="a NotEscapeSequence in an untagged TemplateMiddle")
    g = "templates"
    tv = "sec-static-semantics-tv"
    add(f, g, "`\\u{41}\\x42\\u0043`", V("ABC"), clauses=C(TEMPLATE_LEX, tv), note="escapes cooked")
    add(f, g, "`\\0`", V("\x00"), clauses=C(TEMPLATE_LEX, tv), note="\\0 not followed by a digit is cooked")
    add(f, g, "`\\01`", SE, rows=("EE0020",), clauses=C(TEMPLATE_EE, TEMPLATE_LEX),
        tests=T("test/language/expressions/template-literal/invalid-legacy-octal-escape-sequence.js"),
        note="a legacy octal escape is a NotEscapeSequence in an untagged template")
    add(f, g, "((s) => s[0])`\\01`", V(None), rows=("EE0020",), clauses=C(TEMPLATE_EE, tv),
        note="a tagged template's NotEscapeSequence cooks to undefined")
    add(f, g, "`\\8`", SE, rows=("EE0020",), clauses=C(TEMPLATE_EE, TEMPLATE_LEX),
        note="\\8 is a NotEscapeSequence in an untagged template")
    add(f, g, "`$`", V("$"), clauses=C(TEMPLATE_LEX), note="a dollar sign alone is template text")
    add(f, g, "`\\${1}`", V("${1}"), clauses=C(TEMPLATE_LEX, tv), note="an escaped dollar sign opens no substitution")
    add(f, g, "`${`a${1}b`}`", V("a1b"), clauses=C("sec-template-literals"), note="a template nested in a substitution")
    add(f, g, "`${{ a: 1 }.a}`", V("1"), clauses=C("sec-template-literals"), note="braces inside a substitution")
    add(f, g, "`a\rb`", V("a\nb"), clauses=C(tv), note="a carriage return in a template is cooked as a line feed")
    add(f, g, "`a\r\nb`", V("a\nb"), clauses=C(tv), note="CR LF in a template is cooked as one line feed")
    add(f, g, "String.raw`a\r\nb`", V("a\nb"), clauses=C("sec-static-semantics-trv"),
        note="CR LF in a template's raw text is a line feed")
    add(f, g, "`a\u2028b`", V("a\u2028b"), clauses=C(tv), note="a line separator in a template stays")
    add(f, g, "`a\\\nb`", V("ab"), clauses=C(tv), note="a line continuation in a template is cooked to nothing")
    add(f, g, "String.raw`${1}}}x`", V("1}}x"), corr=("T28",), clauses=C("sec-static-semantics-trv"),
        note="braces after a substitution are raw template text")
    add(f, g, "String.raw`}}a${1}`", V("}}a1"), corr=("T28",), clauses=C("sec-static-semantics-trv"),
        note="braces at a template's start are raw template text")
    # T6, T7, T8: a substitution is tokenized as the source it is.
    add(f, "t6", "`${#!\n1}`", SE, corr=("T6",), clauses=C("sec-hashbang", "sec-template-literals"),
        note="a hashbang only begins a Script, so #! after a substitution's ${ is no token (a correction of "
             "Broiler.VM that step 1 makes)")
    add(f, "t6", "`${-->\n1}`", SE, corr=("T6",), clauses=C("sec-html-like-comments", "sec-template-literals"),
        note="--> after a substitution's ${ is no SingleLineHTMLCloseComment, which a line terminator precedes, so it "
             "is -- and > (a correction of Broiler.VM that step 1 makes)")
    add(f, "t6", "`${\n-->\n1}`", V("1"), rows=("EG0001",), corr=("T6",), clauses=C("sec-html-like-comments"),
        note="--> after a line terminator inside a substitution is a SingleLineHTMLCloseComment", impl=HTML_COMMENTS)
    add(f, "t7", "`${1 <!-- }\n}`", V("1"), rows=("EG0001",), corr=("T7",), clauses=C("sec-html-like-comments"),
        note="<!-- inside a substitution begins a SingleLineHTMLOpenComment, so the } after it is in the comment "
             "(a correction of Broiler.VM that step 1 makes)", impl=HTML_COMMENTS)
    add(f, "t8", "`${typeof /}/}`", V("object"), corr=("T8",), clauses=C(REGEXP, "sec-template-literals"),
        note="a regular expression holding } after typeof inside a substitution (a correction of Broiler.VM that "
             "step 1 makes)")
    add(f, "t8", "`${(function () { return /`/; })().source}`", V("`"), corr=("T8",),
        clauses=C(REGEXP, "sec-template-literals"),
        note="a regular expression holding a backquote after return inside a substitution (a correction of "
             "Broiler.VM that step 1 makes)")


def lexical_lines_and_comments() -> None:
    f = "lexical"
    lt = "sec-line-terminators"
    g = "line-terminators"
    for name, ch in (("a line feed", "\n"), ("a carriage return", "\r"), ("CR LF", "\r\n"),
                     ("a line separator", "\u2028"), ("a paragraph separator", "\u2029")):
        add(f, g, "1" + ch + "+ 2", V(3), clauses=C(lt), note=f"{name} between tokens")
    add(f, g, "var a = 1\u2028a", V(1), clauses=C(lt, ASI), note="a line separator ends a statement by ASI")
    add(f, g, "'a\u2028b'", V("a\u2028b"), clauses=C(STR, lt), note="a line separator in a string literal")
    add(f, g, "'a\u2029b'", V("a\u2029b"), clauses=C(STR, lt), note="a paragraph separator in a string literal")
    add(f, g, "'a\nb'", SE, clauses=C(STR), tests=T("test/language/line-terminators/invalid-string-lf.js"),
        note="a line feed in a string literal")
    add(f, g, "'a\rb'", SE, clauses=C(STR), tests=T("test/language/line-terminators/invalid-string-cr.js"),
        note="a carriage return in a string literal")
    add(f, g, "'a\\\nb'", V("ab"), clauses=C(STR, "sec-static-semantics-sv"), note="a line continuation")
    add(f, g, "'a\\\r\nb'", V("ab"), clauses=C(STR, "sec-static-semantics-sv"), note="a line continuation over CR LF")
    add(f, g, "'a\\\u2028b'", V("ab"), clauses=C(STR, "sec-static-semantics-sv"),
        note="a line continuation over a line separator")
    add(f, g, "1 // c\u2028+ 2", V(3), clauses=C("sec-comments", lt), note="a line separator ends a single-line comment")
    add(f, g, "/a\nb/", SE, clauses=C(REGEXP), tests=T("test/language/line-terminators/invalid-regexp-lf.js"),
        note="a line feed in a regular expression")
    add(f, g, "/a\u2028b/", SE, clauses=C(REGEXP), tests=T("test/language/line-terminators/invalid-regexp-ls.js"),
        note="a line separator in a regular expression")
    add(f, g, "var a = 1 /*\n*/ a", V(1), clauses=C("sec-comments", ASI),
        note="a multi-line comment holding a line terminator is a line terminator for ASI")
    add(f, g, "var a = 6, b = 2, g = 3; a\n/b/g", V(1), corr=("T24a",), clauses=C(ASI, LEX),
        note="after a line terminator a / that continues an expression is division")
    g = "comments"
    add(f, g, "/**/1", V(1), clauses=C("sec-comments"), note="an empty multi-line comment")
    add(f, g, "/* * / */ 2", V(2), clauses=C("sec-comments"), note="* and / apart inside a comment")
    add(f, g, "/* /* */ 3", V(3), clauses=C("sec-comments"), note="multi-line comments do not nest")
    add(f, g, "4 // c", V(4), clauses=C("sec-comments"), note="a single-line comment at the end of the source text")
    # The hashbang.
    g = "hashbang"
    hb = "sec-hashbang"
    add(f, g, "#! a comment", V(None), clauses=C(hb), tests=T("test/language/comments/hashbang/not-empty.js"),
        note="a hashbang alone is a Script")
    add(f, g, "#!x\n1", V(1), clauses=C(hb, "sec-performeval"), tests=T("test/language/comments/hashbang/eval.js"),
        note="a hashbang begins eval code")
    add(f, g, " #!x\n1", SE, clauses=C(hb), tests=T("test/language/comments/hashbang/preceding-whitespace.js"),
        note="a hashbang after white space")
    add(f, g, "1\n#!x", SE, clauses=C(hb), tests=T("test/language/comments/hashbang/statement-block.js"),
        note="a hashbang after a statement")
    add(f, g, "\\u0023!x", SE, clauses=C(hb), tests=T("test/language/comments/hashbang/escaped-hash-u0023.js"),
        note="an escaped number sign begins no hashbang")
    add(f, g, "#!x\u2028 1", V(1), clauses=C(hb, lt),
        tests=T("test/language/comments/hashbang/line-terminator-line-separator.js"),
        note="a line separator ends a hashbang")
    add(f, g, "#!x\n1", V(1), corr=("N26",), clauses=C(hb, "sec-performeval"),
        tests=T("test/language/comments/hashbang/eval.js"), op="PerformEval",
        script="\"use strict\"; eval(" + js("#!x\n1") + ")",
        note="a hashbang begins the eval code of a strict caller's direct eval, whose strictness changes no token")
    # HTML-like comments (B.1.1).
    g = "html-comments"
    hc = "sec-html-like-comments"
    add(f, g, "var a = 1; a <!-- a = 2", V(1), rows=("EG0001",), clauses=C(hc),
        tests=T("test/annexB/language/comments/single-line-html-open.js"),
        note="<!-- begins a SingleLineHTMLOpenComment", impl=HTML_COMMENTS)
    add(f, g, "--> c\n1", V(1), rows=("EG0001",), clauses=C(hc),
        tests=T("test/annexB/language/comments/single-line-html-close-first-line-1.js"),
        note="--> at the start of the source text begins an HTMLCloseComment", impl=HTML_COMMENTS)
    add(f, g, "var a = 1\n--> c\na", V(1), rows=("EG0001",), clauses=C(hc),
        tests=T("test/annexB/language/comments/single-line-html-close.js"),
        note="--> after a line terminator begins a SingleLineHTMLCloseComment", impl=HTML_COMMENTS)
    add(f, g, "/*\n*/ --> c\n1", V(1), rows=("EG0001",), clauses=C(hc),
        tests=T("test/annexB/language/comments/multi-line-html-close.js"),
        note="--> after a multi-line comment holding a line terminator", impl=HTML_COMMENTS)
    add(f, g, "/* */ --> c\n1", V(1), rows=("EG0001",), clauses=C(hc),
        tests=T("test/annexB/language/comments/single-line-html-close-first-line-2.js"),
        note="--> after a SingleLineDelimitedComment at the start of the source text", impl=HTML_COMMENTS)
    add(f, g, "var x = 2; x --> 1", V(True), clauses=C(hc, "sec-postfix-decrement-operator"),
        note="--> after a token on the same line is -- and >")
    add(f, g, "1 --> 0", SE, clauses=C(hc, "sec-update-expressions-static-semantics-early-errors"),
        note="--> after a literal on the same line is a decrement of the literal")


def lexical_regexp() -> None:
    f, g = "lexical", "regexp-delimiting"
    rx = C(REGEXP, LEX, "sec-lexical-and-regexp-grammars")
    cases = [
        ("(/a/).source", "a", "after ( a / begins a regular expression"),
        ("[/a/][0].source", "a", "after ["),
        ("({ x: /a/ }).x.source", "a", "after : in an object literal"),
        ("[1, /a/][1].source", "a", "after ,"),
        ("; /a/.source", "a", "after ;"),
        ("var r = /a/; r.source", "a", "after ="),
        ("!/a/", False, "after !"),
        ("true ? /a/.source : 0", "a", "after ?"),
        ("false ? 0 : /a/.source", "a", "after : of a conditional"),
        ("1 && /a/.source", "a", "after &&"),
        ("0 || /a/.source", "a", "after ||"),
        ("null ?? /a/.source", "a", "after ??"),
        ("(() => /a/)().source", "a", "after =>"),
        ("\"\" + /a/", "/a/", "after +"),
        ("-/1/.source", -1, "after -"),
        ("2 * /3/.source", 6, "after *"),
        ("7 % /4/.source", 3, "after %"),
        ("1 | /2/.source", 3, "after |"),
        ("-1 < /a/.lastIndex", True, "after <"),
        ("1 > /a/.lastIndex", True, "after >"),
        ("\"a\" === /a/.source", True, "after ==="),
        ("[.../ab/.source].length", 2, "after ..."),
        ("typeof /a/", "object", "after typeof"),
        ("void /a/", None, "after void"),
        ("\"source\" in /a/", True, "after in"),
        ("({}) instanceof /a/.constructor", False, "after instanceof"),
        ("new /a/.constructor(\"b\").source", "b", "after new"),
        ("(function () { return /a/; })().source", "a", "after return"),
        ("try { throw /a/; } catch (e) { e.source }", "a", "after throw"),
        ("switch (1) { case /a/.lastIndex + 1: 2 }", 2, "after case"),
        ("{ /a/.source }", "a", "after { of a block"),
        ("{} /a/.source", "a", "after } of a block"),
        ("function f() {} /a/.source", "a", "after } of a function declaration"),
        ("if (true) /a/.source", "a", "after ) of an if statement's head"),
        ("if (false) ; else /a/.source", "a", "after else"),
        ("function* g() { yield /a/; } g().next().value.source", "a", "after yield in a generator"),
        ("var a = 6, b = 2, g = 3; a /b/g", 1, "after an identifier a / is division"),
        ("6 /2/ 3", 1, "after a numeric literal a / is division"),
        ("\"6\" /2/ 3", 1, "after a string literal a / is division"),
        ("`6` /2/ 3", 1, "after a template a / is division"),
        ("(6) /2/ 3", 1, "after ) of a parenthesised expression a / is division"),
        ("[6][0] /2/ 3", 1, "after ] a / is division"),
        ("var a = 6, b = 2; a++ /b/ 3", 1, "after a postfix ++ a / is division"),
        ("/[/]/.source", "[/]", "a / inside a class does not end the literal"),
        ("/a\\/b/.source", "a\\/b", "an escaped / does not end the literal"),
    ]
    for text, value, note in cases:
        add(f, g, text, V(value), clauses=rx, note=note)
    corrections = [
        ("var b = 2, g = 3; var x = function () {} /b/g; x", NAN, "T24b",
         "after } of a function expression a / is division"),
        ("var b = 2, g = 3; var x = {} /b/g; x", NAN, "T24b", "after } of an object literal a / is division"),
        ("var r = \"\"; for (var c of /ab/.source) r += c; r", "ab", "T24c",
         "after of in a for-of head a / begins a regular expression"),
        ("class C extends /a/.constructor {} typeof C", "function", "T24c",
         "after extends a / begins a regular expression"),
        ("async function f() { for await (var x of []) /a/g; } typeof f", "function", "T24d",
         "after a for-await head a / begins a regular expression (a correction of both engines; Broiler.VM's at "
         "step 1)"),
        ("++/x/.lastIndex", 1, "T24e",
         "after a prefix ++ a / begins a regular expression (a correction of both engines; Broiler.VM's at step 1)"),
        ("var await = 6, b = 2, g = 3; await /b/g", 1, "T24f",
         "after await used as an identifier in a Script a / is division (a correction of both engines; "
         "Broiler.VM's at step 1)"),
    ]
    for text, value, row, note in corrections:
        add(f, g, text, V(value), corr=(row,), clauses=rx, note=note)


def lexical_asi() -> None:
    f, g = "lexical", "asi"
    add(f, g, "var a = 1\nvar b = 2\na + b", V(3), clauses=C(ASI), note="a semicolon inserted at each line break")
    add(f, g, "(function () { return\n1; })()", V(None), clauses=C(ASI),
        tests=T("test/language/asi/S7.9_A3.js"), note="a line terminator after return ends the statement")
    add(f, g, "var a = 1, b = 2\na\n++b\nb", V(3), clauses=C(ASI), tests=T("test/language/asi/S7.9_A5.2_T1.js"),
        note="++ after a line terminator is prefix")
    add(f, g, "throw\n1", SE, clauses=C(ASI), tests=T("test/language/asi/S7.9_A4.js"),
        note="a line terminator after throw")
    add(f, g, "var f = function () { return 2; }\nvar a = f\n(1)\na", V(2), clauses=C(ASI),
        note="no semicolon is inserted before a ( that continues a call")
    add(f, g, "{ 1 } 2", V(2), clauses=C(ASI), note="a semicolon inserted before }")
    add(f, g, "do ; while (false) 3", V(3), clauses=C(ASI), tests=T("test/language/asi/do-while-same-line.js"),
        note="a semicolon inserted after a do-while statement's )")
    add(f, g, "do {}; while (false)", SE, corr=("P10",), clauses=C("sec-do-while-statement"),
        tests=T("test/language/asi/S7.9_A9_T8.js"), note="a statement between a do-while's body and while")
    add(f, g, "for (var i = 0\ni < 1; i++) ;", SE, clauses=C(ASI), tests=T("test/language/asi/S7.9_A6.2_T1.js"),
        note="no semicolon is inserted in a for statement's head")
    add(f, g, "var f = a\n=> 1", SE, rows=("EE0025",), clauses=C(ASI, "sec-arrow-function-definitions"),
        tests=T("test/language/expressions/arrow-function/syntax/early-errors/asi-restriction-invalid.js"),
        note="a line terminator before =>")
    add(f, g, "var x = 1, y = 1\nx\n++\ny\ny", V(2), clauses=C(ASI), note="++ between two line terminators is prefix")
    add(f, g, "var async = 1\nasync\nfunction f() {}\nasync", V(1), clauses=C(ASI, "sec-async-function-definitions"),
        note="async followed by a line terminator is an identifier")
    add(f, g, "({ async\nm() {} })", SE, corr=("P09",), clauses=C("sec-async-function-definitions"),
        tests=T("test/language/expressions/object/method-definition/async-meth-escaped-async.js"),
        note="a line terminator after async in a method")


def expressions() -> None:
    f = "expressions"
    I = IDS_ID
    add(f, "ee0005", "var arguments = 2; arguments", V(2), rows=("EE0005",), clauses=C(I),
        note="arguments as a binding in sloppy code")
    add(f, "ee0005", "\"use strict\"; var a, eval;", SE, rows=("EE0005",), clauses=C(I),
        tests=T("test/language/statements/variable/eval-strict-list-final.js"), note="eval as a binding in strict code")
    add(f, "ee0006", "var yield = 1; yield", V(1), rows=("EE0006",), clauses=C(I), note="yield as a binding in sloppy code")
    add(f, "ee0006", "\"use strict\"; var yield;", SE, rows=("EE0006",), clauses=C(I),
        tests=T("test/language/future-reserved-words/yield-strict.js"), note="yield as a binding in strict code")
    add(f, "ee0007", "var await = 1; await", V(1), rows=("EE0007",), clauses=C(I),
        note="await as a binding in a Script")
    add(f, "ee0008", "function f() { var yield = 1; return yield; } f()", V(1), rows=("EE0008",), clauses=C(I),
        note="yield as a binding outside a generator")
    add(f, "ee0008", "function* g() { var yield; }", SE, rows=("EE0008",), clauses=C(I),
        tests=T("test/language/statements/generators/yield-as-binding-identifier.js"),
        note="yield as a binding in a generator")
    add(f, "ee0009", "function f() { var await = 3; return await; } f()", V(3), rows=("EE0009",), clauses=C(I),
        note="await as a binding outside an async function")
    add(f, "ee0009", "async function f() { var await; }", SE, rows=("EE0009",), clauses=C(I),
        tests=T("test/language/statements/async-function/await-as-binding-identifier.js"),
        note="await as a binding in an async function")
    add(f, "ee0010", "var yi\\u0065ld = 4; yield", V(4), rows=("EE0010",), clauses=C(I),
        note="an escaped yield as a binding in sloppy code outside a generator")
    add(f, "ee0010", "function* g() { var yi\\u0065ld; }", SE, rows=("EE0010",), clauses=C(I),
        tests=T("test/language/statements/generators/yield-as-binding-identifier-escaped.js"),
        note="an escaped yield as a binding in a generator")
    add(f, "ee0011", "var aw\\u0061it = 5; await", V(5), rows=("EE0011",), clauses=C(I),
        note="an escaped await as a binding in a Script")
    add(f, "ee0011", "async function f() { var aw\\u0061it; }", SE, rows=("EE0011",), clauses=C(I),
        tests=T("test/language/statements/async-function/await-as-binding-identifier-escaped.js"),
        note="an escaped await as a binding in an async function")
    add(f, "ee0012", "var implements = 6; implements", V(6), rows=("EE0012",), clauses=C(I),
        note="implements as a binding in sloppy code")
    add(f, "ee0012", "\"use strict\"; var implements;", SE, rows=("EE0012",), clauses=C(I),
        tests=T("test/language/future-reserved-words/implements-strict.js"), note="implements as a binding in strict code")
    add(f, "ee0012", "\"use strict\"; var l\\u0065t;", SE, rows=("EE0012",), corr=("T23",), clauses=C(I),
        tests=T("test/language/future-reserved-words/let-strict-escaped.js"), note="an escaped let as a binding in strict code")
    add(f, "ee0013", "var \\u0061wait = 7; \\u0061wait", V(7), rows=("EE0013",), clauses=C(I),
        note="an escaped await as an identifier in a Script")
    add(f, "ee0014", "({ v\\u0061r: 8 }).var", V(8), rows=("EE0014",), clauses=C(I),
        note="an escaped reserved word as a property name")
    add(f, "ee0014", "var v\\u0061r = 1;", SE, rows=("EE0014",), clauses=C(I),
        tests=T("test/language/identifiers/val-var-via-escape-hex.js"), note="an escaped reserved word as a binding")
    O = "sec-object-initializer-static-semantics-early-errors"
    add(f, "ee0016", "class C { #m() { return 1; } static t(o) { return o.#m(); } } C.t(new C)", V(1),
        rows=("EE0016",), clauses=C(O, "sec-class-definitions"), note="a private method in a class")
    add(f, "ee0016", "({ #m() {} });", SE, rows=("EE0016",), clauses=C(O),
        tests=T("test/language/expressions/object/method-definition/private-name-early-error-method.js"),
        note="a private method in an object literal")
    add(f, "ee0017", "var a; ({ a = 1 } = {}); a", V(1), rows=("EE0017",), clauses=C(O),
        note="a CoverInitializedName in a pattern")
    add(f, "ee0017", "({ a = 1 });", SE, rows=("EE0017",), clauses=C(O),
        tests=T("test/language/expressions/object/cover-initialized-name.js"),
        note="a CoverInitializedName in an object literal")
    G = "sec-grouping-operator-static-semantics-early-errors"
    add(f, "ee0025", "(1, 2)", V(2), rows=("EE0025",), clauses=C(G), note="a parenthesised expression")
    add(f, "ee0025", "(...a);", SE, rows=("EE0025",), clauses=C(G),
        note="a rest element in a cover that is no arrow's parameters")
    L = "sec-left-hand-side-expressions-static-semantics-early-errors"
    add(f, "ee0026", "var o = { f: () => 2 }; o?.f()", V(2), rows=("EE0026",), clauses=C(L), note="an optional call")
    add(f, "ee0026", "var a; a?.b`c`;", SE, rows=("EE0026",), clauses=C(L),
        tests=T("test/language/expressions/optional-chaining/early-errors-tail-position-op-template-string.js"),
        note="a template after an optional chain")
    add(f, "ee0027", "import.meta;", SE, rows=("EE0027",), clauses=C(L),
        tests=T("test/language/expressions/import.meta/syntax/goal-script.js"), note="import.meta in a Script")
    U = "sec-update-expressions-static-semantics-early-errors"
    add(f, "ee0028", "var x = 1; x++; x", V(2), rows=("EE0028",), clauses=C(U), note="a postfix increment of a name")
    add(f, "ee0028", "1++;", SE, rows=("EE0028",), clauses=C(U),
        tests=T("test/language/expressions/postfix-increment/this.js"), note="a postfix increment of a literal")
    add(f, "ee0029", "var y = 1; --y", V(0), rows=("EE0029",), clauses=C(U), note="a prefix decrement of a name")
    add(f, "ee0029", "++this;", SE, rows=("EE0029",), clauses=C(U),
        tests=T("test/language/expressions/prefix-decrement/this.js"), note="a prefix increment of this")
    D = "sec-delete-operator-static-semantics-early-errors"
    add(f, "ee0030", "delete x", V(True), rows=("EE0030",), clauses=C(D), note="delete of a name in sloppy code")
    add(f, "ee0030", "\"use strict\"; delete x;", SE, rows=("EE0030",), clauses=C(D),
        tests=T("test/language/expressions/delete/identifier-strict.js"), note="delete of a name in strict code")
    add(f, "ee0030", "class C { #p; m() { delete this.#p; } }", SE, rows=("EE0030",), clauses=C(D),
        tests=T("test/language/expressions/class/elements/syntax/early-errors/delete/"
                "field-delete-err-delete-member-expression-privatename.js"),
        note="delete of a private reference")
    add(f, "ee0031", "delete ((x))", V(True), rows=("EE0031",), clauses=C(D),
        note="delete of a parenthesised name in sloppy code")
    add(f, "ee0031", "\"use strict\"; delete ((x));", SE, rows=("EE0031",), clauses=C(D),
        tests=T("test/language/expressions/delete/identifier-strict-recursive.js"),
        note="delete of a parenthesised name in strict code")
    A = "sec-assignment-operators-static-semantics-early-errors"
    add(f, "ee0032", "var a, b; [a, b] = [1, 2]; b", V(2), rows=("EE0032",), clauses=C(A),
        note="an array literal that covers an AssignmentPattern")
    add(f, "ee0032", "var a, b; [...a, b] = [];", SE, rows=("EE0032",), clauses=C(A),
        tests=T("test/language/expressions/assignment/dstr/array-rest-before-element.js"),
        note="an array literal with a rest element before the last, which covers no AssignmentPattern")
    add(f, "ee0033", "var a; a = 3", V(3), rows=("EE0033",), clauses=C(A), note="an assignment to a name")
    add(f, "ee0033", "1 = 2;", SE, rows=("EE0033",), clauses=C(A),
        tests=T("test/language/expressions/assignment/target-number.js"), note="an assignment to a literal")
    add(f, "ee0034", "var a = 1; a += 2", V(3), rows=("EE0034",), clauses=C(A), note="a compound assignment to a name")
    add(f, "ee0034", "1 += 2;", SE, rows=("EE0034",), clauses=C(A),
        tests=T("test/language/expressions/compound-assignment/add-non-simple.js"),
        note="a compound assignment to a literal")
    add(f, "ee0035", "var a = null; a ??= 4", V(4), rows=("EE0035",), clauses=C(A), note="a logical assignment to a name")
    add(f, "ee0035", "1 &&= 2;", SE, rows=("EE0035",), clauses=C(A),
        tests=T("test/language/expressions/logical-assignment/lgcl-and-non-simple.js"),
        note="a logical assignment to a literal")
    DA = "sec-destructuring-assignment-static-semantics-early-errors"
    add(f, "ee0036", "var x; ({ x } = { x: 5 }); x", V(5), rows=("EE0036",), clauses=C(DA),
        note="a shorthand property of a simple name")
    add(f, "ee0036", "\"use strict\"; ({ eval } = {});", SE, rows=("EE0036",), clauses=C(DA),
        tests=T("test/language/expressions/assignment/dstr/obj-id-simple-strict.js"),
        note="a shorthand property eval in strict code, whose target is not simple")
    add(f, "ee0037", "var a; ({ ...a } = { b: 1 }); a.b", V(1), rows=("EE0037",), clauses=C(DA),
        note="an AssignmentRestProperty of a name")
    add(f, "ee0037", "({ ...{ a } } = {});", SE, rows=("EE0037",), corr=("P31",), clauses=C(DA),
        note="an AssignmentRestProperty whose target is an object literal (a correction of both engines; "
             "Broiler.VM's at step 1)")
    add(f, "ee0037", "var a; ({ ...[a] } = {});", SE, rows=("EE0037",), corr=("P31",), clauses=C(DA),
        note="an AssignmentRestProperty whose target is an array literal (a correction of both engines; "
             "Broiler.VM's at step 1)")
    add(f, "ee0037", "var a; [...[a]] = [7]; a", V(7), rows=("EE0037",), corr=("P31",), clauses=C(DA),
        note="an AssignmentRestElement may be an array literal")
    add(f, "ee0038", "var a; [{ a }] = [{ a: 6 }]; a", V(6), rows=("EE0038",), clauses=C(DA),
        note="a nested object literal that covers an AssignmentPattern")
    add(f, "ee0038", "[{ get x() {} }] = [];", SE, rows=("EE0038",), clauses=C(DA),
        tests=T("test/language/expressions/assignment/dstr/array-elem-nested-obj-invalid.js"),
        note="a nested object literal with a getter, which covers no AssignmentPattern")
    add(f, "ee0039", "var o = {}; [o.p] = [7]; o.p", V(7), rows=("EE0039",), clauses=C(DA),
        note="a member expression as a destructuring target")
    add(f, "ee0039", "[1] = [];", SE, rows=("EE0039",), clauses=C(DA), note="a literal as a destructuring target")
    add(f, "ee0039", "\"use strict\"; [arguments] = [];", SE, rows=("EE0039",), clauses=C(DA, STRICT),
        tests=T("test/language/expressions/assignment/dstr/array-elem-target-simple-strict.js"),
        note="arguments as a destructuring target in strict code")
    add(f, "p15b", "var b; ({ a: (b = 7) } = {});", SE, rows=("EE0039",), corr=("P15b",), clauses=C(DA),
        note="a parenthesised assignment as a destructuring target is not simple (a correction of Broiler.VM that "
             "step 1 makes)")
    add(f, "p15b", "var b; ({ a: ([b]) } = { a: [1] });", SE, rows=("EE0039",), corr=("P15b",), clauses=C(DA),
        note="a parenthesised array literal as a destructuring target is not simple (a correction of Broiler.VM that "
             "step 1 makes)")
    add(f, "p15b", "var b; ({ a: (b) } = { a: 8 }); b", V(8), rows=("EE0039",), corr=("P15b",), clauses=C(DA),
        note="a parenthesised name as a destructuring target is simple")


def statements() -> None:
    f = "statements"
    B = "sec-block-static-semantics-early-errors"
    add(f, "ee0040", "var r; { let a = 1; let b = 2; r = a + b; } r", V(3), rows=("EE0040",), clauses=C(B),
        note="two lexical names in a block")
    add(f, "ee0040", "{ let a; let a; }", SE, rows=("EE0040",), clauses=C(B),
        tests=T("test/language/block-scope/syntax/redeclaration/let-name-redeclaration-attempt-with-let.js"),
        note="one lexical name declared twice in a block")
    add(f, "ee0041", "var r; { let a = 1; var b = 2; r = a + b; } r", V(3), rows=("EE0041",), clauses=C(B),
        note="a lexical and a var name in a block")
    add(f, "ee0041", "{ let a; var a; }", SE, rows=("EE0041",), clauses=C(B),
        tests=T("test/language/block-scope/syntax/redeclaration/var-redeclaration-attempt-after-let.js"),
        note="one name declared lexically and with var in a block")
    LC = "sec-let-and-const-declarations-static-semantics-early-errors"
    add(f, "ee0042", "var let = 1; let", V(1), rows=("EE0042",), clauses=C(LC), note="let as a var binding in sloppy code")
    add(f, "ee0042", "let let = 1;", SE, rows=("EE0042",), clauses=C(LC),
        tests=T("test/language/statements/let/syntax/let-let-declaration-split-across-two-lines.js"),
        note="let bound by a let declaration")
    add(f, "ee0043", "let a = 1, b = 2; a + b", V(3), rows=("EE0043",), clauses=C(LC), note="two names in one BindingList")
    add(f, "ee0043", "let a, a;", SE, rows=("EE0043",), clauses=C(LC), note="one name twice in one BindingList")
    add(f, "ee0044", "const c = 4; c", V(4), rows=("EE0044",), clauses=C(LC), note="a const with an initialiser")
    add(f, "ee0044", "const c;", SE, rows=("EE0044",), clauses=C(LC),
        tests=T("test/language/statements/const/syntax/block-scope-syntax-const-declarations-without-initialiser.js"),
        note="a const with no initialiser")
    IF = "sec-if-statement-static-semantics-early-errors"
    add(f, "ee0045", "var r = 0; if (true) L: r = 1; else ; r", V(1), rows=("EE0045",), clauses=C(IF),
        note="a labelled statement as an if's first clause")
    add(f, "ee0045", "if (true) L: function f() {} else ;", SE, rows=("EE0045",), clauses=C(IF),
        tests=T("test/language/statements/if/labelled-fn-stmt-first.js"),
        note="a labelled function as an if's first clause")
    add(f, "ee0046", "var r = 0; if (false) ; else L: r = 2; r", V(2), rows=("EE0046",), clauses=C(IF),
        note="a labelled statement as an if's second clause")
    add(f, "ee0046", "if (false) ; else L: function f() {}", SE, rows=("EE0046",), clauses=C(IF),
        tests=T("test/language/statements/if/labelled-fn-stmt-second.js"),
        note="a labelled function as an if's second clause")
    add(f, "ee0047", "var r = 0; if (true) L: r = 3; r", V(3), rows=("EE0047",), clauses=C(IF),
        note="a labelled statement as an if's clause")
    add(f, "ee0047", "if (true) L: function f() {}", SE, rows=("EE0047",), clauses=C(IF),
        tests=T("test/language/statements/if/labelled-fn-stmt-lone.js"), note="a labelled function as an if's clause")
    add(f, "ee0048", "var r = 0; do L: r++; while (false); r", V(1), rows=("EE0048",),
        clauses=C("sec-do-while-statement-static-semantics-early-errors"), note="a labelled statement as a do's body")
    add(f, "ee0048", "do L: function f() {} while (false);", SE, rows=("EE0048",),
        clauses=C("sec-do-while-statement-static-semantics-early-errors"),
        tests=T("test/language/statements/do-while/labelled-fn-stmt.js"), note="a labelled function as a do's body")
    add(f, "ee0049", "var r = 0; while (r < 1) L: r++; r", V(1), rows=("EE0049",),
        clauses=C("sec-while-statement-static-semantics-early-errors"), note="a labelled statement as a while's body")
    add(f, "ee0049", "while (false) L: function f() {}", SE, rows=("EE0049",),
        clauses=C("sec-while-statement-static-semantics-early-errors"),
        tests=T("test/language/statements/while/labelled-fn-stmt.js"), note="a labelled function as a while's body")
    FOR = "sec-for-statement-static-semantics-early-errors"
    add(f, "ee0050", "var r = 0; for (; r < 1;) L: r++; r", V(1), rows=("EE0050",), clauses=C(FOR),
        note="a labelled statement as a for's body")
    add(f, "ee0050", "for (; false;) L: function f() {}", SE, rows=("EE0050",), clauses=C(FOR),
        tests=T("test/language/statements/for/labelled-fn-stmt-expr.js"), note="a labelled function as a for's body")
    add(f, "ee0051", "var r = 0; for (let i = 0; i < 2; i++) { var j = i; r += j; } r", V(1), rows=("EE0051",),
        clauses=C(FOR), note="a var in a for statement whose head declares other names")
    add(f, "ee0051", "for (let i = 0; ;) { var i; }", SE, rows=("EE0051",), clauses=C(FOR),
        tests=T("test/language/statements/for/head-let-bound-names-in-stmt.js"),
        note="a var of a name the for statement's head declares")
    FIO = "sec-for-in-and-for-of-statements-static-semantics-early-errors"
    add(f, "ee0052", "var r = 0; for (var x in { a: 1 }) L: r++; r", V(1), rows=("EE0052",), clauses=C(FIO),
        note="a labelled statement as a for-in's body")
    add(f, "ee0052", "for (var x in {}) L: function f() {}", SE, rows=("EE0052",), clauses=C(FIO),
        tests=T("test/language/statements/for-in/labelled-fn-stmt-var.js"), note="a labelled function as a for-in's body")
    add(f, "ee0053", "var a; for ([a] of [[5]]) ; a", V(5), rows=("EE0053",), clauses=C(FIO),
        note="an array literal that covers an AssignmentPattern in a for-of head")
    add(f, "ee0053", "var a, b; for ([...a, b] of []) ;", SE, rows=("EE0053",), clauses=C(FIO),
        tests=T("test/language/statements/for-of/dstr/array-rest-before-element.js"),
        note="an array literal that covers no AssignmentPattern in a for-of head")
    add(f, "ee0055", "var r = 0; for (let x of [1, 2]) r += x; r", V(3), rows=("EE0055",), clauses=C(FIO),
        note="a ForDeclaration binding x")
    add(f, "ee0055", "for (let let of []) ;", SE, rows=("EE0055",), clauses=C(FIO),
        tests=T("test/language/statements/for-of/head-let-bound-names-let.js"), note="a ForDeclaration binding let")
    add(f, "ee0056", "var r; for (let x of [1]) { var y = x; r = y; } r", V(1), rows=("EE0056",), clauses=C(FIO),
        note="a var of another name in a for-of's body")
    add(f, "ee0056", "for (let x of []) { var x; }", SE, rows=("EE0056",), clauses=C(FIO),
        tests=T("test/language/statements/for-of/head-let-bound-names-in-stmt.js"),
        note="a var of the name the ForDeclaration binds")
    add(f, "ee0057", "var r; for (const [a, b] of [[1, 2]]) r = a + b; r", V(3), rows=("EE0057",), clauses=C(FIO),
        note="a ForDeclaration binding two names")
    add(f, "ee0057", "for (const [a, a] of []) ;", SE, rows=("EE0057",), clauses=C(FIO),
        tests=T("test/language/statements/for-of/head-const-bound-names-dup.js"),
        note="a ForDeclaration binding one name twice")
    W = "sec-with-statement-static-semantics-early-errors"
    add(f, "ee0060", "var o = { p: 7 }; var r; with (o) r = p; r", V(7), rows=("EE0060",), clauses=C(W),
        note="with in sloppy code")
    add(f, "ee0060", "\"use strict\"; with ({}) ;", SE, rows=("EE0060",), clauses=C(W),
        tests=T("test/language/statements/with/strict-script.js"), note="with in strict code")
    add(f, "ee0061", "var r = 0; with ({}) L: r = 1; r", V(1), rows=("EE0061",), clauses=C(W),
        note="a labelled statement as a with's body")
    add(f, "ee0061", "with ({}) L: function f() {}", SE, rows=("EE0061",), clauses=C(W),
        tests=T("test/language/statements/with/labelled-fn-stmt.js"), note="a labelled function as a with's body")
    SW = "sec-switch-statement-static-semantics-early-errors"
    add(f, "ee0062", "var r; switch (1) { case 1: let a = 1; r = a; } r", V(1), rows=("EE0062",), clauses=C(SW),
        note="a lexical name in a case block")
    add(f, "ee0062", "switch (0) { case 1: let a; case 2: let a; }", SE, rows=("EE0062",), clauses=C(SW),
        tests=T("test/language/statements/switch/syntax/redeclaration/let-name-redeclaration-attempt-with-let.js"),
        note="one lexical name declared twice in a case block")
    add(f, "ee0063", "var r; switch (1) { case 1: let a = 2; var b = a; r = b; } r", V(2), rows=("EE0063",),
        clauses=C(SW), note="a lexical and a var name in a case block")
    add(f, "ee0063", "switch (0) { case 1: let a; default: var a; }", SE, rows=("EE0063",), clauses=C(SW),
        tests=T("test/language/statements/switch/syntax/redeclaration/var-name-redeclaration-attempt-with-let.js"),
        note="one name declared lexically and with var in a case block")
    LB = "sec-labelled-statements-static-semantics-early-errors"
    add(f, "ee0064", "\"use strict\"; L: { function f() {} } 2", V(2), rows=("EE0064",), clauses=C(LB),
        note="a label on a block holding a function declaration in strict code")
    add(f, "ee0064", "\"use strict\"; L: function f() {}", SE, rows=("EE0064",), clauses=C(LB),
        tests=T("test/language/statements/labeled/decl-fun-strict.js"),
        note="a labelled function declaration in strict code")
    TRY = "sec-try-statement-static-semantics-early-errors"
    add(f, "ee0065", "var r; try { throw [1, 2]; } catch ([a, b]) { r = a + b; } r", V(3), rows=("EE0065",),
        clauses=C(TRY), note="a catch parameter pattern binding two names")
    add(f, "ee0065", "try {} catch ([a, a]) {}", SE, rows=("EE0065",), clauses=C(TRY),
        tests=T("test/language/statements/try/early-catch-duplicates.js"),
        note="a catch parameter pattern binding one name twice")
    add(f, "ee0066", "var r; try { throw 1; } catch (e) { { let e = 2; r = e; } } r", V(2), rows=("EE0066",),
        clauses=C(TRY), note="a lexical declaration of the catch parameter's name in a nested block")
    add(f, "ee0066", "try {} catch (e) { let e; }", SE, rows=("EE0066",), clauses=C(TRY),
        tests=T("test/language/statements/try/early-catch-lex.js"),
        note="a lexical declaration of the catch parameter's name in the catch block")
    add(f, "ee0067", "var r; try { throw 1; } catch (e) { var f = e; r = f; } r", V(1), rows=("EE0067",),
        clauses=C(TRY), note="a var of another name in a catch block")
    add(f, "ee0067", "try {} catch ([e]) { var e; }", SE, rows=("EE0067",), clauses=C(TRY),
        note="a var of a name a catch parameter pattern binds, which Annex B does not admit")
    # Explicit resource management: ECMA-262 main's rules, not the pinned edition's (catalogue section 1).
    U = "test/language/statements/using/syntax/"
    AU = "test/language/statements/await-using/syntax/"
    erm = ("ECMA-262 main at 4feb0272 merged the rule after the pinned edition, so the vector cites the test262 tests "
           "that assert it")
    add(f, "ep0001", "var r; { using x = null; r = 1; } r", V(1), rows=("EP0001",),
        tests=T(U + "using-allows-bindingidentifier.js"), note="a using declaration binding x; " + erm)
    add(f, "ep0002", "var r; { using x = null, y = null; r = 2; } r", V(2), rows=("EP0002",),
        tests=T(U + "using-allows-multiple-bindings.js"), note="a using declaration binding two names; " + erm)
    add(f, "ep0003", "async function f() { await using x = null; } typeof f", V("function"), rows=("EP0003",),
        tests=T(AU + "await-using-allows-bindingidentifier.js"),
        note="await using covers an AwaitUsingDeclarationHead; " + erm)
    add(f, "ep0004", "async function f() { { await using y = null; } } typeof f", V("function"), rows=("EP0004",),
        tests=T(AU + "await-using-allows-bindingidentifier.js"), note="an await using declaration binding y; " + erm)
    add(f, "ep0005", "async function f() { await using x = null, y = null; } typeof f", V("function"),
        rows=("EP0005",), tests=T(AU + "await-using-allows-multiple-bindings.js"),
        note="an await using declaration binding two names; " + erm)
    add(f, "ep0006", "switch (0) { case 0: using x = null; }", SE, rows=("EP0006",),
        tests=T(U + "using-invalid-switchstatement-caseclause.js", U + "with-initializer-case-expression-statement-list.js"),
        note="a using declaration directly in a case clause; " + erm)
    add(f, "ep0007", "switch (0) { default: using x = null; }", SE, rows=("EP0007",),
        tests=T(U + "using-invalid-switchstatement-defaultclause.js", U + "with-initializer-default-statement-list.js"),
        note="a using declaration directly in a default clause; " + erm)


def functions_and_classes() -> None:
    f = "functions-and-classes"
    P = "sec-parameter-lists-static-semantics-early-errors"
    FN = "sec-function-definitions-static-semantics-early-errors"
    AR = "sec-arrow-function-definitions-static-semantics-early-errors"
    MD = "sec-method-definitions-static-semantics-early-errors"
    GN = "sec-generator-function-definitions-static-semantics-early-errors"
    AG = "sec-async-generator-function-definitions-static-semantics-early-errors"
    CL = "sec-class-definitions-static-semantics-early-errors"
    AF = "sec-async-function-definitions-static-semantics-early-errors"
    AA = "sec-async-arrow-function-definitions-static-semantics-early-errors"

    def pair(row: str, clause: str, accepted: str, value, refused: str, tests: Sequence[str], acc_note: str,
             ref_note: str, corr: Sequence[str] = ()) -> None:
        add(f, row.lower(), accepted, V(value), rows=(row,), clauses=C(clause), note=acc_note)
        add(f, row.lower(), refused, SE, rows=(row,), corr=corr, clauses=C(clause), tests=T(*tests), note=ref_note)

    pair("EE0068", P, "((a, b) => a + b)(1, 2)", 3, "(a, a) => 1;",
         ["test/language/expressions/arrow-function/params-duplicate.js"],
         "an arrow's two distinct parameters", "an arrow's parameter bound twice")
    pair("EE0069", P, "function f(a, b = 2) { return a + b; } f(1)", 3, "function f(a, a = 1) {}",
         ["test/language/statements/function/dflt-params-duplicates.js"],
         "a non-simple list's distinct parameters", "a non-simple list binding one name twice")
    pair("EE0070", FN, "function f(a, a) { return a; } f(1, 2)", 2, "function f(a, a) { \"use strict\"; }",
         ["test/language/statements/function/param-duplicated-strict-body-1.js"],
         "a sloppy simple list binding one name twice", "a list binding one name twice in a function made strict "
         "by its body")
    pair("EE0071", FN, "function arguments() { return 4; } arguments()", 4,
         "function arguments() { \"use strict\"; }",
         ["test/language/statements/function/name-arguments-strict-body.js"],
         "a sloppy function named arguments", "a function named arguments made strict by its body")
    pair("EE0072", FN, "function f(a = 1) { return a; } f()", 1, "function f(a = 1) { \"use strict\"; }",
         ["test/language/statements/function/use-strict-with-non-simple-param.js"],
         "a non-simple list without a Use Strict Directive", "a Use Strict Directive with a non-simple list")
    pair("EE0073", FN, "function f(a) { { let a = 2; return a; } } f(1)", 2, "function f(a) { let a; }", [],
         "a parameter's name declared lexically in a nested block", "a parameter's name declared lexically in "
         "the body")
    pair("EE0078", FN, "function f() { let a = 1; { let a = 2; } return a; } f()", 1, "function f() { let a; let a; }",
         [], "one lexical name in a body and a nested block", "one lexical name twice in a body")
    pair("EE0079", FN, "function f() { let a = 1; var b = 2; return a + b; } f()", 3, "function f() { let a; var a; }",
         [], "a lexical and a var name in a body", "one name declared lexically and with var in a body")
    pair("EE0080", FN, "function f() { L: { break L; } L: { break L; } return 1; } f()", 1, "function f() { L: L: ; }",
         [], "one label on two sibling statements", "a label inside a statement with the same label")
    pair("EE0083", AR, "function* g() { var f = (a = 1) => a; yield f(); } g().next().value", 1,
         "function* g() { (a = yield) => 1; }", ["test/language/expressions/arrow-function/param-dflt-yield-expr.js"],
         "an arrow's default with no yield in a generator", "an arrow's default holding a yield expression")
    pair("EE0084", AR, "async function f() { var g = (a = 1) => a; return g(); } typeof f", "function",
         "async function f() { (a = await 1) => 1; }",
         ["test/language/expressions/async-arrow-function/await-as-param-nested-arrow-body-position.js"],
         "an arrow's default with no await in an async function", "an arrow's default holding an await expression")
    pair("EE0085", AR, "((a = 1) => a)()", 1, "(a = 1) => { \"use strict\"; };",
         ["test/language/expressions/arrow-function/syntax/early-errors/use-strict-with-non-simple-param.js"],
         "an arrow's non-simple list", "an arrow's Use Strict Directive with a non-simple list")
    pair("EE0086", AR, "((a) => { { let a = 2; return a; } })(1)", 2, "(a) => { let a; };", [],
         "an arrow's parameter name declared lexically in a nested block", "an arrow's parameter name declared "
         "lexically in its body")
    pair("EE0087", AR, "((...a) => a.length)(1, 2)", 2, "([...a, b]) => 1;",
         ["test/language/expressions/arrow-function/dstr/ary-ptrn-rest-not-final-id.js"],
         "an arrow's rest parameter", "a cover that is no ArrowFormalParameters")
    pair("EE0088", MD, "({ m(a = 1) { return a; } }).m()", 1, "({ m(a = 1) { \"use strict\"; } });",
         ["test/language/expressions/object/method-definition/use-strict-with-non-simple-param.js"],
         "a method's non-simple list", "a method's Use Strict Directive with a non-simple list")
    pair("EE0089", MD, "({ m(a) { { let a = 2; return a; } } }).m(1)", 2, "({ m(a) { let a; } });",
         ["test/language/expressions/object/method-definition/name-param-redecl.js"],
         "a method's parameter name declared lexically in a nested block", "a method's parameter name declared "
         "lexically in its body")
    pair("EE0090", MD, "var o = { set p([a, b]) { this.q = a + b; } }; o.p = [1, 2]; o.q", 3,
         "({ set p([a, a]) {} });", [], "a setter's pattern binding two names", "a setter's pattern binding one "
         "name twice")
    pair("EE0091", MD, "var o = { set p(v = 1) { this.q = v; } }; o.p = undefined; o.q", 1,
         "({ set p(v = 1) { \"use strict\"; } });",
         ["test/language/expressions/object/method-definition/setter-use-strict-with-non-simple-param.js"],
         "a setter's parameter with a default", "a setter's Use Strict Directive with a non-simple parameter")
    pair("EE0092", MD, "var o = { set p(v) { { let v = 2; this.q = v; } } }; o.p = 1; o.q", 2,
         "({ set p(v) { let v; } });", [], "a setter's parameter name declared lexically in a nested block",
         "a setter's parameter name declared lexically in its body")
    pair("EE0094", GN, "({ *g(a = 1) { yield a; } }).g().next().value", 1, "({ *g(a = yield) {} });",
         ["test/language/expressions/object/method-definition/generator-param-init-yield.js"],
         "a generator method's default", "a generator method's default holding a yield expression")
    pair("EE0095", GN, "({ *g(a = 2) { yield a; } }).g().next().value", 2, "({ *g(a = 1) { \"use strict\"; } });",
         ["test/language/expressions/object/method-definition/generator-use-strict-with-non-simple-param.js"],
         "a generator method's non-simple list", "a generator method's Use Strict Directive with a non-simple list")
    pair("EE0096", GN, "({ *g(a) { { let a = 3; yield a; } } }).g(1).next().value", 3, "({ *g(a) { let a; } });",
         ["test/language/expressions/object/method-definition/generator-param-redecl-let.js"],
         "a generator method's parameter name declared lexically in a nested block",
         "a generator method's parameter name declared lexically in its body")
    pair("EE0097", GN, "function* g(a, a) { yield a; } g(1, 2).next().value", 2,
         "function* g(a, a) { \"use strict\"; }", ["test/language/statements/generators/dflt-params-duplicates.js"],
         "a sloppy generator's simple list binding one name twice",
         "a generator's list binding one name twice in a generator made strict by its body")
    pair("EE0098", GN, "function* arguments() { yield 5; } arguments().next().value", 5,
         "\"use strict\"; function* eval() {}", [], "a sloppy generator named arguments",
         "a strict generator named eval")
    pair("EE0099", GN, "function* g(a = 1) { yield a; } g().next().value", 1, "function* g(a = 1) { \"use strict\"; }",
         ["test/language/statements/generators/use-strict-with-non-simple-param.js"],
         "a generator's non-simple list", "a generator's Use Strict Directive with a non-simple list")
    pair("EE0100", GN, "function* g(a) { { let a = 2; yield a; } } g(1).next().value", 2, "function* g(a) { let a; }",
         [], "a generator's parameter name declared lexically in a nested block",
         "a generator's parameter name declared lexically in its body")
    pair("EE0101", GN, "function* g(a = 1) { yield a + 1; } g().next().value", 2, "function* g(a = yield) {}",
         ["test/language/statements/generators/param-dflt-yield.js"], "a generator's default",
         "a generator's default holding a yield expression")
    pair("EE0107", AG, "typeof ({ async *g(a = 1) {} }).g", "function", "({ async *g(a = yield) {} });", [],
         "an async generator method's default", "an async generator method's default holding a yield expression")
    pair("EE0108", AG, "typeof ({ async *g(a = 2) {} }).g", "function", "({ async *g(a = await 1) {} });", [],
         "an async generator method's default with no await", "an async generator method's default holding an "
         "await expression")
    pair("EE0109", AG, "typeof ({ async *g(a = 3) { yield a; } }).g", "function",
         "({ async *g(a = 1) { \"use strict\"; } });",
         ["test/language/expressions/object/method-definition/async-gen-meth-array-destructuring-param-strict-body.js"],
         "an async generator method's non-simple list", "an async generator method's Use Strict Directive with a "
         "non-simple list")
    pair("EE0110", AG, "typeof ({ async *g(a) { { let a; } } }).g", "function", "({ async *g(a) { let a; } });", [],
         "an async generator method's parameter name declared lexically in a nested block",
         "an async generator method's parameter name declared lexically in its body")
    pair("EE0111", AG, "async function* g(a, a) {} typeof g", "function", "async function* g(a, a) { \"use strict\"; }",
         ["test/language/statements/async-generator/dflt-params-duplicates.js"],
         "a sloppy async generator's simple list binding one name twice",
         "an async generator's list binding one name twice in an async generator made strict by its body")
    pair("EE0112", AG, "async function* arguments() {} typeof arguments", "function",
         "(async function* eval() { \"use strict\"; });",
         ["test/language/expressions/async-generator/early-errors-expression-binding-identifier-eval.js"],
         "a sloppy async generator named arguments", "an async generator expression named eval made strict by its "
         "body")
    pair("EE0113", AG, "async function* g(a = 1) {} typeof g", "function", "async function* g(a = 1) { \"use strict\"; }",
         ["test/language/statements/async-generator/array-destructuring-param-strict-body.js"],
         "an async generator's non-simple list", "an async generator's Use Strict Directive with a non-simple list")
    pair("EE0114", AG, "async function* g(a) { { let a; } } typeof g", "function", "async function* g(a) { let a; }",
         ["test/language/expressions/async-generator/early-errors-expression-formals-body-duplicate-let.js"],
         "an async generator's parameter name declared lexically in a nested block",
         "an async generator's parameter name declared lexically in its body")
    pair("EE0115", AG, "async function* g(a = 1) { yield a; } typeof g", "function", "(async function* (a = yield) {});",
         ["test/language/expressions/async-generator/early-errors-expression-formals-contains-yield-expr.js"],
         "an async generator's default", "an async generator's default holding a yield expression")
    pair("EE0116", AG, "async function* g(a = 2) { await a; } typeof g", "function",
         "(async function* (a = await 1) {});",
         ["test/language/expressions/async-generator/early-errors-expression-formals-contains-await-expr.js"],
         "an async generator's default with no await", "an async generator's default holding an await expression")
    pair("EE0122", CL, "class C { constructor() { this.a = 1; } } new C().a", 1,
         "class C { constructor() {} constructor() {} }",
         ["test/language/statements/class/elements/syntax/early-errors/grammar-class-body-ctor-duplicate.js"],
         "one constructor", "two constructors")
    pair("EE0123", CL, "class C { get #p() { return 2; } set #p(v) {} static t(o) { return o.#p; } } C.t(new C)", 2,
         "class C { #p; #p; }",
         ["test/language/statements/class/elements/syntax/early-errors/grammar-privatemeth-duplicate-meth-meth.js"],
         "a private getter and setter of one name", "one private name declared twice")
    pair("EE0125", CL, "class C { static get constructor() { return 3; } } C.constructor", 3,
         "class C { get constructor() {} }",
         ["test/language/statements/class/elements/syntax/early-errors/grammar-special-meth-ctor-get.js"],
         "a static getter named constructor", "a getter named constructor")
    pair("EE0127", CL, "class C { prototype() { return 5; } } new C().prototype()", 5, "class C { static prototype() {} }",
         ["test/language/statements/class/elements/syntax/early-errors/grammar-static-meth-prototype.js"],
         "a prototype method named prototype", "a static method named prototype")
    pair("EE0128", CL, "class C { c = 6; } new C().c", 6, "class C { constructor; }",
         ["test/language/statements/class/elements/fields-literal-name-propname-constructor.js"],
         "a field named c", "a field named constructor")
    pair("EE0129", CL, "class C { static p = 7; } C.p", 7, "class C { static prototype; }",
         ["test/language/statements/class/elements/fields-literal-name-static-propname-prototype.js"],
         "a static field named p", "a static field named prototype")
    pair("EE0132", CL, "class C { #c = 8; static t(o) { return o.#c; } } C.t(new C)", 8, "class C { #constructor; }",
         ["test/language/statements/class/elements/syntax/early-errors/grammar-privatename-constructor.js"],
         "a private field #c", "a private name #constructor")
    pair("EE0133", CL, "var r; class C { static { let a = 1; r = a; } } r", 1, "class C { static { let a; let a; } }",
         ["test/language/statements/class/static-init-invalid-lex-dup.js"],
         "a lexical name in a static block", "one lexical name twice in a static block")
    pair("EE0134", CL, "var r; class C { static { let a = 2; var b = a; r = b; } } r", 2,
         "class C { static { let a; var a; } }", ["test/language/statements/class/static-init-invalid-lex-var.js"],
         "a lexical and a var name in a static block", "one name declared lexically and with var in a static block")
    pair("EE0135", CL, "var r = 0; class C { static { L: { r = 3; break L; } } } r", 3, "class C { static { L: L: ; } }",
         ["test/language/statements/class/static-init-invalid-label-dup.js"],
         "a label in a static block", "a label inside a statement with the same label in a static block")
    pair("EE0140", CL, "var r; class C { static { r = typeof async function () { await 1; }; } } r", "function",
         "class C { static { await 0; } }", ["test/language/statements/class/static-init-invalid-await.js"],
         "await in an async function nested in a static block", "await in a static block")
    pair("EE0141", AF, "typeof ({ async m(a = 1) { return a; } }).m", "function",
         "({ async m(a = 1) { \"use strict\"; } });",
         ["test/language/statements/class/async-method/array-destructuring-param-strict-body.js"],
         "an async method's non-simple list", "an async method's Use Strict Directive with a non-simple list")
    pair("EE0143", AF, "typeof ({ async m(a = 2) {} }).m", "function", "({ async m(a = await 1) {} });",
         ["test/language/expressions/object/method-definition/early-errors-object-method-await-in-formals-default.js"],
         "an async method's default with no await", "an async method's default holding an await expression")
    pair("EE0144", AF, "typeof ({ async m(a) { { let a; } } }).m", "function", "({ async m(a) { let a; } });", [],
         "an async method's parameter name declared lexically in a nested block",
         "an async method's parameter name declared lexically in its body")
    pair("EE0145", AF, "async function f(a = 1) {} typeof f", "function", "async function f(a = 1) { \"use strict\"; }",
         ["test/language/statements/async-function/early-errors-declaration-NSPL-with-USD.js"],
         "an async function's non-simple list", "an async function's Use Strict Directive with a non-simple list")
    pair("EE0146", AF, "async function f(a = 1) { await a; } typeof f", "function", "async function f(a = await 1) {}",
         ["test/language/statements/async-function/early-errors-declaration-await-in-formals-default.js"],
         "an async function's default with no await", "an async function's default holding an await expression")
    pair("EE0147", AF, "async function f(a, a) {} typeof f", "function", "async function f(a, a) { \"use strict\"; }",
         ["test/language/statements/async-function/early-errors-declaration-duplicate-parameters.js"],
         "a sloppy async function's simple list binding one name twice",
         "an async function's list binding one name twice in an async function made strict by its body")
    pair("EE0148", AF, "async function arguments() {} typeof arguments", "function", "\"use strict\"; async function eval() {}",
         ["test/language/statements/async-function/early-errors-declaration-binding-identifier-eval.js"],
         "a sloppy async function named arguments", "a strict async function named eval")
    pair("EE0149", AF, "async function f(a) { { let a; } } typeof f", "function", "async function f(a) { let a; }",
         ["test/language/statements/async-function/early-errors-declaration-formals-body-duplicate.js"],
         "an async function's parameter name declared lexically in a nested block",
         "an async function's parameter name declared lexically in its body")
    pair("EE0154", AA, "typeof (async a => { { let a; } })", "function", "async a => { let a; };", [],
         "an async arrow's parameter name declared lexically in a nested block",
         "an async arrow's parameter name declared lexically in its body")
    pair("EE0155", AA, "typeof (async (a, ...b) => 1)", "function", "async (a, ...b,) => 1;",
         ["test/language/expressions/async-arrow-function/rest-params-trailing-comma-early-error.js"],
         "an async arrow head with a rest parameter", "a trailing comma after an async arrow head's rest parameter")
    pair("EE0156", AA, "function* g() { yield typeof (async (a = 1) => a); } g().next().value", "function",
         "function* g() { async (a = yield) => 1; }", [], "an async arrow head's default in a generator",
         "an async arrow head's default holding a yield expression")
    pair("EE0157", AA, "typeof (async (a = 1) => await a)", "function", "async function f() { async (a = await 1) => 1; }",
         ["test/language/expressions/async-arrow-function/early-errors-arrow-await-in-formals-default.js"],
         "an async arrow head's default with no await", "an async arrow head's default holding an await expression")
    pair("EE0158", AA, "typeof (async (a) => { { let a; } })", "function", "async (a) => { let a; };",
         ["test/language/expressions/async-arrow-function/early-errors-arrow-formals-body-duplicate.js"],
         "an async arrow's parameter name declared lexically in a nested block",
         "an async arrow's parameter name declared lexically in its body")
    pair("EE0159", AA, "typeof (async (a = 1) => a)", "function", "async (a = 1) => { \"use strict\"; };",
         ["test/language/expressions/async-arrow-function/early-errors-arrow-NSPL-with-USD.js"],
         "an async arrow's non-simple list", "an async arrow's Use Strict Directive with a non-simple list")
    # EX0033 and P24: return in a class static block.
    add(f, "ex0033", "var r; class C { static { r = (function () { return 2; })(); } } r", V(2), rows=("EX0033",),
        clauses=C("sec-class-definitions"), note="return in a function nested in a static block")
    add(f, "ex0033", "class C { static { return; } }", SE, rows=("EX0033",), clauses=C("sec-class-definitions"),
        tests=T("test/language/statements/class/static-init-invalid-return.js"), note="return in a static block")
    add(f, "ex0033", "function f() { class C { static { return; } } }", SE, rows=("EX0033",), corr=("P24",),
        clauses=C("sec-class-definitions"), tests=T("test/language/statements/class/static-init-invalid-return.js"),
        note="return in a static block of a class inside a function")


def scripts() -> None:
    f = "scripts"
    S = "sec-scripts-static-semantics-early-errors"
    add(f, "ee0160", "let a = 1; const b = 2; a + b", V(3), rows=("EE0160",), clauses=C(S),
        note="two lexical names at a script's top level")
    add(f, "ee0160", "let a; let a;", SE, rows=("EE0160",), clauses=C(S),
        tests=T("test/language/statements/class/syntax/early-errors/class-definition-evaluation-scriptbody-duplicate-binding.js"),
        note="one lexical name twice at a script's top level")
    add(f, "ee0161", "let a = 1; var b = 2; a + b", V(3), rows=("EE0161",), clauses=C(S),
        note="a lexical and a var name at a script's top level")
    add(f, "ee0161", "let a; var a;", SE, rows=("EE0161",), clauses=C(S),
        note="one name declared lexically and with var at a script's top level")
    add(f, "ee0163", "function f() { return new.target === undefined; } f()", V(True), rows=("EE0163",), clauses=C(S),
        note="new.target in a function")
    add(f, "ee0163", "new.target;", SE, rows=("EE0163",), clauses=C(S), tests=T("test/language/global-code/new.target.js"),
        note="new.target at a script's top level")
    add(f, "ee0163", "() => new.target;", SE, rows=("EE0163",), clauses=C(S),
        tests=T("test/language/global-code/new.target-arrow.js"), note="new.target in an arrow at a script's top level")
    add(f, "d32", "class C { f = new.target === undefined; } new C().f", V(True), rows=("EE0163",), corr=("D32",),
        clauses=C(S, "sec-static-semantics-contains"),
        note="new.target in a class field initialiser, which Contains does not enter (a correction of Broiler.VM "
             "that step 1 makes)")
    add(f, "d32", "class C { f = () => new.target; } new C().f() === undefined", V(True), rows=("EE0163",),
        corr=("D32",), clauses=C(S, "sec-static-semantics-contains"),
        note="new.target in an arrow in a class field initialiser (a correction of Broiler.VM that step 1 makes)")
    add(f, "ee0164", "L: { break L; } L: { } 1", V(1), rows=("EE0164",), clauses=C(S),
        note="one label on two sibling statements")
    add(f, "ee0164", "L: L: ;", SE, rows=("EE0164",), clauses=C(S),
        note="a label inside a statement with the same label")
    add(f, "ex0032", "function f() { return 1; } f()", V(1), rows=("EX0032",), clauses=C("sec-return-statement"),
        note="return in a function")
    add(f, "ex0032", "return;", SE, rows=("EX0032",), clauses=C("sec-scripts", "sec-return-statement"),
        tests=T("test/language/global-code/return.js"), note="return at a script's top level")
    add(f, "ex0010", "var o = { import: 1, export: 2 }; o.import + o.export", V(3), rows=("EX0010",),
        clauses=C("sec-scripts"), note="import and export as property names")
    add(f, "ex0010", "export var a;", SE, rows=("EX0010",), clauses=C("sec-scripts", "sec-exports"),
        tests=T("test/language/global-code/export.js"), note="an ExportDeclaration in a Script")
    add(f, "ex0010", "import a from \"m\";", SE, rows=("EX0010",), clauses=C("sec-scripts", "sec-imports"),
        tests=T("test/language/global-code/import.js"), note="an ImportDeclaration in a Script")
    add(f, "ep0008", "var r; { using x = null; r = 3; } r", V(3), rows=("EP0008",),
        tests=T("test/language/statements/using/syntax/using-allows-bindingidentifier.js"),
        note="a using declaration in a block; ECMA-262 main at 4feb0272 merged the rule after the pinned edition, so "
             "the vector cites the test262 tests that assert it")
    add(f, "ep0008", "using x = null;", SE, rows=("EP0008",), corr=("P25",),
        tests=T("test/language/statements/using/syntax/using-not-allowed-at-top-level-of-script.js"),
        note="a using declaration at a script's top level; ECMA-262 main at 4feb0272 merged the rule after the pinned "
             "edition, so the vector cites the test262 tests that assert it")
    add(f, "ep0008", "using x = null;", SE, rows=("EP0008",), corr=("P25",), op="PerformEval",
        tests=T("test/language/statements/using/syntax/using-not-allowed-at-top-level-of-eval.js"),
        note="a using declaration at eval code's top level; ECMA-262 main at 4feb0272 merged the rule after the "
             "pinned edition, so the vector cites the test262 tests that assert it")
    # EA0001, EA0002: PerformEval's steps.
    E = "sec-performeval"
    add(f, "ea0001", "1 + 1", V(2), rows=("EA0001",), op="PerformEval", clauses=C(E), note="eval code that parses")
    add(f, "ea0001", "1 +", SE, rows=("EA0001",), op="PerformEval", clauses=C(E),
        tests=T("test/language/eval-code/direct/parse-failure-1.js"), note="eval code that does not parse")
    add(f, "ea0002", "function f() { return new.target === undefined; } f()", V(True), rows=("EA0002",),
        op="PerformEval", clauses=C(E, "sec-static-semantics-contains"),
        note="new.target in a function declared in eval code, which Contains does not enter")
    add(f, "ea0002", "new.target", SE, rows=("EA0002",), op="PerformEval", clauses=C(E),
        tests=T("test/language/eval-code/direct/new.target.js"),
        note="new.target in the eval code of a direct eval in no function")
    # EX0008, EX0009: the grammar, with the corrections of Broiler.JS the correspondence names that a Script carries.
    g = "sec-ecmascript-language-statements-and-declarations"
    add(f, "ex0008", "1; 2", V(2), rows=("EX0008",), clauses=C("sec-expression-statement"), note="two statements")
    add(f, "ex0008", "var a = 1 2;", SE, rows=("EX0008",), clauses=C("sec-variable-statement", ASI),
        tests=T("test/language/asi/S7.9_A10_T2.js"), note="a numeric literal after an initialiser on its line")
    add(f, "ex0009", "if (1) 2", V(2), rows=("EX0009",), clauses=C("sec-if-statement"), note="an if statement")
    add(f, "ex0009", "if 1) 2", SE, rows=("EX0009",), clauses=C("sec-if-statement"),
        tests=T("test/language/asi/S7.9.2_A1_T1.js"), note="an if statement with no (")
    add(f, "grammar", "while (0) const x = 1;", SE, rows=("EX0008",), corr=("P11",), clauses=C(g),
        tests=T("test/language/statements/while/decl-const.js"), note="a lexical declaration as a while's body")
    add(f, "grammar", "switch (0) { default: default: }", SE, rows=("EX0008",), corr=("P12",),
        clauses=C("sec-switch-statement"), tests=T("test/language/statements/switch/S12.11_A2_T1.js"),
        note="two default clauses")
    add(f, "grammar", "var a = 1, f = 2; a & f ~ 0", SE, rows=("EX0008",), corr=("P13",),
        clauses=C("sec-bitwise-not-operator"), note="~ is no binary operator")
    add(f, "grammar", "switch => 1;", SE, rows=("EX0008",), corr=("P14",), clauses=C("sec-arrow-function-definitions"),
        tests=T("test/language/expressions/arrow-function/syntax/early-errors/arrowparameters-bindingidentifier-identifier-futurereservedword.js"),
        note="a reserved word as an arrow's parameter")
    add(f, "grammar", "var x = true; x = case;", SE, rows=("EX0008",), corr=("P22",),
        clauses=C("sec-keywords-and-reserved-words"), tests=T("test/language/identifiers/val-case.js"),
        note="a reserved word as a reference")
    add(f, "grammar", "function f(...a,) {}", SE, rows=("EX0008",), corr=("P23",),
        clauses=C("sec-function-definitions"), tests=T("test/language/statements/function/rest-params-trailing-comma-early-error.js"),
        note="a comma after a rest parameter")
    add(f, "grammar", "var c = 1; class C extends c ? Object : Object {}", SE, rows=("EX0008",), corr=("P18",),
        clauses=C("sec-class-definitions"), note="a ClassHeritage is a LeftHandSideExpression, not a conditional")
    add(f, "grammar", "class C { m() { super.#x; } }", SE, rows=("EX0008",), corr=("P18",),
        clauses=C("sec-class-definitions"),
        tests=T("test/language/statements/class/elements/syntax/early-errors/super-private-access-invalid.js"),
        note="a private name after super")


def annex_b() -> None:
    f = "annex-b"
    add(f, "eg0003", "L: function f() { return 1; } f()", V(1), rows=("EG0003",), clauses=C("sec-labelled-function-declarations"),
        tests=T("test/annexB/language/statements/labeled/function-declaration.js"),
        note="a labelled function declaration in sloppy code", impl=ANNEX_B)
    add(f, "eg0003", "L: function* g() {}", SE, rows=("EG0003",),
        clauses=C("sec-labelled-function-declarations", "sec-labelled-statements"),
        tests=T("test/language/statements/labeled/decl-gen.js"),
        note="a labelled generator declaration, which no LabelledItem admits")
    add(f, "eg0004", "{ function f() { return 1; } function f() { return 2; } } f()", V(2), rows=("EG0004",),
        clauses=C("sec-block-level-function-declarations-web-legacy-compatibility-semantics"),
        tests=T("test/annexB/language/function-code/function-redeclaration-block.js"),
        note="one function name declared twice in a block in sloppy code", impl=ANNEX_B)
    add(f, "eg0004", "{ function f() {} function* f() {} }", SE, rows=("EG0004",),
        clauses=C("sec-block-level-function-declarations-web-legacy-compatibility-semantics",
                  "sec-block-static-semantics-early-errors"),
        tests=T("test/language/block-scope/syntax/redeclaration/function-name-redeclaration-attempt-with-generator.js"),
        note="a function and a generator of one name in a block")
    add(f, "eg0005", "if (true) function f() { return 3; } f()", V(3), rows=("EG0005",),
        clauses=C("sec-functiondeclarations-in-ifstatement-statement-clauses"),
        tests=T("test/annexB/language/global-code/if-decl-no-else-global-existing-fn-update.js"),
        note="a function declaration as an if's clause in sloppy code", impl=ANNEX_B)
    add(f, "eg0005", "\"use strict\"; if (true) function f() {}", SE, rows=("EG0005",),
        clauses=C("sec-functiondeclarations-in-ifstatement-statement-clauses", "sec-if-statement"),
        tests=T("test/language/statements/if/if-fun-no-else-strict.js"),
        note="a function declaration as an if's clause in strict code")
    add(f, "eg0006", "var r; try { throw 1; } catch (e) { var e = 2; r = e; } r", V(2), rows=("EG0006",),
        clauses=C("sec-variablestatements-in-catch-blocks"),
        tests=T("test/annexB/language/statements/try/catch-redeclared-var-statement.js"),
        note="a var of the catch parameter's name in its block", impl=ANNEX_B)
    add(f, "eg0006", "try {} catch ({ e }) { for (var e of []) ; }", SE, rows=("EG0006",),
        clauses=C("sec-variablestatements-in-catch-blocks", "sec-try-statement-static-semantics-early-errors"),
        note="a var of a name a catch parameter pattern binds, which the extension does not admit")
    add(f, "eg0007", "for (var i = 0 in {}) ; i", V(0), rows=("EG0007",), clauses=C("sec-initializers-in-forin-statement-heads"),
        tests=T("test/annexB/language/statements/for-in/nonstrict-initializer.js"),
        note="an initialiser in a for-in head in sloppy code", impl=ANNEX_B)
    add(f, "eg0007", "\"use strict\"; for (var i = 0 in {}) ;", SE, rows=("EG0007",),
        clauses=C("sec-initializers-in-forin-statement-heads"),
        tests=T("test/annexB/language/statements/for-in/strict-initializer.js"),
        note="an initialiser in a for-in head in strict code")
    add(f, "eg0008", "var r; function f() {} try { f() = 1; } catch (e) { r = e instanceof ReferenceError; } r", V(True),
        rows=("EG0008",), clauses=C("sec-runtime-errors-for-function-call-assignment-targets"),
        tests=T("test/annexB/language/expressions/assignmenttargettype/callexpression.js"),
        note="a call as an assignment target in sloppy code throws a ReferenceError when it runs", impl=ANNEX_B)
    add(f, "eg0008", "\"use strict\"; function f() {} f() = 1;", SE, rows=("EG0008",),
        clauses=C("sec-runtime-errors-for-function-call-assignment-targets",
                  "sec-assignment-operators-static-semantics-early-errors"),
        tests=T("test/language/expressions/assignmenttargettype/direct-callexpression.js"),
        note="a call as an assignment target in strict code")


# ---- The rows ------------------------------------------------------------------------------------------------------

MODULE = "only the Module goal reaches it, which schema 1 carries no field for (decision D25)"
MODULE_REFUSAL = ("the refusal needs the Module goal, which schema 1 carries no field for (decision D25); a Script "
                  "carries the acceptance")
MODULE_ACCEPTANCE = ("the acceptance needs the Module goal, which schema 1 carries no field for (decision D25); a "
                     "Script carries the refusal")
PROVIDER = ("the regular-expression validator is a provider of the request, a parameter each engine supplies "
            "(decisions D7 and D26)")
PROPOSAL = "a proposal flag of the request, off by default (decision D17)"
ERM = ("no source schema 1 can cite asserts the refusal: the rule is ECMA-262 main's (4feb0272), not the pinned "
       "edition's, and no test262 test at ccaac100 asserts it")
ERM_ACCEPTANCE = ("no source schema 1 can cite asserts an acceptance under the rule: it is ECMA-262 main's "
                  "(4feb0272), not the pinned edition's, and no test262 test at ccaac100 asserts one")

# A syntax row with a half or both halves uncovered: (what is uncovered, the reason).
UNCOVERED: dict[str, tuple[str, str]] = {
    "EE0007": ("refused", MODULE_REFUSAL),
    "EE0013": ("refused", MODULE_REFUSAL),
    "EE0027": ("accepted", MODULE_ACCEPTANCE),
    "EG0001": ("refused", MODULE_REFUSAL),
    **{row: ("both", MODULE) for row in ("EE0168", "EE0169", "EE0173", "EE0174", "EE0178", "EE0179", "EE0180",
                                        "EE0181", "EE0182")},
    "EE0019": ("both", PROVIDER),
    **{f"EE{n:04d}": ("both", PROVIDER) for n in range(183, 214)},
    "EG0002": ("both", PROVIDER),
    **{row: ("both", PROPOSAL) for row in ("EP0009", "EP0010", "EP0011", "EP0012", "EP0013", "EP0014")},
    **{row: ("refused", ERM) for row in ("EP0001", "EP0002", "EP0003", "EP0004", "EP0005")},
    **{row: ("accepted", ERM_ACCEPTANCE) for row in ("EP0006", "EP0007")},
    **{row: ("both", "only CreateDynamicFunction's FormalParameters and FunctionBody goals reach it, which no op of "
                     "the area names (a Script's call of Function is no parse of the Script or of eval code)")
       for row in ("EA0010", "EA0011", "EA0012")},
    "EP0015": ("both", "only ShadowRealm.prototype.evaluate's PerformShadowRealmEval reaches it, which no op of the "
                       "area names"),
    "EP0016": ("both", "only ShadowRealm.prototype.evaluate's PerformShadowRealmEval reaches it, which no op of the "
                       "area names"),
    "EX0011": ("both", "the nesting bound is a parameter of the request with each engine's default (decision D14)"),
    "EX0012": ("both", "the tree-depth bound is the library's constant, derived at stage 4e (decision D14), and the "
                       "specification sets none; the home's part G judges it"),
}


def syntax_rows(path: Path = STAGES) -> list[str]:
    with path.open(encoding="utf-8", newline="") as stream:
        return [row["id"] for row in csv.DictReader(stream, delimiter="\t") if row["stage"] == "syntax"]


def check_rows(cases: Sequence[Case], rows: Sequence[str]) -> list[str]:
    """Every syntax row has an accepted and a refused vector, or ``UNCOVERED`` names what it lacks and why; no case
    names a row that is not a syntax row, and ``UNCOVERED`` names no half a vector covers."""
    problems = []
    accepted: dict[str, int] = {}
    refused: dict[str, int] = {}
    for case in cases:
        for row in case.rows:
            if row not in rows:
                problems.append(f"{case.group}: {row} is not a syntax row of the stage table")
            (refused if case.refused else accepted)[row] = (refused if case.refused else accepted).get(row, 0) + 1
    for row in UNCOVERED:
        if row not in rows:
            problems.append(f"UNCOVERED names {row}, which is not a syntax row")
    for row in rows:
        missing = UNCOVERED.get(row, ("none", ""))[0]
        want_accepted = missing not in ("accepted", "both")
        want_refused = missing not in ("refused", "both")
        if want_accepted != (row in accepted):
            problems.append(f"{row}: {'no' if want_accepted else 'an'} accepted vector, against UNCOVERED")
        if want_refused != (row in refused):
            problems.append(f"{row}: {'no' if want_refused else 'a'} refused vector, against UNCOVERED")
    return problems


def coverage(cases: Sequence[Case], rows: Sequence[str]) -> dict[str, list[str]]:
    """The syntax rows by coverage: both halves, the accepted or the refused alone, or neither."""
    out: dict[str, list[str]] = {"both": [], "accepted only": [], "refused only": [], "none": []}
    for row in rows:
        missing = UNCOVERED.get(row, ("none", ""))[0]
        key = {"none": "both", "refused": "accepted only", "accepted": "refused only", "both": "none"}[missing]
        out[key].append(row)
    return out


# ---- Checks of the sources a vector cites --------------------------------------------------------------------------


def check_clauses(cases: Sequence[Case], spec: Path) -> list[str]:
    ids = set(re.findall(r'id="(sec-[^"]+)"', spec.read_text(encoding="utf-8")))
    return sorted({c for case in cases for c in case.clauses if c not in ids})


def check_tests(cases: Sequence[Case], test262: Path) -> list[str]:
    return sorted({t for case in cases for t in case.tests if not (test262 / t).is_file()})


# ---- The independent check ------------------------------------------------------------------------------------------

# Where the reference disagrees with the specification, the vector follows the specification and this table names it:
# the id, and what the reference does. --check-node fails on any disagreement not named here, and on any named here
# that does not occur.
NODE_DISAGREES: dict[str, str] = {
    # ECMA-262 13.15.1 and B.3.6: a CallExpression is an invalid assignment target in strict code, an early error; the
    # reference parses the Script and throws a ReferenceError when the assignment runs, as it does in sloppy code.
    "syntax/annex-b/eg0008/0002": "parse accepted, outcome error: ReferenceError",
}


def node_requests(vectors: Sequence[tuple[str, Case]]) -> list[dict[str, str]]:
    return [{"id": identifier, "op": case.op, "in": case.source, "script": case.script} for identifier, case in vectors]


def check_node(node: str, vectors: Sequence[tuple[str, Case]]) -> tuple[list[str], dict[str, str]]:
    """Runs every vector under the reference; answers the problems and each disagreement found."""
    version = subprocess.run([node, "--version"], capture_output=True, text=True, check=False).stdout.strip()
    if version != REFERENCE_VERSION:
        return [f"{node} is {version or 'not runnable'}, not the reference {REFERENCE_VERSION}"], {}
    with tempfile.TemporaryDirectory() as scratch:
        requests = Path(scratch) / "requests.json"
        requests.write_text(json.dumps(node_requests(vectors)), encoding="utf-8")
        result = subprocess.run([node, str(CHECKER), str(requests), str(CORPUS / "prelude.js")], capture_output=True,
                                text=True, check=False, timeout=600)
    if result.returncode != 0:
        return [f"syntaxcheck.mjs ended with {result.returncode}: {result.stderr.strip()[:400]}"], {}
    answers = {line["id"]: line for line in map(json.loads, result.stdout.splitlines())}
    found: dict[str, str] = {}
    for identifier, case in vectors:
        answer = answers[identifier]
        if case.refused:
            if answer["parse"] != "SyntaxError" or answer["outcome"] != SE:
                found[identifier] = f"parse {answer['parse']}, outcome {answer['outcome']}"
        elif answer["outcome"] != case.out:
            found[identifier] = f"parse {answer['parse']}, outcome {answer['outcome']}"
    problems = [f"{i}: the reference answers {found[i]}, which NODE_DISAGREES does not name"
                for i in found if i not in NODE_DISAGREES]
    problems += [f"{i}: NODE_DISAGREES names it, but the reference agrees" for i in NODE_DISAGREES if i not in found]
    problems += [f"{i}: the reference answers {found[i]}, not what NODE_DISAGREES names, {NODE_DISAGREES[i]}"
                 for i in NODE_DISAGREES if i in found and found[i] != NODE_DISAGREES[i]]
    return problems, found


# ---- Classifications and writing -----------------------------------------------------------------------------------


def read_divergences(path: Path) -> dict[str, dict[str, str]]:
    """syntax-divergences.txt: `reason <key>: <text>` lines, then `<engine> <key> <id>` lines; by id, each engine's text."""
    if not path.exists():
        return {}
    reasons: dict[str, str] = {}
    out: dict[str, dict[str, str]] = {}
    for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        if not line or line.startswith("#"):
            continue
        if line.startswith("reason "):
            key, sep, text = line[len("reason "):].partition(": ")
            if not sep or not text or key in reasons:
                raise GeneratorError(f"{path.name}:{number}: a reason is `reason <key>: <text>`, each key once")
            reasons[key] = text
            continue
        parts = line.split(" ")
        if len(parts) != 3 or parts[0] not in ("broiler-js", "broiler-vm") or parts[1] not in reasons:
            raise GeneratorError(f"{path.name}:{number}: `{line}` is not `<engine> <reason key> <id>`")
        engine, key, identifier = parts
        if engine in out.setdefault(identifier, {}):
            raise GeneratorError(f"{path.name}:{number}: {identifier} is classified twice for {engine}")
        out[identifier][engine] = reasons[key]
    return out


def identifiers(cases: Sequence[Case]) -> list[tuple[str, Case]]:
    numbers: dict[tuple[str, str], int] = {}
    out = []
    for case in cases:
        key = (case.file, case.group)
        numbers[key] = numbers.get(key, 0) + 1
        out.append((f"syntax/{case.file}/{case.group}/{numbers[key]:04d}", case))
    return out


def render_record(identifier: str, case: Case, divergence: dict[str, str] | None = None) -> str:
    divergence = divergence or {}
    cls = case.cls
    for engine in divergence:
        if cls == "impl-defined" and engine not in case.per_engine:
            raise GeneratorError(f"{identifier}: an impl-defined vector classified for {engine}, whose case states "
                                 "no per-engine expectation for it (SCHEMA.md section 5)")
    if divergence and cls == "normative":
        cls = "divergence"
    lines = [f"id: {identifier}", "area: syntax", f"op: {case.op}", "in: str:" + quote(case.source),
             f"out: {case.out}", f"class: {cls}"]
    for engine in sorted(case.per_engine):
        lines.append(f"per-engine.{engine}: {case.per_engine[engine]}")
    for engine in sorted(divergence):
        lines.append(f"divergence.{engine}: {divergence[engine]}")
    prov = [SPEC + c for c in case.clauses] + ["test262:" + t for t in case.tests]
    lines.append("prov: " + " ".join(dict.fromkeys(prov)))
    lines.append(f"script: {case.script}")
    lines.append("note: " + case.note)
    lines.append(f"since: {SINCE}")
    for line in lines:
        if any(ord(c) > 0x7E or ord(c) < 0x20 for c in line):
            raise GeneratorError(f"{identifier}: a field holds a character outside printable ASCII: {line!r}")
    return "\n".join(lines)


HEADER = [
    "Expected values: the specification's, ECMA-262 ES2026 at 0248456c, each vector citing the clause that decides it",
    "and, for a refusal, the test262 tests at ccaac100 that assert it; the explicit-resource-management rules, which",
    "ECMA-262 main merged after the pinned edition, cite their test262 tests alone. Every vector was checked against",
    "the reference Node.js v26.12.0 (syntaxcheck.mjs); the generator names each place where it disagrees.",
]


def build() -> list[Case]:
    CASES.clear()
    lexical()
    expressions()
    statements()
    functions_and_classes()
    scripts()
    annex_b()
    return list(CASES)


def write_files(vectors: Sequence[tuple[str, Case]], out: Path, divergences: dict[str, dict[str, str]]) -> dict[str, int]:
    divergences = dict(divergences)
    rendered: dict[str, list[str]] = {name: [] for name in FILES}
    for identifier, case in vectors:
        rendered[case.file].append(render_record(identifier, case, divergences.pop(identifier, None)))
    if divergences:
        raise GeneratorError(f"{DIVERGENCES.name} classifies ids the area does not have: {sorted(divergences)[:10]}")
    out.mkdir(parents=True, exist_ok=True)
    counts = {}
    for name, title in FILES.items():
        header = [f"Generated by scripts/conformance/{GENERATOR}; do not edit by hand.",
                  f"The syntax area: {title}.", *HEADER]
        text = "\n".join(f"# {line}".rstrip() for line in header)
        text += "\nschema: 1\n\n" + "\n\n".join(rendered[name]) + "\n"
        (out / f"{name}.vectors").write_text(text, encoding="utf-8", newline="\n")
        counts[name] = len(rendered[name])
    return counts


def generate(out: Path, spec: Path | None = None, test262: Path | None = None) -> dict[str, int]:
    cases = build()
    problems = check_rows(cases, syntax_rows())
    if spec is not None:
        problems += [f"a clause id the pinned text does not have: {c}" for c in check_clauses(cases, spec)]
    if test262 is not None:
        problems += [f"a test262 path the pinned revision does not have: {t}" for t in check_tests(cases, test262)]
    if problems:
        raise GeneratorError("\n".join(problems))
    vectors = identifiers(cases)
    return write_files(vectors, out, read_divergences(DIVERGENCES))


def main(argv: Sequence[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n", 1)[0])
    parser.add_argument("--out", default=str(DEFAULT_OUT))
    parser.add_argument("--check-node", help="the reference Node.js v26.12.0: check every vector against it and write "
                                             "nothing")
    parser.add_argument("--spec", help="ECMA-262's spec.html at 0248456c, to check every clause id")
    parser.add_argument("--test262", help="a test262 checkout at ccaac100, to check every test path")
    arguments = parser.parse_args(argv)
    try:
        if arguments.check_node:
            cases = build()
            problems = check_rows(cases, syntax_rows())
            if arguments.spec:
                problems += [f"a clause id the pinned text does not have: {c}"
                             for c in check_clauses(cases, Path(arguments.spec))]
            if arguments.test262:
                problems += [f"a test262 path the pinned revision does not have: {t}"
                             for t in check_tests(cases, Path(arguments.test262))]
            node_problems, found = check_node(arguments.check_node, identifiers(cases))
            for identifier in sorted(found):
                print(f"disagrees: {identifier}: {found[identifier]}")
            problems += node_problems
            if problems:
                raise GeneratorError("\n".join(problems))
            print(f"{len(cases)} vectors checked against the reference; {len(found)} disagreements, each named")
            return 0
        counts = generate(Path(arguments.out), Path(arguments.spec) if arguments.spec else None,
                          Path(arguments.test262) if arguments.test262 else None)
    except GeneratorError as error:
        print(f"{GENERATOR}: {error}", file=sys.stderr)
        return 2
    for name, count in counts.items():
        print(f"{name}.vectors: {count} vectors")
    return 0


if __name__ == "__main__":
    sys.exit(main())
