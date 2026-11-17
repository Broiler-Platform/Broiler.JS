#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Write the semantics area of the conformance corpus (corpus revisions 42 and 43, milestone SJL-15).

    python3 -I scripts/conformance/generate_semantics_vectors.py [--out conformance/vectors/semantics]
    python3 -I scripts/conformance/generate_semantics_vectors.py --check-node <Node v26.12.0>
        [--spec <ECMA-262 spec.html at 0248456c>] [--test262 <test262 checkout at ccaac100>]

THE AREA (decision D61, recorded in Broiler.VM's JSD-0088: SJL-15's `semantics` vectors follow the `syntax` vectors'
route, decision D25 of JSD-0083, with schema 1 unchanged). A vector pins one Script's outcome where a fact of the
static semantics decides it: an early error outside the parse, which the semantic model reports, or a completion value
that a fact of the model (the `arguments` need, a separate variable environment, direct-eval contamination, a dynamic
marker, an Annex B.3.3 alias, a jump target, a private name, a named-evaluation name) decides. `in` holds the Script's
source as one `str:` value and `script` holds the same source, a one-line ASCII Script the engine-level runners
evaluate. `op` says what decides the vector: `ScriptEvaluation`, the Script's own static semantics (a refusal is an early
error of the Script, so the Script runs none of its code), and `DirectEval`, the eval code a direct `eval` in the Script
evaluates, in the context the Script gives it (PerformEval's steps 7 and 11, and EvalDeclarationInstantiation's checks),
a refusal being the eval call's SyntaxError. The two names are the area's own: the `syntax` area's `ParseScript` and
`PerformEval` are refused outside `syntax/` (decision D-js-ops of Broiler.JS's docs/roadmap/SharedLibraries.md, section
"Shared static semantics (SJL-15)"). `out` is `error: SyntaxError` for a refusal, or the completion value. Eight files,
one per kind of fact: super, jumps, arguments, private-names, scopes, annex-b, named-evaluation and expressions.

THE ROWS. Every row of the stage table (Broiler.ECMAScript/docs/frontend/stages.tsv) whose stage is `semantics` and whose
input a Script or a direct eval in one can carry has at least one accepted and one refused vector, named in the vectors'
notes (``check_rows``). The rows only a module goal reaches, or no op of the area, or no host of either engine's
configuration, are listed in ``UNCOVERED`` with the reason, so the count of rows covered is checkable. Beside them, the
facts a completion value shows carry vectors of their own, each naming its row of the correspondence (Broiler.VM's
docs/evidence/sjl-15-001/correspondence.md; this engine's column in docs/evidence/sjl-15-001/correspondence-broiler-js.md):
FF3, FF4, FF5, RS2, AB1, LB1, PN1 and NE1. A refusal some engine may check only when a function's body is compiled is
also given in a form that runs code before that body: the construct inside a function the Script calls in a try
statement, where an early error refuses the whole Script before any of it runs (the rule's class T, decision D64's
second half). Revision 43 adds, for each row whose construct eval code may hold at its own top level and which a
compiler checks as it lowers, its refusal in eval code a direct eval evaluates, and three named-evaluation names in eval
code (``eval_code``), which bundle sjl-15-001's probes found Broiler.JS leaving unchecked or unnamed in eval code; each
vector's `since` is 43.

THE EXPECTED VALUES are the specification's, ECMA-262 ES2026 at 0248456c (sources/ecma262.pin), each vector citing the
clauses that decide it and, where one asserts the vector's outcome, the test262 tests at ccaac100 the catalogue names
for its row; an acceptance's completion value follows from the evaluation semantics, and its source is chosen so that the
value is plain. No expected value is read from an engine or from the reference. Every vector is checked against an
independent reference, the Node.js v26.12.0 bundle sjl-9-001's reference.txt names, by --check-node, through the
syntax area's syntaxcheck.mjs, which parses `in` as a Script and evaluates `script` after the prelude: a
`ScriptEvaluation` refusal must be a SyntaxError there at parse time, a `DirectEval` refusal must parse and then throw a
SyntaxError, and an accepted vector must complete there with the vector's `out`; where the reference disagrees with the
specification the vector follows the specification and ``NODE_DISAGREES`` names it, and the check fails on any
disagreement it does not name or any it names that does not occur.

THE CLASSES. `normative` unless the specification leaves the outcome to the host. Annex B's extensions are required of a
web-browser host and optional for any other, so a vector whose outcome holds only with B.3.2's block-level function
semantics or B.3.4's catch parameters is `impl-defined`, with each engine's `per-engine` expectation the extension's
outcome, since both engines' requests turn Annex B on for scripts (the same records the syntax area's impl-defined
vectors cite). Each engine's first run on revision 42, which adds the area, is classified at revision 43 in
scripts/conformance/semantics-divergences.txt, which this generator reads.

The generator is deterministic and reads nothing but this file, the stage table and semantics-divergences.txt: the same
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
DEFAULT_OUT = CORPUS / "semantics"
STAGES = REPOSITORY / "Broiler.ECMAScript" / "docs" / "frontend" / "stages.tsv"
DIVERGENCES = SCRIPTS / "semantics-divergences.txt"
CHECKER = SCRIPTS / "syntaxcheck.mjs"
SINCE = 42              # the corpus revision that added the area
EVAL_CODE = 43          # the corpus revision that added the eval-code refusals the probes of bundle sjl-15-001 asked for
CORPUS_REVISION = 43    # the corpus revision this generator writes: REVISIONS.md's last row
GENERATOR = "generate_semantics_vectors.py"
REFERENCE_VERSION = "v26.12.0"

SPEC = "spec:es2026@0248456c#"
SE = "error: SyntaxError"
OPS = ("ScriptEvaluation", "DirectEval")

FILES = {
    "super": "super properties and super calls: where a method, a constructor, a field initialiser, a static block or "
             "eval code may hold one (HasDirectSuper, Contains and PerformEval's steps 11.f and 11.g)",
    "jumps": "break and continue: their iteration statements and their label sets, across function and static "
             "block boundaries (ContainsUndefinedBreakTarget and ContainsUndefinedContinueTarget), and the jump "
             "targets of a label set (row LB1)",
    "arguments": "the arguments object: where it may not be named (ContainsArguments, PerformEval's step 11.h) and when "
                 "a function needs it (row FF3)",
    "private-names": "private names: AllPrivateIdentifiersValid in a Script and in eval code, and eval code's visible "
                     "private names and their timing (row PN1)",
    "scopes": "environments: a separate variable environment for the body (row FF4), direct-eval contamination (row "
              "FF5), with (row RS2), and EvalDeclarationInstantiation's conflicts",
    "annex-b": "Annex B's block-level functions (B.3.2): when a block function is also var-scoped, and what blocks it "
               "(row AB1)",
    "named-evaluation": "NamedEvaluation: which anonymous function definitions take a name, and which (row NE1)",
    "expressions": "the object literal's duplicate __proto__ and the for-in and for-of head's assignment target",
}

# ---- Records the impl-defined vectors cite --------------------------------------------------------------------------

ANNEX_B = ("Annex B is required of a web-browser host and optional for any other, so the outcome is the host's: both "
           "engines' requests turn it on for scripts (Broiler.VM's JSD-0083, decision D17, and its bundle sjl-13-001's "
           "correspondence, row R08, same; Broiler.JS's docs/roadmap/SharedLibraries.md, the frontend's syntax half, "
           "the request's switches), so each engine's expectation is the extension's outcome")


# ---- Typed values and scripts ------------------------------------------------------------------------------------


class GeneratorError(RuntimeError):
    """The generator cannot write the area, and says why."""


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


def typed(value) -> str:
    """A Python value as the schema's typed value (section 3)."""
    if value is None:
        return "undefined"
    if isinstance(value, bool):
        return "bool:true" if value else "bool:false"
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


def printable(text: str) -> bool:
    return all(0x20 <= ord(c) <= 0x7E for c in text)


# ---- Cases --------------------------------------------------------------------------------------------------------


@dataclass
class Case:
    """One vector."""
    file: str
    group: str
    op: str
    script: str
    out: str
    rows: tuple[str, ...]
    corr: tuple[str, ...]
    clauses: tuple[str, ...]
    tests: tuple[str, ...]
    note: str
    cls: str = "normative"
    per_engine: dict[str, str] = field(default_factory=dict)
    since: int = SINCE

    @property
    def refused(self) -> bool:
        return self.out == SE


CASES: list[Case] = []

# The correspondence row (Broiler.VM's docs/evidence/sjl-15-001/correspondence.md) that holds each catalogue row's early
# error, where it is not EE1, the early errors outside the parse: the private names' rows are PN1's, and
# EvalDeclarationInstantiation's conflicts between eval code's var names and the caller's lexical names DE3's.
ROW_CORRESPONDENCE = {"EE0167": "PN1", "EA0009": "PN1", "EA0006": "DE3", "EA0007": "DE3"}


