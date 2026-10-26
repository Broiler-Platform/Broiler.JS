#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Classify the parse corpus at the shared front end's syntax half (milestone SJL-13.1, task 1.2).

    python3 -I scripts/frontend/classify_parse_corpus.py write --test262 <archive or checkout> \
        --vm-baseline <parse-corpus-after-run1.tsv.gz> --correspondence <correspondence-runs/analysis>
    python3 -I scripts/frontend/classify_parse_corpus.py check  (the same three options)

`write` writes docs/evidence/sjl-13-001/classification/ (the README beside the output says what each file holds);
`check` builds the same files in memory and compares them with the committed ones byte for byte. Both run from the
repository root. Exit codes: 0 done (or `check` identical), 1 `check` found a difference, 2 an input was refused or
an input of the corpus has no expectation (nothing is written).

WHAT IT WRITES. For every input of the parse corpus (conformance/parse/corpus.manifest), under each of its variants
and in each of the two configurations of decision D26 (V, Broiler.VM's request; J, Broiler.JS's request), and for
every eval-family input also as eval code under the request-flag sets E0 to E7 in configuration V, one line: the
expected outcome at the syntax half (`accepted`, `refused`, or `either` for an input the syntax half leaves to
SJL-15's model), for a refusal the catalogue rows whose shared codes may come first, the reason (a code of
REASONS below) and its basis (catalogue rows, rows of SJL-13.1's correspondence, and baseline facts).

THE INPUTS, each checked against the SHA-256 below before anything is read from it:
  - this repository: the corpus manifest; the early-error catalogue (Broiler.ECMAScript/docs/frontend/
    early-errors.tsv); the stage table beside it (stages.tsv: its digest is recorded in the output, not pinned
    here); the catalogue's table of test262's parse-negative tests (docs/evidence/sjl-12-001/catalogue/
    parse-negative.tsv); and Broiler.JS's SJL-12 after baseline (docs/evidence/sjl-12-001/after/
    parse-corpus-c.tsv.gz: entry P, the parser alone, and entry K, CoreScript.Compile);
  - test262 at the corpus's pin, from Broiler.VM's archive of it or a checkout at the pin, read for each test's
    front matter (negative, flags, features), which must agree with the manifest;
  - Broiler.VM's SJL-12 after baseline (its bundle sjl-12-001, after/parse-corpus-after-run1.tsv.gz: sub-population
    B1 in the bytecode form, and B3, eval code under E0 to E7);
  - the corpus join of SJL-13.1's correspondence (Broiler.VM's bundle sjl-13-001, correspondence-runs/analysis/:
    rows-P-baseline.tsv.gz, every input whose outcome differs between Broiler.VM's B1 and Broiler.JS's P or K, with
    the correspondence row that resolves it; rows-P-agree.tsv.gz, every test262 positive all three refuse).

