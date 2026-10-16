#!/usr/bin/env python3
# PROVENANCE (corpus revision 18, milestone SJL-7.3, 2026-10-16). This file is a copy of Broiler.Unicode's
# scripts/reference-arms/unicoderef.py, git blob d120e4c1077a3448be2e56f5af97730957f2103d, as committed at
# Broiler.Unicode efbdc1dbcb9a42fa3abe254f894a7f2241a69601 ("SJL-7.2 (reference arms): the population, the
# arbiter and the classification"). Its documentation is that commit's scripts/reference-arms/README.md (section
# "The arbiter"), and its tests, scripts/reference-arms/tests/test_unicoderef_standards.py and
# test_unicoderef_cases.py, are copied from the same commit into scripts/conformance/tests/, each marked the same way.
# It is the Unicode area's reference computation, `ref:unicoderef@1`: its REFERENCE_VERSION is unchanged,
# because no computation changed. The corpus changes only what it needs, each change marked in place with
# "CORPUS CHANGE" and listed here:
#   1. PIN_FILE is the corpus's pin, conformance/vectors/sources/unicode.pin, not Broiler.Unicode's.
#   2. Pins reads that pin's stanzas (`file` or `spec-table`, then `path`, `bytes`, `sha256`) and finds each
#      file under a directory its caller names (the generator's `--ucd`, normally Broiler.Unicode's
#      data/unicode/pins/), where the original read one-line entries relative to the pin's own directory;
#      the checks of length and SHA-256 before a byte is used, and the refusal (PinError), are unchanged.
#   3. main() takes that directory as `--ucd`, and database() reads it from the environment variable
#      BROILER_UNICODE_PINS, defaulting to a Broiler.Unicode checkout beside this repository's.
# The docstring below is the original's: where it says data/unicode/pins/unicode.pin, read the corpus's pin
# and the --ucd directory. Nothing else differs from the original, whose `git hash-object` is the blob above.
"""The arbiter of bundle sjl-7-001's reference-arm comparison: Unicode's algorithms, written from the standards.

    python3 scripts/reference-arms/unicoderef.py --population <directory> --out <directory> [--jobs N]

docs/evidence/sjl-7-001/reference-arms.md names it: "an independent reference computation, written in Python
from the standards' text - the UCD's data files, UAX #15, UAX #29, the Unicode Standard's section 3.13 and
ECMA-262's tables and their algorithms - reading the same pinned inputs and no generated table". This module
reads the files data/unicode/pins/unicode.pin names, each checked against the pin's length and SHA-256 before
a byte of it is used, and nothing else: no table of this repository's src/, no output of its generators, no
output of either engine, no host Unicode data (Python's unicodedata is never imported). A pinned file whose
bytes are not the pin's is refused (PinError; the command exits 2).

What it computes, each from the clause named beside it:

- every set the lookups answer, as range lists: General_Category's values and groups (UnicodeData.txt, the
  groups from PropertyValueAliases.txt's own comments), every binary property of ECMA-262's table
  (PropList.txt, DerivedCoreProperties.txt, DerivedNormalizationProps.txt, DerivedBinaryProperties.txt,
  emoji-data.txt; Any, ASCII and Assigned as UTS #18 defines them), Script (Scripts.txt, Unknown by default)
  and Script_Extensions (ScriptExtensions.txt, a code point it does not list taking its Script, UAX #24);
- the exact lookup of a lone name and of a Name=Value pair, as ECMA-262's static semantics of
  UnicodePropertyValueExpression and UnicodeMatchProperty / UnicodeMatchPropertyValue admit them: only the
  names and aliases of its three tables and the values PropertyValueAliases.txt lists for General_Category
  and Script (Script_Extensions takes Script's), compared exactly; a binary property only alone; a property
  of strings only alone and only under the v flag (`resolve_lone(name, unicode_sets=True)`);
- simple case folding (CaseFolding.txt, status C and S), orbits, and ECMA-262's Canonicalize without u
  (22.2.2.7.3): the full default upper case of one code unit, kept only when it is one code unit and does
  not map a unit at or above 128 below it;
- full case mapping (The Unicode Standard, section 3.13, Default Case Conversion; SpecialCasing.txt with its
  conditions Final_Sigma, After_Soft_Dotted, More_Above, Before_Dot, After_I and their negations) in the
  default, Turkic (tr, az) and Lithuanian (lt) languages; a lone surrogate maps to itself;
- NFC, NFD, NFKC and NFKD (UAX #15: full decomposition, canonical ordering, canonical composition with
  Full_Composition_Exclusion, Hangul algorithmically); a lone surrogate is a starter with no decomposition;
- membership of a string in each of the seven properties of strings (emoji-sequences.txt,
  emoji-zwj-sequences.txt; RGI_Emoji their union);
- UAX #29's extended grapheme cluster, word and sentence boundaries (Unicode 17.0.0's rules GB1-GB999,
  WB1-WB999, SB1-SB998), as positions in UTF-16 code units, 0 and the length included; a lone surrogate is a
  code point of its own, with the property values the files' @missing lines give it.

**A string is a list of UTF-16 code units** at the edges (the population's notation) and a list of code
points inside: a high surrogate followed by a low one is one code point, any other surrogate a code point of
its own.

The command writes, for every population file it finds, the arbiter's answer to every operation the rule
runs on each input, one line per input line, in the same order (the README gives every format), and
sets.tsv, string-sets.tsv, folding.tsv, orbits.tsv and canonicalize.tsv for the code-point operations of
class U1, with reference-manifest.json recording each output's line count and SHA-256.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import multiprocessing
import re
import sys
import time
from bisect import bisect_right
from collections import defaultdict
from functools import lru_cache
from itertools import groupby
from pathlib import Path

HERE = Path(__file__).resolve().parent
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

REFERENCE_VERSION = "unicoderef@1"
REPOSITORY = HERE.parents[1]
# CORPUS CHANGE 1: the corpus's pin. The original read REPOSITORY / "data" / "unicode" / "pins" / "unicode.pin".
PIN_FILE = REPOSITORY / "conformance" / "vectors" / "sources" / "unicode.pin"
# CORPUS CHANGE 3: where database() finds the pinned files, unless BROILER_UNICODE_PINS names the directory.
PINS_DIRECTORY_VARIABLE = "BROILER_UNICODE_PINS"
DEFAULT_PINS_DIRECTORY = REPOSITORY.parent / "broiler.unicode" / "data" / "unicode" / "pins"
CODE_POINTS = 0x110000
REFUSED = "refused"
OVER = "over"

LANGUAGES = ("default", "turkic", "lithuanian")
CASING_OPERATIONS = (("upper", "default"), ("lower", "default"), ("upper", "turkic"), ("lower", "turkic"),
                     ("upper", "lithuanian"), ("lower", "lithuanian"))
FORMS = ("NFC", "NFD", "NFKC", "NFKD")
LANGUAGE_OF_TAG = {"tr": "turkic", "az": "turkic", "lt": "lithuanian"}

# Hangul, UAX #15 section 16 (The Unicode Standard, section 3.12).
S_BASE, L_BASE, V_BASE, T_BASE = 0xAC00, 0x1100, 0x1161, 0x11A7
L_COUNT, V_COUNT, T_COUNT = 19, 21, 28
N_COUNT = V_COUNT * T_COUNT
S_COUNT = L_COUNT * N_COUNT

HEX4 = [f"{u:04X}" for u in range(0x10000)]


# --------------------------------------------------------------------------------------------------
# The pin.

class PinError(Exception):
    """A pinned input whose bytes are not the pin's, or that no pin line names: it is refused."""