def add(file: str, group: str, script: str, out: str, *, rows: Sequence[str] = (), corr: Sequence[str] = (),
        clauses: Sequence[str] = (), tests: Sequence[str] = (), note: str = "", op: str = "ScriptEvaluation",
        impl: str | None = None, since: int = SINCE) -> None:
    """One vector. `rows` are the catalogue rows it covers, `corr` the correspondence rows it shows; `impl` makes it
    impl-defined, each engine's expectation being `out`, with the reason given; `since` is the corpus revision that added
    it."""
    if file not in FILES:
        raise GeneratorError(f"no file {file}")
    if op not in OPS:
        raise GeneratorError(f"{group}: no op {op}")
    if not printable(script):
        raise GeneratorError(f"{group}: a script is one line of printable ASCII: {script!r}")
    if not clauses:
        raise GeneratorError(f"{group}: {script!r} cites no clause of the pinned edition")
    if not rows and not corr:
        raise GeneratorError(f"{group}: {script!r} names no row")
    if op == "DirectEval" and "eval(" not in script:
        raise GeneratorError(f"{group}: a DirectEval vector's script calls eval")
    if since not in (SINCE, EVAL_CODE):
        raise GeneratorError(f"{group}: a vector is added at revision {SINCE} or {EVAL_CODE}, not {since}")
    if rows and not corr:
        # A catalogue row's vector names the correspondence row that holds the row's early error.
        corr = tuple(dict.fromkeys(ROW_CORRESPONDENCE.get(row, "EE1") for row in rows))
    note_parts = []
    if rows:
        note_parts.append(("catalogue row " if len(rows) == 1 else "catalogue rows ") + ", ".join(rows))
    if corr:
        note_parts.append(("correspondence row " if len(corr) == 1 else "correspondence rows ") + ", ".join(corr))
    verdict = "refused" if out == SE else "accepted"
    text = f"{'; '.join(note_parts)}: {verdict}, {note}" if note else f"{'; '.join(note_parts)}: {verdict}"
    case = Case(file, group, op, script, out, tuple(rows), tuple(corr), tuple(clauses), tuple(tests), text, since=since)
    if impl is not None:
        case.cls = "impl-defined"
        case.per_engine = {"broiler-js": out, "broiler-vm": out}
        case.note += "; " + impl
    CASES.append(case)


def C(*ids: str) -> tuple[str, ...]:
    return tuple(ids)


def T(*paths: str) -> tuple[str, ...]:
    return tuple(paths)


def try_call(construct: str) -> str:
    """The construct inside a function the Script calls in a try statement, so that a refusal at the function's compile
    shows: an early error refuses the whole Script, and the Script's value is the catch's or "ran" otherwise."""
    return "var r = 'ran'; try { (function () { " + construct + " })(); } catch (e) { r = e.name; } r"


LAZY = "inside a function the Script calls in a try statement, which an early error refuses before any of it runs"

# Clause ids of the pinned edition used more than once.
FN_EE = "sec-function-definitions-static-semantics-early-errors"
GEN_EE = "sec-generator-function-definitions-static-semantics-early-errors"
AGEN_EE = "sec-async-generator-function-definitions-static-semantics-early-errors"
ASYNC_EE = "sec-async-function-definitions-static-semantics-early-errors"
CLASS_EE = "sec-class-definitions-static-semantics-early-errors"
OBJ_EE = "sec-object-initializer-static-semantics-early-errors"
SCRIPT_EE = "sec-scripts-static-semantics-early-errors"
HAS_SUPER = "sec-static-semantics-hasdirectsuper"
CONTAINS = "sec-static-semantics-contains"
SUPER = "sec-super-keyword"
PERFORM_EVAL = "sec-performeval"
EVAL_DI = "sec-evaldeclarationinstantiation"
FDI = "sec-functiondeclarationinstantiation"
GDI = "sec-globaldeclarationinstantiation"
CONTINUE_EE = "sec-continue-statement-static-semantics-early-errors"
BREAK_EE = "sec-break-statement-static-semantics-early-errors"
UNDEF_BREAK = "sec-static-semantics-containsundefinedbreaktarget"
UNDEF_CONTINUE = "sec-static-semantics-containsundefinedcontinuetarget"
LABELLED = "sec-runtime-semantics-labelledevaluation"
CONTAINS_ARGS = "sec-static-semantics-containsarguments"
PRIVATE_VALID = "sec-static-semantics-allprivateidentifiersvalid"
ANNEX_B_FN = "sec-block-level-function-declarations-web-legacy-compatibility-semantics"
NAMED = "sec-runtime-semantics-namedevaluation"
ANON = "sec-isanonymousfunctiondefinition"
ASSIGN = "sec-assignment-operators-runtime-semantics-evaluation"
VAR_STMT = "sec-variable-statement-runtime-semantics-evaluation"
STRICT = "sec-strict-mode-code"


