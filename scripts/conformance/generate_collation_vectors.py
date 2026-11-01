#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Write the collation vectors of the conformance corpus (milestone SJL-17, work package SJL-17.2).

    python3 -I scripts/conformance/generate_collation_vectors.py --node <Node v26.12.0>
        [--out conformance/vectors/collation] [--cldr Broiler.ECMAScript/data/intl/cldr] [--ucd data/unicode/pins]
        [--spec <ecma402 checkout>]

THE AREA. Four files under conformance/vectors/collation/, the inputs decision E9 of Broiler.VM's JSD-0084 names
(SJL-17's area verdict): root order across scripts, from the archived CollationTest files and the CLDR 48 reference,
with canonically equivalent and ignorable sequences and Han across blocks (root.vectors); every tailoring of decision
E3's list, root, German phonebook and search, root search, en, es, sv and th (tailorings.vectors); the four options
Intl.Collator resolves, sensitivity, ignorePunctuation, numeric and caseFirst (options.vectors); and lone surrogates
(surrogates.vectors). Every vector names, in its note, the rows of bundle sjl-17-001's correspondence from code
(Broiler.VM's docs/evidence/sjl-17-001/correspondence.md, rows O1 to O19) whose inputs it takes, so that each engine's
first run can be classified by those rows (``ROWS``; every row with a behavioural difference is reached,
``check_rows``). The note's rows are written from the case's own (``rows_text``), and the generator refuses a vector
whose note names any other set (``render``). A pair or sort at `sensitivity: "base"` that holds a width or compatibility
form (a string whose NFKD is not its NFD) takes row O5b as well, whose correction is that base folds those differences.

THE AREA FIELD. The schema's areas (SCHEMA.md section 2) have no `collation`, and the validator that holds them is the
shared Conformance library's code, which this work package does not change: collation is a service of ECMA-402, so
these vectors are of the area `intl`, their identifiers `intl/collation/<file>/<group>/<n>`, and they sit in their own
directory, as SJL-17's plan places them, beside the Intl area's five files of revision 27 under intl/.

THE EXPECTED VALUES. The reference is the Intl area's (decision D17 of Broiler.VM's JSD-0082): ICU 78.3 with CLDR 48.0,
through the reference Node.js release bundle sjl-9-001's reference.txt names, v26.12.0, retrieved and checked by its
tarball's SHA-256, driven by intlref.mjs (``ref:intlref@1``), which refuses any other Node.js. It is neither engine's
code. Beside it, the archived CollationTest files of CLDR 48.2.0 (UCA 17), the data home's pinned
Broiler.ECMAScript/data/intl/cldr/cldr-48.2.0/common/uca/CollationTest_CLDR_{NON_IGNORABLE,SHIFTED}_SHORT.txt, each
held to its length and SHA-256 in the data home's cldr.pin before a byte of it is used, are read by this generator
(``ref:collationtest@1``): a vector of two consecutive lines of either file takes its relation from the reference, at
the strength Intl.Collator's `sensitivity: "variant"` gives, and the generator refuses the area if the reference ever
orders the second line before the first, which the file forbids. Only those vectors cite the files: the lone
surrogates' notes name the lines that key a surrogate code point (found in the files when the area is written,
``SURROGATE_LINES``), but no surrogate vector is a pair of them. The canonical-equivalence vectors are held to `=`,
which CompareStrings requires of canonically equivalent strings, and the generator refuses the area otherwise.

Han across blocks (decision E9's reading, finding F1 and row O3). UTS #35 Part 5, Collation, in CLDR 48's edition
(https://www.unicode.org/reports/tr35/tr35-76/tr35-collation.html#File_Format_FractionalUCA_txt, retrieved 2026-11-01,
SHA-256 0b004c8c7590f3f77fdc647a1f32874ef6a16ad9ca606135eef64647b3ab6e43) leaves the order of Han characters to the
implementation: "For Han characters, an implementation may choose either to implement the order defined in the UCA and
the [Unified_Ideograph] data, or to implement the order defined by the [radical ...] lines", and "The CollationTest_CLDR
files only contain Han characters that are in the same relative order using implicit weights or the radical-stroke
order". The reference takes the radical-stroke order, which CLDR 48.2.0's own root.xml (its `private-unihan`
collation, which the data home pins) names "the CLDR collation root table's radical-stroke order"; that sentence
describes CLDR's root table and binds no implementation. So the vectors of the root's `han` and `scripts` groups are
computed a second time, by the UCA over the data home's pinned allkeys_CLDR.txt with UCA 17's implicit weights
(``ImplicitReference``, ``ref:ucaimplicit@1``), which is neither engine's code and is the other order UTS #35 allows:
where the two answers agree the vector is `normative` and cites both; where they differ, which the generator allows
only where the difference is the order of Han characters among themselves (``han_only_difference``), the vector is
`impl-defined`.

THE SECOND REFERENCE (``ref:ucaimplicit@1``). UTS #10 (UCA 17) over CLDR 48.2.0's root data as row O3 names it: each
string to NFD by the corpus's Unicode reference (unicoderef.py over the pinned UCD 17.0.0, sources/unicode.pin, read
from --ucd), each longest match in allkeys_CLDR.txt (held to the data home's cldr.pin) its collation elements, every
unified ideograph it does not list the implicit weights of UTS #10 section 10.1.3 (base FB40 for the CJK Unified
Ideographs and CJK Compatibility Ideographs blocks, FB80 for the other Unified_Ideograph code points), non-ignorable,
compared level by level to the strength the sensitivity gives. It weighs only what those groups hold: it refuses a
combining mark (no discontiguous match is needed), any other code point allkeys_CLDR.txt does not list, and options
other than none and `sensitivity: "base"`. Every non-Han vector of the two groups is held to the reference's answer, so
the second reference is checked against the first wherever the Han order does not enter.