class Pins:
    """CORPUS CHANGE 2. The corpus's unicode.pin: stanzas of `file <name>` (or `spec-table <name>`), `path <path
    under the directory>`, `bytes <length>` and `sha256 <digest>`, each file found under `directory`, the
    directory the caller names (the generator's --ucd). The original read Broiler.Unicode's one-line entries,
    `file <path> <bytes> <sha256>` relative to the pin's directory and `spec-table <path> <bytes> <sha256>`
    relative to the repository root; an entry's key stays its name, so key() and read() are unchanged."""

    def __init__(self, pin_file: Path = PIN_FILE, directory: Path | None = None) -> None:
        self.pin_file = Path(pin_file).resolve()
        if directory is None:
            raise PinError("no directory of pinned files was given (the generator's --ucd)")
        self.directory = Path(directory).resolve()
        raw = self.pin_file.read_bytes()
        self.sha256 = hashlib.sha256(raw).hexdigest()
        self.version = None
        self.entries: dict[str, tuple[Path, int, str]] = {}
        stanzas: list[dict[str, str]] = []
        for line in raw.decode("utf-8").splitlines():
            fields = line.split()
            if not fields or fields[0].startswith("#"):
                continue
            if len(fields) != 2:
                raise PinError(f"malformed pin line: {line!r}")
            key, value = fields
            if key == "version":
                if self.version is not None or stanzas:
                    raise PinError(f"a second or misplaced version line: {line!r}")
                self.version = value
            elif key in ("file", "spec-table"):
                stanzas.append({"kind": key, "name": value})
            elif key in ("path", "bytes", "sha256") and stanzas and key not in stanzas[-1]:
                stanzas[-1][key] = value
            else:
                raise PinError(f"malformed pin line: {line!r}")
        for stanza in stanzas:
            if set(stanza) != {"kind", "name", "path", "bytes", "sha256"} or stanza["name"] in self.entries:
                raise PinError(f"an incomplete or repeated pin stanza: {stanza}")
            path = (self.directory / stanza["path"]).resolve()
            if self.directory not in path.parents or path.name != stanza["name"]:
                raise PinError(f"{stanza['name']}: its path {stanza['path']} is not a file of that name under the directory")
            self.entries[stanza["name"]] = (path, int(stanza["bytes"]), stanza["sha256"].lower())
        self.read_files: dict[str, str] = {}

    def key(self, name: str) -> str:
        matches = [k for k in self.entries if k == name or k.endswith("/" + name)]
        if len(matches) != 1:
            raise PinError(f"{name}: {len(matches)} pin lines name it")
        return matches[0]

    def read_bytes(self, name: str) -> bytes:
        key = self.key(name)
        path, size, digest = self.entries[key]
        try:
            data = path.read_bytes()
        except OSError as error:
            raise PinError(f"{key}: {error}") from error
        actual = hashlib.sha256(data).hexdigest()
        if len(data) != size or actual != digest:
            raise PinError(f"{key}: {len(data)} bytes with SHA-256 {actual}; the pin says {size} bytes with {digest}")
        self.read_files[key] = digest
        return data

    def read(self, name: str) -> str:
        return self.read_bytes(name).decode("utf-8")


# --------------------------------------------------------------------------------------------------
# Reading the files, and range lists.

def records(text: str):
    """The data lines of a UCD file: the part before '#', split at ';', each field stripped."""
    for line in text.split("\n"):
        data = line.split("#", 1)[0]
        if data.strip():
            yield [field.strip() for field in data.split(";")]


def code_range(field: str) -> tuple[int, int]:
    first, _, last = field.partition("..")
    return int(first, 16), int(last or first, 16)


def code_points_of(field: str) -> tuple[int, ...]:
    return tuple(int(h, 16) for h in field.split())


def normalize_ranges(ranges) -> list[tuple[int, int]]:
    out: list[list[int]] = []
    for first, last in sorted(ranges):
        if out and first <= out[-1][1] + 1:
            out[-1][1] = max(out[-1][1], last)
        else:
            out.append([first, last])
    return [(a, b) for a, b in out]


def ranges_of_points(points) -> list[tuple[int, int]]:
    return normalize_ranges((p, p) for p in points)


def subtract_points(ranges, excluded: set[int]) -> list[tuple[int, int]]:
    out = []
    marks = sorted(excluded)
    for first, last in ranges:
        start = first
        for p in marks[bisect_right(marks, first - 1):bisect_right(marks, last)]:
            if start <= p - 1:
                out.append((start, p - 1))
            start = p + 1
        if start <= last:
            out.append((start, last))
    return out


def subtract_ranges(ranges, removed) -> list[tuple[int, int]]:
    flags = bytearray(CODE_POINTS)
    for a, b in ranges:
        flags[a:b + 1] = b"\x01" * (b - a + 1)
    for a, b in removed:
        flags[a:b + 1] = bytes(b - a + 1)
    return ranges_of_flags(flags)


def complement(ranges) -> list[tuple[int, int]]:
    out = []
    start = 0
    for a, b in normalize_ranges(ranges):
        if start < a:
            out.append((start, a - 1))
        start = b + 1
    if start < CODE_POINTS:
        out.append((start, CODE_POINTS - 1))
    return out


def runs(values) -> list[tuple[int, int, int]]:
    """(first, last, value) for each run of equal values of a per-code-point array."""
    out = []
    start = 0
    for value, group in groupby(values):
        length = sum(1 for _ in group)
        out.append((start, start + length - 1, value))
        start += length
    return out


def ranges_of_flags(flags: bytearray) -> list[tuple[int, int]]:
    return [(a, b) for a, b, v in runs(flags) if v]


def flags_of(ranges) -> bytearray:
    flags = bytearray(CODE_POINTS)
    for a, b in ranges:
        flags[a:b + 1] = b"\x01" * (b - a + 1)
    return flags


def format_ranges(ranges) -> str:
    """Ranges in UCD notation, separated by single spaces: `0041..005A 00B5`; the empty set is `-`."""
    if not ranges:
        return "-"
    return " ".join(f"{a:04X}" if a == b else f"{a:04X}..{b:04X}" for a, b in ranges)


def parse_ranges(text: str) -> list[tuple[int, int]]:
    if text == "-":
        return []
    return [code_range(part) for part in text.split(" ")]


def ranges_contain(ranges, cp: int) -> bool:
    starts = [a for a, _ in ranges]
    i = bisect_right(starts, cp) - 1
    return i >= 0 and ranges[i][1] >= cp


# --------------------------------------------------------------------------------------------------
# UTF-16.

def code_points(units) -> list[int]:
    """The code points of a list of UTF-16 code units: a high surrogate followed by a low one is one code
    point, any other surrogate a code point of its own (ECMA-262 6.1.4, CodePointAt)."""
    out = []
    i, n = 0, len(units)
    while i < n:
        u = units[i]
        if 0xD800 <= u <= 0xDBFF and i + 1 < n and 0xDC00 <= units[i + 1] <= 0xDFFF:
            out.append(0x10000 + ((u - 0xD800) << 10) + (units[i + 1] - 0xDC00))
            i += 2
        else:
            out.append(u)
            i += 1
    return out


def code_units(cps) -> list[int]:
    out = []
    for c in cps:
        if c >= 0x10000:
            c -= 0x10000
            out.append(0xD800 + (c >> 10))
            out.append(0xDC00 + (c & 0x3FF))
        else:
            out.append(c)
    return out


def parse_units(field: str) -> list[int]:
    """A text field: UTF-16 code units in 4-digit hexadecimal separated by single spaces; empty is ""."""
    return [int(h, 16) for h in field.split(" ")] if field else []


def format_units(units) -> str:
    return " ".join([HEX4[u] for u in units])


def format_cps(cps) -> str:
    return format_units(code_units(cps))


def digest_notation(length: int, sha256: str) -> str:
    """A result too long to write: its length in code units and the SHA-256 of its UTF-16LE bytes."""
    return f"len:{length}:sha256:{sha256}"


def units_digest(units) -> str:
    return digest_notation(len(units), hashlib.sha256(utf16le(units)).hexdigest())


def utf16le(units) -> bytes:
    return b"".join(u.to_bytes(2, "little") for u in units)


# --------------------------------------------------------------------------------------------------
# The database.