def super_area() -> None:
    f = "super"
    add(f, "ee0015", "({ m() { return super.hasOwnProperty === Object.prototype.hasOwnProperty; } }).m()", V(True),
        rows=("EE0015",), clauses=C(OBJ_EE, SUPER), note="a super property in an object literal's method")
    add(f, "ee0015", "({ m() { super(); } });", SE, rows=("EE0015",), clauses=C(OBJ_EE, HAS_SUPER),
        tests=T("test/language/expressions/object/method-definition/early-errors-object-method-body-contains-super-call.js"),
        note="a super call in an object literal's method")
    add(f, "ee0015", try_call("({ m() { super(); } });"), SE, rows=("EE0015",), clauses=C(OBJ_EE, HAS_SUPER),
        note="a super call in an object literal's method, " + LAZY)
    add(f, "ee0074", "function f(a = { m() { return super.constructor === Object; } }.m()) { return a; } f()", V(True),
        rows=("EE0074",), clauses=C(FN_EE, CONTAINS), note="a super property in a method nested in a parameter is not "
                                                           "contained by the function's parameters")
    add(f, "ee0074", "function f(a = super.x) {}", SE, rows=("EE0074",), clauses=C(FN_EE),
        tests=T("test/language/statements/function/early-params-super-prop.js"),
        note="a super property in a function declaration's parameters")
    add(f, "ee0075", "function f() { return { m() { return super.constructor === Object; } }.m(); } f()", V(True),
        rows=("EE0075",), clauses=C(FN_EE, CONTAINS), note="a super property in a method nested in the body is not "
                                                           "contained by the function's body")
    add(f, "ee0075", "function f() { super.x; }", SE, rows=("EE0075",), clauses=C(FN_EE),
        tests=T("test/language/statements/function/early-body-super-prop.js"),
        note="a super property in a function declaration's body")
    add(f, "ee0076", "function f(a = class extends Object { constructor() { super(); this.v = 2; } }) { return new a().v; } f()",
        V(2), rows=("EE0076",), clauses=C(FN_EE, CONTAINS),
        note="a super call in a derived class nested in a parameter is not contained by the function's parameters")
    add(f, "ee0076", "function f(a = super()) {}", SE, rows=("EE0076",), clauses=C(FN_EE),
        tests=T("test/language/statements/function/early-params-super-call.js"),
        note="a super call in a function declaration's parameters")
    add(f, "ee0077", "function f() { class B {} class C extends B { constructor() { super(); this.v = 4; } } return new C().v; } f()",
        V(4), rows=("EE0077",), clauses=C(FN_EE, CONTAINS),
        note="a super call in a derived class nested in the body is not contained by the function's body")
    add(f, "ee0077", "function f() { super(); }", SE, rows=("EE0077",), clauses=C(FN_EE),
        tests=T("test/language/statements/function/early-body-super-call.js"),
        note="a super call in a function declaration's body")
    add(f, "ee0077", try_call("function g() { super(); }"), SE, rows=("EE0077",), clauses=C(FN_EE),
        note="a super call in a function declaration's body, " + LAZY)
    add(f, "ee0093", "({ *g() { yield super.constructor === Object; } }).g().next().value", V(True), rows=("EE0093",),
        clauses=C(GEN_EE, SUPER), note="a super property in a generator method")
    add(f, "ee0093", "({ *g() { super(); } });", SE, rows=("EE0093",), clauses=C(GEN_EE, HAS_SUPER),
        tests=T("test/language/expressions/object/method-definition/generator-super-call-body.js"),
        note="a super call in a generator method")
    add(f, "ee0102", "function* g(a = { m() { return super.constructor === Object; } }.m()) { yield a; } g().next().value",
        V(True), rows=("EE0102",), clauses=C(GEN_EE, CONTAINS),
        note="a super property in a method nested in a generator's parameters")
    add(f, "ee0102", "function* g(a = super.x) {}", SE, rows=("EE0102",), clauses=C(GEN_EE),
        note="a super property in a generator declaration's parameters")
    add(f, "ee0103", "function* g() { yield { m() { return super.constructor === Object; } }.m(); } g().next().value",
        V(True), rows=("EE0103",), clauses=C(GEN_EE, CONTAINS), note="a super property in a method nested in a "
                                                                     "generator's body")
    add(f, "ee0103", "function* g() { super.x; }", SE, rows=("EE0103",), clauses=C(GEN_EE),
        note="a super property in a generator declaration's body")
    add(f, "ee0104", "function* g(a = class extends Object { constructor() { super(); this.v = 6; } }) { yield new a().v; } g().next().value",
        V(6), rows=("EE0104",), clauses=C(GEN_EE, CONTAINS),
        note="a super call in a derived class nested in a generator's parameters")
    add(f, "ee0104", "function* g(a = super()) {}", SE, rows=("EE0104",), clauses=C(GEN_EE),
        note="a super call in a generator declaration's parameters")
    add(f, "ee0105", "function* g() { class B {} class C extends B { constructor() { super(); this.v = 5; } } yield new C().v; } g().next().value",
        V(5), rows=("EE0105",), clauses=C(GEN_EE, CONTAINS), note="a super call in a derived class nested in a "
                                                                  "generator's body")
    add(f, "ee0105", "function* g() { super(); }", SE, rows=("EE0105",), clauses=C(GEN_EE),
        note="a super call in a generator declaration's body")
    add(f, "ee0106", "typeof ({ async *g() { yield super.x; } }).g", V("function"), rows=("EE0106",),
        clauses=C(AGEN_EE, SUPER), note="a super property in an async generator method")
    add(f, "ee0106", "({ async *g() { super(); } });", SE, rows=("EE0106",), clauses=C(AGEN_EE, HAS_SUPER),
        note="a super call in an async generator method")
    add(f, "ee0117", "var g = async function* (a = { m() { return super.x; } }.m()) {}; typeof g", V("function"),
        rows=("EE0117",), clauses=C(AGEN_EE, CONTAINS), note="a super property in a method nested in an async "
                                                             "generator's parameters")
    add(f, "ee0117", "async function* g(a = super.x) {}", SE, rows=("EE0117",), clauses=C(AGEN_EE),
        tests=T("test/language/expressions/async-generator/early-errors-expression-formals-contains-super-property.js"),
        note="a super property in an async generator declaration's parameters")
    add(f, "ee0118", "var g = async function* () { return { m() { return super.x; } }; }; typeof g", V("function"),
        rows=("EE0118",), clauses=C(AGEN_EE, CONTAINS), note="a super property in a method nested in an async "
                                                             "generator's body")
    add(f, "ee0118", "async function* g() { super.x; }", SE, rows=("EE0118",), clauses=C(AGEN_EE),
        tests=T("test/language/expressions/async-generator/early-errors-expression-body-contains-super-property.js"),
        note="a super property in an async generator declaration's body")
    add(f, "ee0119", "var g = async function* (a = class extends Object { constructor() { super(); } }) {}; typeof g",
        V("function"), rows=("EE0119",), clauses=C(AGEN_EE, CONTAINS),
        note="a super call in a derived class nested in an async generator's parameters")
    add(f, "ee0119", "async function* g(a = super()) {}", SE, rows=("EE0119",), clauses=C(AGEN_EE),
        tests=T("test/language/expressions/async-generator/early-errors-expression-formals-contains-super-call.js"),
        note="a super call in an async generator declaration's parameters")
    add(f, "ee0120", "var g = async function* () { class B {} return class extends B { constructor() { super(); } }; }; typeof g",
        V("function"), rows=("EE0120",), clauses=C(AGEN_EE, CONTAINS),
        note="a super call in a derived class nested in an async generator's body")
    add(f, "ee0120", "async function* g() { super(); }", SE, rows=("EE0120",), clauses=C(AGEN_EE),
        tests=T("test/language/expressions/async-generator/early-errors-expression-body-contains-super-call.js"),
        note="a super call in an async generator declaration's body")
    add(f, "ee0121", "class B { constructor() { this.v = 3; } } class C extends B { constructor() { super(); } } new C().v",
        V(3), rows=("EE0121",), clauses=C(CLASS_EE, HAS_SUPER), note="a super call in a derived class's constructor")
    add(f, "ee0121", "class C { constructor() { super(); } }", SE, rows=("EE0121",), clauses=C(CLASS_EE, HAS_SUPER),
        tests=T("test/language/statements/class/elements/syntax/early-errors/grammar-ctor-super-no-heritage.js"),
        note="a super call in the constructor of a class with no heritage")
    add(f, "ee0124", "class C { m() { return super.constructor === Object; } } new C().m()", V(True), rows=("EE0124",),
        clauses=C(CLASS_EE, SUPER), note="a super property in a class's method")
    add(f, "ee0124", "class C { m() { super(); } }", SE, rows=("EE0124",), clauses=C(CLASS_EE, HAS_SUPER),
        tests=T("test/language/statements/class/definition/early-errors-class-method-body-contains-super-call.js"),
        note="a super call in a class's method")
    add(f, "ee0124", try_call("class C { m() { super(); } }"), SE, rows=("EE0124",), clauses=C(CLASS_EE, HAS_SUPER),
        note="a super call in a class's method, " + LAZY)
    add(f, "ee0126", "class C { static m() { return super.call === Function.prototype.call; } } C.m()", V(True),
        rows=("EE0126",), clauses=C(CLASS_EE, SUPER), note="a super property in a static method")
    add(f, "ee0126", "class C { static m() { super(); } }", SE, rows=("EE0126",), clauses=C(CLASS_EE, HAS_SUPER),
        tests=T("test/language/statements/class/elements/syntax/early-errors/grammar-static-meth-super.js"),
        note="a super call in a static method")
    add(f, "ee0131", "class C { x = super.constructor === Object; } new C().x", V(True), rows=("EE0131",),
        clauses=C(CLASS_EE, SUPER), note="a super property in a field initialiser")
    add(f, "ee0131", "class C { x = super(); }", SE, rows=("EE0131",), clauses=C(CLASS_EE, CONTAINS),
        tests=T("test/language/statements/class/elements/literal-name-init-err-contains-super.js"),
        note="a super call in a field initialiser")
    add(f, "ee0139", "var r; class C { static { r = super.call === Function.prototype.call; } } r", V(True),
        rows=("EE0139",), clauses=C(CLASS_EE, SUPER), note="a super property in a static block")
    add(f, "ee0139", "class C { static { super(); } }", SE, rows=("EE0139",), clauses=C(CLASS_EE, CONTAINS),
        tests=T("test/language/statements/class/static-init-invalid-super-call.js"),
        note="a super call in a static block")
    add(f, "ee0142", "typeof ({ async m() { return super.x; } }).m", V("function"), rows=("EE0142",),
        clauses=C(ASYNC_EE, SUPER), note="a super property in an async method")
    add(f, "ee0142", "({ async m() { super(); } });", SE, rows=("EE0142",), clauses=C(ASYNC_EE, HAS_SUPER),
        note="a super call in an async method")
    add(f, "ee0150", "var f = async function (a = { m() { return super.x; } }.m()) {}; typeof f", V("function"),
        rows=("EE0150",), clauses=C(ASYNC_EE, CONTAINS), note="a super property in a method nested in an async "
                                                              "function's parameters")
    add(f, "ee0150", "async function f(a = super.x) {}", SE, rows=("EE0150",), clauses=C(ASYNC_EE),
        tests=T("test/language/statements/async-function/early-errors-declaration-formals-contains-super-property.js"),
        note="a super property in an async function declaration's parameters")
    add(f, "ee0151", "var f = async function () { return { m() { return super.x; } }; }; typeof f", V("function"),
        rows=("EE0151",), clauses=C(ASYNC_EE, CONTAINS), note="a super property in a method nested in an async "
                                                              "function's body")
    add(f, "ee0151", "async function f() { super.x; }", SE, rows=("EE0151",), clauses=C(ASYNC_EE),
        tests=T("test/language/statements/async-function/early-errors-declaration-body-contains-super-property.js"),
        note="a super property in an async function declaration's body")
    add(f, "ee0152", "var f = async function (a = class extends Object { constructor() { super(); } }) {}; typeof f",
        V("function"), rows=("EE0152",), clauses=C(ASYNC_EE, CONTAINS),
        note="a super call in a derived class nested in an async function's parameters")
    add(f, "ee0152", "async function f(a = super()) {}", SE, rows=("EE0152",), clauses=C(ASYNC_EE),
        tests=T("test/language/statements/async-function/early-errors-declaration-formals-contains-super-call.js"),
        note="a super call in an async function declaration's parameters")
    add(f, "ee0153", "var f = async function () { class B {} return class extends B { constructor() { super(); } }; }; typeof f",
        V("function"), rows=("EE0153",), clauses=C(ASYNC_EE, CONTAINS),
        note="a super call in a derived class nested in an async function's body")
    add(f, "ee0153", "async function f() { super(); }", SE, rows=("EE0153",), clauses=C(ASYNC_EE),
        tests=T("test/language/statements/async-function/early-errors-declaration-body-contains-super-call.js"),
        note="a super call in an async function declaration's body")
    add(f, "ee0162", "super.x;", SE, rows=("EE0162",), clauses=C(SCRIPT_EE),
        tests=T("test/language/global-code/super-prop.js"), note="a super property at a Script's top level")
    add(f, "ee0162", "var f = () => super.x;", SE, rows=("EE0162",), clauses=C(SCRIPT_EE, CONTAINS),
        tests=T("test/language/global-code/super-prop-arrow.js"),
        note="a super property in an arrow at a Script's top level, which Contains looks into")
    add(f, "ee0162", "({ m() { return eval('super.constructor') === Object; } }).m()", V(True), op="DirectEval",
        rows=("EE0162", "EA0003"), clauses=C(SCRIPT_EE, PERFORM_EVAL),
        note="super in eval code a direct eval in a method processes")
    add(f, "ea0003", "({ m() { return (() => eval('super.constructor'))() === Object; } }).m()", V(True),
        op="DirectEval", rows=("EA0003",), clauses=C(PERFORM_EVAL, "sec-getthisenvironment"),
        note="a super property in eval code an arrow in a method evaluates")
    add(f, "ea0003", "eval('super.x')", SE, op="DirectEval", rows=("EA0003",), clauses=C(PERFORM_EVAL),
        tests=T("test/language/eval-code/direct/super-prop.js"),
        note="a super property in eval code at a Script's top level")
    add(f, "ea0003", "function f() { return eval('super.x'); } f()", SE, op="DirectEval", rows=("EA0003",),
        clauses=C(PERFORM_EVAL), note="a super property in eval code in a function that is no method")
    add(f, "ea0004", "class B { constructor() { this.v = 7; } } class C extends B { constructor() { eval('super()'); } } new C().v",
        V(7), op="DirectEval", rows=("EA0004",), clauses=C(PERFORM_EVAL),
        note="a super call in eval code in a derived constructor")
    add(f, "ea0004", "class B { constructor() { this.v = 8; } } class C extends B { constructor() { (() => eval('super()'))(); } } new C().v",
        V(8), op="DirectEval", rows=("EA0004",), clauses=C(PERFORM_EVAL, "sec-getthisenvironment"),
        note="a super call in eval code an arrow in a derived constructor evaluates")
    add(f, "ea0004", "({ m() { eval('super()'); } }).m()", SE, op="DirectEval", rows=("EA0004",),
        clauses=C(PERFORM_EVAL), tests=T("test/language/eval-code/direct/super-call-method.js"),
        note="a super call in eval code in a method")
    add(f, "ea0004", "class B {} class C extends B { m() { eval('super()'); } } new C().m()", SE, op="DirectEval",
        rows=("EA0004",), clauses=C(PERFORM_EVAL), note="a super call in eval code in a derived class's method")