THE CLASSES. `normative` where the pinned text and the collation data both engines adopt (CLDR 48.2.0's root and
decision E3's list, which carries every locale and collation the vector names for both engines) fix the answer, as the
Intl area of revision 27 reads its locale data; `impl-defined` where the answer moves with a choice the specifications
leave to the implementation. First, the available locales, which ECMA-402 leaves to the implementation: Broiler.VM's
list stays de, de-DE, en and en-US (rule N28, decision E3), so a vector under `es`, `sv` or `th` expects of Broiler.VM
the reference's answer for `en-US`, the locale ResolveLocale's LookupMatcher gives it over that list (``vm_script``,
row O16), and of Broiler.JS, whose shared data carries the tailoring (decision E3), the vector's out. Second, the Han
order, which UTS #35 leaves to the implementation: out is the reference's radical-stroke answer, and both engines'
expectation is the second reference's, since both take CLDR 48.2.0's root as Broiler.VM reads it (row O3: "CLDR 48.2.0's
root as Broiler.VM reads it replaces the host's ICU" for Broiler.JS, which adopts Broiler.VM's collator, decision E2).
No expected value is read from either engine's run.

Each engine's first run on the revision that adds the area is classified in the next revision, in
scripts/conformance/collation-divergences.txt, which this generator reads (absent until then).

The generator is deterministic: the same reference and inputs write the same bytes (its tests regenerate the area
where the reference is present).
"""

from __future__ import annotations

import argparse
from dataclasses import dataclass, field
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile
from typing import Sequence

SCRIPTS = Path(__file__).resolve().parent
if str(SCRIPTS) not in sys.path:
    sys.path.insert(0, str(SCRIPTS))

import unicoderef  # noqa: E402

REPOSITORY = SCRIPTS.parents[1]
CORPUS = REPOSITORY / "conformance" / "vectors"
DEFAULT_OUT = CORPUS / "collation"
INTLREF = SCRIPTS / "intlref.mjs"
DEFAULT_CLDR = REPOSITORY / "Broiler.ECMAScript" / "data" / "intl" / "cldr"
# The data home's copy of the pinned UCD 17.0.0, laid out as sources/unicode.pin's `path` lines name it.
DEFAULT_UCD = REPOSITORY / "data" / "unicode" / "pins"
GENERATOR = "generate_collation_vectors.py"
DIVERGENCES = SCRIPTS / "collation-divergences.txt"

# THE ONE CONSTANT. The corpus revision that adds the area, which every vector's `since` names. It is the next free
# revision after origin/main's when the change merges (revision 30 there on 2026-11-01), and moves with it if another
# area's revisions merge first. The revision that classifies the first runs is the one after it.
SINCE = 31
CORPUS_REVISION = SINCE  # the corpus revision this generator writes: REVISIONS.md's last row

ECMA402 = "spec:ecma402@3d4c558#"
ES2026 = "spec:es2026@0248456c#"
REF = "ref:intlref@1"
REF_TEST = "ref:collationtest@1"
REF_IMPLICIT = "ref:ucaimplicit@1"
COMPARE = ECMA402 + "sec-collator-comparestrings"
SLOTS = ECMA402 + "sec-intl-collator-internal-slots"
CONSTRUCTOR = ECMA402 + "sec-intl.collator"
COMPARE_GETTER = ECMA402 + "sec-intl.collator.prototype.compare"
RESOLVED = ECMA402 + "sec-intl.collator.prototype.resolvedoptions"
LOCALE_COMPARE = ECMA402 + "sup-String.prototype.localeCompare"
SORT = ES2026 + "sec-array.prototype.sort"

FILES = {
    "root": "root order across scripts: the archived CollationTest files, scripts, canonical equivalence and "
            "ignorables, compatibility forms, Han across blocks",
    "tailorings": "the tailorings of decision E3's list: root, en, de, German phonebook and search, root search, es, "
                  "sv and th",
    "options": "the four options: sensitivity, ignorePunctuation, numeric and caseFirst, by option, by keyword and "
               "through localeCompare",
    "surrogates": "lone surrogates, alone, in longer strings and through localeCompare",
}

# The rows of bundle sjl-17-001's correspondence (Broiler.VM's docs/evidence/sjl-17-001/correspondence.md) and the
# class JSD-0084 gives each. BEHAVIOURAL are the rows whose inputs a vector can carry; O10 (the option reads), O11a
# and O11b (resolved options and the collations), O12 (the bound compare's identity), O14 and O15 (invariant globalization and the host's default locale, which no run of the corpus sets), O17 (the
# charges, which no engine-level runner measures) and O18 (the orderings dataset, which part B of each rule holds)
# are reached by other evidence.
ROWS = {
    "O1": "data policy, C5", "O2": "data policy, C5", "O3": "data policy, C5", "O4": "same",
    "O5a": "a correction of Broiler.JS", "O5b": "a correction of Broiler.JS", "O5c": "data policy, C5",
    "O6": "same", "O7": "a correction of Broiler.JS", "O8": "a correction of Broiler.JS", "O9": "same", "O13": "same",
    "O16": "data policy, C5", "O19": "same",
}
BEHAVIOURAL = tuple(ROWS)

# Decision E3 and rule N28: Broiler.VM's Collator locales stay de, de-DE, en and en-US, with en-US its default
# (JSD-0082 decision D7), so LookupMatcher gives it en-US for es, sv and th, and orders by the root.
VM_MOVES = {"es": "en-US", "sv": "en-US", "th": "en-US"}
VM_REASON = ("decision E3 and rule N28: Broiler.VM's Collator locales stay de, de-DE, en and en-US, so it resolves "
             "{moves} (LookupMatcher over its list, row O16) and its expectation is the reference's answer for that "
             "locale; Broiler.JS's shared data carries the tailoring (decision E3), so its expectation is out")
VM_ROW = "O16"  # the row VM_REASON names; a case whose locale moves carries it

# Han across blocks: UTS #35 Part 5 (CLDR 48) leaves the order to the implementation (the module's docstring).
HAN_SAME = ("the UCA's implicit weights over CLDR 48.2.0's allkeys_CLDR.txt give the same answer (ref:ucaimplicit@1), "
            "so it holds under either Han order UTS #35 Part 5 (CLDR 48) allows")
HAN_MOVES = ("the answer moves with the order of Han characters, which UTS #35 Part 5 (CLDR 48) leaves to the "
             "implementation (\"an implementation may choose either to implement the order defined in the UCA and the "
             "[Unified_Ideograph] data, or to implement the order defined by the [radical ...] lines\"): out is the "
             "reference's, by radical and stroke; both engines take CLDR 48.2.0's root as Broiler.VM reads it, "
             "allkeys_CLDR.txt with UCA 17's implicit weights (row O3; Broiler.JS adopts Broiler.VM's collator, "
             "decision E2), so each engine's expectation is that order's answer (ref:ucaimplicit@1)")
SECOND_REFERENCE_GROUPS = ("scripts", "han")  # the root's groups ref:ucaimplicit@1 computes

REFERENCE_VERSIONS = {"node": "26.12.0", "icu": "78.3", "cldr": "48.0"}


class GeneratorError(RuntimeError):
    """The generator cannot write the area, and says why."""


# ---- Values and literals ------------------------------------------------------------------------------


def units(text: str) -> list[int]:
    """A Python string as UTF-16 code units; a lone surrogate is one unit."""
    data = text.encode("utf-16-le", "surrogatepass")
    return [data[i] | (data[i + 1] << 8) for i in range(0, len(data), 2)]


def typed(text: str) -> str:
    """The schema's `str:` value (section 3): printable ASCII as itself, `"` and `\\` escaped, every other unit
    \\uXXXX in upper case."""
    out = ['str:"']
    for unit in units(text):
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
    """A JavaScript string literal of ASCII alone; the same escapes as the typed value."""
    return typed(text)[4:]


def show(text: str) -> str:
    """A string for a note: printable ASCII as itself, other units as U+XXXX."""
    out = []
    for unit in units(text):
        out.append(chr(unit) if 0x20 < unit <= 0x7E else f"U+{unit:04X}")
    return "".join(out) if out else "(empty)"


def options_js(options: dict | None) -> str:
    if not options:
        return ""
    parts = []
    for key, value in options.items():
        if isinstance(value, bool):
            parts.append(f"{key}:{'true' if value else 'false'}")
        else:
            parts.append(f"{key}:{js(value)}")
    return "{" + ",".join(parts) + "}"


def collator_js(locale: str | None, options: dict | None) -> str:
    arguments = []
    if locale is not None or options:
        arguments.append("undefined" if locale is None else js(locale))
    if options:
        arguments.append(options_js(options))
    return "new Intl.Collator(" + ",".join(arguments) + ")"


RELATION = '["<","=",">"][Math.sign({call})+1]'


def compare_script(locale: str | None, options: dict | None, x: str, y: str) -> str:
    return RELATION.format(call=f"{collator_js(locale, options)}.compare({js(x)},{js(y)})")


def locale_compare_script(locale: str | None, options: dict | None, x: str, y: str) -> str:
    arguments = [js(y)]
    if locale is not None or options:
        arguments.append("undefined" if locale is None else js(locale))
    if options:
        arguments.append(options_js(options))
    return RELATION.format(call=f"{js(x)}.localeCompare({','.join(arguments)})")


# The sort: the words in the order given, sorted by the collator's bound compare (a stable sort, so equal words keep
# the given order), written with the relation between each two neighbours.
SORT_SCRIPT = ('(function(c,w){{w.sort(c.compare);var s=w[0];for(var i=1;i<w.length;i++)s+=" "+["<","=",">"]'
               '[Math.sign(c.compare(w[i-1],w[i]))+1]+" "+w[i];return s}})({collator},[{words}])')


def sort_script(locale: str | None, options: dict | None, words: Sequence[str]) -> str:
    return SORT_SCRIPT.format(collator=collator_js(locale, options), words=",".join(js(w) for w in words))


def resolved_script(locale: str | None, options: dict | None, member: str) -> str:
    return f"{collator_js(locale, options)}.resolvedOptions().{member}"


def vm_script(script: str) -> tuple[str, list[str]]:
    """The script with every locale Broiler.VM's list lacks replaced by the one it resolves to (decision E3)."""
    moves: list[str] = []

    def replace(match: re.Match) -> str:
        tag = match.group(2)
        language = tag.split("-")[0]
        if language in VM_MOVES:
            moves.append(f"{tag} to {VM_MOVES[language]}")
            return match.group(1) + js(VM_MOVES[language])
        return match.group(0)

    moved = re.sub(r'(new Intl\.Collator\(|\.localeCompare\("(?:[^"\\]|\\.)*",)"([A-Za-z0-9-]+)"', replace, script)
    return moved, list(dict.fromkeys(moves))


# ---- Cases -------------------------------------------------------------------------------------------


@dataclass
class Case:
    file: str
    group: str
    op: str
    inputs: list[str]
    script: str
    rows: tuple[str, ...]
    note: str
    prov: list[str]
    expect: str | None = None          # an outcome the generator holds the reference's answer to
    forbid: str | None = None          # an outcome the reference must not give (a CollationTest pair's `>`)
    vm_moves: list[str] = field(default_factory=list)
    # What ref:ucaimplicit@1 computes for the case: (options, the strings, whether they are sorted), or None.
    second: tuple[dict | None, tuple[str, ...], bool] | None = None
    second_out: str | None = None      # its answer, once computed
    han: bool = False                  # whether a string of the case holds a unified ideograph


CASES: list[Case] = []
SEEN: set[str] = set()


def rows_text(rows: Sequence[str]) -> str:
    names = list(rows)
    if len(names) == 1:
        return f"row {names[0]} ({ROWS[names[0]]})"
    return "rows " + ", ".join(names[:-1]) + " and " + names[-1] + " (" + "; ".join(f"{r} {ROWS[r]}" for r in names) + ")"


ROW_NAME = re.compile(r"\bO\d+[a-z]?\b")


def rows_named(note: str) -> set[str]:
    """The rows of the correspondence a note names."""
    return set(ROW_NAME.findall(note))


def add(file: str, group: str, op: str, inputs: Sequence[str], script: str, rows: Sequence[str], note: str,
        prov: Sequence[str], expect: str | None = None, forbid: str | None = None,
        second: tuple[dict | None, tuple[str, ...], bool] | None = None) -> None:
    # A script is vectored once: a pair two lists share (neighbours in the scripts' sort and in the Latin one, say)
    # keeps the first case that names it.
    if script in SEEN:
        return
    SEEN.add(script)
    CASES.append(Case(file, group, op, [typed(v) for v in inputs], script, tuple(rows), note, list(prov), expect,
                      forbid, second=second))


def with_compatibility_row(options: dict | None, strings: Sequence[str], rows: Sequence[str]) -> tuple[str, ...]:
    """Row O5b joins a case at `sensitivity: "base"` that holds a width or compatibility form (a string whose NFKD is
    not its NFD): the correction that base folds those differences (the correspondence's row O5b, Broiler.JS's
    SharedLibraries.md "Collation (SJL-17.1)")."""
    out = tuple(rows)
    if (options or {}).get("sensitivity") == "base" and any(SECOND.compatibility_form(s) for s in strings):
        out = tuple(dict.fromkeys(out + ("O5b",)))
    return out


COMPATIBILITY_NOTE = "base folds width and compatibility differences"


def pair(file: str, group: str, locale: str | None, options: dict | None, x: str, y: str, rows: Sequence[str],
         note: str, prov: Sequence[str] = (), expect: str | None = None, forbid: str | None = None,
         through: str = "compare", second: bool = False) -> None:
    """A pair: its note is written `<x> against <y> under <where>: <rows>: <note>`, the rows from the case's own."""
    inputs = ([] if locale is None else [locale]) + [x, y]
    if through == "localeCompare":
        script = locale_compare_script(locale, options, x, y)
        op = "String.prototype.localeCompare"
        base = [LOCALE_COMPARE, COMPARE]
    else:
        script = compare_script(locale, options, x, y)
        op = "Intl.Collator.prototype.compare"
        base = [COMPARE_GETTER, COMPARE]
    rows = with_compatibility_row(options, (x, y), rows)
    if "O5b" in rows and COMPATIBILITY_NOTE not in note:
        note += f" ({COMPATIBILITY_NOTE}, row O5b)"
    where = (f"{locale}" if locale is not None else "the default locale") + (f" {options_js(options)}" if options else "")
    add(file, group, op, inputs, script, rows, f"{show(x)} against {show(y)} under {where}: {rows_text(rows)}: {note}",
        base + list(prov) + [REF], expect, forbid, second=(options, (x, y), False) if second else None)


def sort(file: str, group: str, locale: str | None, options: dict | None, words: Sequence[str],
         rows: Sequence[str], note: str, prov: Sequence[str] = (), second: bool = False) -> None:
    """A sort: its note is written `<n> strings sorted under <where>, ...: <rows>: <note>`."""
    inputs = ([] if locale is None else [locale]) + list(words)
    rows = with_compatibility_row(options, words, rows)
    if "O5b" in rows and COMPATIBILITY_NOTE not in note:
        note += f" ({COMPATIBILITY_NOTE}, row O5b)"
    where = (f"{locale}" if locale is not None else "the default locale") + (f" {options_js(options)}" if options else "")
    add(file, group, "Array.prototype.sort with Intl.Collator.prototype.compare", inputs,
        sort_script(locale, options, words), rows,
        f"{len(words)} strings sorted under {where}, each neighbour's relation written between them: "
        f"{rows_text(rows)}: {note}",
        [SORT, COMPARE_GETTER, COMPARE] + list(prov) + [REF], second=(options, tuple(words), True) if second else None)


# ---- The archived CollationTest files -------------------------------------------------------------------

UCA_DIRECTORY = "cldr-48.2.0/common/uca"
COLLATION_TEST_FILES = {
    "non-ignorable": ("CollationTest_CLDR_NON_IGNORABLE_SHORT.txt", {"sensitivity": "variant"}),
    "shifted": ("CollationTest_CLDR_SHIFTED_SHORT.txt", {"sensitivity": "variant", "ignorePunctuation": True}),
}
ROOT_XML = "cldr-48.2.0/common/collation/root.xml"
RADICAL_STROKE = "the CLDR collation root table's radical-stroke order"
# Every STRIDE-th pair of consecutive lines of each file (a prime, so the pairs fall at no period of the files), and
# every pair that holds a lone surrogate: the CollationTest files key a surrogate code point as an unassigned one.
STRIDE = 401


def read_pin(cldr: Path) -> dict[str, tuple[int, str]]:
    entries = {}
    for line in (cldr / "cldr.pin").read_text(encoding="utf-8").splitlines():
        fields = line.split(" ")
        if fields[0] == "file" and len(fields) == 4:
            entries[fields[1]] = (int(fields[2]), fields[3])
    return entries


def pinned_bytes(cldr: Path, relative: str) -> bytes:
    """A file of the data home's CLDR archive, held to its pin line before a byte of it is used."""
    entries = read_pin(cldr)
    if relative not in entries:
        raise GeneratorError(f"{relative} has no line in {cldr / 'cldr.pin'}")
    data = (cldr / relative).read_bytes()
    length, digest = entries[relative]
    if len(data) != length or hashlib.sha256(data).hexdigest() != digest:
        raise GeneratorError(f"{relative} is not the file {cldr / 'cldr.pin'} pins ({len(data)} bytes, "
                             f"SHA-256 {hashlib.sha256(data).hexdigest()})")
    return data


def collation_test_lines(data: bytes) -> list[tuple[int, str]]:
    """(line number, string) for every test line: code points in hexadecimal, a `#` comment after them."""
    out = []
    for number, raw in enumerate(data.decode("utf-8").split("\n"), 1):
        body = raw.split("#", 1)[0].strip()
        if not body:
            continue
        out.append((number, "".join(chr(int(h, 16)) for h in body.split(" "))))
    return out


def is_surrogate_string(text: str) -> bool:
    return any(0xD800 <= ord(c) <= 0xDFFF for c in text)


# The first and last line of each file whose string holds a surrogate code point, as the files are read when the
# area is written; the lone surrogates' notes cite them.
SURROGATE_LINES: dict[str, tuple[int, int]] = {}


def cases_collation_test(cldr: Path) -> None:
    SURROGATE_LINES.clear()
    for name, (filename, options) in COLLATION_TEST_FILES.items():
        lines = collation_test_lines(pinned_bytes(cldr, f"{UCA_DIRECTORY}/{filename}"))
        surrogate_lines = [number for number, text in lines if is_surrogate_string(text)]
        if not surrogate_lines:
            raise GeneratorError(f"{filename} holds no line with a surrogate code point")
        SURROGATE_LINES[name] = (surrogate_lines[0], surrogate_lines[-1])
        chosen = set(range(0, len(lines) - 1, STRIDE))
        for index in range(len(lines) - 1):
            if is_surrogate_string(lines[index][1]) or is_surrogate_string(lines[index + 1][1]):
                chosen.add(index)
        rows = ("O2", "O9") if name == "non-ignorable" else ("O2", "O5a", "O9")
        for index in sorted(chosen):
            (first, x), (second, y) = lines[index], lines[index + 1]
            rows_here = rows if (is_surrogate_string(x) or is_surrogate_string(y)) else rows[:-1]
            pair("root", f"collationtest-{name}", "en", options, x, y, rows_here,
                 f"lines {first} and {second} of {filename} (CLDR 48.2.0, UCA 17), which order "
                 "the first at or before "
                 "the second; the relation at the tertiary strength is the reference's"
                 + (", and the file keys a surrogate code point as an unassigned one (row O9)"
                    if (is_surrogate_string(x) or is_surrogate_string(y)) else ""),
                 prov=[REF_TEST], forbid='value: str:">"')


# ---- Root order across scripts (rows O1, O3, O4) -----------------------------------------------------------

SCRIPT_WORDS = ["a", "z", "ß", "ss", "æ", "ae", "ø", "œ", "þ", "ð", "ı", "α",
                "ω", "а", "я", "ა", "ա", "א", "ת", "ا", "ي", "अ",
                "ह", "ก", "ฮ", "가", "힣", "あ", "ア", "ん", "一", "丁",
                "中", "\U00020000", "\U0001f600", "1", "١", "१", "½", "①", "Ꭰ",
                "ሀ", "ཀ", "ᠠ", "ꀀ"]
PUNCTUATION_WORDS = [" ", "_", "-", ",", ";", ":", "!", "?", ".", "'", '"', "(", ")", "[", "@", "*", "/", "\\", "&",
                     "#", "%", "`", "^", "+", "<", "=", "|", "~", "$", "€", "0", "a"]
LATIN_WORDS = ["a", "A", "à", "á", "â", "ä", "å", "æ", "b", "c", "ç", "d",
               "ð", "đ", "e", "é", "è", "ê", "ë", "h", "i", "ı", "I", "İ",
               "l", "ł", "n", "ñ", "o", "ö", "ø", "œ", "s", "ss", "ß", "ẞ", "t",
               "th", "þ", "u", "ü", "z", "ž"]
# Each script's letters in a run, so that every neighbour inside a script is vectored as well as the scripts' order.
SCRIPT_RUNS = {
    "Greek": ["α", "ά", "Α", "β", "γ", "σ", "ς", "Σ", "ω", "ώ"],
    "Cyrillic": ["а", "А", "б", "е", "ё", "ж", "и", "й", "я", "і"],
    "Georgian": ["ა", "ბ", "ჰ", "Ა"],
    "Armenian": ["ա", "Ա", "բ", "և"],
    "Hebrew": ["א", "ב", "ך", "כ", "ת", "אַ"],
    "Arabic": ["ا", "أ", "ب", "ة", "ت", "ي", "ى", "ﺍ"],
    "Devanagari": ["अ", "आ", "क", "क्ष", "ह", "क़"],
    "Thai": ["ก", "ข", "เก", "กา", "ฮ", "๐"],
    "Hangul": ["가", "가", "각", "나", "힣", "ㄱ"],
    "kana": ["あ", "ア", "ぁ", "か", "が", "カ", "ん", "ン", "ー", "ｱ"],
    "digits": ["0", "1", "١", "१", "１", "2", "9", "½", "①", "⅓"],
}
# Han across blocks (row O3, finding F1): the core unified ideographs (U+4E00 to U+9FFF), those added to the block
# after Unicode 1.1 (U+9FA6 on), the twelve unified ideographs of the compatibility block (U+FA0E and its kin),
# Extensions A to J, and two compatibility ideographs that decompose to core ones.
HAN = ["一", "丁", "丂", "中", "人", "子", "水", "龜", "龥", "龦", "鿌",
       "鿪", "鿿", "﨎", "﨏", "﨑", "﨟", "﨩", "㐀", "㐁", "䶵", "䶿",
       "\U00020000", "\U00020001", "\U0002a6df", "\U0002a700", "\U0002b740", "\U0002b820", "\U0002ceb0",
       "\U00030000", "\U00031350", "\U0002ebf0", "\U000323b0", "豈", "\U0002f800"]
# The pairs across blocks row O3 names or reads, each vectored by itself as well as among HAN's neighbours.
HAN_PAIRS = [("中", "\U00020000"), ("一", "\U00020000"), ("丁", "\U00020000"), ("一", "㐀"),
             ("中", "㐀"), ("鿿", "㐀"), ("㐀", "\U00020000"), ("䶿", "\U00020000"),
             ("\U00020000", "\U0002a700"), ("\U0002a700", "\U00030000"), ("一", "﨎"),
             ("水", "\U0002ebf0"), ("中", "\U000323b0"), ("人", "\U00031350"), ("豈", "豈"),
             ("\U0002f800", "丽"), ("一", "丁"), ("丁", "中")]
EQUIVALENT = [("å", "å"), ("Å", "Å"), ("Ω", "Ω"), ("q̣̇", "q̣̇"),
              ("ḍ̇", "ḍ̇"), ("ä", "ä"), ("á֚̕", "á֚̕"),
              ("가", "가"), ("à֮̀̕b", "à֮̀̕b"),
              ("ṩ", "ṩ"), ("Ǻ", "Ǻ"), ("क़", "क़"),
              ("\U0001d15e", "\U0001d157\U0001d165"), ("ཱི", "ཱི")]
IGNORABLE = [("ab", "a­b"), ("ab", "a​b"), ("ab", "a\u0000b"), ("ab", "a﻿b"), ("", "­"),
             ("ab", "a‍b"), ("ab", "a\u0001b")]
COMPATIBILITY = [("ﬁ", "fi"), ("ǆ", "dž"), ("①", "1"), ("Ａ", "A"), ("ª", "a"),
                 ("½", "1/2"), ("Ⅳ", "IV"), ("²", "2"), ("㌀", "アパート"),
                 ("เก", "กเ"), ("", " "), ("a b", "ab"), ("a-b", "ab"), ("a", "á")]


def cases_root(neighbours: dict[str, list[str]]) -> None:
    for locale in ("en", "und"):
        sort("root", "scripts", locale, None, SCRIPT_WORDS, ("O1", "O3"),
             "CLDR 48.2.0's root over Latin, Greek, Cyrillic, Georgian, Armenian, Hebrew, "
             "Arabic, Devanagari, Thai, Hangul, kana, Cherokee, Ethiopic, Tibetan, Mongolian, Yi, Han across two "
             "blocks, an emoji, digits of three scripts and two compatibility digits"
             + ("; und resolves as each engine's available-locale policy says, and orders by the root" if locale == "und" else ""),
             second=True)
    for x, y in neighbours["scripts-en"]:
        rows = ("O1", "O3") if any(SECOND.has_han(s) for s in (x, y)) else ("O1",)
        pair("root", "scripts", "en", None, x, y, rows,
             "neighbours in the reference's order of the scripts' sort, CLDR 48.2.0's root", second=True)
    sort("root", "punctuation", "en", None, PUNCTUATION_WORDS, ("O1",),
         "the root's order of spaces, punctuation, symbols, a digit and a letter, non-ignorable")
    for x, y in neighbours["punctuation-en"]:
        pair("root", "punctuation", "en", None, x, y, ("O1",),
             "neighbours in the reference's order of the punctuation sort")
    sort("root", "latin", "en", None, LATIN_WORDS, ("O1",),
         "the root's Latin letters with their accented and special forms")
    for x, y in neighbours["latin-en"]:
        pair("root", "latin", "en", None, x, y, ("O1",),
             "neighbours in the reference's order of the Latin sort")
    for script, words in SCRIPT_RUNS.items():
        sort("root", "within-scripts", "en", None, words, ("O1",),
             f"{script} in the root's order")
        for x, y in neighbours[f"run-{script}"]:
            pair("root", "within-scripts", "en", None, x, y, ("O1",),
                 f"neighbours in the reference's order of {script}")
    sort("root", "han", "en", None, HAN, ("O3",), HAN_NOTE, second=True)
    for x, y in neighbours["han-en"]:
        pair("root", "han", "en", None, x, y, ("O3",), HAN_NOTE, second=True)
    for x, y in HAN_PAIRS:
        pair("root", "han", "en", None, x, y, ("O3",), HAN_NOTE, second=True)
        pair("root", "han", "en", {"sensitivity": "base"}, x, y, ("O3",), HAN_NOTE, second=True)
    for x, y in EQUIVALENT:
        for options in (None, {"sensitivity": "variant", "ignorePunctuation": True}):
            pair("root", "equivalence", "en", options, x, y, ("O4",),
                 "canonically equivalent strings, which CompareStrings must answer +0 for",
                 expect='value: str:"="')
        pair("root", "equivalence", "en", None, x, y, ("O4",),
             "canonically equivalent strings, which CompareStrings must answer +0 for, through "
             "localeCompare", prov=[], expect='value: str:"="', through="localeCompare")
    for x, y in IGNORABLE:
        pair("root", "ignorables", "en", None, x, y, ("O4",),
             "a completely ignorable code point (UTS #10) inside or beside a string")
        pair("root", "ignorables", "en", {"sensitivity": "base"}, x, y, ("O4",),
             "a completely ignorable code point (UTS #10) inside or beside a string")
    for x, y in COMPATIBILITY:
        for options in (None, {"sensitivity": "base"}, {"sensitivity": "accent"}):
            pair("root", "compatibility", "en", options, x, y, ("O4",),
                 "a compatibility or Thai prevowel form against its decomposition, or a space, a "
                 "hyphen or an accent, at the strength the sensitivity gives")


HAN_NOTE = ("Han across blocks (finding F1): the reference orders them by radical and stroke, which CLDR 48.2.0's "
            f"root.xml (collation private-unihan) names \"{RADICAL_STROKE}\", and the archived CollationTest files "
            "hold only Han characters that order alike by that order and by the UCA's implicit weights")


# ---- Tailorings (rows O16, O5c, O19, O6) ---------------------------------------------------------------------

DE_WORDS = ["Ab", "Ae", "Äb", "Af", "Od", "Öb", "Oe", "Ub", "Üb", "Ue", "ss", "ß", "st", "a",
            "ä", "ae", "b", "Ä", "AE", "o", "ö", "oe", "u", "ü", "ue"]
ES_WORDS = ["n", "ñ", "o", "nz", "ña", "ch", "cz", "d", "ll", "lz", "m", "c", "l", "Ñ", "N",
            "año", "anzuelo", "llama", "luz", "chico", "cuna"]
SV_WORDS = ["z", "å", "ä", "ö", "æ", "ø", "v", "w", "y", "ü", "a", "o", "Å",
            "Ä", "Ö", "aa", "é", "e", "th", "þ", "đ", "d"]
TH_WORDS = ["ก", "เก", "ข", "แก", "ก่", "กา", "", " ", "ฯ",
            "a", "a b", "ab", "๐", "1", "ก์", "*", "เข", "ฮ"]
TH_PAIRS = [("", " "), ("", "*"), (" ", "*"), ("a b", "ab"), ("a-b", "ab"), ("a.b", "ab"), ("เก", "ก"),
            ("เก", "กเ"), ("ก่", "ก"), ("กา", "ก"),
            ("ฯ", "ก"), ("๐", "1"), ("ก์", "ก")]
ES_PAIRS = [("n", "ñ"), ("nz", "ñ"), ("ñ", "o"), ("ll", "lz"), ("llama", "luz"), ("ch", "cz"),
            ("chico", "cuna"), ("Ñ", "ñ"), ("N", "Ñ"), ("año", "anzuelo")]
SV_PAIRS = [("z", "å"), ("å", "ä"), ("ä", "ö"), ("æ", "ä"), ("ø", "ö"),
            ("v", "w"), ("y", "ü"), ("aa", "å"), ("Å", "å"), ("é", "e"), ("th", "þ"),
            ("d", "đ"), ("z", "ö")]
DE_PAIRS = [("ä", "ae"), ("Ä", "AE"), ("ä", "af"), ("ö", "oe"), ("ü", "ue"),
            ("Äb", "Ae"), ("Äb", "Af"), ("ß", "ss"), ("ß", "st"), ("a", "ä")]


def tailoring_variants() -> list[tuple[str, str, dict | None, tuple[str, ...], str]]:
    """(group, locale, options, rows, what) for every tailoring of decision E3's list."""
    return [
        ("root", "und", None, ("O1", "O16"), "the root collation, und"),
        ("en", "en", None, ("O1", "O16"), "en, whose CLDR 48.2.0 tailoring is empty: the root"),
        ("de", "de", None, ("O16",), "de's standard collation, the root"),
        ("de-phonebook", "de-u-co-phonebk", None, ("O16",), "German phonebook (de-u-co-phonebk), a tailoring both "
                                                             "engines' data carry"),
        ("de-phonebook", "de", {"collation": "phonebk"}, ("O16",), "German phonebook by the collation option"),
        ("de-search", "de", {"usage": "search"}, ("O16", "O6"), "German search, a tailoring both engines' data carry"),
        ("de-search", "de", {"usage": "search", "sensitivity": "base"}, ("O16", "O6"), "German search at base"),
        ("root-search", "en", {"usage": "search"}, ("O16", "O6"), "root search under en"),
        ("root-search", "und", {"usage": "search"}, ("O16", "O6"), "root search under und"),
        ("root-search", "en", {"usage": "search", "sensitivity": "base"}, ("O16", "O6"), "root search at base"),
        ("es", "es", None, ("O16",), "es, CLDR 48.2.0's Spanish tailoring (decision E3)"),
        ("sv", "sv", None, ("O16",), "sv, CLDR 48.2.0's Swedish tailoring (decision E3)"),
        ("th", "th", None, ("O16", "O5c", "O19"), "th, CLDR 48.2.0's Thai tailoring with its [alternate shifted] "
                                                  "(decisions E3 and E5)"),
    ]


def cases_tailorings() -> None:
    words = {"root": DE_WORDS + ES_WORDS[:6], "en": DE_WORDS, "de": DE_WORDS, "de-phonebook": DE_WORDS,
             "de-search": DE_WORDS, "root-search": DE_WORDS + ["ا", "أ", "آ", "ﺎ", "≠", "="],
             "es": ES_WORDS, "sv": SV_WORDS, "th": TH_WORDS}
    pairs = {"root": DE_PAIRS + ES_PAIRS[:4], "en": DE_PAIRS, "de": DE_PAIRS, "de-phonebook": DE_PAIRS,
             "de-search": DE_PAIRS, "root-search": DE_PAIRS + [("ا", "أ"), ("ا", "ﺎ"),
                                                               ("=", "≠"), ("آ", "ا")],
             "es": ES_PAIRS, "sv": SV_PAIRS, "th": TH_PAIRS}
    for group, locale, options, rows, what in tailoring_variants():
        sort("tailorings", group, locale, options, words[group], rows, what)
        for x, y in pairs[group]:
            pair("tailorings", group, locale, options, x, y, rows, what)
    # Thai's default ignorePunctuation comes from the tailoring's [alternate shifted] (decision E5, row O5c).
    for locale in ("th", "th-TH"):
        add("tailorings", "th", "Intl.Collator.prototype.resolvedOptions().ignorePunctuation", [locale],
            resolved_script(locale, None, "ignorePunctuation"), ("O5c",),
            f"{rows_text(('O5c',))}: {locale}'s default ignorePunctuation is locale data, CLDR 48.2.0's Thai "
            "[alternate shifted] (decision E5)", [RESOLVED, SLOTS, REF])
    for x, y in TH_PAIRS[:6]:
        pair("tailorings", "th", "th", {"ignorePunctuation": False}, x, y, ("O19", "O5c"),
             "Thai with ignorePunctuation false, where punctuation and spaces count, over the tailoring's default")
        pair("tailorings", "th", "th", {"ignorePunctuation": True}, x, y, ("O19", "O5c"),
             "Thai with ignorePunctuation true")
    for x, y in ES_PAIRS[:4] + SV_PAIRS[:4] + DE_PAIRS[:3]:
        for locale in ("es", "sv", "de-u-co-phonebk"):
            pair("tailorings", "locale-compare", locale, None, x, y, ("O16",),
                 "a tailoring through localeCompare", through="localeCompare")


# ---- The four options (rows O5a, O5b, O6, O7, O8) -------------------------------------------------------------

SENSITIVITY_PAIRS = [("a", "A"), ("a", "á"), ("a", "Á"), ("A", "Á"), ("a", "ä"), ("á", "à"),
                     ("a", "b"), ("ä", "Ä"), ("a", "ⓐ"), ("a", "ａ"), ("a", "ª"),
                     ("æ", "ae"), ("ä", "Ä"), ("e", "é"), ("E", "é")]
PUNCTUATION_PAIRS = [("ab", "a b"), ("ab", "a-b"), ("ab", "a_b"), ("ab", "a.b"), ("ab", "a b"), ("a b", "a b"),
                     ("ab", "a‐b"), ("ab", "a$b"), ("ab", "a+b"), ("ab", "a€b"), ("a$b", "a+b"),
                     ("a+b", "a€b"), ("", " "), ("", "-"), (" ", "-"), ("aB", "a b"), ("a b", "a-b"),
                     ("a,b", "a;b"), ("a'b", "a\"b"), ("co-op", "coop"), ("co op", "co_op")]
NUMERIC_PAIRS = [("a1", "a2"), ("a2", "a10"), ("a01", "a1"), ("a001", "a01"), ("a02", "a2"), ("a9", "a10"),
                 ("a٢", "a١٠"), ("a２", "a１０"), ("a٢", "a10"), ("1.5", "1.10"),
                 ("9", "10"), ("10", "100"), ("b1", "a2"), ("a", "a1"), ("file-2", "file10"), ("x100", "x99"),
                 ("0", "00"), ("a0", "a")]
CASE_PAIRS = [("a", "A"), ("b", "B"), ("ab", "Ab"), ("aB", "Ab"), ("AB", "Ab"), ("ä", "Ä"), ("a", "b"),
              ("A", "b"), ("Ab", "ab"), ("aa", "aA")]
NUMERIC_WORDS = ["a10", "a2", "a02", "a1", "a01", "a001", "a9", "a١٠", "a٢", "a２", "10", "9",
                 "1.5", "1.10", "b1", "a"]
CASE_WORDS = ["a", "A", "b", "B", "ab", "Ab", "aB", "AB", "ä", "Ä"]
SENSITIVITY_WORDS = ["a", "A", "á", "Á", "b", "ä", "Ä"]
PUNCTUATION_SORT = ["ab", "a b", "a-b", "a_b", "a.b", "a b", "aB", "a‐b", "a$b", "a+b", "a€b", "",
                    " ", "-"]


def cases_options() -> None:
    for sensitivity in ("base", "accent", "case", "variant"):
        for usage in (None, "search"):
            options = {"sensitivity": sensitivity} if usage is None else {"sensitivity": sensitivity, "usage": usage}
            sort("options", "sensitivity", "en", options, SENSITIVITY_WORDS, ("O6",),
                 f"sensitivity {sensitivity} is the strength UTS #10 and ECMA-402's table give")
            for x, y in SENSITIVITY_PAIRS:
                pair("options", "sensitivity", "en", options, x, y, ("O6",),
                     f"sensitivity {sensitivity}" + (", usage search" if usage else ""),
                     prov=[SLOTS])
    for x, y in SENSITIVITY_PAIRS[:5]:
        pair("options", "sensitivity", "de", {"sensitivity": "base", "usage": "search"}, x, y, ("O6",),
             "German search at base")
    for ignore in (False, True):
        for sensitivity in (None, "base"):
            options = {"ignorePunctuation": ignore} if sensitivity is None else {"ignorePunctuation": ignore,
                                                                                 "sensitivity": sensitivity}
            sort("options", "ignore-punctuation", "en", options, PUNCTUATION_SORT, ("O5a",),
                 f"ignorePunctuation {str(ignore).lower()} is UTS #10's "
                 "shifted alternate up to CLDR's maxVariable, punctuation: spaces and punctuation, not symbols")
            for x, y in PUNCTUATION_PAIRS:
                # Row O5b joins the pairs at base that hold NBSP (with_compatibility_row): base folds its tertiary
                # difference from a space.
                pair("options", "ignore-punctuation", "en", options, x, y, ("O5a",),
                     f"ignorePunctuation {str(ignore).lower()}" + (" at base" if sensitivity else ""),
                     prov=[SLOTS])
    for x, y in PUNCTUATION_PAIRS[:6]:
        pair("options", "ignore-punctuation", "en", {"ignorePunctuation": True}, x, y, ("O5a",),
             "ignorePunctuation true through localeCompare",
             through="localeCompare")
    numeric_ways = [
        ("en", {"numeric": True}, "the option numeric true"),
        ("en", {"numeric": False}, "the option numeric false"),
        ("en", None, "no option: numeric's default, false"),
        ("en-u-kn", None, "the keyword -u-kn"),
        ("en-u-kn-true", None, "the keyword -u-kn-true"),
        ("en-u-kn-false", None, "the keyword -u-kn-false"),
        ("en-u-kn", {"numeric": False}, "the option numeric false over the keyword -u-kn"),
        ("en-u-kn-false", {"numeric": True}, "the option numeric true over the keyword -u-kn-false"),
        ("de", {"numeric": True}, "German with numeric true"),
        ("de-u-co-phonebk-kn", None, "German phonebook with -u-kn"),
    ]
    for locale, options, what in numeric_ways:
        sort("options", "numeric", locale, options, NUMERIC_WORDS, ("O7",),
             f"{what}; numeric collation compares runs of decimal digits of any "
             "script by value (UTS #35's kn)")
        for x, y in NUMERIC_PAIRS:
            pair("options", "numeric", locale, options, x, y, ("O7",),
                 what, prov=[SLOTS])
    for x, y in NUMERIC_PAIRS[:8]:
        pair("options", "numeric", "en", {"numeric": True}, x, y, ("O7",),
             "numeric true through localeCompare", through="localeCompare")
    case_ways = [
        ("en", {"caseFirst": "upper"}, "caseFirst upper"),
        ("en", {"caseFirst": "lower"}, "caseFirst lower"),
        ("en", {"caseFirst": "false"}, "caseFirst false"),
        ("en", {"caseFirst": "upper", "sensitivity": "case"}, "caseFirst upper with sensitivity case"),
        ("en", {"caseFirst": "lower", "sensitivity": "case"}, "caseFirst lower with sensitivity case"),
        ("en", {"caseFirst": "false", "sensitivity": "case"}, "caseFirst false with sensitivity case"),
        ("en", {"caseFirst": "upper", "sensitivity": "base"}, "caseFirst upper with sensitivity base"),
        ("en-u-kf-upper", None, "the keyword -u-kf-upper"),
        ("en-u-kf-lower", None, "the keyword -u-kf-lower"),
        ("en-u-kf-upper", {"caseFirst": "lower"}, "the option caseFirst lower over the keyword -u-kf-upper"),
        ("en-u-kf-lower", {"caseFirst": "false"}, "the option caseFirst false over the keyword -u-kf-lower"),
        ("de-u-co-phonebk-kf-upper", None, "German phonebook with -u-kf-upper"),
        ("de", {"caseFirst": "upper", "usage": "search"}, "German search with caseFirst upper"),
        ("en-u-kn-kf-upper", None, "the keywords -u-kn and -u-kf-upper together"),
    ]
    for locale, options, what in case_ways:
        words = CASE_WORDS + (["ae", "Ae", "AE", "af"] if "phonebk" in locale else []) + \
            (["a2", "A10", "a10"] if "kn" in locale else [])
        sort("options", "case-first", locale, options, words, ("O8",),
             f"{what}; caseFirst orders case on the tertiary and case levels "
             "(UTS #35's kf)")
        for x, y in CASE_PAIRS:
            pair("options", "case-first", locale, options, x, y, ("O8",),
                 what, prov=[SLOTS])
    for x, y in CASE_PAIRS[:5]:
        pair("options", "case-first", "en", {"caseFirst": "upper"}, x, y, ("O8",),
             "caseFirst upper through localeCompare", through="localeCompare")


# ---- Lone surrogates (row O9) -----------------------------------------------------------------------------------

SURROGATE_PAIRS = [("\ud800", "\udc00"), ("\ud800", "�"), ("\udc00", "�"), ("\ud800", "a"), ("\ud800", "\ud800"),
                   ("\ud800", "\udbff"), ("a\ud800", "a"), ("a\ud800", "a\udc00"), ("\U00010000", "\ud800"),
                   ("\U00010000", "�"), ("\ud800b", "\udc00a"), ("�", "￿"), ("\ud83d", "\U0001f600"),
                   ("\udfff", "一"), ("\ud800", "͸"), ("ä\ud800", "ä\ud800"),
                   ("\ud800", ""), ("\udfff", ""), ("\udbff", "\udc00"), ("\ud800́", "\ud800"),
                   ("́\ud800", "\ud800́"), ("\udc00\ud800", "𐀀"), ("a\udc00b", "a\ud800b"),
                   ("\ud800", ""), ("\ud800", " "), ("\ud800", "\U000e0001"), ("\ud800!", "\ud800?"),
                   ("\ud800a", "\ud800A"), ("\ud800", "\U0010ffff")]


def surrogate_reading() -> str:
    """The CollationTest files' reading of a surrogate, with the lines that hold one (not a pair this vector takes:
    the vectors of lone surrogates do not cite the files, ref:collationtest@1)."""
    (a, b), (c, d) = SURROGATE_LINES["non-ignorable"], SURROGATE_LINES["shifted"]
    return ("a lone surrogate is its code point, which UCA 17's CollationTest files key as an unassigned code point's "
            f"implicit weight (lines {a} to {b} of {COLLATION_TEST_FILES['non-ignorable'][0]} and {c} to {d} of "
            f"{COLLATION_TEST_FILES['shifted'][0]}, the files' reading; this vector is no pair of their lines)")


def cases_surrogates() -> None:
    for options in (None, {"sensitivity": "base"}, {"ignorePunctuation": True}):
        for x, y in SURROGATE_PAIRS:
            pair("surrogates", "lone", "en", options, x, y, ("O9",), surrogate_reading())
    for x, y in SURROGATE_PAIRS[:10]:
        pair("surrogates", "lone", None, None, x, y, ("O9", "O13"),
             "a lone surrogate through localeCompare with no locale", through="localeCompare")
    sort("surrogates", "lone", "en", None, sorted({s for p in SURROGATE_PAIRS for s in p}), ("O9",),
         "lone surrogates among the code points around them")


# ---- The second reference (ref:ucaimplicit@1) -------------------------------------------------------------------

ALLKEYS = "allkeys_CLDR.txt"
# UTS #10 (UCA 17) section 10.1.3, Table 16: the base of a unified ideograph's implicit primary. Core Han is
# Unified_Ideograph in the blocks CJK Unified Ideographs and CJK Compatibility Ideographs; every other Unified_Ideograph
# code point takes the second base.
CORE_HAN_BLOCKS = ((0x4E00, 0x9FFF), (0xF900, 0xFAFF))
CORE_HAN_BASE = 0xFB40
OTHER_HAN_BASE = 0xFB80
STRENGTHS = {None: 3, "variant": 3, "base": 1}
ELEMENT = re.compile(r"\[([.*])([0-9A-F]{4})\.([0-9A-F]{4})\.([0-9A-F]{4})\]")


class ImplicitReference:
    """The UCA over CLDR 48.2.0's allkeys_CLDR.txt with UCA 17's implicit weights (the module's docstring, THE SECOND
    REFERENCE), and the NFD and NFKD of the corpus's Unicode reference over the pinned UCD 17.0.0."""

    def __init__(self, cldr: Path, ucd: Path) -> None:
        try:
            self.ucd = unicoderef.UCD(unicoderef.Pins(unicoderef.PIN_FILE, ucd))
        except unicoderef.PinError as error:
            raise GeneratorError(f"the pinned UCD under {ucd}: {error}") from error
        self.table: dict[tuple[int, ...], tuple[tuple[int, int, int], ...]] = {}
        self.longest = 1
        for number, raw in enumerate(pinned_bytes(cldr, f"{UCA_DIRECTORY}/{ALLKEYS}").decode("utf-8").split("\n"), 1):
            body = raw.split("#", 1)[0].strip()
            if not body or body.startswith("@"):
                continue
            source, separator, weights = body.partition(";")
            weights = weights.strip()
            elements = ELEMENT.findall(weights)
            if not separator or not elements or "".join(f"[{v}{p}.{s}.{t}]" for v, p, s, t in elements) != weights:
                raise GeneratorError(f"{ALLKEYS}:{number}: not `<code points> ; <collation elements>`: {raw!r}")
            key = tuple(int(h, 16) for h in source.split())
            self.table[key] = tuple((int(p, 16), int(s, 16), int(t, 16)) for _, p, s, t in elements)
            self.longest = max(self.longest, len(key))

    @staticmethod
    def code_points(text: str) -> list[int]:
        if is_surrogate_string(text):
            raise GeneratorError(f"the second reference weighs no lone surrogate: {show(text)}")
        return [ord(c) for c in text]

    def nfd(self, text: str) -> list[int]:
        return self.ucd.decompose(self.code_points(text), False)

    def compatibility_form(self, text: str) -> bool:
        """Whether the string's NFKD is not its NFD: it holds a width or compatibility form."""
        if is_surrogate_string(text):
            return False
        cps = [ord(c) for c in text]
        return self.ucd.decompose(cps, True) != self.ucd.decompose(cps, False)

    def is_han_code_point(self, cp: int) -> bool:
        return self.ucd.has("Unified_Ideograph", cp)

    def has_han(self, text: str) -> bool:
        return not is_surrogate_string(text) and any(self.is_han_code_point(cp) for cp in self.nfd(text))

    def is_han_only(self, text: str) -> bool:
        cps = [] if is_surrogate_string(text) else self.nfd(text)
        return bool(cps) and all(self.is_han_code_point(cp) for cp in cps)

    def elements(self, text: str) -> list[tuple[int, int, int]]:
        cps = self.nfd(text)
        if any(self.ucd.ccc[cp] for cp in cps):
            raise GeneratorError(f"the second reference weighs no combining mark: {show(text)}")
        out: list[tuple[int, int, int]] = []
        i = 0
        while i < len(cps):
            for length in range(min(self.longest, len(cps) - i), 0, -1):
                found = self.table.get(tuple(cps[i:i + length]))
                if found is not None:
                    out.extend(found)
                    i += length
                    break
            else:
                cp = cps[i]
                if not self.is_han_code_point(cp):
                    raise GeneratorError(f"the second reference weighs no unlisted code point but a unified "
                                         f"ideograph: U+{cp:04X} in {show(text)}")
                core = any(a <= cp <= b for a, b in CORE_HAN_BLOCKS)
                out.append(((CORE_HAN_BASE if core else OTHER_HAN_BASE) + (cp >> 15), 0x0020, 0x0002))
                out.append(((cp & 0x7FFF) | 0x8000, 0x0000, 0x0000))
                i += 1
        return out

    def key(self, text: str, strength: int) -> tuple[tuple[int, ...], ...]:
        """The sort key, non-ignorable: each level's non-zero weights, to the strength."""
        elements = self.elements(text)
        return tuple(tuple(e[level] for e in elements if e[level]) for level in range(strength))

    def relation(self, x: str, y: str, strength: int) -> str:
        a, b = self.key(x, strength), self.key(y, strength)
        return "<" if a < b else "=" if a == b else ">"

    def answer(self, options: dict | None, strings: Sequence[str], is_sort: bool) -> str:
        """The outcome the case's script renders, as this reference computes it."""
        if set(options or {}) - {"sensitivity"} or (options or {}).get("sensitivity") not in STRENGTHS:
            raise GeneratorError(f"the second reference takes no options but sensitivity base: {options}")
        strength = STRENGTHS[(options or {}).get("sensitivity")]
        if not is_sort:
            return f'value: str:"{self.relation(strings[0], strings[1], strength)}"'
        # Array.prototype.sort is stable: equal strings keep the given order.
        order = sorted(strings, key=lambda s: self.key(s, strength))
        text = order[0]
        for before, after in zip(order, order[1:]):
            text += f" {self.relation(before, after, strength)} {after}"
        return "value: " + typed(text)


SECOND: ImplicitReference | None = None


def han_only_difference(reference: ImplicitReference, case: Case, first: str, second: str) -> bool:
    """Whether the two answers differ only in the order of Han characters among themselves: for a pair, both strings
    are Han; for a sort, both orders agree up to the first Han string and after the last, and hold only Han strings
    between them."""
    options, strings, is_sort = case.second
    if not is_sort:
        return all(reference.is_han_only(s) for s in strings)
    if any(" " in s for s in strings):
        raise GeneratorError(f"{case.file}/{case.group}: a sort the second reference reads holds a space")
    tokens = [decode_str(answer).split(" ") for answer in (first, second)]
    spans = []
    for parts in tokens:
        han = [i for i in range(0, len(parts), 2) if reference.has_han(parts[i])]
        if not han:
            return False
        spans.append((han[0], han[-1]))
        if not all(reference.is_han_only(parts[i]) for i in range(han[0], han[-1] + 1, 2)):
            return False
    (a1, b1), (a2, b2) = spans
    return (a1, b1) == (a2, b2) and tokens[0][:a1] == tokens[1][:a2] and tokens[0][b1 + 1:] == tokens[1][b2 + 1:]


# ---- The reference ------------------------------------------------------------------------------------------


def run_driver(node: str, requests: list[dict], scratch: Path) -> dict[str, str]:
    request_path = scratch / "requests.json"
    answer_path = scratch / "answers.json"
    request_path.write_text(json.dumps(requests), encoding="utf-8")
    environment = {k: v for k, v in os.environ.items() if k not in ("LANG", "LANGUAGE", "TZ") and not k.startswith("LC_")}
    environment["TZ"] = "UTC"
    process = subprocess.run([node, str(INTLREF), str(CORPUS / "prelude.js"), str(request_path), str(answer_path)],
                             env=environment, capture_output=True, text=True)
    if process.returncode != 0:
        raise GeneratorError(f"intlref.mjs exited with {process.returncode}: {process.stderr.strip()}")
    answers = json.loads(answer_path.read_text(encoding="utf-8"))
    for key, value in REFERENCE_VERSIONS.items():
        if answers["versions"].get(key) != value:
            raise GeneratorError(f"the reference's {key} is {answers['versions'].get(key)}, not {value}")
    return {a["id"]: a["outcome"] for a in answers["answers"]}


def decode_str(rendered: str) -> str:
    if not rendered.startswith('value: str:"') or not rendered.endswith('"'):
        raise GeneratorError(f"the reference answered {rendered!r}, not a string")
    body = rendered[len('value: str:"'):-1]
    out: list[int] = []
    i = 0
    while i < len(body):
        if body[i] == "\\":
            if body[i + 1] == "u":
                out.append(int(body[i + 2:i + 6], 16))
                i += 6
                continue
            out.append(ord(body[i + 1]))
            i += 2
            continue
        out.append(ord(body[i]))
        i += 1
    data = b"".join(u.to_bytes(2, "little") for u in out)
    return data.decode("utf-16-le", "surrogatepass")


# The sorts whose neighbours are vectored pair by pair: the reference sorts each list once (a request the area does
# not keep), and each two neighbours of its order become a vector.
NEIGHBOUR_LISTS = {
    "scripts-en": SCRIPT_WORDS, "punctuation-en": PUNCTUATION_WORDS, "latin-en": LATIN_WORDS, "han-en": HAN,
    **{f"run-{name}": words for name, words in SCRIPT_RUNS.items()},
}


def neighbours(node: str, scratch: Path) -> dict[str, list[tuple[str, str]]]:
    requests = [{"id": key, "script": f"JSON.stringify([{','.join(js(w) for w in words)}]"
                                      f".sort(new Intl.Collator(\"en\").compare))"}
                for key, words in NEIGHBOUR_LISTS.items()]
    answers = run_driver(node, requests, scratch)
    out = {}
    for key in NEIGHBOUR_LISTS:
        order = json.loads(decode_str(answers[key]), strict=False)
        out[key] = list(zip(order, order[1:]))
    return out


def check_rows(cases: Sequence[Case]) -> list[str]:
    reached = {row for case in cases for row in case.rows}
    return [row for row in BEHAVIOURAL if row not in reached]


def check_clauses(cases: Sequence[Case], spec: Path) -> list[str]:
    ids = set()
    for name in ("collator.html", "locale-sensitive-functions.html"):
        ids.update(re.findall(r'id="([^"]+)"', (spec / "spec" / name).read_text(encoding="utf-8")))
    cited = {entry[len(ECMA402):] for case in cases for entry in case.prov if entry.startswith(ECMA402)}
    return sorted(cited - ids)


def read_divergences(path: Path) -> dict[str, dict[str, str]]:
    """collation-divergences.txt, as intl-divergences.txt: `reason <key>: <text>`, then `<engine> <key> <id>`."""
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


# ---- Writing --------------------------------------------------------------------------------------------------


def render(case: Case, identifier: str, out: str, vm_out: str | None, divergence: dict[str, str]) -> str:
    cls = "normative"
    per_engine: dict[str, str] = {}
    notes = [case.note]
    prov = list(case.prov)
    if case.vm_moves:
        cls = "impl-defined"
        per_engine = {"broiler-js": out, "broiler-vm": vm_out}
        notes.append(VM_REASON.format(moves=" and ".join(case.vm_moves)))
    if case.second_out is not None:
        prov.append(REF_IMPLICIT)
        if case.second_out != out:
            if case.vm_moves:
                raise GeneratorError(f"{identifier}: a vector whose locale moves and whose Han order moves")
            cls = "impl-defined"
            per_engine = {"broiler-js": case.second_out, "broiler-vm": case.second_out}
            notes.append(HAN_MOVES)
        elif case.han:
            notes.append(HAN_SAME)
    for engine in divergence:
        if cls == "impl-defined" and engine not in per_engine:
            raise GeneratorError(f"{identifier}: an impl-defined vector classified for {engine}, whose case states "
                                 "no per-engine expectation for it (SCHEMA.md section 5)")
    if divergence and cls == "normative":
        cls = "divergence"
    lines = [f"id: {identifier}", "area: intl", f"op: {case.op}",
             "in: " + (" ".join(case.inputs) if case.inputs else "undefined"), f"out: {out}", f"class: {cls}"]
    for engine in sorted(per_engine):
        lines.append(f"per-engine.{engine}: {per_engine[engine]}")
    for engine in sorted(divergence):
        lines.append(f"divergence.{engine}: {divergence[engine]}")
    lines.append("prov: " + " ".join(dict.fromkeys(prov)))
    lines.append(f"script: {case.script}")
    note = "; ".join(n.rstrip(".") for n in notes)
    if rows_named(note) != set(case.rows):
        raise GeneratorError(f"{identifier}: its note names the rows {sorted(rows_named(note))}, its case carries "
                             f"{sorted(case.rows)}")
    lines.append("note: " + note)
    lines.append(f"since: {SINCE}")
    for line in lines:
        if any(ord(c) > 0x7E or ord(c) < 0x20 for c in line):
            raise GeneratorError(f"{identifier}: a field holds a character outside printable ASCII: {line!r}")
    return "\n".join(lines)


HEADER = [
    "Expected values: ICU 78.3 with CLDR 48.0 through Node.js v26.12.0, the reference bundle sjl-9-001's reference.txt",
    "names (ref:intlref@1), held, where a vector takes two consecutive lines of CLDR 48.2.0's archived CollationTest",
    "files, to the order the file gives them (ref:collationtest@1). The root's scripts and Han vectors are computed again",
    "by the UCA over CLDR 48.2.0's allkeys_CLDR.txt with UCA 17's implicit weights (ref:ucaimplicit@1), the other Han",
    "order UTS #35 Part 5 (CLDR 48) allows: a Han vector whose answer moves with that order is impl-defined, with that",
    "order's answer for both engines. Each note names rows of bundle sjl-17-001's correspondence (Broiler.VM's",
    "docs/evidence/sjl-17-001/correspondence.md), the rows its case carries. The area field is intl: the schema has no",
    "collation area (generate_collation_vectors.py, THE AREA FIELD).",
]


def generate(node: str, cldr: Path, out: Path, spec: Path | None, ucd: Path = DEFAULT_UCD) -> dict[str, int]:
    global SECOND
    CASES.clear()
    SEEN.clear()
    SECOND = ImplicitReference(cldr, ucd)
    root_xml = " ".join(pinned_bytes(cldr, ROOT_XML).decode("utf-8").split())
    if RADICAL_STROKE not in root_xml:
        raise GeneratorError(f"{ROOT_XML} no longer names {RADICAL_STROKE!r}, which the Han vectors' notes quote")
    with tempfile.TemporaryDirectory() as scratch:
        order = neighbours(node, Path(scratch))
        cases_collation_test(cldr)
        cases_root(order)
        cases_tailorings()
        cases_options()
        cases_surrogates()
        cases = list(CASES)
        missing = check_rows(cases)
        if missing:
            raise GeneratorError(f"no vector reaches the rows {missing}")
        if spec is not None:
            unknown = check_clauses(cases, spec)
            if unknown:
                raise GeneratorError(f"clause ids the pinned text does not have: {unknown}")
        scripts: dict[str, int] = {}
        for index, case in enumerate(cases):
            if case.script in scripts:
                raise GeneratorError(f"the script of {case.file}/{case.group} is vectored twice: {case.script}")
            scripts[case.script] = index
        requests = []
        for index, case in enumerate(cases):
            requests.append({"id": f"c{index}", "script": case.script})
            moved, moves = vm_script(case.script)
            if moves:
                case.vm_moves = moves
                case.rows = tuple(dict.fromkeys(case.rows + (VM_ROW,)))
                requests.append({"id": f"c{index}-vm", "script": moved})
        answers = run_driver(node, requests, Path(scratch))
    problems = []
    for index, case in enumerate(cases):
        answer = answers[f"c{index}"]
        if case.second is not None:
            # ref:ucaimplicit@1: held to the reference wherever the Han order does not enter.
            if (case.file, case.group) not in {("root", g) for g in SECOND_REFERENCE_GROUPS}:
                raise GeneratorError(f"{case.file}/{case.group}: the second reference computes only the root's "
                                     f"groups {SECOND_REFERENCE_GROUPS}")
            options, strings, is_sort = case.second
            case.han = any(SECOND.has_han(s) for s in strings)
            case.second_out = SECOND.answer(options, strings, is_sort)
            if case.second_out != answer and not han_only_difference(SECOND, case, answer, case.second_out):
                problems.append(f"{case.file}/{case.group}: the second reference answered {case.second_out} and the "
                                f"reference {answer}, not only in the order of Han characters: {case.script}")
        if case.expect is not None and answer != case.expect:
            problems.append(f"{case.file}/{case.group}: the reference answered {answer} where {case.expect} is "
                            f"required: {case.script}")
        if case.forbid is not None and answer == case.forbid:
            problems.append(f"{case.file}/{case.group}: the reference answered {answer}, which the CollationTest file "
                            f"forbids: {case.script}")
        if not answer.startswith("value: "):
            problems.append(f"{case.file}/{case.group}: the reference answered {answer}: {case.script}")
    if problems:
        raise GeneratorError("\n".join(problems))
    divergences = read_divergences(DIVERGENCES)
    rendered: dict[str, list[str]] = {name: [] for name in FILES}
    numbers: dict[tuple[str, str], int] = {}
    for index, case in enumerate(cases):
        key = (case.file, case.group)
        numbers[key] = numbers.get(key, 0) + 1
        identifier = f"intl/collation/{case.file}/{case.group}/{numbers[key]:04d}"
        vm_out = answers.get(f"c{index}-vm")
        rendered[case.file].append(render(case, identifier, answers[f"c{index}"], vm_out,
                                          divergences.pop(identifier, {})))
    if divergences:
        raise GeneratorError(f"{DIVERGENCES.name} classifies ids the area does not have: {sorted(divergences)[:10]}")
    out.mkdir(parents=True, exist_ok=True)
    counts = {}
    for name, title in FILES.items():
        head = [f"Generated by scripts/conformance/{GENERATOR}; do not edit by hand.",
                f"The collation vectors (milestone SJL-17.2), {name}.vectors: {title}.", *HEADER]
        text = "\n".join(f"# {line}".rstrip() for line in head)
        text += "\nschema: 1\n\n" + "\n\n".join(rendered[name]) + "\n"
        (out / f"{name}.vectors").write_text(text, encoding="utf-8", newline="\n")
        counts[name] = len(rendered[name])
    return counts


def main(argv: Sequence[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n", 1)[0])
    parser.add_argument("--node", required=True, help="the reference Node.js v26.12.0")
    parser.add_argument("--out", default=str(DEFAULT_OUT))
    parser.add_argument("--cldr", default=str(DEFAULT_CLDR), help="the data home's CLDR archive, with its cldr.pin")
    parser.add_argument("--ucd", default=str(DEFAULT_UCD),
                        help="the directory of the pinned UCD 17.0.0 files sources/unicode.pin names")
    parser.add_argument("--spec", help="a checkout of tc39/ecma402 at the pinned revision, to check clause ids")
    arguments = parser.parse_args(argv)
    try:
        counts = generate(arguments.node, Path(arguments.cldr), Path(arguments.out),
                          Path(arguments.spec) if arguments.spec else None, Path(arguments.ucd))
    except GeneratorError as error:
        print(f"{GENERATOR}: {error}", file=sys.stderr)
        return 2
    for name, count in counts.items():
        print(f"{name}.vectors: {count} vectors")
    return 0


if __name__ == "__main__":
    sys.exit(main())