class UCD:
    """Every table the arbiter reads, built from the pinned files and nothing else."""

    def __init__(self, pins: Pins | None = None) -> None:
        self.pins = pins or Pins()  # CORPUS CHANGE 2: Pins() with no directory now refuses (PinError)
        self._load_unicode_data()
        self._load_names()
        self._load_binary_properties()
        self._load_scripts()
        self._load_case_folding()
        self._load_special_casing()
        self._load_normalization()
        self._load_breaks()
        self._load_string_properties()
        self._sets: dict[str, list[tuple[int, int]]] | None = None

    # ---- UnicodeData.txt: General_Category, Canonical_Combining_Class, decompositions, simple case mappings.

    def _load_unicode_data(self) -> None:
        text = self.pins.read("UnicodeData.txt")
        self.gc_names = ["Cn"]  # PropertyValueAliases.txt: @missing General_Category Unassigned
        index = {"Cn": 0}
        self.gc = bytearray(CODE_POINTS)
        self.ccc = bytearray(CODE_POINTS)
        self.canonical: dict[int, tuple[int, ...]] = {}
        self.compatibility: dict[int, tuple[int, ...]] = {}
        self.simple_upper: dict[int, int] = {}
        self.simple_lower: dict[int, int] = {}
        self.simple_title: dict[int, int] = {}
        first = None
        for line in text.split("\n"):
            if not line:
                continue
            f = line.split(";")
            cp = int(f[0], 16)
            if f[2] not in index:
                index[f[2]] = len(self.gc_names)
                self.gc_names.append(f[2])
            if f[1].endswith(", First>"):
                first = cp
                continue
            start = first if f[1].endswith(", Last>") else cp
            first = None
            self.gc[start:cp + 1] = bytes([index[f[2]]]) * (cp - start + 1)
            ccc = int(f[3])
            if ccc:
                self.ccc[start:cp + 1] = bytes([ccc]) * (cp - start + 1)
            if f[5]:
                if f[5].startswith("<"):
                    self.compatibility[cp] = code_points_of(f[5].split(">", 1)[1])
                else:
                    self.canonical[cp] = code_points_of(f[5])
            if f[12]:
                self.simple_upper[cp] = int(f[12], 16)
            if f[13]:
                self.simple_lower[cp] = int(f[13], 16)
            if f[14]:
                self.simple_title[cp] = int(f[14], 16)
        self.gc_index = index

    def general_category(self, cp: int) -> str:
        return self.gc_names[self.gc[cp]]

    # ---- Names: PropertyAliases.txt, PropertyValueAliases.txt and ECMA-262's three tables.

    def _load_names(self) -> None:
        self.property_aliases: list[list[str]] = [rec for rec in records(self.pins.read("PropertyAliases.txt"))]
        self.property_short = {alias: rec[0] for rec in self.property_aliases for alias in rec}
        self.property_long = {alias: rec[1] for rec in self.property_aliases for alias in rec}
        self.value_aliases: dict[str, list[list[str]]] = {}
        self.gc_groups: dict[str, list[str]] = {}
        for line in self.pins.read("PropertyValueAliases.txt").split("\n"):
            data, _, comment = line.partition("#")
            if not data.strip():
                continue
            rec = [field.strip() for field in data.split(";")]
            self.value_aliases.setdefault(rec[0], []).append(rec[1:])
            if rec[0] == "gc" and "|" in comment:
                self.gc_groups[rec[1]] = [member.strip() for member in comment.split("|")]

        self.ecma_binary = dict(parse_ecma_table(self.pins.read("table-binary-unicode-properties.html")))
        self.ecma_nonbinary = dict(parse_ecma_table(self.pins.read("table-nonbinary-unicode-properties.html")))
        self.ecma_strings = [alias for alias, _ in parse_ecma_table(self.pins.read("table-binary-unicode-properties-of-strings.html"))]

        # The values of each non-binary property: General_Category's and Script's lines of
        # PropertyValueAliases.txt; Script_Extensions has none and "uses the same values as Script".
        self.values_of: dict[str, dict[str, str]] = {}
        self.value_long: dict[str, dict[str, str]] = {}
        for canonical in sorted(set(self.ecma_nonbinary.values())):
            short = self.property_short[canonical]
            key = short if short in self.value_aliases else ("sc" if short == "scx" else None)
            if key is None:
                raise PinError(f"PropertyValueAliases.txt lists no values for {canonical}")
            aliases = {}
            for rec in self.value_aliases[key]:
                for alias in rec:
                    aliases[alias] = rec[1]
            self.values_of[canonical] = aliases
            self.value_long[canonical] = {rec[0]: rec[1] for rec in self.value_aliases[key]}
        gc_values = self.values_of["General_Category"]
        clashes = (set(gc_values) & set(self.ecma_binary)) | (set(gc_values) & set(self.ecma_strings)) | (set(self.ecma_binary) & set(self.ecma_strings))
        if clashes:
            raise PinError(f"a lone name names two things: {sorted(clashes)}")

    # ---- Binary properties.

    def _load_binary_properties(self) -> None:
        collected: dict[str, list[tuple[int, int]]] = defaultdict(list)
        incb: dict[str, list[tuple[int, int]]] = defaultdict(list)
        for name in ("PropList.txt", "DerivedCoreProperties.txt", "DerivedNormalizationProps.txt",
                     "DerivedBinaryProperties.txt", "emoji-data.txt"):
            for rec in records(self.pins.read(name)):
                if len(rec) == 2:
                    collected[rec[1]].append(code_range(rec[0]))
                elif rec[1] == "InCB":
                    incb[rec[2]].append(code_range(rec[0]))
        self.binary: dict[str, list[tuple[int, int]]] = {name: normalize_ranges(r) for name, r in collected.items()}
        # UTS #18 (which ECMA-262's table cites for these three): Any, ASCII, Assigned.
        self.binary["Any"] = [(0, CODE_POINTS - 1)]
        self.binary["ASCII"] = [(0, 0x7F)]
        self.binary["Assigned"] = normalize_ranges((a, b) for a, b, v in runs(self.gc) if v != 0)
        missing = sorted(set(self.ecma_binary.values()) - set(self.binary))
        if missing:
            raise PinError(f"no pinned file gives the binary properties {missing}")
        self.cased = flags_of(self.binary["Cased"])
        self.case_ignorable = flags_of(self.binary["Case_Ignorable"])
        self.soft_dotted = flags_of(self.binary["Soft_Dotted"])
        self.ext_pict = flags_of(self.binary["Extended_Pictographic"])
        self.ideographic = flags_of(self.binary["Ideographic"])
        self._starts: dict[str, list[int]] = {}
        self.incb_names = ["None", "Linker", "Consonant", "Extend"]
        self.incb = bytearray(CODE_POINTS)
        for value, ranges in incb.items():
            for a, b in ranges:
                self.incb[a:b + 1] = bytes([self.incb_names.index(value)]) * (b - a + 1)

    def has(self, prop: str, cp: int) -> bool:
        ranges = self.binary[prop]
        starts = self._starts.get(prop)
        if starts is None:
            starts = self._starts[prop] = [a for a, _ in ranges]
        i = bisect_right(starts, cp) - 1
        return i >= 0 and ranges[i][1] >= cp

    # ---- Scripts.txt and ScriptExtensions.txt (UAX #24).

    def _load_scripts(self) -> None:
        to_short = {alias: rec[0] for rec in self.value_aliases["sc"] for alias in rec}
        self.sc_names = ["Zzzz"]  # Scripts.txt: @missing Unknown
        index = {"Zzzz": 0}
        self.sc = bytearray(CODE_POINTS)
        for rec in records(self.pins.read("Scripts.txt")):
            a, b = code_range(rec[0])
            short = to_short[rec[1]]
            if short not in index:
                index[short] = len(self.sc_names)
                self.sc_names.append(short)
            self.sc[a:b + 1] = bytes([index[short]]) * (b - a + 1)
        self.scx_explicit: dict[int, tuple[str, ...]] = {}
        for rec in records(self.pins.read("ScriptExtensions.txt")):
            a, b = code_range(rec[0])
            values = tuple(sorted(to_short[v] for v in rec[1].split()))
            for cp in range(a, b + 1):
                self.scx_explicit[cp] = values

    def script(self, cp: int) -> str:
        return self.sc_names[self.sc[cp]]

    def script_extensions(self, cp: int) -> tuple[str, ...]:
        return self.scx_explicit.get(cp) or (self.script(cp),)

    # ---- CaseFolding.txt.

    def _load_case_folding(self) -> None:
        self.scf: dict[int, int] = {}
        for rec in records(self.pins.read("CaseFolding.txt")):
            if rec[1] in ("C", "S"):
                self.scf[int(rec[0], 16)] = int(rec[2], 16)
        members: dict[int, list[int]] = defaultdict(list)
        for c, target in self.scf.items():
            members[target].append(c)
        self.orbits: dict[int, tuple[int, ...]] = {}
        for target, sources in members.items():
            orbit = sorted(set(sources) | ({target} if self.scf.get(target, target) == target else set()))
            for c in orbit:
                self.orbits[c] = tuple(orbit)

    def fold(self, cp: int) -> int:
        """Simple case folding: CaseFolding.txt's status C and S mappings, every other code point itself."""
        return self.scf.get(cp, cp)

    def orbit(self, cp: int) -> tuple[int, ...]:
        """Every code point whose simple case folding is cp's, cp included."""
        return self.orbits.get(cp, (cp,))

    def canonicalize_without_u(self, unit: int) -> int:
        """ECMA-262 22.2.2.7.3 Canonicalize (rer, ch), rer.[[IgnoreCase]] true and neither u nor v: the full
        default upper case of the one-unit string; ch itself unless that is one code unit, and ch itself when
        ch is at or above 128 and the unit below it."""
        upper = code_units(self.change_case([unit], True, "default"))
        if len(upper) != 1:
            return unit
        cu = upper[0]
        if unit >= 128 and cu < 128:
            return unit
        return cu

    # ---- SpecialCasing.txt: The Unicode Standard section 3.13.

    def _load_special_casing(self) -> None:
        self.special_unconditional: dict[int, tuple[tuple[int, ...], tuple[int, ...], tuple[int, ...]]] = {}
        self.special_conditional: dict[int, list[tuple[str | None, tuple[str, ...], tuple[tuple[int, ...], ...]]]] = defaultdict(list)
        for rec in records(self.pins.read("SpecialCasing.txt")):
            cp = int(rec[0], 16)
            mappings = (code_points_of(rec[1]), code_points_of(rec[2]), code_points_of(rec[3]))
            condition = rec[4].split() if len(rec) > 4 else []
            if not condition:
                self.special_unconditional[cp] = mappings
                continue
            language = None
            contexts = []
            for token in condition:
                if token[0].islower():
                    if token not in LANGUAGE_OF_TAG:
                        raise PinError(f"SpecialCasing.txt names a language this arbiter does not know: {token}")
                    language = LANGUAGE_OF_TAG[token]
                else:
                    if token.removeprefix("Not_") not in CONTEXTS:
                        raise PinError(f"SpecialCasing.txt names a context this arbiter does not know: {token}")
                    contexts.append(token)
            self.special_conditional[cp].append((language, tuple(contexts), mappings))
        # A language's own lines are tried before the lines of every language.
        for cp, entries in self.special_conditional.items():
            entries.sort(key=lambda e: e[0] is None)

    def _holds(self, context: str, cps, i: int) -> bool:
        negated = context.startswith("Not_")
        result = CONTEXTS[context.removeprefix("Not_")](self, cps, i)
        return not result if negated else result

    def map_at(self, cps, i: int, upper: bool, language: str):
        """The full case mapping of cps[i] in its context: the language's conditional lines, then the
        conditional lines of every language, then the unconditional line, then UnicodeData.txt's simple
        mapping, then the code point itself (section 3.13, toUppercase and toLowercase)."""
        c = cps[i]
        column = 2 if upper else 0
        for entry_language, contexts, mappings in self.special_conditional.get(c, ()):
            if entry_language is not None and entry_language != language:
                continue
            if all(self._holds(context, cps, i) for context in contexts):
                return mappings[column]
        special = self.special_unconditional.get(c)
        if special is not None:
            return special[column]
        return ((self.simple_upper if upper else self.simple_lower).get(c, c),)

    def change_case(self, cps, upper: bool, language: str = "default") -> list[int]:
        out: list[int] = []
        conditional, unconditional = self.special_conditional, self.special_unconditional
        simple = self.simple_upper if upper else self.simple_lower
        column = 2 if upper else 0
        for i, c in enumerate(cps):
            if c in conditional:
                out.extend(self.map_at(cps, i, upper, language))
            elif c in unconditional:
                out.extend(unconditional[c][column])
            else:
                out.append(simple.get(c, c))
        return out

    def to_upper(self, units, language: str = "default") -> list[int]:
        return code_units(self.change_case(code_points(units), True, language))

    def to_lower(self, units, language: str = "default") -> list[int]:
        return code_units(self.change_case(code_points(units), False, language))

    # ---- Normalization: UAX #15.

    def _load_normalization(self) -> None:
        exclusions = set()
        for a, b in self.binary["Full_Composition_Exclusion"]:
            exclusions.update(range(a, b + 1))
        self.composition_exclusions = exclusions
        self.primary: dict[tuple[int, int], int] = {}
        for cp, d in self.canonical.items():
            if len(d) == 2 and cp not in exclusions:
                self.primary[d] = cp
        self.second_elements = {b for _, b in self.primary} | set(range(V_BASE, V_BASE + V_COUNT)) | set(range(T_BASE + 1, T_BASE + T_COUNT))

        @lru_cache(maxsize=None)
        def full(cp: int, compat: bool) -> tuple[int, ...]:
            if S_BASE <= cp < S_BASE + S_COUNT:
                s = cp - S_BASE
                t = s % T_COUNT
                parts = (L_BASE + s // N_COUNT, V_BASE + (s % N_COUNT) // T_COUNT)
                return parts + ((T_BASE + t,) if t else ())
            d = self.canonical.get(cp)
            if d is None and compat:
                d = self.compatibility.get(cp)
            if d is None:
                return (cp,)
            return tuple(x for part in d for x in full(part, compat))

        self.nfd_map: dict[int, tuple[int, ...]] = {}
        self.nfkd_map: dict[int, tuple[int, ...]] = {}
        for cp in list(self.canonical) + list(self.compatibility) + list(range(S_BASE, S_BASE + S_COUNT)):
            d = full(cp, False)
            if d != (cp,):
                self.nfd_map[cp] = d
            k = full(cp, True)
            if k != (cp,):
                self.nfkd_map[cp] = k

    def decompose(self, cps, compat: bool) -> list[int]:
        table = self.nfkd_map if compat else self.nfd_map
        out: list[int] = []
        for c in cps:
            d = table.get(c)
            if d is None:
                out.append(c)
            else:
                out.extend(d)
        # Canonical ordering: each run of non-starters, stably sorted by combining class.
        ccc = self.ccc
        i, n = 0, len(out)
        while i < n:
            if ccc[out[i]] == 0:
                i += 1
                continue
            j = i + 1
            while j < n and ccc[out[j]] != 0:
                j += 1
            if j - i > 1:
                out[i:j] = sorted(out[i:j], key=ccc.__getitem__)
            i = j
        return out

    def compose_pair(self, a: int, b: int) -> int | None:
        if L_BASE <= a < L_BASE + L_COUNT and V_BASE <= b < V_BASE + V_COUNT:
            return S_BASE + ((a - L_BASE) * V_COUNT + (b - V_BASE)) * T_COUNT
        if S_BASE <= a < S_BASE + S_COUNT and (a - S_BASE) % T_COUNT == 0 and T_BASE < b < T_BASE + T_COUNT:
            return a + (b - T_BASE)
        return self.primary.get((a, b))

    def compose(self, cps: list[int]) -> list[int]:
        """The Canonical Composition Algorithm: each character, from the second, joins the last starter
        when it is not blocked from it and they have a primary composite."""
        if not cps:
            return cps
        ccc = self.ccc
        out = [cps[0]]
        starter = 0
        last_class = ccc[cps[0]]
        if last_class != 0:
            last_class = 256  # no starter yet: nothing composes until one comes
        for c in cps[1:]:
            c_class = ccc[c]
            composite = self.compose_pair(out[starter], c) if (last_class < c_class or last_class == 0) else None
            if composite is not None:
                out[starter] = composite
                continue
            if c_class == 0:
                starter = len(out)
            last_class = c_class
            out.append(c)
        return out

    def normalize(self, units, form: str) -> list[int]:
        cps = code_points(units)
        if form == "NFD":
            return code_units(self.decompose(cps, False))
        if form == "NFKD":
            return code_units(self.decompose(cps, True))
        if form == "NFC":
            return code_units(self.compose(self.decompose(cps, False)))
        if form == "NFKC":
            return code_units(self.compose(self.decompose(cps, True)))
        raise ValueError(form)

    def boundary_before(self, cp: int, form: str) -> bool:
        """Whether normalization in the form never looks across the start of cp: cp and the first code
        point of its decomposition are starters, and, composing, that first code point is never the second
        of a primary composite (so a string split before cp normalizes as its two halves do)."""
        first = (self.nfkd_map if form in ("NFKC", "NFKD") else self.nfd_map).get(cp, (cp,))[0]
        if self.ccc[cp] != 0 or self.ccc[first] != 0:
            return False
        return form in ("NFD", "NFKD") or first not in self.second_elements

    # ---- Properties of strings: emoji-sequences.txt and emoji-zwj-sequences.txt.

    def _load_string_properties(self) -> None:
        members: dict[str, set[tuple[int, ...]]] = defaultdict(set)
        for name in ("emoji-sequences.txt", "emoji-zwj-sequences.txt"):
            for rec in records(self.pins.read(name)):
                if ".." in rec[0]:
                    a, b = code_range(rec[0])
                    members[rec[1]].update((c,) for c in range(a, b + 1))
                else:
                    members[rec[1]].add(code_points_of(rec[0]))
        union = set()
        for value in members.values():
            union |= value
        members["RGI_Emoji"] = union
        if set(members) != set(self.ecma_strings):
            raise PinError(f"the sequence files' properties {sorted(members)} are not ECMA-262's {self.ecma_strings}")
        self.string_members = {name: frozenset(members[name]) for name in self.ecma_strings}

    def in_string_property(self, units, name: str) -> bool:
        return tuple(code_points(units)) in self.string_members[name]

    def string_properties(self, units) -> list[str]:
        key = tuple(code_points(units))
        return [name for name in self.ecma_strings if key in self.string_members[name]]

    # ---- The break properties: UAX #29's auxiliary files.

    def _load_breaks(self) -> None:
        def load(name: str) -> tuple[list[str], bytearray]:
            names = ["Other"]  # each file's @missing line: Other
            values = bytearray(CODE_POINTS)
            for rec in records(self.pins.read(name)):
                a, b = code_range(rec[0])
                if rec[1] not in names:
                    names.append(rec[1])
                values[a:b + 1] = bytes([names.index(rec[1])]) * (b - a + 1)
            return names, values

        self.gcb_names, self.gcb = load("GraphemeBreakProperty.txt")
        self.wb_names, self.wb = load("WordBreakProperty.txt")
        self.sb_names, self.sb = load("SentenceBreakProperty.txt")

    def grapheme_boundaries(self, units) -> list[int]:
        return _unit_positions(units, self.grapheme_breaks(code_points(units)))

    def word_boundaries(self, units) -> list[int]:
        return _unit_positions(units, self.word_breaks(code_points(units)))

    def sentence_boundaries(self, units) -> list[int]:
        return _unit_positions(units, self.sentence_breaks(code_points(units)))

    def grapheme_breaks(self, cps) -> list[int]:
        """UAX #29 section 3.1.1, GB1 to GB999, as code-point indices of the boundaries."""
        n = len(cps)
        if n == 0:
            return [0]
        g = [self.gcb_names[self.gcb[c]] for c in cps]
        incb = [self.incb_names[self.incb[c]] for c in cps]
        pict = [self.ext_pict[c] for c in cps]
        out = [0]
        for i in range(1, n):
            a, b = g[i - 1], g[i]
            if a == "CR" and b == "LF":                                       # GB3
                continue
            if a in ("Control", "CR", "LF") or b in ("Control", "CR", "LF"):  # GB4, GB5
                out.append(i)
                continue
            if a == "L" and b in ("L", "V", "LV", "LVT"):                     # GB6
                continue
            if a in ("LV", "V") and b in ("V", "T"):                          # GB7
                continue
            if a in ("LVT", "T") and b == "T":                                # GB8
                continue
            if b in ("Extend", "ZWJ"):                                        # GB9
                continue
            if b == "SpacingMark":                                            # GB9a
                continue
            if a == "Prepend":                                                # GB9b
                continue
            if incb[i] == "Consonant":                                        # GB9c
                j, linker = i - 1, False
                while j >= 0 and incb[j] in ("Extend", "Linker"):
                    linker = linker or incb[j] == "Linker"
                    j -= 1
                if linker and j >= 0 and incb[j] == "Consonant":
                    continue
            if a == "ZWJ" and pict[i]:                                        # GB11
                j = i - 2
                while j >= 0 and g[j] == "Extend":
                    j -= 1
                if j >= 0 and pict[j]:
                    continue
            if a == "Regional_Indicator" and b == "Regional_Indicator":       # GB12, GB13
                j = i - 1
                while j >= 0 and g[j] == "Regional_Indicator":
                    j -= 1
                if (i - 1 - j) % 2 == 1:
                    continue
            out.append(i)                                                     # GB999
        out.append(n)
        return out

    def word_breaks(self, cps) -> list[int]:
        """UAX #29 section 4.1.1, WB1 to WB999, with WB4's rewriting: a run of Extend, Format and ZWJ is
        absorbed into the character before it unless that is sot, CR, LF or Newline."""
        n = len(cps)
        if n == 0:
            return [0]
        w = [self.wb_names[self.wb[c]] for c in cps]
        pict = [self.ext_pict[c] for c in cps]
        ignored = ("Extend", "Format", "ZWJ")
        hard = ("Newline", "CR", "LF")
        owner = list(range(n))  # the index of the character each one is absorbed into (itself if none)
        absorbed = [False] * n
        for k in range(1, n):
            if w[k] in ignored and w[owner[k - 1]] not in hard:
                owner[k] = owner[k - 1]
                absorbed[k] = True
        effective = [k for k in range(n) if not absorbed[k]]
        position = {k: e for e, k in enumerate(effective)}

        def at(e: int) -> str | None:
            return w[effective[e]] if 0 <= e < len(effective) else None

        ah = ("ALetter", "Hebrew_Letter")
        mid_letter = ("MidLetter", "MidNumLet", "Single_Quote")
        mid_num = ("MidNum", "MidNumLet", "Single_Quote")
        out = [0]
        for i in range(1, n):
            a, b = w[i - 1], w[i]
            if a == "CR" and b == "LF":                                       # WB3
                continue
            if a in hard or b in hard:                                        # WB3a, WB3b
                out.append(i)
                continue
            if a == "ZWJ" and pict[i]:                                        # WB3c
                continue
            if a == "WSegSpace" and b == "WSegSpace":                         # WB3d
                continue
            if absorbed[i]:                                                   # WB4
                continue
            e = position[i]
            prev2, prev, cur, nxt = at(e - 2), at(e - 1), at(e), at(e + 1)
            if prev in ah and cur in ah:                                      # WB5
                continue
            if prev in ah and cur in mid_letter and nxt in ah:                # WB6
                continue
            if prev2 in ah and prev in mid_letter and cur in ah:              # WB7
                continue
            if prev == "Hebrew_Letter" and cur == "Single_Quote":             # WB7a
                continue
            if prev == "Hebrew_Letter" and cur == "Double_Quote" and nxt == "Hebrew_Letter":  # WB7b
                continue
            if prev2 == "Hebrew_Letter" and prev == "Double_Quote" and cur == "Hebrew_Letter":  # WB7c
                continue
            if prev == "Numeric" and cur == "Numeric":                        # WB8
                continue
            if prev in ah and cur == "Numeric":                               # WB9
                continue
            if prev == "Numeric" and cur in ah:                               # WB10
                continue
            if prev2 == "Numeric" and prev in mid_num and cur == "Numeric":   # WB11
                continue
            if prev == "Numeric" and cur in mid_num and nxt == "Numeric":     # WB12
                continue
            if prev == "Katakana" and cur == "Katakana":                      # WB13
                continue
            if prev in ah + ("Numeric", "Katakana", "ExtendNumLet") and cur == "ExtendNumLet":  # WB13a
                continue
            if prev == "ExtendNumLet" and cur in ah + ("Numeric", "Katakana"):  # WB13b
                continue
            if prev == "Regional_Indicator" and cur == "Regional_Indicator":  # WB15, WB16
                j = e - 1
                while j >= 0 and at(j) == "Regional_Indicator":
                    j -= 1
                if (e - 1 - j) % 2 == 1:
                    continue
            out.append(i)                                                     # WB999
        out.append(n)
        return out

    def sentence_breaks(self, cps) -> list[int]:
        """UAX #29 section 5.1.1, SB1 to SB998, with SB5's rewriting: a run of Extend and Format is absorbed
        into the character before it unless that is sot, Sep, CR or LF."""
        n = len(cps)
        if n == 0:
            return [0]
        s = [self.sb_names[self.sb[c]] for c in cps]
        ignored = ("Extend", "Format")
        para = ("Sep", "CR", "LF")
        owner = list(range(n))
        absorbed = [False] * n
        for k in range(1, n):
            if s[k] in ignored and s[owner[k - 1]] not in para:
                owner[k] = owner[k - 1]
                absorbed[k] = True
        effective = [k for k in range(n) if not absorbed[k]]
        position = {k: e for e, k in enumerate(effective)}

        def at(e: int) -> str | None:
            return s[effective[e]] if 0 <= e < len(effective) else None

        def back_over(e: int, classes) -> int:
            while e >= 0 and at(e) in classes:
                e -= 1
            return e

        out = [0]
        for i in range(1, n):
            a, b = s[i - 1], s[i]
            if a == "CR" and b == "LF":                                       # SB3
                continue
            if a in para:                                                     # SB4
                out.append(i)
                continue
            if absorbed[i]:                                                   # SB5
                continue
            e = position[i]
            prev2, prev, cur = at(e - 2), at(e - 1), at(e)
            if prev == "ATerm" and cur == "Numeric":                          # SB6
                continue
            if prev2 in ("Upper", "Lower") and prev == "ATerm" and cur == "Upper":  # SB7
                continue
            # The SATerm Close* Sp* before the boundary, if any.
            after_sp = back_over(e - 1, ("Sp",))
            after_close = back_over(after_sp, ("Close",))
            term = at(after_close)
            if term == "ATerm":                                               # SB8
                k = e
                while at(k) is not None and at(k) not in ("OLetter", "Upper", "Lower", "Sep", "CR", "LF", "STerm", "ATerm"):
                    k += 1
                if at(k) == "Lower":
                    continue
            if term in ("STerm", "ATerm"):
                if cur in ("SContinue", "STerm", "ATerm"):                    # SB8a
                    continue
                if after_sp == e - 1 and cur in ("Close", "Sp") + para:        # SB9
                    continue
                if cur in ("Sp",) + para:                                     # SB10
                    continue
                out.append(i)                                                 # SB11
                continue
            # SB998: Any × Any
        out.append(n)
        return out

    def word_status(self, units) -> str:
        """Informative only (row G4: the status is implementation-defined, class (b)): for each word segment,
        Y when it holds a code point of Word_Break ALetter, Hebrew_Letter, Numeric or Katakana, or one that
        is Ideographic or of Script Hiragana; N otherwise. Broiler.VM's rule as correspondence row G4 reads it."""
        cps = code_points(units)
        breaks = self.word_breaks(cps)
        hiragana = self.sc_names.index("Hira")
        out = []
        for start, end in zip(breaks, breaks[1:]):
            like = any(self.wb_names[self.wb[c]] in ("ALetter", "Hebrew_Letter", "Numeric", "Katakana")
                       or self.sc[c] == hiragana or self.ideographic[c] for c in cps[start:end])
            out.append("Y" if like else "N")
        return "".join(out)

    # ---- The lookups: ECMA-262 22.2.2.9 and the static semantics of UnicodePropertyValueExpression.

    def resolve_lone(self, name: str, unicode_sets: bool = False) -> str:
        """LoneUnicodePropertyNameOrValue: a General_Category value or alias of PropertyValueAliases.txt, a
        binary property or alias of the binary table, or, under v only, a property of strings of its table;
        anything else is refused. Compared exactly: no case folding, no skipped characters."""
        gc_value = self.values_of["General_Category"].get(name)
        if gc_value is not None:
            return f"General_Category={gc_value}"
        binary = self.ecma_binary.get(name)
        if binary is not None:
            return binary
        if name in self.ecma_strings and unicode_sets:
            return name
        return REFUSED

    def resolve_pair(self, prop: str, value: str) -> str:
        """UnicodePropertyName=UnicodePropertyValue: a non-binary property or alias of the table
        (UnicodeMatchProperty), and a value or alias PropertyValueAliases.txt lists for it
        (UnicodeMatchPropertyValue); a binary property or a property of strings never takes a value."""
        canonical = self.ecma_nonbinary.get(prop)
        if canonical is None:
            return REFUSED
        long = self.values_of[canonical].get(value)
        if long is None:
            return REFUSED
        return f"{canonical}={long}"

    def resolve(self, kind: str, prop: str, value: str, unicode_sets: bool) -> str:
        return self.resolve_lone(prop, unicode_sets) if kind == "lone" else self.resolve_pair(prop, value)

    # ---- The sets.

    def sets(self) -> dict[str, list[tuple[int, int]]]:
        """Every set a lookup answers, by the name its resolution gives, and the lexical sets."""
        if self._sets is not None:
            return self._sets
        out: dict[str, list[tuple[int, int]]] = {}
        gc_runs = runs(self.gc)
        gc_by_short: dict[str, list[tuple[int, int]]] = defaultdict(list)
        for a, b, v in gc_runs:
            gc_by_short[self.gc_names[v]].append((a, b))
        for rec in self.value_aliases["gc"]:
            short, long = rec[0], rec[1]
            members = self.gc_groups.get(short, [short])
            out[f"General_Category={long}"] = normalize_ranges(r for m in members for r in gc_by_short.get(m, []))
        for canonical in sorted(set(self.ecma_binary.values()), key=str):
            out[canonical] = self.binary[canonical]
        sc_by_short: dict[str, list[tuple[int, int]]] = defaultdict(list)
        for a, b, v in runs(self.sc):
            sc_by_short[self.sc_names[v]].append((a, b))
        explicit = set(self.scx_explicit)
        explicit_by_short: dict[str, list[int]] = defaultdict(list)
        for cp, values in self.scx_explicit.items():
            for v in values:
                explicit_by_short[v].append(cp)
        for rec in self.value_aliases["sc"]:
            short, long = rec[0], rec[1]
            out[f"Script={long}"] = normalize_ranges(sc_by_short.get(short, []))
            base = subtract_points(normalize_ranges(sc_by_short.get(short, [])), explicit)
            out[f"Script_Extensions={long}"] = normalize_ranges(base + [(c, c) for c in explicit_by_short.get(short, [])])
        for name in self.ecma_strings:
            out[name] = ranges_of_points(m[0] for m in self.string_members[name] if len(m) == 1)
        pattern_syntax = self.binary["Pattern_Syntax"]
        id_start = subtract_ranges(self.binary["ID_Start"], pattern_syntax)
        id_continue = subtract_ranges(self.binary["ID_Continue"], pattern_syntax)
        out["lexical:ID_Start-Pattern_Syntax"] = id_start
        out["lexical:ID_Continue-Pattern_Syntax"] = id_continue
        # ECMA-262 12.7: IdentifierStartChar :: UnicodeIDStart | $ | _ ; IdentifierPartChar :: UnicodeIDContinue | $ | ZWNJ | ZWJ
        out["lexical:IdentifierStartChar"] = normalize_ranges(id_start + [(0x24, 0x24), (0x5F, 0x5F)])
        out["lexical:IdentifierPartChar"] = normalize_ranges(id_continue + [(0x24, 0x24), (0x200C, 0x200D)])
        # ECMA-262 22.2.1: RegExpIdentifierStart and RegExpIdentifierPart take, code point by code point, the
        # same characters as IdentifierStartChar and IdentifierPartChar (their escapes are the parser's).
        out["lexical:RegExpIdentifierStart"] = out["lexical:IdentifierStartChar"]
        out["lexical:RegExpIdentifierPart"] = out["lexical:IdentifierPartChar"]
        # ECMA-262 12.2: WhiteSpace :: TAB VT FF ZWNBSP USP (USP: General_Category Zs); 12.3 LineTerminator.
        out["lexical:WhiteSpace"] = normalize_ranges([(0x9, 0x9), (0xB, 0xC), (0xFEFF, 0xFEFF)] + gc_by_short["Zs"])
        out["lexical:LineTerminator"] = [(0xA, 0xA), (0xD, 0xD), (0x2028, 0x2029)]
        self._sets = out
        return out


def _unit_positions(units, breaks: list[int]) -> list[int]:
    """Code-point indices of boundaries as UTF-16 offsets."""
    offsets = [0]
    i, n = 0, len(units)
    while i < n:
        u = units[i]
        i += 2 if 0xD800 <= u <= 0xDBFF and i + 1 < n and 0xDC00 <= units[i + 1] <= 0xDFFF else 1
        offsets.append(i)
    return [offsets[b] for b in breaks]


def parse_ecma_table(text: str) -> list[tuple[str, str]]:
    """(name or alias, canonical name) for every row of one of ECMA-262's archived property tables; a row
    whose canonical cell spans several rows lends it to the rows it spans; a one-column table names each
    property once."""
    out = []
    canonical, carried = None, 0
    for row in re.findall(r"<tr>(.*?)</tr>", text, re.S):
        cells = re.findall(r"<td([^>]*)>(.*?)</td>", row, re.S)
        if not cells:
            continue
        names = [re.search(r"`([^`]+)`", body).group(1) for _, body in cells]
        if len(names) >= 2:
            canonical = names[1]
            span = re.search(r'rowspan="(\d+)"', cells[1][0])
            carried = int(span.group(1)) - 1 if span else 0
        elif carried > 0:
            carried -= 1
        else:
            canonical = names[0]
        out.append((names[0], canonical))
    return out


# --------------------------------------------------------------------------------------------------
# The casing conditions of SpecialCasing.txt, as section 3.13's Table 3-17 defines them.

def _final_sigma(ucd: UCD, cps, i: int) -> bool:
    """Before C: a cased letter, then zero or more case-ignorable characters; after C: not zero or more
    case-ignorable characters and then a cased letter. Section 3.13.1: "The regular-expression operator * in
    Table 3-17 is "possessive," consuming as many characters as possible, with no backup." Cased and
    Case_Ignorable overlap (U+0345, U+02B0...), and a character in both is consumed as case-ignorable, so
    U+0345 alone before C does not satisfy Before, and U+0345 alone after C satisfies After."""
    j = i - 1
    while j >= 0 and ucd.case_ignorable[cps[j]]:
        j -= 1
    if j < 0 or not ucd.cased[cps[j]]:
        return False
    j = i + 1
    n = len(cps)
    while j < n and ucd.case_ignorable[cps[j]]:
        j += 1
    return not (j < n and ucd.cased[cps[j]])


def _after_soft_dotted(ucd: UCD, cps, i: int) -> bool:
    """A Soft_Dotted character before C, with no intervening character of combining class 0 or 230."""
    j = i - 1
    while j >= 0:
        if ucd.soft_dotted[cps[j]]:
            return True
        if ucd.ccc[cps[j]] in (0, 230):
            return False
        j -= 1
    return False


def _more_above(ucd: UCD, cps, i: int) -> bool:
    """C followed by a character of combining class 230, with no intervening character of class 0 or 230."""
    j = i + 1
    while j < len(cps):
        k = ucd.ccc[cps[j]]
        if k == 230:
            return True
        if k == 0:
            return False
        j += 1
    return False


def _before_dot(ucd: UCD, cps, i: int) -> bool:
    """C followed by U+0307, with no intervening character of combining class 0 or 230."""
    j = i + 1
    while j < len(cps):
        if cps[j] == 0x0307:
            return True
        if ucd.ccc[cps[j]] in (0, 230):
            return False
        j += 1
    return False


def _after_i(ucd: UCD, cps, i: int) -> bool:
    """An uppercase I before C, with no intervening character of combining class 0 or 230."""
    j = i - 1
    while j >= 0:
        if cps[j] == 0x0049:
            return True
        if ucd.ccc[cps[j]] in (0, 230):
            return False
        j -= 1
    return False


CONTEXTS = {"Final_Sigma": _final_sigma, "After_Soft_Dotted": _after_soft_dotted, "More_Above": _more_above,
            "Before_Dot": _before_dot, "After_I": _after_i}


# --------------------------------------------------------------------------------------------------
# Repeated inputs: class U7's results of 2^24 code units and one more, without building them.

def repeat_units(piece: list[int], count: int, suffix: list[int]) -> list[int]:
    return piece * count + suffix


def joins_cleanly(a: list[int], b: list[int]) -> bool:
    """Whether a + b has exactly a's code points and then b's (no surrogate pair formed across the joint)."""
    return code_points(a + b) == code_points(a) + code_points(b)


def repeat_result(ucd: UCD, family: str, operation: tuple[str, str] | str, piece: list[int], count: int,
                  suffix: list[int]) -> tuple[int, str]:
    """The length and SHA-256 (of the UTF-16LE bytes) of an operation's result on piece * count + suffix.

    Exact, not estimated: casing, when every code point of the piece and the suffix has combining class 0 and
    is not Case_Ignorable, decides each code point's mapping by its neighbours alone (every condition's scan
    stops at the first character it reads), so the result is the first piece's mappings, the middle piece's
    count - 2 times, the last piece's and the suffix's, each read off piece * 3 + suffix; normalization, when
    the form has a boundary before the piece's first code point and the suffix's (UCD.boundary_before), is
    the piece's normal form count times and the suffix's. Otherwise the whole input is built and computed."""
    p_cps, s_cps = code_points(piece), code_points(suffix)
    clean = bool(piece) and joins_cleanly(piece, piece) and joins_cleanly(piece, suffix)
    if family == "casing":
        upper, language = operation[0] == "upper", operation[1]
        local = count >= 3 and clean and all(ucd.ccc[c] == 0 and not ucd.case_ignorable[c] for c in p_cps + s_cps)
        if local:
            seq = p_cps * 3 + s_cps
            outs = [code_units(ucd.map_at(seq, i, upper, language)) for i in range(len(seq))]
            k = len(p_cps)
            first = [u for o in outs[:k] for u in o]
            middle = [u for o in outs[k:2 * k] for u in o]
            last = [u for o in outs[2 * k:3 * k] for u in o]
            tail = [u for o in outs[3 * k:] for u in o]
            return _streamed([(first, 1), (middle, count - 2), (last, 1), (tail, 1)])
        result = code_units(ucd.change_case(code_points(repeat_units(piece, count, suffix)), upper, language))
        return len(result), hashlib.sha256(utf16le(result)).hexdigest()
    form = operation
    if clean and ucd.boundary_before(p_cps[0], form) and (not s_cps or ucd.boundary_before(s_cps[0], form)):
        return _streamed([(ucd.normalize(piece, form), count), (ucd.normalize(suffix, form), 1)])
    result = ucd.normalize(repeat_units(piece, count, suffix), form)
    return len(result), hashlib.sha256(utf16le(result)).hexdigest()


def _streamed(parts: list[tuple[list[int], int]]) -> tuple[int, str]:
    digest = hashlib.sha256()
    length = 0
    for units, times in parts:
        if times <= 0 or not units:
            continue
        block = utf16le(units)
        length += len(units) * times
        chunk = max(1, (1 << 20) // len(block))
        full, rest = divmod(times, chunk)
        big = block * chunk
        for _ in range(full):
            digest.update(big)
        digest.update(block * rest)
    return length, digest.hexdigest()


# --------------------------------------------------------------------------------------------------
# One answer, in the comparison's notation (README.md, "differences.tsv").

OPERATION_ALIASES = {
    "toUpperCase": "upper", "toLowerCase": "lower", "toLocaleUpperCase": "upper", "toLocaleLowerCase": "lower",
    "normalization": "normalize", "graphemes": "grapheme", "words": "word", "sentences": "sentence",
    "string-properties": "string-property", "binary-property": "binary", "general-category": "gc",
    "script": "sc", "script-extensions": "scx", "simple-fold": "fold", "canonicalize-without-u": "canonicalize",
}


def parse_language(text: str) -> str:
    """A casing parameter's language: default, turkic or lithuanian, or a locale or a list of locales, whose
    first element decides (ECMA-402's TransformCase); what follows '@' (a host culture) or ';' is not read."""
    base = text.split("@", 1)[0].split(";", 1)[0].split(",", 1)[0].strip().strip("[]'\"")
    if base in LANGUAGES:
        return base
    if base in ("", "-", "und", "root", "en", "en-US"):
        return "default"
    tag = base.lower().split("-")[0].split("_")[0]
    return LANGUAGE_OF_TAG.get(tag, "default")


def parse_max(parameter: str) -> int | None:
    match = re.search(r"max=(\d+)", parameter)
    return int(match.group(1)) if match else None


def parse_text_input(field: str) -> tuple[list[int] | None, tuple[list[int], int, list[int]] | None]:
    """A text input: code units in hexadecimal, or `repeat:<piece>:<count>:<suffix>` for a giant one."""
    if field.startswith("repeat"):
        parts = re.split(r"[:\t]", field)
        return None, (parse_units(parts[1]), int(parts[2]), parse_units(parts[3]) if len(parts) > 3 else [])
    return parse_units(field.strip()), None


def parse_name_input(field: str) -> tuple[str, str, str]:
    """A lookup's input: `<kind>/<property>/<value>` (no name holds a '/'), or `lone:<name>`, `pair:<p>=<v>`."""
    if "/" in field:
        kind, prop, value = field.split("/", 2)
        return kind, prop, value
    kind, _, rest = field.partition(":")
    if kind == "pair":
        prop, _, value = rest.partition("=")
        return kind, prop, value
    return "lone", rest, ""


def parse_code_point(field: str) -> int:
    return int(field.strip().removeprefix("U+").removeprefix("u+"), 16)


def text_result(units: list[int], maximum: int | None) -> str:
    if maximum is not None and len(units) > maximum:
        return OVER
    return format_units(units)


def answer(ucd: UCD, operation: str, input_field: str, parameter: str) -> str | None:
    """The arbiter's answer to one comparison, in its notation; None for an operation with no arbiter
    (a charge, whose arbiter is arm V) or one it does not know."""
    operation = OPERATION_ALIASES.get(operation, operation)
    if operation == "lookup":
        kind, prop, value = parse_name_input(input_field)
        return ucd.resolve(kind, prop, value, parameter.strip() in ("v", "unicodeSets", "v-flag"))
    if operation in ("binary", "gc", "sc", "scx", "lexical", "fold", "orbit", "canonicalize"):
        cp = parse_code_point(input_field)
        if operation == "binary":
            name = ucd.ecma_binary.get(parameter, parameter)
            return "true" if ucd.has(name, cp) else "false"
        if operation == "lexical":
            sets = ucd.sets()
            key = parameter if parameter in sets else "lexical:" + parameter
            if key not in sets:
                return None
            return "true" if ranges_contain(sets[key], cp) else "false"
        if operation == "gc":
            return ucd.general_category(cp)
        if operation == "sc":
            return ucd.script(cp)
        if operation == "scx":
            return " ".join(ucd.script_extensions(cp))
        if operation == "fold":
            return f"{ucd.fold(cp):04X}"
        if operation == "orbit":
            return " ".join(f"{c:04X}" for c in ucd.orbit(cp))
        return f"{ucd.canonicalize_without_u(cp):04X}" if cp <= 0xFFFF else None
    if operation in ("upper", "lower", "normalize", "NFC", "NFD", "NFKC", "NFKD"):
        units, repeat = parse_text_input(input_field)
        maximum = parse_max(parameter)
        if operation in ("upper", "lower"):
            family, op = "casing", (operation, parse_language(parameter))
        else:
            form = operation if operation != "normalize" else parameter.split(";", 1)[0].split("@", 1)[0].strip().upper()
            family, op = "normalization", form
        if repeat is not None:
            length, digest = repeat_result(ucd, family, op, *repeat)
            if maximum is not None and length > maximum:
                return OVER
            return digest_notation(length, digest)
        if family == "casing":
            result = ucd.to_upper(units, op[1]) if op[0] == "upper" else ucd.to_lower(units, op[1])
        else:
            result = ucd.normalize(units, op)
        return text_result(result, maximum)
    if operation == "string-property":
        units, _ = parse_text_input(input_field)
        return "true" if ucd.in_string_property(units, parameter) else "false"
    if operation in ("grapheme", "word", "sentence", "word-status"):
        units, _ = parse_text_input(input_field)
        if operation == "word-status":
            return ucd.word_status(units)
        boundaries = {"grapheme": ucd.grapheme_boundaries, "word": ucd.word_boundaries,
                      "sentence": ucd.sentence_boundaries}[operation](units)
        return " ".join(str(b) for b in boundaries)
    return None


# --------------------------------------------------------------------------------------------------
# The command: the arbiter's answer for every population input.

_UCD: UCD | None = None


def _casing_line(line: str) -> str:
    field = line.split("\t", 1)[0]
    cps = code_points(parse_units(field))
    results = [format_cps(_UCD.change_case(cps, op == "upper", language)) for op, language in CASING_OPERATIONS]
    return field + "\t" + "\t".join(results)


def _normalization_line(line: str) -> str:
    field = line.split("\t", 1)[0]
    cps = code_points(parse_units(field))
    nfd = _UCD.decompose(cps, False)
    nfkd = _UCD.decompose(cps, True)
    results = [format_cps(_UCD.compose(list(nfd))), format_cps(nfd), format_cps(_UCD.compose(list(nfkd))), format_cps(nfkd)]
    return field + "\t" + "\t".join(results)


def _string_property_line(line: str) -> str:
    field = line.split("\t", 1)[0]
    names = _UCD.string_properties(parse_units(field))
    return field + "\t" + (",".join(names) if names else "-")


def _segmentation_line(line: str) -> str:
    field = line.split("\t", 1)[0]
    units = parse_units(field)
    cps = code_points(units)
    results = [" ".join(str(b) for b in _unit_positions(units, f(cps)))
               for f in (_UCD.grapheme_breaks, _UCD.word_breaks, _UCD.sentence_breaks)]
    return field + "\t" + "\t".join(results) + "\t" + _UCD.word_status(units)


def _names_line(line: str) -> str:
    kind, prop, value = line.split("\t")
    return f"{line}\t{_UCD.resolve(kind, prop, value, False)}\t{_UCD.resolve(kind, prop, value, True)}"


def _limits_line(line: str) -> str:
    fields = line.split("\t")
    if fields[0] == "repeat":
        piece, count, suffix, tag = parse_units(fields[1]), int(fields[2]), parse_units(fields[3]), fields[4]
        family = tag.split(":", 1)[0]
        operations = CASING_OPERATIONS if family == "casing" else FORMS
        results = [digest_notation(*repeat_result(_UCD, family, op, piece, count, suffix)) for op in operations]
        return family + "\t" + "\t".join(results)
    units, tag = parse_units(fields[0]), fields[1]
    family = tag.split(":", 1)[0]
    if family == "casing":
        cps = code_points(units)
        results = [format_cps(_UCD.change_case(cps, op == "upper", language)) for op, language in CASING_OPERATIONS]
    else:
        results = [format_units(_UCD.normalize(units, form)) for form in FORMS]
    return family + "\t" + "\t".join(results)


def _work(task: tuple[str, list[str]]) -> list[str]:
    kind, lines = task
    function = {"casing": _casing_line, "normalization": _normalization_line, "string-properties": _string_property_line,
                "segmentation": _segmentation_line, "names": _names_line, "limits": _limits_line}[kind]
    return [function(line) for line in lines]


POPULATION_FILES = (("names", "names.tsv", "names-ref.tsv"),
                    ("casing", "casing.tsv", "casing-ref.tsv"),
                    ("normalization", "normalization.tsv", "normalization-ref.tsv"),
                    ("string-properties", "string-properties.tsv", "string-properties-ref.tsv"),
                    ("segmentation", "segmentation.tsv", "segmentation-ref.tsv"),
                    ("limits", "limits.tsv", "limits-ref.tsv"))


class Output:
    def __init__(self, out: Path) -> None:
        self.out = out
        self.files: dict[str, dict[str, object]] = {}

    def write(self, name: str, lines: list[str], what: str) -> None:
        data = "".join(line + "\n" for line in lines).encode("utf-8")
        (self.out / name).write_bytes(data)
        self.files[name] = {"lines": len(lines), "sha256": hashlib.sha256(data).hexdigest(), "what": what}
        print(f"{name}: {len(lines)}", file=sys.stderr)


def write_code_point_answers(ucd: UCD, output: Output) -> None:
    sets = ucd.sets()
    output.write("sets.tsv", [f"{name}\t{format_ranges(ranges)}" for name, ranges in sets.items()],
                 "every set a lookup answers, by its resolution's name, and the lexical sets: <name>\\t<ranges>")
    output.write("string-sets.tsv", [f"{name}\t{format_cps(member)}" for name in ucd.ecma_strings
                                     for member in sorted(ucd.string_members[name])],
                 "every member string of each property of strings: <property>\\t<units>")
    output.write("folding.tsv", [f"{c:04X}\t{t:04X}" for c, t in sorted(ucd.scf.items()) if c != t],
                 "simple case folding: <code point>\\t<folding>, for every code point that does not fold to itself")
    orbits = sorted(set(ucd.orbits.values()))
    output.write("orbits.tsv", [" ".join(f"{c:04X}" for c in orbit) for orbit in orbits],
                 "every orbit of more than one code point; every other code point's orbit is itself")
    output.write("canonicalize.tsv", [f"{u:04X}\t{ucd.canonicalize_without_u(u):04X}" for u in range(0x10000)
                                      if ucd.canonicalize_without_u(u) != u],
                 "Canonicalize without u: <unit>\\t<result>, for every code unit it changes")


def verify_population(population: Path) -> dict | None:
    manifest_path = population / "manifest.json"
    if not manifest_path.exists():
        return None
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    for info in manifest.get("classes", {}).values():
        name = info.get("file")
        if not name:
            continue
        actual = hashlib.sha256((population / name).read_bytes()).hexdigest()
        if actual != info["sha256"]:
            raise PinError(f"{name}: SHA-256 {actual}, the population's manifest says {info['sha256']}")
    return manifest


def main(argv: list[str] | None = None) -> int:
    global _UCD
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--population", type=Path, help="the generator's output directory")
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--jobs", type=int, default=multiprocessing.cpu_count())
    parser.add_argument("--pins", type=Path, default=PIN_FILE, help="unicode.pin (default: this repository's)")
    # CORPUS CHANGE 3: the directory of the pinned files, which the corpus does not archive.
    parser.add_argument("--ucd", type=Path, required=True, help="the directory the pin's paths are under")
    args = parser.parse_args(argv)
    started = time.perf_counter()
    try:
        _UCD = UCD(Pins(args.pins, args.ucd))
        manifest = verify_population(args.population) if args.population else None
    except PinError as error:
        print(f"refused: {error}", file=sys.stderr)
        return 2
    args.out.mkdir(parents=True, exist_ok=True)
    output = Output(args.out)
    timings = {"load": round(time.perf_counter() - started, 1)}
    write_code_point_answers(_UCD, output)
    if args.population:
        context = multiprocessing.get_context("fork")
        with context.Pool(args.jobs) as pool:
            for kind, source, target in POPULATION_FILES:
                path = args.population / source
                if not path.exists():
                    continue
                t0 = time.perf_counter()
                lines = path.read_text(encoding="utf-8").split("\n")[:-1]
                size = 2048
                chunks = [(kind, lines[i:i + size]) for i in range(0, len(lines), size)]
                results = [line for chunk in pool.imap(_work, chunks) for line in chunk]
                output.write(target, results, f"the arbiter's answers for {source}, line for line")
                timings[target] = round(time.perf_counter() - t0, 1)
    reference = {
        "reference": "scripts/reference-arms/unicoderef.py", "version": REFERENCE_VERSION,
        "pin": {"file": str(args.pins), "sha256": _UCD.pins.sha256, "version": _UCD.pins.version},
        "inputs": dict(sorted(_UCD.pins.read_files.items())),
        "population": {"manifest_sha256": hashlib.sha256((args.population / "manifest.json").read_bytes()).hexdigest()} if manifest else None,
        "files": output.files,
        "seconds": timings,
    }
    (args.out / "reference-manifest.json").write_text(json.dumps(reference, indent=2) + "\n", encoding="utf-8")
    print(f"done in {time.perf_counter() - started:.1f} s", file=sys.stderr)
    return 0


def pins_directory() -> Path:
    """CORPUS CHANGE 3: the directory database() reads, BROILER_UNICODE_PINS or a Broiler.Unicode checkout
    beside this repository's."""
    import os
    return Path(os.environ.get(PINS_DIRECTORY_VARIABLE) or DEFAULT_PINS_DIRECTORY)


@lru_cache(maxsize=1)
def database() -> UCD:
    """One database per process, for the tests and the classifier."""
    return UCD(Pins(PIN_FILE, pins_directory()))  # CORPUS CHANGE 3: the original called UCD()


if __name__ == "__main__":
    sys.exit(main())