def jumps() -> None:
    f = "jumps"
    add(f, "ee0058", "var n = 0; for (var i = 0; i < 3; i++) { if (i === 1) continue; n += i; } n", V(2),
        rows=("EE0058",), clauses=C(CONTINUE_EE, "sec-continue-statement-runtime-semantics-evaluation"),
        note="continue in an iteration statement")
    add(f, "ee0058", "continue;", SE, rows=("EE0058",), clauses=C(CONTINUE_EE),
        tests=T("test/language/statements/continue/S12.7_A1_T1.js"), note="continue at a Script's top level")
    add(f, "ee0058", "while (0) { (function () { continue; }); }", SE, rows=("EE0058",), clauses=C(CONTINUE_EE),
        note="continue in a function nested in a loop, which is a boundary")
    add(f, "ee0058", "for (;;) { class C { static { continue; } } }", SE, rows=("EE0058",), clauses=C(CONTINUE_EE),
        tests=T("test/language/statements/continue/static-init-without-label.js"),
        note="continue in a static block nested in a loop, which is a boundary")
    add(f, "ee0058", try_call("continue;"), SE, rows=("EE0058",), clauses=C(CONTINUE_EE),
        note="continue in a function body with no loop, " + LAZY)
    add(f, "ee0059", "var r = 0; switch (1) { case 1: r = 5; break; case 2: r = 6; } r", V(5), rows=("EE0059",),
        clauses=C(BREAK_EE, "sec-switch-statement"), note="break in a switch statement")
    add(f, "ee0059", "break;", SE, rows=("EE0059",), clauses=C(BREAK_EE),
        tests=T("test/language/statements/break/S12.8_A1_T1.js"), note="break at a Script's top level")
    add(f, "ee0059", "do { (function () { break; }); } while (0);", SE, rows=("EE0059",), clauses=C(BREAK_EE),
        note="break in a function nested in a loop, which is a boundary")
    add(f, "ee0059", "switch (0) { case 0: class C { static { break; } } }", SE, rows=("EE0059",), clauses=C(BREAK_EE),
        tests=T("test/language/statements/break/static-init-without-label.js"),
        note="break in a static block nested in a switch, which is a boundary")
    add(f, "ee0081", "function f() { L: { break L; } return 1; } f()", V(1), rows=("EE0081",),
        clauses=C(FN_EE, UNDEF_BREAK), note="break to a label of the function's own body")
    add(f, "ee0081", "L: { (function () { break L; }); }", SE, rows=("EE0081",), clauses=C(FN_EE, UNDEF_BREAK),
        tests=T("test/language/statements/break/S12.8_A6.js"), note="break to a label outside the function")
    add(f, "ee0082", "function f() { var n = 0; L: for (var i = 0; i < 3; i++) { n++; continue L; } return n; } f()",
        V(3), rows=("EE0082",), clauses=C(FN_EE, UNDEF_CONTINUE), note="continue to a loop's label in a function body")
    add(f, "ee0082", "L: do { (function () { continue L; }); } while (0);", SE, rows=("EE0082",),
        clauses=C(FN_EE, UNDEF_CONTINUE), tests=T("test/language/statements/continue/S12.7_A6.js"),
        note="continue to a loop's label outside the function")
    add(f, "ee0136", "var r = 0; class C { static { L: { r = 1; break L; } } } r", V(1), rows=("EE0136",),
        clauses=C(CLASS_EE, UNDEF_BREAK), note="break to a label of the static block's own")
    add(f, "ee0136", "L: { class C { static { break L; } } }", SE, rows=("EE0136",), clauses=C(CLASS_EE, UNDEF_BREAK),
        tests=T("test/language/statements/class/static-init-invalid-undefined-break-target.js"),
        note="break to a label outside the static block")
    add(f, "ee0137", "var n = 0; class C { static { L: for (var i = 0; i < 2; i++) { n++; continue L; } } } n", V(2),
        rows=("EE0137",), clauses=C(CLASS_EE, UNDEF_CONTINUE), note="continue to a loop's label in a static block")
    add(f, "ee0137", "L: do { class C { static { continue L; } } } while (0);", SE, rows=("EE0137",),
        clauses=C(CLASS_EE, UNDEF_CONTINUE),
        tests=T("test/language/statements/class/static-init-invalid-undefined-continue-target.js"),
        note="continue to a loop's label outside the static block")
    add(f, "ee0165", "var r = 0; L: { r = 2; break L; r = 3; } r", V(2), rows=("EE0165",),
        clauses=C(SCRIPT_EE, UNDEF_BREAK, LABELLED), note="break to a labelled block's label")
    add(f, "ee0165", "break L;", SE, rows=("EE0165",), clauses=C(SCRIPT_EE, UNDEF_BREAK),
        tests=T("test/language/statements/break/S12.8_A1_T2.js"), note="break to a label no statement has")
    add(f, "ee0165", "L: { } break L;", SE, rows=("EE0165",), clauses=C(SCRIPT_EE, UNDEF_BREAK),
        note="break to the label of a statement it is not in")
    add(f, "ee0166", "var n = 0; L: for (var i = 0; i < 3; i++) { if (i) continue L; n++; } n", V(1), rows=("EE0166",),
        clauses=C(SCRIPT_EE, UNDEF_CONTINUE), note="continue to a loop's label")
    add(f, "ee0166", "continue L;", SE, rows=("EE0166",), clauses=C(SCRIPT_EE, UNDEF_CONTINUE),
        tests=T("test/language/statements/continue/S12.7_A1_T2.js"), note="continue to a label no statement has")
    add(f, "ee0166", "L: { continue L; }", SE, rows=("EE0166",), clauses=C(SCRIPT_EE, UNDEF_CONTINUE),
        tests=T("test/language/statements/block/labeled-continue.js"),
        note="continue to a labelled block's label, which is no iteration statement")
    # Row LB1: a label set's jump targets (JSD-0083's hand-off H20).
    add(f, "lb1", "var n = 0; A: B: for (var i = 0; i < 2; i++) { n++; continue A; } n", V(2), rows=("EE0166",),
        corr=("LB1",), clauses=C(UNDEF_CONTINUE, LABELLED),
        note="continue to the outer label of a loop's label set, at a Script's top level")
    add(f, "lb1", "function f() { var n = 0; A: B: for (var i = 0; i < 3; i++) { n++; continue A; } return n; } f()",
        V(3), rows=("EE0082",), corr=("LB1",), clauses=C(UNDEF_CONTINUE, LABELLED),
        note="continue to the outer label of a loop's label set, in a function body")
    add(f, "lb1", "var n = 0; A: B: C: do { n++; if (n < 3) continue B; } while (n < 5); n", V(5), rows=("EE0166",),
        corr=("LB1",), clauses=C(UNDEF_CONTINUE, LABELLED),
        note="continue to the middle label of a do-while statement's label set")
    add(f, "lb1", "var s = ''; o: for (var i = 0; i < 2; i++) { for (var j = 0; j < 2; j++) { if (j) continue o; s += i + '' + j; } } s",
        V("0010"), rows=("EE0166",), corr=("LB1",), clauses=C(UNDEF_CONTINUE, LABELLED),
        note="continue to an outer loop's label")
    add(f, "lb1", "var n = 0; A: { B: for (var i = 0; i < 2; i++) { n++; continue B; } } n", V(2), rows=("EE0166",),
        corr=("LB1",), clauses=C(UNDEF_CONTINUE, LABELLED), note="continue to a loop's label inside a labelled block")