HOW AN EXPECTATION IS MADE (the README states the same in prose):
  1. A test262 test whose front matter says `negative: phase: parse`: the catalogue rows that explain it
     (parse-negative.tsv: the rows of each engine's first refusing site and the rows whose test262 cell names it;
     EG0001 for the module variant only). If a row the stage table assigns `syntax` refuses in the configuration
     (a proposal row only with its flag off; a regular-expression row only where that configuration's validator
     refused, decision D7 and the correspondence's F29(c)), the input is expected refused, with those rows first.
     If no such row refuses and a row is `semantics`, the input is `either`. If the only rows are a proposal's or a
     validator's that the configuration admits, it is expected accepted.
  2. Every other input that both engines' baselines treat alike (Broiler.VM's B1, Broiler.JS's P and K) is
     expected as they treat it: accepted; or, where both parsers refuse it, refused with the rows of Broiler.VM's
     first code (the catalogue's vm_code) that the configuration's syntax half makes.
  3. An input the engines' baselines treat differently has a row of the correspondence's join, and that row
     decides (BUCKETS below; a `D:stage-E` input of no test262 negative by the stage of the catalogue row its
     compile-stage message names, COMPILE_MESSAGES below).
  4. A `json` row is read under the Script goal, as SJL-12's tool read it (the correspondence's P04): the syntax
     half has no JSON goal, and Broiler.JS's entry P is the Script grammar's answer.
  5. An eval request takes Broiler.VM's B3 answer at the syntax half: accepted if B3 accepted it; if B3 refused it,
     the expectation of the reading (a B1 variant or an earlier flag set) whose first diagnostic is the same, else
     the stage that emits Broiler.VM's code (the code tables below): a parser-only code is refused, a binder-only
     code is either, and a strict-mode parser code is refused when the strict reading's sloppy twin accepts the
     text or refuses it later in the text.

Every mode is deterministic: no clock, no locale, no hash order and no file-system order reaches the output; the
compressed table is written with a zero time stamp and no file name.
"""

from __future__ import annotations

import argparse
import gzip
import hashlib
import io
import os
import re
import subprocess
import sys
import tarfile
from dataclasses import dataclass
from pathlib import Path, PurePosixPath

ROOT = Path(__file__).resolve().parents[2]

MANIFEST = "conformance/parse/corpus.manifest"
CATALOGUE = "Broiler.ECMAScript/docs/frontend/early-errors.tsv"
STAGES = "Broiler.ECMAScript/docs/frontend/stages.tsv"
PARSE_NEGATIVE = "docs/evidence/sjl-12-001/catalogue/parse-negative.tsv"
JS_BASELINE = "docs/evidence/sjl-12-001/after/parse-corpus-c.tsv.gz"
OUT = "docs/evidence/sjl-13-001/classification"

TEST262_REVISION = "ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e"
PINS = {
    MANIFEST: "1c5342104766f14d0c17d950537e4ed078a0c438c3652f8c65bff8c6c347e9cc",
    CATALOGUE: "7a9109b0db685df98a7327cd95b9337cc538337f834fe4f22692b249058aa4f1",
    PARSE_NEGATIVE: "88f8598ca201864d728cb7dafb521aa373e74e3bf28bc2e5c4139f463cea83c9",
    JS_BASELINE: "a0da21b223a85333d19e655aef32321a815e9eb597a5f8bf7483c9b3e4c19c07",
    "test262": "f58ce79141529c9fa33592e22ff3ff0d83b83830ac8e372ecd32e1623db1c040",
    "vm-baseline": "cc81ab4ffd98e4b61866722c25dbdc4b9c5eb8ec1de02f68c0429a178b5bd4b8",
    "rows-P-baseline.tsv.gz": "eacc28023c03168f1ecf7f381ed2cb441a2aba92c9151cd1e749e2bdb3bb9140",
    "rows-P-agree.tsv.gz": "abfc47be51650df27f1e90903c99b5179cf856b9a6dac0dca1612f02ede7448e",
}
NAMES = {
    "test262": f"test262 at {TEST262_REVISION} (Broiler.VM src/tests/conformance/pins/test262-{TEST262_REVISION}.tar.gz)",
    "vm-baseline": "Broiler.VM docs/evidence/sjl-12-001/after/parse-corpus-after-run1.tsv.gz",
    "rows-P-baseline.tsv.gz": "Broiler.VM docs/evidence/sjl-13-001/correspondence-runs/analysis/rows-P-baseline.tsv.gz",
    "rows-P-agree.tsv.gz": "Broiler.VM docs/evidence/sjl-13-001/correspondence-runs/analysis/rows-P-agree.tsv.gz",
}

STRICT_PROLOGUE = '"use strict";\n'
EVAL_SETS = ("E0", "E1", "E2", "E3", "E4", "E5", "E6", "E7")
STRICT_SETS = ("E1", "E3")

# The two configurations of decision D26. A proposal flag is named as the stage table's `switch` column names it.
CONFIGS = ("V", "J")
PROPOSALS_ON = {
    "V": frozenset(),
    "J": frozenset({
        "proposal:decorators",
        "proposal:decorators-accessor",
        "proposal:import-source-call",
        "proposal:import-defer-call",
    }),
}

# Broiler.VM's codes by the stage that emits them at the SJL-13.1 head (the correspondence's
# analysis/jsparser-refusals.tsv, binder-refusals.tsv and lowering-refusals.tsv).
PARSER_ONLY_CODES = frozenset({"2001", "2002", "2003", "2004", "2005", "2006", "2007", "2102", "2103", "2210", "2401"})
BINDER_ONLY_CODES = frozenset({"2206", "2207", "2208", "2403"})
ENGINE_CODES = {"2301": "EX0014", "2302": "EX0015", "2303": "EX0016", "2405": "EX0021", "2406": "EX0022"}
# Codes the parser reports under strict mode only. 2205's strictness-dependent binder checks are B07 (syntax rows)
# and B21 (EE0054); no eval request of the corpus reaches B21.
STRICT_ONLY_PARSER_CODES = frozenset({"2101", "2201", "2209", "2210", "2205"})

REASONS = (
    ("test262", "the test's front matter: a parse-negative test refused by the syntax rows of the catalogue that explain it, or a test that is not parse-negative and that both engines' baselines accept"),
    ("semantics-only", "every catalogue row that refuses the input is one the stage table assigns `semantics` (SJL-15's model): either outcome is admitted"),
    ("engines", "an input of Broiler.VM's or Broiler.JS's corpus that both engines' baselines accept, or that both parsers refuse (the rows of Broiler.VM's first code)"),
    ("stage", "both engines refuse it, one only after its parse: the stage of the catalogue row decides (`syntax`: refused; `semantics`: either)"),
    ("correction", "a row of the correspondence corrects one engine (or both); the expectation is the specification's"),
    ("proposal", "a proposal's construct: refused where the configuration's flag for its form is off (decision D17, finding F16)"),
    ("provider", "a regular-expression literal the configuration's validator decides (decision D7; the correspondence's R18 to R20 and F29(c))"),
    ("limit", "the contract's nesting or tree-depth limit (decision D14): refused at Broiler.VM's bound in V; in J the bound is the library's supported maximum, and a refusal past it is part A's outcome L"),
    ("engine-row", "an engine refuses it by an `engine` row of the stage table (a format ceiling, an import attribute the host cannot honour, a JSON module's content), which the syntax half never reports"),
    ("module-graph", "Broiler.VM's B1 compiles a module with the graph the conformance root reaches, so a neighbour's refusal refuses it; its own text is accepted at the syntax half (the correspondence's P34)"),
    ("tool", "a difference of the SJL-12 tool's entry K, not of the syntax half: K has no module record (P36) and no top-level await (R04)"),
    ("fixture-error", "a test262 fixture that holds a deliberate error, so the manifest's positive does not apply (the correspondence's P35)"),
    ("json-as-script", "a JSON input, read under the Script goal as SJL-12's tool read it (the correspondence's P04): the syntax half has no JSON goal"),
    ("eval-vm", "an eval request Broiler.VM's B3 accepts"),
    ("eval-as", "an eval request whose first diagnostic in B3 is that of another reading of the same text; that reading's expectation"),
    ("eval-stage", "an eval request whose first diagnostic in B3 no other reading has: the stage Broiler.VM's code shows"),
)
REASON_CODES = {code for code, _ in REASONS}

# The correspondence's join: every bucket it names, the correspondence rows that hold it (for a test262 parse-negative
# test, and for every other input), and how an input that is no test262 parse-negative test is resolved. A test262
# parse-negative test is expected by its catalogue rows (rule 1), whatever its bucket, which adds its rows to the basis.
D_BUCKETS = {
    "(EE0003)": "D39",
    "(EE0006;EE0007;EE0008;EE0009)": "D37",
    "(EE0006;EE0007;EE0008;EE0009;EE0087;EE0155;EP0003;EX0008;EX0009)": "D37",
    "(EE0006;EE0007;EE0008;EE0009;EE0087;EE0155;EX0008)": "D37",
    "(EE0006;EE0007;EE0140;EX0008;EX0023)": "D37",
    "(EE0006;EE0007;EX0008)": "D37",
    "(EE0010;EE0011;EE0012;EE0013;EE0014)": "D33",
    "(EE0012)": "D37",
    "(EE0028;EE0029;EG0008)": "D39",
    "(EE0030;EE0031)": "D34",
    "(EE0033;EE0034;EE0035;EE0039;EG0008)": "D39",
    "(EE0036;EE0039)": "D39",
    "(EE0040;EE0062;EE0078;EE0133;EE0160;EE0168;EG0004)": "D35",
    "(EE0041;EE0063;EE0079;EE0134;EE0161;EE0168;EE0169)": "D35",
    "(EE0042;EE0055;EP0001;EP0004)": "D39",
    "(EE0045;EE0046;EE0047;EE0048;EE0049;EE0050;EE0052;EE0060;EE0061)": "D36",
    "(EE0051;EE0056)": "D35",
    "(EE0054;EG0008)": "D39",
    "(EE0064;EG0003;EG0005;EX0008)": "D36",
    "(EE0066)": "D35",
    "(EE0073;EE0086;EE0089;EE0092;EE0096;EE0100;EE0110;EE0114;EE0144;EE0149;EE0154;EE0158)": "D39",
    "(EE0083;EE0094;EE0101;EE0107;EE0115;EE0156)": "D38",
    "(EE0084;EE0108;EE0116;EE0143;EE0146;EE0157)": "D38",
    "(EE0130;EE0138)": "D39",
    "(EE0132)": "D39",
    "(EX0025)": "D39",
    "(strict reserved word or static-block await)": "D37",
    "(super property in a non-method function)": "D41",
}
BUCKETS = {
    # bucket: (rows for a test262 parse-negative test, rows for any other input, resolution of any other input)
    "P02": ((), ("P02",), "correction: Broiler.JS refuses `using` and `await using` at a module's top level, which the Module goal admits; accepted"),
    "P04": ((), ("P04",), "json-as-script: read under the Script goal"),
    "P05": ((), ("P05b",), "engine-row EX0021: Broiler.VM's lowering refuses an import attribute it cannot honour; accepted"),
    "P06a": (("P06a",), ("P06a",), "proposal: the import phases' call forms; refused in V, accepted in J"),
    "P07": (("P07",), (), "-"),
    "P09": (("P09",), (), "-"),
    "P10": (("P10",), (), "-"),
    "P11": (("P11",), (), "-"),
    "P12": (("P12",), (), "-"),
    "P13": ((), ("P13",), "correction: `~` is unary only; refused"),
    "P14": (("P14",), (), "-"),
    "P15": (("P15a",), ("P15b", "D49"), "correction: a parenthesised nested destructuring target that is not simple; refused"),
    "P18": (("P18",), (), "-"),
    "P19": ((), ("P19",), "proposal: decorators; refused in V, accepted in J"),
    "P20": ((), ("P20",), "proposal: auto-accessors; refused in V, accepted in J"),
    "P21": (("P21",), (), "-"),
    "P22": (("P22",), (), "-"),
    "P23": (("P23",), (), "-"),
    "P24": (("P24",), (), "-"),
    "P34": ((), ("P34",), "module-graph: accepted"),
    "P36": ((), ("P36",), "tool: entry K has no module record; accepted"),
    "R:engine-kept-format-ceiling": ((), ("R34",), "engine-row: a format ceiling of Broiler.VM's lowering; accepted"),
    "R:nesting-and-tree-depth": ((), ("R12", "R13"), "limit: refused in V; accepted in J, where a refusal past the bound is outcome L"),
    "R:regexp-validator": (("R18", "R20"), ("R18",), "provider: each configuration takes its engine's validator (R19, or R20)"),
    "R:top-level-await-at-K": ((), ("R04",), "tool: entry K has no top-level await; accepted"),
    "D:stage-E (JS checks it in its compiler)": (("D31",), ("D31",), "stage: the catalogue row Broiler.JS's compile-stage message names; refused if `syntax`, either if `semantics`"),
    "D:JS over-refuses (duplicate parameters, sloppy async or generator)": ((), ("D42",), "correction: sloppy accepted; the strict variant refused by the duplicate-parameter rows"),
}
for _label, _row in D_BUCKETS.items():
    _bucket = "D:early-error JS misses " + _label
    if _label == "(strict reserved word or static-block await)":
        BUCKETS[_bucket] = ((), (_row,), "correction: refused by the rows of Broiler.VM's code")
    elif _label == "(super property in a non-method function)":
        BUCKETS[_bucket] = ((), (_row,), "semantics-only: the super-property rows are `semantics`")
    else:
        BUCKETS[_bucket] = ((_row,), (), "-")

# Broiler.JS's compile-stage messages (entry K of the SJL-12 after baseline) read as the catalogue rows whose
# js_site throws them; used only for a `D:stage-E` input that is no test262 parse-negative test.
COMPILE_MESSAGES = (
    (r"keyword is only valid inside a derived class constructor", "super.call.outside-derived-constructor"),
    (r"keyword is only valid inside a method or constructor", "super.property.outside-method super.outside-method"),
    (r"^Assignment to eval or arguments is not allowed in strict mode", "assignment.target.invalid"),
    (r"^Cannot use 'import\.meta' outside a module", "meta.import-meta.outside-module"),
    (r"^Delete of an unqualified identifier in strict mode", "strict.delete.unqualified-or-private"),
    (r"^Duplicate export name", "module.export.duplicate"),
    (r"^Duplicate parameter name not allowed in this context", "scope.parameter.duplicate"),
    (r"^Export '.*' is not defined in module", "module.export.undeclared"),
    (r"^Getter must not have any formal parameters", "syntax.token.unexpected syntax.token.missing"),
    (r"^Setter must have exactly one formal parameter", "syntax.token.unexpected syntax.token.missing"),
    (r"^Setter function argument must not be a rest parameter", "syntax.token.unexpected syntax.token.missing"),
    (r"^Identifier '.*' has already been declared", "scope.lexical.duplicate scope.lexical.var-collision"),
    (r"^Illegal break statement", "jump.break.no-target jump.break.undefined-label"),
    (r"^Illegal continue statement", "jump.continue.no-iteration jump.continue.undefined-label"),
    (r"^In strict mode code, functions can only be declared at top level or inside a block", "strict.labelled-function annexb.if-function.strict"),
    (r"^Invalid catch parameter name in strict mode", "strict.binding.eval-or-arguments"),
    (r"^Invalid declaration in strict mode", "strict.binding.eval-or-arguments strict.reserved-word"),
    (r"^Invalid function name in strict mode", "strict.binding.eval-or-arguments"),
    (r"^Invalid label name in strict mode", "strict.reserved-word"),
    (r"^Invalid left-hand side expression for update", "assignment.target.invalid"),
    (r"^Invalid left-hand side in assignment", "assignment.target.invalid assignment.target.not-simple"),
    (r"^Invalid parameter name in strict mode", "strict.binding.eval-or-arguments strict.reserved-word"),
    (r"^Invalid parenthesized destructuring pattern", "assignment.target.not-simple cover.assignment-pattern"),
    (r"^Private name is not declared in an enclosing class", "object.method.private-name class.private-name.undeclared"),
    (r"^Strict mode code may not include a with statement", "strict.with"),
    (r"^Unexpected legacy octal literal in strict mode", "strict.legacy-octal-literal strict.legacy-octal-escape"),
    (r"^Octal escape sequences are not allowed in strict mode", "strict.legacy-octal-escape"),
)

# The grammar's own rows for a token no production admits (EX0008) or a token a production requires and does not find
# (EX0009): a proposal's construct whose flag is off is refused by the edition's grammar, so either may come first
# beside the proposal's row (decision D17 has the library report the proposal's row where the parse reaches it).
GRAMMAR_FALLBACK = ("EX0008", "EX0009")

# Inputs of the regular-expression bucket whose disagreement the correspondence's R20 (Broiler.JS's validator
# misses the error) explains; every other input of that bucket is R19's (Broiler.VM's validator is wrong).
R20_INPUTS = frozenset({"J000305"})


class Refused(Exception):
    """An input the classification will not use, or an input it cannot explain; nothing is written."""


# ---- reading -------------------------------------------------------------------------------------------------


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def pinned(path: Path, key: str) -> bytes:
    if not path.is_file():
        raise Refused(f"{path}: no such file")
    data = path.read_bytes()
    digest = sha256(data)
    if digest != PINS[key]:
        raise Refused(f"{path}: SHA-256 {digest}, not {PINS[key]} ({NAMES.get(key, key)})")
    return data


def tsv(data: bytes, where: str) -> tuple[dict[str, str], list[dict[str, str]]]:
    """A tab-separated table: its `#%` fields and its rows, keyed by the first line that is not a comment."""
    fields: dict[str, str] = {}
    header: list[str] | None = None
    rows: list[dict[str, str]] = []
    for line in data.decode("utf-8").split("\n"):
        if not line:
            continue
        if line.startswith("#"):
            if line.startswith("#%"):
                key, _, value = line[2:].strip().partition("\t")
                fields[key] = value
            continue
        cells = line.split("\t")
        if header is None:
            header = cells
            continue
        if len(cells) != len(header):
            raise Refused(f"{where}: a row has {len(cells)} cells, not {len(header)}: {line[:80]}")
        rows.append(dict(zip(header, cells)))
    if header is None:
        raise Refused(f"{where}: no column line")
    return fields, rows


FRONT_MATTER = re.compile(r"/\*---(.*?)---\*/", re.S)


class Suite:
    """test262's files under test/, from Broiler.VM's archive of the pin or from a checkout at the pin."""

    def __init__(self, location: Path, wanted: set[str]):
        self.files: dict[str, bytes] = {}
        if location.is_file():
            pinned(location, "test262")
            prefix = f"test262-{TEST262_REVISION}/"
            with tarfile.open(location, "r:gz") as tar:
                for member in tar:
                    if not member.isfile() or not member.name.startswith(prefix):
                        continue
                    relative = member.name[len(prefix):]
                    if relative in wanted:
                        extracted = tar.extractfile(member)
                        assert extracted is not None
                        self.files[relative] = extracted.read()
        elif location.is_dir():
            head = subprocess.run(["git", "-C", str(location), "rev-parse", "HEAD"], capture_output=True,
                                  text=True, check=False).stdout.strip()
            if head != TEST262_REVISION:
                raise Refused(f"{location}: checkout at {head or 'no commit'}, not the pin {TEST262_REVISION}")
            for relative in wanted:
                full = location / relative
                if full.is_file():
                    self.files[relative] = full.read_bytes()
        else:
            raise Refused(f"{location}: no test262 archive or checkout there")
        missing = sorted(wanted - set(self.files))
        if missing:
            raise Refused(f"test262: {len(missing)} manifest paths not found, the first {missing[0]}")


def front_matter(text: str, where: str) -> dict:
    import yaml  # PyYAML, as scripts/conformance/build_parse_corpus.py reads the same front matter

    match = FRONT_MATTER.search(text)
    if match is None:
        raise Refused(f"{where}: no front matter")
    data = yaml.safe_load(match.group(1))
    if not isinstance(data, dict):
        raise Refused(f"{where}: front matter is not a mapping")
    return data


def variants_of(flags: set[str]) -> str:
    if "module" in flags:
        return "module"
    if "raw" in flags:
        return "raw"
    if "onlyStrict" in flags:
        return "strict"
    if "noStrict" in flags:
        return "sloppy"
    return "strict,sloppy"


@dataclass(frozen=True)
class Input:
    id: str
    origin: str
    path: str
    goal: str
    variants: tuple[str, ...]
    negative: str
    eval_family: bool
    provenance: str
    text: str
    features: tuple[str, ...]


@dataclass(frozen=True)
class Expectation:
    expect: str            # accepted | refused | either
    first: tuple[str, ...]  # catalogue rows, for a refusal
    reason: str
    basis: tuple[str, ...]

    def cells(self) -> list[str]:
        basis = list(dict.fromkeys(self.basis))
        return [self.expect, ";".join(self.first) or "-", self.reason, ";".join(basis) or "-"]


class Classifier:
    def __init__(self, test262: Path, vm_baseline: Path, analysis: Path):
        self.digests: list[tuple[str, str]] = []
        manifest_data = pinned(ROOT / MANIFEST, MANIFEST)
        self.digests.append((MANIFEST, PINS[MANIFEST]))
        catalogue_data = pinned(ROOT / CATALOGUE, CATALOGUE)
        self.digests.append((CATALOGUE, PINS[CATALOGUE]))
        stages_data = (ROOT / STAGES).read_bytes()
        self.digests.append((STAGES, sha256(stages_data)))
        negative_data = pinned(ROOT / PARSE_NEGATIVE, PARSE_NEGATIVE)
        self.digests.append((PARSE_NEGATIVE, PINS[PARSE_NEGATIVE]))
        js_data = gzip.decompress(pinned(ROOT / JS_BASELINE, JS_BASELINE))
        self.digests.append((JS_BASELINE, PINS[JS_BASELINE]))
        vm_data = gzip.decompress(pinned(vm_baseline, "vm-baseline"))
        self.digests.append((NAMES["vm-baseline"], PINS["vm-baseline"]))
        join_data = gzip.decompress(pinned(analysis / "rows-P-baseline.tsv.gz", "rows-P-baseline.tsv.gz"))
        self.digests.append((NAMES["rows-P-baseline.tsv.gz"], PINS["rows-P-baseline.tsv.gz"]))
        agree_data = gzip.decompress(pinned(analysis / "rows-P-agree.tsv.gz", "rows-P-agree.tsv.gz"))
        self.digests.append((NAMES["rows-P-agree.tsv.gz"], PINS["rows-P-agree.tsv.gz"]))
        self.digests.append((NAMES["test262"], PINS["test262"]))

        fields, manifest_rows = tsv(manifest_data, MANIFEST)
        if fields.get("pin-test262") != TEST262_REVISION:
            raise Refused(f"{MANIFEST}: pin-test262 {fields.get('pin-test262')}, not {TEST262_REVISION}")
        wanted = {row["path"] for row in manifest_rows if row["origin"] == "test262"}
        suite = Suite(test262, wanted)
        self.inputs: list[Input] = []
        for row in manifest_rows:
            if row["origin"] == "test262":
                data = suite.files[row["path"]]
                where = row["path"]
            else:
                data = (ROOT / "conformance/parse" / row["path"]).read_bytes()
                where = row["path"]
            if sha256(data) != row["sha256"]:
                raise Refused(f"{where}: SHA-256 differs from the manifest's")
            text = data.decode("utf-8")
            features: tuple[str, ...] = ()
            if row["origin"] == "test262" and row["provenance"] == "test":
                meta = front_matter(text, where)
                flags = set(meta.get("flags") or [])
                negative = meta.get("negative")
                negative_field = f"{negative['phase']}:{negative['type']}" if negative else "-"
                if negative_field != row["negative"] or variants_of(flags) != row["variants"]:
                    raise Refused(f"{where}: the front matter disagrees with the manifest")
                features = tuple(sorted(meta.get("features") or []))
            self.inputs.append(Input(row["id"], row["origin"], row["path"], row["goal"],
                                     tuple(row["variants"].split(",")), row["negative"],
                                     row["eval-family"] == "yes", row["provenance"], text, features))

        _, catalogue_rows = tsv(catalogue_data, CATALOGUE)
        self.catalogue = {row["id"]: row for row in catalogue_rows}
        _, stage_rows = tsv(stages_data, STAGES)
        self.stage = {row["id"]: row["stage"] for row in stage_rows}
        self.switch = {row["id"]: row["switch"] for row in stage_rows}
        if set(self.stage) != set(self.catalogue):
            raise Refused(f"{STAGES}: its rows are not the catalogue's")
        for rid, value in self.stage.items():
            if value not in ("syntax", "request", "engine", "semantics"):
                raise Refused(f"{STAGES}: {rid} has stage {value}")
        self.by_message: dict[str, list[str]] = {}
        self.by_code: dict[str, list[str]] = {}
        for rid in sorted(self.catalogue):
            row = self.catalogue[rid]
            self.by_message.setdefault(row["message_id"], []).append(rid)
            for code in row["vm_code"].split(";"):
                if re.fullmatch(r"2\d\d\d", code):
                    self.by_code.setdefault(code, []).append(rid)
        self.regexp_rows = frozenset(r for r, s in self.switch.items() if s == "regexp-provider")

        _, negatives = tsv(negative_data, PARSE_NEGATIVE)
        self.parse_negative = {row["test"]: row for row in negatives}
        _, js_rows = tsv(js_data, JS_BASELINE)
        self.js = {(r["id"], r["variant"], r["entry"]): (r["outcome"], r["message"]) for r in js_rows}
        _, vm_rows = tsv(vm_data, NAMES["vm-baseline"])
        self.vm: dict[tuple[str, str, str], tuple[str, str]] = {}
        for r in vm_rows:
            if (r["population"] == "B1" and r["form"] == "bytecode") or r["population"] == "B3":
                self.vm[(r["id"], r["population"], r["variant"])] = (r["outcome"], r["diagnostics"])
        _, join_rows = tsv(join_data, "rows-P-baseline.tsv.gz")
        self.join = {(r["id"], r["variant"]): r["row"] for r in join_rows}
        _, agree_rows = tsv(agree_data, "rows-P-agree.tsv.gz")
        self.agree = {(r["id"], r["variant"]): r["row"] for r in agree_rows}
        negatives = {inp.id for inp in self.inputs if inp.negative.startswith("parse:")}
        unknown = sorted({row for row in self.join.values() if row not in BUCKETS})
        if unknown:
            raise Refused(f"the correspondence's join names buckets no rule here resolves: {unknown}")
        for (iid, variant), bucket in sorted(self.join.items()):
            for_negative, for_other, _ = BUCKETS[bucket]
            if not (for_negative if iid in negatives else for_other):
                raise Refused(f"{iid} {variant}: bucket {bucket} has no rows for this kind of input")

    # ---- the syntax half's rows in a configuration -----------------------------------------------------------

    def refuses(self, rid: str, config: str, provider_refused: bool) -> bool:
        """Whether row `rid` is one the syntax half reports in `config`."""
        if self.stage[rid] != "syntax":
            return False
        switch = self.switch[rid]
        if switch.startswith("proposal:"):
            return switch not in PROPOSALS_ON[config]
        if switch == "regexp-provider":
            return provider_refused
        return True

    def code_rows(self, code: str, config: str, provider_refused: bool = False) -> list[str]:
        return [r for r in self.by_code.get(code, []) if self.refuses(r, config, provider_refused)]

    # ---- rule 1: test262's parse-negative tests -------------------------------------------------------------

    def negative(self, inp: Input, variant: str, config: str) -> Expectation:
        entry = self.parse_negative.get(inp.path)
        if entry is None:
            raise Refused(f"{inp.path}: a parse-negative test the catalogue's table does not name")
        def rows_of(cell: str) -> list[str]:
            return [] if cell in ("-", "") else cell.split(";")
        vm_rows, js_rows, cited = rows_of(entry["vm_rows"]), rows_of(entry["js_rows"]), rows_of(entry["cited_by"])
        every = sorted({r for r in vm_rows + js_rows + cited if r != "EG0001" or variant == "module"})
        if not every:
            raise Refused(f"{inp.path}: no catalogue row explains it")
        vm_outcome, vm_diagnostics = self.vm[(inp.id, "B1", variant)]
        js_outcome = self.js[(inp.id, variant, "P")][0]
        provider = {
            "V": vm_outcome == "refused" and bool(self.regexp_rows & set(vm_rows + cited)),
            "J": js_outcome == "refused" and bool(self.regexp_rows & set(js_rows + cited)),
        }[config]
        join = self.join.get((inp.id, variant))
        extra = BUCKETS[join][0] if join else ()
        refusing = [r for r in every if self.refuses(r, config, provider)]
        if refusing:
            return Expectation("refused", tuple(refusing), "test262", tuple(every) + extra)
        if any(self.stage[r] == "semantics" for r in every):
            return Expectation("either", (), "semantics-only", tuple(every) + extra)
        if all(self.switch[r] == "regexp-provider" for r in every if self.stage[r] == "syntax"):
            if any(self.stage[r] == "syntax" for r in every):
                return Expectation("accepted", (), "provider", tuple(every) + extra)
        if all(self.switch[r].startswith("proposal:") for r in every if self.stage[r] == "syntax"):
            if any(self.stage[r] == "syntax" for r in every):
                return Expectation("accepted", (), "proposal", tuple(every) + extra)
        raise Refused(f"{inp.id} {variant} {config}: rows {every} explain no expectation")

    # ---- rules 2 to 4 -------------------------------------------------------------------------------------------

    def ordinary(self, inp: Input, variant: str, config: str) -> Expectation:
        key = (inp.id, variant)
        vm_outcome, vm_diagnostics = self.vm[(inp.id, "B1", variant)]
        p_outcome, p_message = self.js[(inp.id, variant, "P")]
        k_outcome, k_message = self.js[(inp.id, variant, "K")]
        code = first_code(vm_diagnostics)
        if inp.goal == "json":
            basis = ("P04",) + ((f"VB:{code}",) if vm_outcome == "refused" else ())
            if p_outcome == "accepted":
                return Expectation("accepted", (), "json-as-script", basis)
            return Expectation("refused", ("EX0008", "EX0009"), "json-as-script", basis)
        join = self.join.get(key)
        if join is not None:
            return self.joined(inp, variant, config, join, vm_outcome, code, p_outcome, k_outcome, k_message)
        agree = self.agree.get(key)
        if agree == "P06b":
            forms = self.phase_forms(inp, declaration=True)
            first = sorted(set(forms) | set(GRAMMAR_FALLBACK))
            return Expectation("refused", tuple(first), "proposal", ("P06b",) + tuple(forms) + (f"VB:{code}",))
        if agree == "P35":
            return Expectation("refused", tuple(self.code_rows(code, config)), "fixture-error", ("P35",))
        if agree is not None:
            raise Refused(f"{inp.id} {variant}: agreement row {agree} has no rule")
        if vm_outcome == "accepted" and p_outcome == "accepted" and k_outcome == "accepted":
            return Expectation("accepted", (), "test262" if inp.origin == "test262" else "engines", ())
        if vm_outcome == "refused" and p_outcome == "refused":
            if code in ENGINE_CODES:
                row = ENGINE_CODES[code]
                if inp.origin == "test262" and row == "EX0021":
                    return Expectation("accepted", (), "engine-row", ("EX0021", "P05b", f"VB:{code}"))
                raise Refused(f"{inp.id} {variant}: both refuse, Broiler.VM by the engine row {row}")
            first = self.code_rows(code, config)
            if not first:
                raise Refused(f"{inp.id} {variant}: both refuse, and code {code} names no syntax row")
            return Expectation("refused", tuple(first), "engines", (f"VB:{code}",))
        raise Refused(f"{inp.id} {variant}: the baselines differ and the join has no row for it")

    def joined(self, inp, variant, config, join, vm_outcome, code, p_outcome, k_outcome, k_message) -> Expectation:
        vb = (f"VB:{code}",) if vm_outcome == "refused" else ()
        if join == "P02":
            return Expectation("accepted", (), "correction", ("P02",))
        if join == "P05":
            return Expectation("accepted", (), "engine-row", ("EX0021", "P05b") + vb)
        if join == "P06a":
            forms = self.phase_forms(inp, declaration=False)
            if config == "J":
                return Expectation("accepted", (), "proposal", ("P06a",) + tuple(forms))
            first = sorted(set(forms) | set(GRAMMAR_FALLBACK))
            return Expectation("refused", tuple(first), "proposal", ("P06a",) + tuple(forms) + vb)
        if join in ("P19", "P20"):
            row = "EP0009" if join == "P19" else "EP0010"
            if config == "J":
                return Expectation("accepted", (), "proposal", (join, row))
            first = sorted({row} | set(GRAMMAR_FALLBACK) | ({"EX0001"} if join == "P19" else set()))
            return Expectation("refused", tuple(first), "proposal", (join, row) + vb)
        if join == "P13":
            return Expectation("refused", GRAMMAR_FALLBACK, "correction", ("P13",) + vb)
        if join == "P15":
            if vm_outcome != "accepted":
                raise Refused(f"{inp.id} {variant}: P15 outside P15b")
            return Expectation("refused", ("EE0032", "EE0036", "EE0038", "EE0039"), "correction", ("P15b", "D49"))
        if join == "P34":
            return Expectation("accepted", (), "module-graph", ("P34",) + vb)
        if join == "P36":
            return Expectation("accepted", (), "tool", ("P36",))
        if join == "R:top-level-await-at-K":
            return Expectation("accepted", (), "tool", ("R04",))
        if join == "R:engine-kept-format-ceiling":
            row = ENGINE_CODES.get(code)
            if row is None:
                raise Refused(f"{inp.id} {variant}: a format ceiling with code {code}")
            return Expectation("accepted", (), "engine-row", (row, "R34") + vb)
        if join == "R:nesting-and-tree-depth":
            if config == "J":
                return Expectation("accepted", (), "limit", ("R12", "R13", "EX0011", "EX0012") + vb)
            return Expectation("refused", ("EX0011", "EX0012"), "limit", ("R12", "R13") + vb)
        if join == "R:regexp-validator":
            row = "R20" if inp.id in R20_INPUTS else "R19"
            outcome = vm_outcome if config == "V" else p_outcome
            if outcome == "accepted":
                return Expectation("accepted", (), "provider", ("R18", row))
            return Expectation("refused", tuple(sorted(self.regexp_rows)), "provider", ("R18", row) + vb)
        if join == "D:stage-E (JS checks it in its compiler)":
            rows = self.message_rows(k_message, code)
            refusing = [r for r in rows if self.refuses(r, config, False)]
            if refusing:
                return Expectation("refused", tuple(refusing), "stage", tuple(rows) + ("D31",) + vb)
            if all(self.stage[r] == "semantics" for r in rows):
                return Expectation("either", (), "semantics-only", tuple(rows) + ("D31",) + vb)
            raise Refused(f"{inp.id} {variant}: stage rows {rows}")
        if join == "D:early-error JS misses (strict reserved word or static-block await)":
            return Expectation("refused", tuple(self.code_rows(code, config)), "correction", ("D37",) + vb)
        if join == "D:early-error JS misses (super property in a non-method function)":
            rows = self.by_message["super.property.outside-method"]
            if any(self.stage[r] != "semantics" for r in rows):
                raise Refused("super property rows are not all semantics")
            return Expectation("either", (), "semantics-only", tuple(rows) + ("D41",) + vb)
        if join == "D:JS over-refuses (duplicate parameters, sloppy async or generator)":
            if vm_outcome == "accepted":
                return Expectation("accepted", (), "correction", ("D42",))
            rows = [r for r in self.by_message["scope.parameter.duplicate"] if self.refuses(r, config, False)]
            return Expectation("refused", tuple(rows), "correction", ("D42",) + vb)
        raise Refused(f"{inp.id} {variant}: join row {join} has no rule")

    def phase_forms(self, inp: Input, declaration: bool) -> list[str]:
        """The import-phase rows an input's text holds: the declaration forms or the call forms."""
        if declaration:
            forms = [r for r, pattern in (("EP0011", r"\bimport\s+source\s"), ("EP0013", r"\bimport\s+defer\s"))
                     if re.search(pattern, inp.text)]
        else:
            forms = [r for r, word in (("EP0012", "import.source"), ("EP0014", "import.defer")) if word in inp.text]
        if not forms:
            raise Refused(f"{inp.id}: an import-phase input with no {'declaration' if declaration else 'call'} form")
        return forms

    def message_rows(self, message: str, code: str) -> list[str]:
        for pattern, identifiers in COMPILE_MESSAGES:
            if re.search(pattern, message):
                rows = sorted(r for i in identifiers.split() for r in self.by_message.get(i, []))
                narrowed = [r for r in rows if r in set(self.by_code.get(code, []))]
                return narrowed or rows
        raise Refused(f"no catalogue row is read for the compile-stage message {message!r}")

    # ---- rule 5: eval requests ---------------------------------------------------------------------------------

    def eval_request(self, inp: Input, flags: str, readings: dict[str, tuple[str, Expectation]]) -> Expectation:
        outcome, diagnostics = self.vm[(inp.id, "B3", flags)]
        if outcome == "accepted":
            return Expectation("accepted", (), "eval-vm", (f"B3:{flags}",))
        first = diagnostics.split(";")[0]
        order = ("strict", "raw", "sloppy") if flags in STRICT_SETS else ("sloppy", "raw", "strict")
        for name in order + EVAL_SETS:
            if name in readings and readings[name][0] == first:
                inherited = readings[name][1]
                return Expectation(inherited.expect, inherited.first, "eval-as",
                                   (f"as:{name}",) + inherited.basis)
        code = first.split(":")[0]
        codes = {d.split(":")[0] for d in diagnostics.split(";")}
        if code in PARSER_ONLY_CODES or codes & {"2102"}:
            rows = self.code_rows(code, "V")
            return Expectation("refused", tuple(rows), "eval-stage", (f"B3:{flags}", f"VB:{code}", "parser"))
        if code in BINDER_ONLY_CODES:
            return Expectation("either", (), "eval-stage", (f"B3:{flags}", f"VB:{code}", "binder"))
        sloppy_twin = {"E1": "E0", "E3": "E2"}.get(flags)
        if sloppy_twin and code in STRICT_ONLY_PARSER_CODES:
            twin_outcome, twin_diagnostics = self.vm[(inp.id, "B3", sloppy_twin)]
            if twin_outcome == "accepted" or place(first) < place(twin_diagnostics.split(";")[0]):
                rows = self.code_rows(code, "V")
                return Expectation("refused", tuple(rows), "eval-stage", (f"B3:{flags}", f"VB:{code}", "strict-only"))
        raise Refused(f"{inp.id} {flags}: an eval refusal ({diagnostics[:40]}) no rule reads")

    # ---- the table ----------------------------------------------------------------------------------------------

    def lines(self) -> list[list[str]]:
        out: list[list[str]] = []
        for inp in self.inputs:
            per_variant: dict[str, Expectation] = {}
            for variant in inp.variants:
                for config in CONFIGS:
                    if inp.negative.startswith("parse:"):
                        expectation = self.negative(inp, variant, config)
                    else:
                        expectation = self.ordinary(inp, variant, config)
                    if expectation.reason not in REASON_CODES:
                        raise Refused(f"{inp.id}: reason {expectation.reason}")
                    if config == "V":
                        per_variant[variant] = expectation
                    out.append([inp.id, variant, config] + expectation.cells())
            if inp.eval_family:
                readings: dict[str, tuple[str, Expectation]] = {}
                for variant, expectation in per_variant.items():
                    outcome, diagnostics = self.vm[(inp.id, "B1", variant)]
                    if outcome == "refused":
                        readings[variant] = (diagnostics.split(";")[0], expectation)
                for flags in EVAL_SETS:
                    expectation = self.eval_request(inp, flags, readings)
                    outcome, diagnostics = self.vm[(inp.id, "B3", flags)]
                    if outcome == "refused" and flags not in readings:
                        readings[flags] = (diagnostics.split(";")[0], expectation)
                    out.append([inp.id, flags, "V"] + expectation.cells())
        return out


def place(diagnostic: str) -> tuple[int, int]:
    """A diagnostic's (line, column), from `code:line:column`."""
    _, line, column = diagnostic.split(":")
    return int(line), int(column)


def first_code(diagnostics: str) -> str:
    return diagnostics.split(";")[0].split(":")[0] if diagnostics not in ("", "-") else "-"


# ---- writing -------------------------------------------------------------------------------------------------

COLUMNS = ("id", "variant", "config", "expect", "first", "reason", "basis")


def render(classifier: Classifier) -> dict[str, bytes]:
    lines = classifier.lines()
    head = io.StringIO()
    head.write("#% format\tbroiler-parse-classification\t1\n")
    head.write("#% tool\tscripts/frontend/classify_parse_corpus.py\n")
    for name, digest in classifier.digests:
        head.write(f"#% input\t{name}\t{digest}\n")
    head.write("#% configurations\tV\tJ\n")
    head.write("#% eval-sets\t" + "\t".join(EVAL_SETS) + "\n")
    head.write("\t".join(COLUMNS) + "\n")
    for line in lines:
        for cell in line:
            if not cell or "\t" in cell or "\n" in cell:
                raise Refused(f"a cell may not be empty or hold a tab or line break: {line}")
        head.write("\t".join(line) + "\n")
    table = head.getvalue().encode("utf-8")
    compressed = io.BytesIO()
    with gzip.GzipFile(filename="", mode="wb", fileobj=compressed, mtime=0, compresslevel=9) as stream:
        stream.write(table)
    reasons = "code\tmeaning\n" + "".join(f"{code}\t{meaning}\n" for code, meaning in REASONS)
    joins = "bucket\trows-negative\trows-other\tresolution-other\n" + "".join(
        f"{bucket}\t{';'.join(neg) or '-'}\t{';'.join(other) or '-'}\t{rule}\n"
        for bucket, (neg, other, rule) in sorted(BUCKETS.items()))
    messages = "message\tidentifiers\n" + "".join(f"{p}\t{i}\n" for p, i in COMPILE_MESSAGES)
    return {
        "classification.tsv.gz": compressed.getvalue(),
        "reasons.tsv": reasons.encode("utf-8"),
        "join-rules.tsv": joins.encode("utf-8"),
        "compile-messages.tsv": messages.encode("utf-8"),
    }


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("mode", choices=("write", "check"))
    parser.add_argument("--test262", required=True, type=Path)
    parser.add_argument("--vm-baseline", required=True, type=Path)
    parser.add_argument("--correspondence", required=True, type=Path)
    arguments = parser.parse_args(argv)
    try:
        files = render(Classifier(arguments.test262, arguments.vm_baseline, arguments.correspondence))
    except Refused as error:
        print(f"refused: {error}", file=sys.stderr)
        return 2
    out = ROOT / OUT
    if arguments.mode == "write":
        out.mkdir(parents=True, exist_ok=True)
        for name, data in sorted(files.items()):
            (out / name).write_bytes(data)
        return 0
    differing = [name for name, data in sorted(files.items())
                 if not (out / name).is_file() or (out / name).read_bytes() != data]
    for name in differing:
        print(f"differs: {OUT}/{name}", file=sys.stderr)
    return 1 if differing else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