def arguments_area() -> None:
    f = "arguments"
    add(f, "ee0130", "class C { x = function () { return arguments.length; }; } new C().x(1, 2)", V(2),
        rows=("EE0130",), clauses=C(CLASS_EE, CONTAINS_ARGS),
        note="arguments in a function nested in a field initialiser, which ContainsArguments does not look into")
    add(f, "ee0130", "class C { x = arguments; }", SE, rows=("EE0130",), clauses=C(CLASS_EE, CONTAINS_ARGS),
        tests=T("test/language/statements/class/elements/literal-name-init-err-contains-arguments.js"),
        note="arguments in a field initialiser")
    add(f, "ee0130", "class C { x = () => arguments; }", SE, rows=("EE0130",), clauses=C(CLASS_EE, CONTAINS_ARGS),
        tests=T("test/language/statements/class/elements/arrow-fnc-init-err-contains-arguments.js"),
        note="arguments in an arrow in a field initialiser, which ContainsArguments looks into")
    add(f, "ee0130", try_call("class C { x = arguments; }"), SE, rows=("EE0130",), clauses=C(CLASS_EE, CONTAINS_ARGS),
        note="arguments in a field initialiser, " + LAZY)
    add(f, "ee0138", "var r; class C { static { r = function () { return arguments.length; }(1, 2, 3); } } r", V(3),
        rows=("EE0138",), clauses=C(CLASS_EE, CONTAINS_ARGS),
        note="arguments in a function nested in a static block, which ContainsArguments does not look into")
    add(f, "ee0138", "class C { static { arguments; } }", SE, rows=("EE0138",), clauses=C(CLASS_EE, CONTAINS_ARGS),
        tests=T("test/language/statements/class/static-init-invalid-arguments.js"),
        note="arguments in a static block")
    add(f, "ee0138", "class C { static { var f = () => arguments; } }", SE, rows=("EE0138",),
        clauses=C(CLASS_EE, CONTAINS_ARGS), note="arguments in an arrow in a static block")
    add(f, "ea0005", "class C { x = eval('(function () { return arguments.length; })(1, 2, 3)'); } new C().x", V(3),
        op="DirectEval", rows=("EA0005",), clauses=C(PERFORM_EVAL, CONTAINS_ARGS),
        note="arguments in a function the eval code of a field initialiser holds")
    add(f, "ea0005", "class C { x = eval('arguments'); } new C()", SE, op="DirectEval", rows=("EA0005",),
        clauses=C(PERFORM_EVAL, CONTAINS_ARGS),
        tests=T("test/language/statements/class/elements/direct-eval-err-contains-arguments.js"),
        note="arguments in the eval code of a field initialiser")
    add(f, "ea0005", "class C { x = () => eval('arguments'); } new C().x()", SE, op="DirectEval", rows=("EA0005",),
        clauses=C(PERFORM_EVAL, "sec-getthisenvironment"),
        note="arguments in eval code an arrow in a field initialiser evaluates")
    # Row FF3: the arguments need.
    ff3 = C(FDI, "sec-arguments-exotic-objects")
    add(f, "ff3", "function f(a) { arguments[0] = 2; return a; } f(1)", V(2), corr=("FF3",),
        clauses=C(FDI, "sec-createmappedargumentsobject"),
        note="a sloppy function with a simple parameter list maps arguments to its parameters")
    add(f, "ff3", "function f(a) { 'use strict'; arguments[0] = 2; return a; } f(1)", V(1), corr=("FF3",),
        clauses=C(FDI, "sec-createunmappedargumentsobject"), note="a strict function's arguments object is unmapped")
    add(f, "ff3", "function f() { return eval('arguments.length'); } f(1, 2)", V(2), op="DirectEval", corr=("FF3",),
        clauses=C(FDI, PERFORM_EVAL), note="a direct eval in a function reaches the function's arguments object")
    add(f, "ff3", "function f() { return (() => arguments[1])(); } f(1, 9)", V(9), corr=("FF3",),
        clauses=C(FDI, "sec-arrow-function-definitions-runtime-semantics-evaluation"),
        note="an arrow has no arguments binding and reads the enclosing function's")
    add(f, "ff3", "function f() { var e = eval; return e('typeof arguments'); } f(1)", V("undefined"), corr=("FF3",),
        clauses=C(FDI, PERFORM_EVAL), note="a mention of eval that is no direct call evaluates global code, which has "
                                           "no arguments binding")
    add(f, "ff3", "function f() { var arguments; return typeof arguments; } f()", V("object"), corr=("FF3",),
        clauses=ff3, note="a var named arguments starts with the arguments object")
    add(f, "ff3", "function f() { function arguments() { return 4; } return arguments(); } f()", V(4), corr=("FF3",),
        clauses=ff3, note="a function declaration named arguments means no arguments object is needed")
    add(f, "ff3", "function f() { let arguments = 6; return arguments; } f()", V(6), corr=("FF3",), clauses=ff3,
        note="a lexical declaration named arguments means no arguments object is needed")
    add(f, "ff3", "function f(arguments) { return arguments; } f(5)", V(5), corr=("FF3",), clauses=ff3,
        note="a parameter named arguments means no arguments object is needed")


def private_names() -> None:
    f = "private-names"
    add(f, "ee0167", "class C { #x = 1; m() { return this.#x; } } new C().m()", V(1), rows=("EE0167",),
        clauses=C(SCRIPT_EE, PRIVATE_VALID), note="a private name its class declares")
    add(f, "ee0167", "class C { #x = 2; m() { class D { n(o) { return o.#x; } } return new D().n(this); } } new C().m()",
        V(2), rows=("EE0167",), clauses=C(SCRIPT_EE, PRIVATE_VALID),
        note="a private name an enclosing class declares, used in a nested class")
    add(f, "ee0167", "this.#x;", SE, rows=("EE0167",), clauses=C(SCRIPT_EE, PRIVATE_VALID),
        tests=T("test/language/global-code/invalid-private-names-member-expression-this.js"),
        note="a private name outside any class")
    add(f, "ee0167", "class C { m() { return this.#y; } }", SE, rows=("EE0167",), clauses=C(SCRIPT_EE, PRIVATE_VALID),
        tests=T("test/language/statements/class/elements/privatename-not-valid-earlyerr-script-1.js"),
        note="a private name no enclosing class declares")
    add(f, "ee0167", "class C { #x; static m(o) { return #y in o; } }", SE, rows=("EE0167",),
        clauses=C(SCRIPT_EE, PRIVATE_VALID), note="an undeclared private name before in")
    add(f, "ee0167", try_call("class C { m() { return this.#y; } }"), SE, rows=("EE0167",),
        clauses=C(SCRIPT_EE, PRIVATE_VALID), note="a private name no enclosing class declares, " + LAZY)
    add(f, "ea0009", "class C { #x = 5; m() { return eval('this.#x'); } } new C().m()", V(5), op="DirectEval",
        rows=("EA0009",), clauses=C(PERFORM_EVAL, PRIVATE_VALID),
        note="a private name in eval code in a method of the class that declares it")
    add(f, "ea0009", "eval('this.#x')", SE, op="DirectEval", rows=("EA0009",), clauses=C(PERFORM_EVAL, PRIVATE_VALID),
        note="a private name in eval code outside any class")
    add(f, "ea0009", "class C { #x = 1; m() { return eval('this.#y'); } } new C().m()", SE, op="DirectEval",
        rows=("EA0009",), clauses=C(PERFORM_EVAL, PRIVATE_VALID),
        tests=T("test/language/statements/class/elements/privatename-not-valid-eval-earlyerr-1.js"),
        note="a private name in eval code that the class does not declare")
    # Row PN1: eval code's visible private names, and when an undeclared one is refused.
    add(f, "pn1", "class C { #x = 7; m() { return (() => eval('this.#x'))(); } } new C().m()", V(7), op="DirectEval",
        corr=("PN1",), clauses=C(PERFORM_EVAL, PRIVATE_VALID), note="a private name in eval code an arrow evaluates")
    add(f, "pn1", "class C { #m() { return 9; } n() { return eval('this.#m()'); } } new C().n()", V(9), op="DirectEval",
        corr=("PN1",), clauses=C(PERFORM_EVAL, PRIVATE_VALID), note="a private method in eval code")
    add(f, "pn1", "class C { static #s = 3; static t() { return eval('C.#s'); } } C.t()", V(3), op="DirectEval",
        corr=("PN1",), clauses=C(PERFORM_EVAL, PRIVATE_VALID), note="a static private field in eval code")
    add(f, "pn1", "var r = ''; class C { m() { try { eval(\"r += 'a'; this.#y\"); } catch (e) { r += e.name; } return r; } } new C().m()",
        V("SyntaxError"), op="DirectEval", corr=("PN1",), clauses=C(PERFORM_EVAL, PRIVATE_VALID),
        note="eval code with an undeclared private name is refused before any of it runs, in a class")
    add(f, "pn1", "var r = ''; try { eval(\"r += 'a'; this.#x\"); } catch (e) { r += e.name; } r", V("SyntaxError"),
        op="DirectEval", corr=("PN1",), clauses=C(PERFORM_EVAL, PRIVATE_VALID),
        note="eval code with a private name is refused before any of it runs, outside any class")


def scopes() -> None:
    f = "scopes"
    # Row FF4: a separate variable environment for the body.
    add(f, "ff4", "var x = 'outer'; function f(a = () => x) { var x = 'inner'; return a(); } f()", V("outer"),
        corr=("FF4",), clauses=C(FDI), note="a closure in the parameters does not see the body's var")
    add(f, "ff4", "function f(a, b = function () { return a; }) { var a = 2; return a + ',' + b(); } f(1)", V("2,1"),
        corr=("FF4",), clauses=C(FDI), note="the body's var of a parameter's name is a binding of its own")
    add(f, "ff4", "function f(a = 1) { var a; return a; } f()", V(1), corr=("FF4",), clauses=C(FDI),
        note="the body's var of a parameter's name starts with the parameter's value")
    add(f, "ff4", "function f(a, b) { var a; return a; } f(3)", V(3), corr=("FF4",), clauses=C(FDI),
        note="with no parameter expression the body's var is the parameter's binding")
    # Row FF5: direct-eval contamination.
    add(f, "ff5", "var x = 'outer'; function f() { eval('var x = \"inner\"'); return x; } f()", V("inner"),
        op="DirectEval", corr=("FF5",), clauses=C(PERFORM_EVAL, EVAL_DI),
        note="a sloppy direct eval declares its var in the function's variable environment")
    add(f, "ff5", "function f() { var x = 1; (0, eval)('var x = 2'); return x; } f()", V(1), corr=("FF5",),
        clauses=C(PERFORM_EVAL, EVAL_DI), note="an indirect eval declares its var in the global environment")
    add(f, "ff5", "function f() { var x = 1; var e = eval; e('var x = 2'); return x; } f()", V(1), corr=("FF5",),
        clauses=C(PERFORM_EVAL, EVAL_DI), note="a mention of eval that is no direct call evaluates global code")
    add(f, "ff5", "function f() { var x = 1; (() => eval('var x = 2'))(); return x; } f()", V(1), op="DirectEval",
        corr=("FF5",), clauses=C(PERFORM_EVAL, EVAL_DI),
        note="a direct eval in an arrow declares its var in the arrow's own variable environment")
    add(f, "ff5", "function f() { var x = 1; { eval('var x = 2'); } return x; } f()", V(2), op="DirectEval",
        corr=("FF5",), clauses=C(PERFORM_EVAL, EVAL_DI),
        note="a direct eval in a block declares its var in the function's variable environment")
    add(f, "ff5", "function f() { eval('function g() { return 3; }'); return g(); } f()", V(3), op="DirectEval",
        corr=("FF5",), clauses=C(PERFORM_EVAL, EVAL_DI), note="a direct eval declares a function in the function's "
                                                              "variable environment")
    add(f, "ff5", "function f() { 'use strict'; eval('var y = 1'); return typeof y; } f()", V("undefined"),
        op="DirectEval", corr=("FF5",), clauses=C(PERFORM_EVAL, EVAL_DI, STRICT),
        note="strict eval code has a variable environment of its own")
    add(f, "ff5", "var y = 0; function f() { eval('var y = 1'); } f(); y", V(0), op="DirectEval", corr=("FF5",),
        clauses=C(PERFORM_EVAL, EVAL_DI), note="a direct eval's var in a function does not reach the global one")
    # Row RS2: the with statement's dynamic marker.
    add(f, "rs2", "var x = 'g'; var o = { x: 'o' }; function f() { with (o) { return x; } } f()", V("o"),
        corr=("RS2",), clauses=C("sec-with-statement-runtime-semantics-evaluation", "sec-object-environment-records"),
        note="a with statement's object environment is searched first")
    add(f, "rs2", "var o = { x: 1 }; with (o) { var x = 2; } o.x", V(2), corr=("RS2",),
        clauses=C("sec-with-statement-runtime-semantics-evaluation", VAR_STMT),
        note="a var initialiser in a with body assigns through the object environment")
    add(f, "rs2", "function f(o) { with (o) { return typeof y; } } f({ y: 1 })", V("number"), corr=("RS2",),
        clauses=C("sec-with-statement-runtime-semantics-evaluation", "sec-object-environment-records"),
        note="a name only the with object binds")
    # EvalDeclarationInstantiation's conflicts.
    add(f, "ea0006", "let x = 1; eval('var y = 2;'); x + y", V(3), op="DirectEval", rows=("EA0006",),
        clauses=C(EVAL_DI), note="a var of eval code that no global lexical declaration names")
    add(f, "ea0006", "let x = 1; eval(\"'use strict'; var x = 2; x\") + x", V(3), op="DirectEval", rows=("EA0006",),
        clauses=C(EVAL_DI, STRICT), note="strict eval code's var of a global lexical name is its own")
    add(f, "ea0006", "let x; eval('var x;')", SE, op="DirectEval", rows=("EA0006",), clauses=C(EVAL_DI),
        tests=T("test/language/eval-code/direct/var-env-global-lex-non-strict.js"),
        note="a var of eval code against a global lexical declaration")
    add(f, "ea0006", "class x {} eval('function x() {}')", SE, op="DirectEval", rows=("EA0006",), clauses=C(EVAL_DI),
        note="a function of eval code against a global class declaration")
    add(f, "ea0007", "function f() { let x = 1; { eval('var y = 2;'); } return x + y; } f()", V(3), op="DirectEval",
        rows=("EA0007",), clauses=C(EVAL_DI), note="a var of eval code that no lexical declaration it hoists past names")
    add(f, "ea0007", "function f() { let x; { eval('var x;'); } } f()", SE, op="DirectEval", rows=("EA0007",),
        clauses=C(EVAL_DI), tests=T("test/language/eval-code/direct/var-env-lower-lex-non-strict.js"),
        note="a var of eval code against a function body's lexical declaration it hoists past")
    add(f, "ea0007", "{ let x; eval('var x;'); }", SE, op="DirectEval", rows=("EA0007",), clauses=C(EVAL_DI),
        note="a var of eval code against a block's lexical declaration")
    add(f, "ea0007", "var r; try { throw 1; } catch (x) { eval('var x = 2;'); r = x; } r", V(2), op="DirectEval",
        rows=("EA0007",), clauses=C(EVAL_DI, "sec-variablestatements-in-catch-blocks"),
        tests=T("test/annexB/language/eval-code/direct/var-env-lower-lex-catch-non-strict.js"),
        note="a var of eval code may name a catch parameter (B.3.4)", impl=ANNEX_B)


def annex_b() -> None:
    f = "annex-b"
    ab = C(ANNEX_B_FN, FDI)
    add(f, "ab1", "{ function g() { return 1; } } g()", V(1), corr=("AB1",), clauses=C(ANNEX_B_FN, GDI),
        note="a block function in global code is also var-scoped", impl=ANNEX_B)
    add(f, "ab1", "function f() { { function g() { return 5; } } return g(); } f()", V(5), corr=("AB1",), clauses=ab,
        note="a block function in a function body is also var-scoped", impl=ANNEX_B)
    add(f, "ab1", "function f() { var t = g; { function g() {} } return t; } f()", V(None), corr=("AB1",), clauses=ab,
        note="the var binding exists, undefined, before the block is evaluated", impl=ANNEX_B)
    add(f, "ab1", "eval('{ function g() { return 4; } }'); g()", V(4), op="DirectEval", corr=("AB1",),
        clauses=C(ANNEX_B_FN, EVAL_DI), note="a block function in eval code is also var-scoped", impl=ANNEX_B)
    add(f, "ab1", "function f() { { function arguments() {} } return typeof arguments; } f()", V("function"),
        corr=("AB1",), clauses=ab, note="no var binding is made for arguments, and the declaration's evaluation sets "
                                        "the arguments binding, as the pinned edition's FunctionDeclarationInstantiation "
                                        "reads (test262's annexB/language/function-code/"
                                        "block-decl-func-skip-arguments.js at ccaac100 asserts an earlier edition's "
                                        "reading, which keeps the arguments object, and is not cited)", impl=ANNEX_B)
    add(f, "ab1", "switch (1) { case 1: function g() { return 6; } } g()", V(6), corr=("AB1",),
        clauses=C(ANNEX_B_FN, GDI), note="a function in a switch case is also var-scoped", impl=ANNEX_B)
    add(f, "ab1", "function f() { { function g() { return 1; } } { function g() { return 2; } } return g(); } f()",
        V(2), corr=("AB1",), clauses=ab, note="each of two block functions of one name sets the var binding when its "
                                              "block is evaluated", impl=ANNEX_B)
    add(f, "ab1", "function f(g) { { function g() { return 3; } } return typeof g; } f(1)", V("number"),
        corr=("AB1",), clauses=ab, note="no alias for a parameter's name")
    add(f, "ab1", "function f() { { let g = 1; { function g() {} } } return typeof g; } f()", V("undefined"),
        corr=("AB1",), clauses=ab, note="no alias where the replacing var would conflict with a lexical declaration it "
                                        "hoists through")
    add(f, "ab1", "let g = 0; { function g() {} } typeof g", V("number"), corr=("AB1",), clauses=C(ANNEX_B_FN, GDI),
        note="no alias where the replacing var would conflict with a global lexical declaration")
    add(f, "ab1", "'use strict'; { function g() {} } typeof g", V("undefined"), corr=("AB1",),
        clauses=C(ANNEX_B_FN, STRICT), note="no alias in strict code")


def named_evaluation() -> None:
    f = "named-evaluation"
    ne = C(NAMED, ANON)
    add(f, "ne1", "var f = function () {}; f.name", V("f"), corr=("NE1",), clauses=C(NAMED, VAR_STMT),
        note="an anonymous function in a var initialiser")
    add(f, "ne1", "var g = () => 1; g.name", V("g"), corr=("NE1",), clauses=C(NAMED, VAR_STMT),
        note="an arrow in a var initialiser")
    add(f, "ne1", "let c = class {}; c.name", V("c"), corr=("NE1",), clauses=ne, note="an anonymous class in a let "
                                                                                     "initialiser")
    add(f, "ne1", "var o = { m: function () {} }; o.m.name", V("m"), corr=("NE1",),
        clauses=C(NAMED, "sec-runtime-semantics-propertydefinitionevaluation"),
        note="an anonymous function as a property's value")
    add(f, "ne1", "var h; h = function () {}; h.name", V("h"), corr=("NE1",), clauses=C(NAMED, ASSIGN),
        note="an anonymous function assigned to an identifier")
    add(f, "ne1", "var o = {}; o.q = function () {}; o.q.name", V(""), corr=("NE1",), clauses=C(ASSIGN),
        note="no NamedEvaluation for an assignment to a property")
    add(f, "ne1", "var a; a ??= function () {}; a.name", V("a"), corr=("NE1",), clauses=C(NAMED, ASSIGN),
        note="an anonymous function in a logical assignment")
    add(f, "ne1", "var b = 0; b ||= () => 1; b.name", V("b"), corr=("NE1",), clauses=C(NAMED, ASSIGN),
        note="an arrow in a logical assignment")
    add(f, "ne1", "var x = (function () {}); x.name", V("x"), corr=("NE1",), clauses=ne,
        note="an anonymous function through a parenthesised expression")
    add(f, "ne1", "var y = (0, function () {}); y.name", V(""), corr=("NE1",), clauses=C(ANON),
        note="a comma expression is no anonymous function definition")
    add(f, "ne1", "var z = function w() {}; z.name", V("w"), corr=("NE1",), clauses=C(ANON),
        note="a named function expression keeps its own name")
    add(f, "ne1", "var [k = function () {}] = []; k.name", V("k"), corr=("NE1",),
        clauses=C(NAMED, "sec-runtime-semantics-iteratorbindinginitialization"),
        note="an anonymous function as a binding element's initialiser")
    add(f, "ne1", "var { p = () => 0 } = {}; p.name", V("p"), corr=("NE1",),
        clauses=C(NAMED, "sec-runtime-semantics-keyedbindinginitialization"),
        note="an arrow as a binding property's initialiser")
    add(f, "ne1", "var o = { ['c' + 1]: function () {} }; o.c1.name", V("c1"), corr=("NE1",),
        clauses=C(NAMED, "sec-runtime-semantics-propertydefinitionevaluation"),
        note="an anonymous function as a computed property's value")
    add(f, "ne1", "var s = Symbol('d'); var o = { [s]: function () {} }; o[s].name", V("[d]"), corr=("NE1",),
        clauses=C(NAMED, "sec-setfunctionname"), note="a symbol key names a function by its description")
    add(f, "ne1", "function f(g = function () {}) { return g.name; } f()", V("g"), corr=("NE1",),
        clauses=C(NAMED, "sec-runtime-semantics-iteratorbindinginitialization"),
        note="an anonymous function as a parameter's initialiser")
    add(f, "ne1", "class C { static #m = function () {}; static n() { return C.#m.name; } } C.n()", V("#m"),
        corr=("NE1",), clauses=C(NAMED, "sec-runtime-semantics-classfielddefinitionevaluation"),
        note="an anonymous function as a private field's initialiser")
    add(f, "ne1", "class C { x = function () {}; } new C().x.name", V("x"), corr=("NE1",),
        clauses=C(NAMED, "sec-runtime-semantics-classfielddefinitionevaluation"),
        note="an anonymous function as a field's initialiser")


def expressions() -> None:
    f = "expressions"
    add(f, "ee0018", "var o = { __proto__: null, ['__proto__']: 1 }; Object.getPrototypeOf(o) === null && o.__proto__ === 1",
        V(True), rows=("EE0018",), clauses=C(OBJ_EE), note="a computed __proto__ key is no duplicate")
    add(f, "ee0018", "function f() { var __proto__ = 2; var o = { __proto__: null, __proto__ }; return o.__proto__; } f()",
        V(2), rows=("EE0018",), clauses=C(OBJ_EE), note="a shorthand __proto__ is no duplicate")
    add(f, "ee0018", "var a, b; ({ __proto__: a, __proto__: b } = { ['__proto__']: 1 }); a + b", V(2),
        rows=("EE0018",), clauses=C(OBJ_EE, "sec-static-semantics-assignmenttargettype"),
        note="the rule does not apply to an object literal that covers an assignment pattern")
    add(f, "ee0018", "({ __proto__: null, __proto__: null });", SE, rows=("EE0018",), clauses=C(OBJ_EE),
        tests=T("test/language/expressions/object/__proto__-duplicate.js"), note="two __proto__ properties")
    add(f, "ee0018", "({ __proto__: 1, '__proto__': 2 });", SE, rows=("EE0018",), clauses=C(OBJ_EE),
        note="two __proto__ properties, one of them a string literal's name")
    add(f, "ee0018", try_call("({ __proto__: 1, __proto__: 2 });"), SE, rows=("EE0018",), clauses=C(OBJ_EE),
        note="two __proto__ properties, " + LAZY)
    head = "sec-for-in-and-for-of-statements-static-semantics-early-errors"
    add(f, "ee0054", "var o = {}; for (o.p in { a: 1 }) ; o.p", V("a"), rows=("EE0054",),
        clauses=C(head, "sec-runtime-semantics-forin-div-ofbodyevaluation-lhs-stmt-iterator-lhskind-labelset"),
        note="a property reference as a for-in statement's target")
    add(f, "ee0054", "var a = []; for (a[0] of [5]) ; a[0]", V(5), rows=("EE0054",),
        clauses=C(head, "sec-runtime-semantics-forin-div-ofbodyevaluation-lhs-stmt-iterator-lhskind-labelset"),
        note="an element reference as a for-of statement's target")
    add(f, "ee0054", "for (this in {}) ;", SE, rows=("EE0054",), clauses=C(head, "sec-static-semantics-assignmenttargettype"),
        tests=T("test/language/statements/for-in/head-lhs-non-asnmt-trgt.js"), note="this as a for-in statement's target")
    add(f, "ee0054", "for ((a + b) of []) ;", SE, rows=("EE0054",),
        clauses=C(head, "sec-static-semantics-assignmenttargettype"),
        tests=T("test/language/statements/for-of/head-lhs-cover-non-asnmt-trgt.js"),
        note="a parenthesised sum as a for-of statement's target")


def eval_code() -> None:
    """Revision 43: the refusals of the rows whose construct a direct eval's eval code may hold at its own top level, in
    eval code. Eval code is parsed as a Script (PerformEval), so each is an early error of the eval code, and the eval
    call throws a SyntaxError before any of the eval code runs. Bundle sjl-15-001's probes (Broiler.JS) found the rows
    a compiler checks while it lowers left unchecked in eval code, where the Script's form of each is refused; these
    vectors carry that half."""
    note = "in eval code a direct eval at a Script's top level evaluates, which is parsed as a Script"
    rows = [
        ("super", "ee0015", "EE0015", OBJ_EE, "({ m() { super(); } });", "a super call in an object literal's method"),
        ("super", "ee0074", "EE0074", FN_EE, "function f(a = super.x) {}", "a super property in a function declaration's parameters"),
        ("super", "ee0075", "EE0075", FN_EE, "function f() { super.x; }", "a super property in a function declaration's body"),
        ("super", "ee0076", "EE0076", FN_EE, "function f(a = super()) {}", "a super call in a function declaration's parameters"),
        ("super", "ee0077", "EE0077", FN_EE, "function f() { super(); }", "a super call in a function declaration's body"),
        ("super", "ee0093", "EE0093", GEN_EE, "({ *g() { super(); } });", "a super call in a generator method"),
        ("super", "ee0102", "EE0102", GEN_EE, "function* g(a = super.x) {}", "a super property in a generator declaration's parameters"),
        ("super", "ee0103", "EE0103", GEN_EE, "function* g() { super.x; }", "a super property in a generator declaration's body"),
        ("super", "ee0104", "EE0104", GEN_EE, "function* g(a = super()) {}", "a super call in a generator declaration's parameters"),
        ("super", "ee0105", "EE0105", GEN_EE, "function* g() { super(); }", "a super call in a generator declaration's body"),
        ("super", "ee0106", "EE0106", AGEN_EE, "({ async *g() { super(); } });", "a super call in an async generator method"),
        ("super", "ee0117", "EE0117", AGEN_EE, "async function* g(a = super.x) {}", "a super property in an async generator declaration's parameters"),
        ("super", "ee0118", "EE0118", AGEN_EE, "async function* g() { super.x; }", "a super property in an async generator declaration's body"),
        ("super", "ee0119", "EE0119", AGEN_EE, "async function* g(a = super()) {}", "a super call in an async generator declaration's parameters"),
        ("super", "ee0120", "EE0120", AGEN_EE, "async function* g() { super(); }", "a super call in an async generator declaration's body"),
        ("super", "ee0121", "EE0121", CLASS_EE, "class C { constructor() { super(); } }", "a super call in the constructor of a class with no heritage"),
        ("super", "ee0124", "EE0124", CLASS_EE, "class C { m() { super(); } }", "a super call in a class's method"),
        ("super", "ee0126", "EE0126", CLASS_EE, "class C { static m() { super(); } }", "a super call in a static method"),
        ("super", "ee0131", "EE0131", CLASS_EE, "(function () { class C { x = super(); } });", "a super call in a field initialiser of a class in a function expression"),
        ("super", "ee0139", "EE0139", CLASS_EE, "class C { static { super(); } }", "a super call in a static block"),
        ("super", "ee0142", "EE0142", ASYNC_EE, "({ async m() { super(); } });", "a super call in an async method"),
        ("super", "ee0150", "EE0150", ASYNC_EE, "async function f(a = super.x) {}", "a super property in an async function declaration's parameters"),
        ("super", "ee0151", "EE0151", ASYNC_EE, "async function f() { super.x; }", "a super property in an async function declaration's body"),
        ("super", "ee0152", "EE0152", ASYNC_EE, "async function f(a = super()) {}", "a super call in an async function declaration's parameters"),
        ("super", "ee0153", "EE0153", ASYNC_EE, "async function f() { super(); }", "a super call in an async function declaration's body"),
        ("arguments", "ee0130", "EE0130", CLASS_EE, "class C { x = arguments; }", "arguments in a field initialiser"),
        ("arguments", "ee0138", "EE0138", CLASS_EE, "class C { static { arguments; } }", "arguments in a static block"),
        ("expressions", "ee0054", "EE0054", "sec-for-in-and-for-of-statements-static-semantics-early-errors", "for (this in {}) ;",
         "this as a for-in statement's target"),
    ]
    for file, group, row, clause, construct, what in rows:
        add(file, group, "eval('" + construct + "')", SE, op="DirectEval", rows=(row,), clauses=C(clause, PERFORM_EVAL),
            note=f"{what}, {note}", since=EVAL_CODE)
    # Row NE1 in eval code: the probes found anonymous functions in indirect-eval code given no name.
    names = [("var f = function () {}; f.name", "f", C(NAMED, VAR_STMT), "an anonymous function in a var initialiser"),
             ("var a; a ??= function () {}; a.name", "a", C(NAMED, ASSIGN), "an anonymous function in a logical assignment"),
             ("var x = (function () {}); x.name", "x", C(NAMED, ANON), "an anonymous function through a parenthesised "
                                                                        "expression")]
    for source, name, clauses, what in names:
        add("named-evaluation", "ne1", "eval('" + source + "')", V(name), op="DirectEval", corr=("NE1",),
            clauses=clauses + C(PERFORM_EVAL), note=f"{what}, in eval code a direct eval evaluates", since=EVAL_CODE)


# ---- Coverage -------------------------------------------------------------------------------------------------------

MODULE = "only the Module goal reaches it, which schema 1 has no field for (decision D61, as D25 for the syntax area)"

UNCOVERED: dict[str, tuple[str, str]] = {
    "EE0021": ("both", "a template with 2**32 or more template strings, which no one-line script can carry and the "
                       "runners' time limit could not evaluate"),
    **{row: ("both", MODULE) for row in ("EE0170", "EE0171", "EE0172", "EE0175", "EE0176", "EE0177")},
    "EA0008": ("both", "only a host that does not support B.3.4 reaches its step, and both engines' requests turn Annex B "
                       "on for scripts, so no host of either engine's configuration does"),
    "EP0017": ("both", "only ShadowRealm.prototype.evaluate's PerformShadowRealmEval reaches it, which no op of the area "
                       "names"),
    "EP0018": ("both", "only ShadowRealm.prototype.evaluate's PerformShadowRealmEval reaches it, which no op of the area "
                       "names"),
}

CORRESPONDENCE_ROWS = ("FF3", "FF4", "FF5", "RS2", "AB1", "LB1", "PN1", "NE1")
EARLY_ERROR_ROWS = ("EE1", "DE3")  # the correspondence rows a catalogue row's vector names (ROW_CORRESPONDENCE)


def semantics_rows(path: Path = STAGES) -> list[str]:
    with path.open(encoding="utf-8", newline="") as stream:
        return [row["id"] for row in csv.DictReader(stream, delimiter="\t") if row["stage"] == "semantics"]


def check_rows(cases: Sequence[Case], rows: Sequence[str]) -> list[str]:
    """Every semantics row has an accepted and a refused vector, or ``UNCOVERED`` names what it lacks and why; no case
    names a row that is not a semantics row; every fact of ``CORRESPONDENCE_ROWS`` has a vector."""
    problems = []
    accepted: dict[str, int] = {}
    refused: dict[str, int] = {}
    for case in cases:
        for row in case.rows:
            if row not in rows:
                problems.append(f"{case.group}: {row} is not a semantics row of the stage table")
            target = refused if case.refused else accepted
            target[row] = target.get(row, 0) + 1
    for row in UNCOVERED:
        if row not in rows:
            problems.append(f"UNCOVERED names {row}, which is not a semantics row")
    for row in rows:
        missing = UNCOVERED.get(row, ("none", ""))[0]
        want_accepted = missing not in ("accepted", "both")
        want_refused = missing not in ("refused", "both")
        if want_accepted != (row in accepted):
            problems.append(f"{row}: {'no' if want_accepted else 'an'} accepted vector, against UNCOVERED")
        if want_refused != (row in refused):
            problems.append(f"{row}: {'no' if want_refused else 'a'} refused vector, against UNCOVERED")
    shown = {c for case in cases for c in case.corr}
    problems += [f"correspondence row {c} has no vector" for c in CORRESPONDENCE_ROWS if c not in shown]
    problems += [f"a vector names correspondence row {c}, which the area does not show" for c in sorted(shown)
                 if c not in CORRESPONDENCE_ROWS + EARLY_ERROR_ROWS]
    return problems


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
NODE_DISAGREES: dict[str, str] = {}


def node_requests(vectors: Sequence[tuple[str, Case]]) -> list[dict[str, str]]:
    return [{"id": identifier, "op": case.op, "in": case.script, "script": case.script} for identifier, case in vectors]


def check_node(node: str, vectors: Sequence[tuple[str, Case]]) -> tuple[list[str], dict[str, str]]:
    """Runs every vector under the reference; answers the problems and each disagreement found."""
    try:
        version = subprocess.run([node, "--version"], capture_output=True, text=True, check=False).stdout.strip()
    except OSError:
        version = ""
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
            # A Script's early error refuses its parse; eval code's refusal is the eval call's SyntaxError.
            parse = "SyntaxError" if case.op == "ScriptEvaluation" else "accepted"
            if answer["parse"] != parse or answer["outcome"] != SE:
                found[identifier] = f"parse {answer['parse']}, outcome {answer['outcome']}"
        elif answer["parse"] != "accepted" or answer["outcome"] != case.out:
            found[identifier] = f"parse {answer['parse']}, outcome {answer['outcome']}"
    problems = [f"{i}: the reference answers {found[i]}, which NODE_DISAGREES does not name"
                for i in found if i not in NODE_DISAGREES]
    problems += [f"{i}: NODE_DISAGREES names it, but the reference agrees" for i in NODE_DISAGREES if i not in found]
    problems += [f"{i}: the reference answers {found[i]}, not what NODE_DISAGREES names, {NODE_DISAGREES[i]}"
                 for i in NODE_DISAGREES if i in found and found[i] != NODE_DISAGREES[i]]
    return problems, found


# ---- Classifications and writing -----------------------------------------------------------------------------------


def read_divergences(path: Path) -> dict[str, dict[str, str]]:
    """semantics-divergences.txt: `reason <key>: <text>` lines, then `<engine> <key> <id>` lines; by id, each engine's text."""
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
        out.append((f"semantics/{case.file}/{case.group}/{numbers[key]:04d}", case))
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
    lines = [f"id: {identifier}", "area: semantics", f"op: {case.op}", "in: str:" + quote(case.script),
             f"out: {case.out}", f"class: {cls}"]
    for engine in sorted(case.per_engine):
        lines.append(f"per-engine.{engine}: {case.per_engine[engine]}")
    for engine in sorted(divergence):
        lines.append(f"divergence.{engine}: {divergence[engine]}")
    prov = [SPEC + c for c in case.clauses] + ["test262:" + t for t in case.tests]
    lines.append("prov: " + " ".join(dict.fromkeys(prov)))
    lines.append(f"script: {case.script}")
    lines.append("note: " + case.note)
    lines.append(f"since: {case.since}")
    for line in lines:
        if any(ord(c) > 0x7E or ord(c) < 0x20 for c in line):
            raise GeneratorError(f"{identifier}: a field holds a character outside printable ASCII: {line!r}")
    return "\n".join(lines)


HEADER = [
    "Expected values: the specification's, ECMA-262 ES2026 at 0248456c, each vector citing the clauses that decide it",
    "and, where one asserts the vector's outcome, the test262 tests at ccaac100 the catalogue names for its row. Every",
    "vector was checked against the reference Node.js v26.12.0 (syntaxcheck.mjs); the generator names each place where",
    "it disagrees.",
]


def build() -> list[Case]:
    CASES.clear()
    super_area()
    jumps()
    arguments_area()
    private_names()
    scopes()
    annex_b()
    named_evaluation()
    expressions()
    eval_code()
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
                  f"The semantics area: {title}.", *HEADER]
        text = "\n".join(f"# {line}".rstrip() for line in header)
        text += "\nschema: 1\n\n" + "\n\n".join(rendered[name]) + "\n"
        (out / f"{name}.vectors").write_text(text, encoding="utf-8", newline="\n")
        counts[name] = len(rendered[name])
    return counts


def generate(out: Path, spec: Path | None = None, test262: Path | None = None) -> dict[str, int]:
    cases = build()
    problems = check_rows(cases, semantics_rows())
    scripts = [case.script for case in cases]
    problems += [f"a script is vectored twice: {s}" for s in sorted({s for s in scripts if scripts.count(s) > 1})]
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
            problems = check_rows(cases, semantics_rows())
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
