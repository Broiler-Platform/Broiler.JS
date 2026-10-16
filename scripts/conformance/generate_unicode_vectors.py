#!/usr/bin/env python3
"""Generate the Unicode area of the shared conformance corpus.

    python3 -I scripts/conformance/generate_unicode_vectors.py --ucd <directory> --out conformance/vectors/unicode

`--ucd` is the directory that holds the pinned Unicode data, normally Broiler.Unicode's data/unicode/pins/. The
corpus archives none of it: conformance/vectors/sources/unicode.pin names every file the generator and its
reference read, by length and SHA-256, and a file that differs, or is missing, is refused before a byte of it
is used (the command exits 2 and writes nothing). Run it with `python3 -I`, as any reading of downloaded data.

**The oracle.** Where one of Unicode's own test files covers an input - NormalizationTest.txt, and
GraphemeBreakTest.txt, WordBreakTest.txt and SentenceBreakTest.txt - the expected value is the file's, and the
generator first checks that the reference computation gives the same answer, refusing to write anything if it
does not. Every other expected value is `unicoderef.py`'s (`ref:unicoderef@1`): a copy of the arbiter of
Broiler.Unicode's bundle sjl-7-001, written from the standards' text (the UCD's data files, UAX #15, UAX #29, the
Unicode Standard's section 3.13 and ECMA-262's property tables and their algorithms), reading only the pinned
files. A few outcomes are ECMA-262's own algorithm over that reference's sets, written here from the clause
they cite: RegExp.escape's EncodeForRegExpEscape, TrimString, and white space in source text. No expected value
is taken from either engine.

**The populations** are milestone SJL-7.3's, each script one line of ASCII with `\\u` and `\\u{}` escapes:

- `normalize`: every line of NormalizationTest.txt's parts 0, 2, 3, 4 and 5 in all four forms, its column c1
  normalized to c2, c3, c4 and c5; a systematic sample of part 1 (PART_1_HANGUL_STEP and PART_1_OTHER_STEP
  below); the correspondence's N cells and lone surrogates in all four forms, and localeCompare of a lone
  surrogate (N1); the form argument (N5); and the length of a result at the string ceiling and one past it (N6).
- `casing`: every code point whose default full upper-case mapping differs from itself, through toUpperCase,
  and every one whose lower-case mapping does, through toLowerCase; Final_Sigma in every combination of a set
  of contexts before and after; every conditional line of SpecialCasing.txt, where its condition holds and
  where it does not, through toLocaleUpperCase and toLocaleLowerCase with the tags tr, az, lt and en; the C
  cells; the locale argument (C7 to C9); lone surrogates; results at the string ceiling (C12); and case
  folding through RegExp's ignoreCase, every orbit of CaseFolding.txt under u, with the F1 cells.
- `properties`: `\\p{...}` acceptance under u and v of every property name, value and alias ECMA-262's tables
  and PropertyValueAliases.txt admit, the properties of strings under v alone, and a refused set (each
  admitted name with its case changed, its underscores removed or `Is` before it; a value under the wrong
  property; a binary property given a value; every property PropertyAliases.txt names that the tables do not
  admit, Age and Block among them; malformed bodies); membership at the edges of ranges of every binary
  property, General_Category value, Script value and Script_Extensions value (MEMBERSHIP_RANGES below),
  Katakana_Or_Hiragana included; the properties of strings' members; set operations; and the P and S cells.
- `identifiers`: an escaped code point as IdentifierStart at the four edges of every range of ID_Start, and as
  IdentifierPart at those of every range of ID_Continue, in a script that declares it, with no eval or
  Function; Pattern_Syntax's letters, Other_ID_Start and Other_ID_Continue, `$`, `_`, ZWNJ and ZWJ in both
  positions; the I cells, group names among them; and white space: every WhiteSpace code point and the code
  points an engine may mistake for one, in source text (through indirect eval, the one way an ASCII script
  can put such a code point in source text), in `\\s`, in TrimString and in RegExp.escape, with the W cells.
- `segmenter`: `Intl.Segmenter` with an explicit granularity over every line of the three break tests, the
  outcome the segments' indices joined by commas, and each word line's isWordLike, Y or N per segment; and the
  G cells, with locales that have no effect (G6). ECMA-402 leaves segmentation and word-likeness to the implementation, so these vectors are
  `impl-defined`, with each engine's expectation from its plan.

The output is deterministic: the same inputs give the same bytes. Vector identifiers are stable and are never
reused; a later revision appends.
"""

from __future__ import annotations

import argparse
import math
import struct
import sys
import time
from collections import defaultdict
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import unicoderef as U  # noqa: E402
from generate_number_vectors import js_string_body, string_value  # noqa: E402

# The corpus revision this generator writes, and the revision its vectors were added in. Revision 19 classifies
# every difference of both engines' first runs on revision 18 (unicode-divergences.txt).
CORPUS_REVISION = 19
FIRST_REVISION = 18
GENERATOR = "ref:unicoderef@1"
SPEC = "spec:es2026@0248456c"
UCD_VERSION = "17.0.0"
ENGINES = ("broiler-js", "broiler-vm")
DIVERGENCES = Path(__file__).resolve().parent / "unicode-divergences.txt"

# Part 1 of NormalizationTest.txt holds 17,086 lines, one code point each, 11,172 of them Hangul syllables,
# which the algorithm decomposes arithmetically. The sample: every Hangul syllable whose index from U+AC00 is a
# multiple of 512, and the last one; and every tenth of the other lines, in the file's order, from the first.
PART_1_HANGUL_STEP = 512
PART_1_OTHER_STEP = 10

# Membership: of each set's ranges (maximal runs of code points, in order), the first, the last, and the ones
# at the indices round(k * (n - 1) / 7) for k = 1 to 6, so at most eight; each tested at four code points, one
# before the range, its first, its last and one after it, under u; the first and the last of them under v too.
MEMBERSHIP_RANGES = 8

# Broiler.VM's string ceiling (JsRealm.String's StringLengthCeiling, 2^24 code units). JSD-0074 decision 4
# keeps it for casing and normalization: the owner checks the caller's maximum result length on the whole
# result, Broiler.VM passes its ceiling and keeps its RangeError, and Broiler.JS passes its own string ceiling.
CEILING = 1 << 24
VM_CEILING_POLICY = ("A string past an engine's own limit may be refused. Broiler.VM's string ceiling is 2^24 code "
                     "units (StringLengthCeiling), which it passes to the owner as the maximum result length and past "
                     "which it keeps its RangeError, as JSD-0074 decision 4 has it (and JSD-0070 decision 4 for the "
                     "ceiling itself)")

# The segmenter's notes are short, one per vector; the file's header comment (FILE_COMMENTS) gives them in full.
SEGMENTER_PLAN = ("UAX #29's default boundaries, out and both engines' plans: Broiler.VM's JSD-0074 decision 1, Broiler.JS's "
                  "docs/roadmap/SharedLibraries.md record of SJL-7.1")
WORD_LIKE_PLAN = ("The owner's word status (row G4), out and both engines' plans: Broiler.VM's JSD-0074 (X2, G4), "
                  "Broiler.JS's docs/roadmap/SharedLibraries.md record of SJL-7.1")
FILE_COMMENTS = {
    "segmenter": [
        "# Every vector here is impl-defined: ECMA-402 leaves segmentation, and a segment's isWordLike, to the",
        "# implementation. For the boundaries, out and both engines' expectations are UAX #29's default boundaries",
        "# for Unicode 17.0.0, with no locale tailoring and no dictionary, which both plans commit the engines to:",
        "# Broiler.VM's JSD-0074 (decision 1, the owner's segmentation package; rows G1 to G3 and G6 of bundle",
        "# sjl-7-001's correspondence) and Broiler.JS's docs/roadmap/SharedLibraries.md, its record of SJL-7.1",
        "# (sentence and word boundaries follow UAX #29, G1 and G2; grapheme clusters from the pinned data, G3).",
        "# For isWordLike (row G4), out and both expectations are the owner's word status: a segment is word-like",
        "# when it holds a code point of Word_Break ALetter, Hebrew_Letter, Numeric or Katakana, or one that is",
        "# Ideographic or of Script Hiragana; JSD-0074 (X2: the word status becomes the owner's) and Broiler.JS's",
        "# record of SJL-7.1 (isWordLike becomes the owner's word status). Each note names the line or the cell.",
    ],
}


def clause(*ids: str) -> str:
    return " ".join(f"{SPEC}#{i}" for i in ids)


def ucd(*files: str) -> str:
    return " ".join(f"ucd:{UCD_VERSION}/{name}" for name in files)


def prov(clauses: tuple[str, ...], files: tuple[str, ...]) -> str:
    return " ".join(part for part in (clause(*clauses), GENERATOR, ucd(*files)) if part)


def bits(x: float) -> int:
    return struct.unpack(">Q", struct.pack(">d", x))[0]


def number_out(x: float) -> str:
    return "value: f64:0x7FF8000000000000" if math.isnan(x) else f"value: f64:0x{bits(x):016X}"


def text(units) -> str:
    """A list of UTF-16 code units as a Python string of those code units, lone surrogates included."""
    return "".join(chr(u) for u in units)


def units_of(cps) -> list[int]:
    return U.code_units(list(cps))


def sv(units) -> str:
    return string_value(text(units))


def text_out(units) -> str:
    return f"value: {sv(units)}"


def str_out(s: str) -> str:
    return f"value: {string_value(s)}"


def bool_out(b: bool) -> str:
    return f"value: bool:{'true' if b else 'false'}"


def lit(units) -> str:
    """A JavaScript string literal of the code units, ASCII only: every other unit is \\uXXXX."""
    return f'"{js_string_body(text(units))}"'


def lit_str(s: str) -> str:
    return f'"{js_string_body(s)}"'


def escape(cp: int) -> str:
    """A code point as an escape in an identifier or a group name: \\uXXXX in the BMP, \\u{X} above it."""
    return f"\\u{cp:04X}" if cp <= 0xFFFF else f"\\u{{{cp:X}}}"


def hex_cps(cps) -> str:
    return " ".join(f"U+{c:04X}" for c in cps)


class Refused(Exception):
    """A pinned test file and the reference disagree: the generator writes nothing."""


# --------------------------------------------------------------------------------------------------
# Records.


class Corpus:
    def __init__(self) -> None:
        self.files: dict[str, list[dict[str, str]]] = {}
        self.ids: set[str] = set()
        self.counters: dict[str, int] = {}

    def add(self, file: str, group: str, record: dict[str, str], since: int = FIRST_REVISION) -> None:
        self.counters[group] = self.counters.get(group, 0) + 1
        vector_id = f"unicode/{group}/{self.counters[group]:04d}"
        assert vector_id not in self.ids
        self.ids.add(vector_id)
        full = {"id": vector_id, "area": "unicode"}
        full.update(record)
        full.setdefault("class", "normative")
        full["since"] = str(since)
        for key, value in full.items():
            assert "\n" not in value and value == value.strip(), (key, value)
        assert all(ord(ch) < 0x80 for ch in full["script"]), full["script"]
        assert full["in"], full
        self.files.setdefault(file, []).append(full)

    def classify(self, path: Path) -> None:
        """Applies unicode-divergences.txt, as generate_date_vectors.py applies its own file; revision 18 had
        none, and revision 19 adds it."""
        if not path.exists():
            return
        reasons: dict[str, str] = {}
        by_id = {record["id"]: record for records in self.files.values() for record in records}
        for line_number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
            if not line or line.startswith("#"):
                continue
            if line.startswith("reason "):
                key, _, reason = line[len("reason "):].partition(": ")
                if not reason or key in reasons:
                    raise SystemExit(f"{path.name}:{line_number}: a malformed or repeated reason")
                reasons[key] = reason
                continue
            engine, key, vector_id = line.split(" ")
            record = by_id.get(vector_id)
            if engine not in ENGINES or key not in reasons or record is None:
                raise SystemExit(f"{path.name}:{line_number}: '{line}' names no engine, reason or vector")
            if f"divergence.{engine}" in record or (
                    record["class"] == "impl-defined" and f"per-engine.{engine}" not in record):
                raise SystemExit(f"{path.name}:{line_number}: {vector_id} cannot take this classification")
            if record["class"] == "normative":
                record["class"] = "divergence"
            record[f"divergence.{engine}"] = reasons[key]

    def write(self, out: Path) -> None:
        out.mkdir(parents=True, exist_ok=True)
        order = ["id", "area", "op", "in", "out", "class"]
        for name, records in sorted(self.files.items()):
            lines = [
                "# Generated by scripts/conformance/generate_unicode_vectors.py; do not edit by hand.",
                "# Expected values: Unicode's test files and scripts/conformance/unicoderef.py, over the pinned UCD 17.0.0.",
                *FILE_COMMENTS.get(name, []),
                "schema: 1",
                "",
            ]
            for record in records:
                keys = order + sorted(k for k in record if k.startswith("per-engine.")) + sorted(
                    k for k in record if k.startswith("divergence.")) + ["prov", "script"] + (
                    ["note"] if "note" in record else []) + ["since"]
                for key in keys:
                    if key in record:
                        lines.append(f"{key}: {record[key]}")
                lines.append("")
            (out / f"{name}.vectors").write_text("\n".join(lines), encoding="utf-8", newline="\n")


# --------------------------------------------------------------------------------------------------
# Normalization.

FORMS = ("NFC", "NFD", "NFKC", "NFKD")
NORMALIZE = ("sec-string.prototype.normalize",)
NORMALIZATION_DATA = ("UnicodeData.txt", "DerivedNormalizationProps.txt")


def normalization_test_lines(db: U.UCD) -> dict[int, list[tuple[int, list[list[int]]]]]:
    """NormalizationTest.txt by part: (line number, [c1, c2, c3, c4, c5] as code units); every line's
    invariants are checked against the reference, all five columns in all four forms, before any is used."""
    parts: dict[int, list[tuple[int, list[list[int]]]]] = defaultdict(list)
    part = None
    for number, line in enumerate(db.pins.read("NormalizationTest.txt").split("\n"), 1):
        if line.startswith("@Part"):
            part = int(line.split()[0][len("@Part"):])
            continue
        data = line.split("#", 1)[0].strip()
        if not data:
            continue
        columns = [U.code_units(U.code_points_of(field)) for field in data.split(";")[:5]]
        c1, c2, c3, c4, c5 = columns
        n = db.normalize
        if not (c2 == n(c1, "NFC") == n(c2, "NFC") == n(c3, "NFC") and c4 == n(c4, "NFC") == n(c5, "NFC")
                and c3 == n(c1, "NFD") == n(c2, "NFD") == n(c3, "NFD") and c5 == n(c4, "NFD") == n(c5, "NFD")
                and all(c4 == n(c, "NFKC") and c5 == n(c, "NFKD") for c in columns)):
            raise Refused(f"NormalizationTest.txt line {number}: the reference disagrees")
        parts[part].append((number, columns))
    if sorted(parts) != [0, 1, 2, 3, 4, 5]:
        raise Refused(f"NormalizationTest.txt has the parts {sorted(parts)}, not 0 to 5")
    return parts


def part_1_sample(lines):
    hangul, other = [], []
    for number, columns in lines:
        cps = U.code_points(columns[0])
        if len(cps) == 1 and U.S_BASE <= cps[0] < U.S_BASE + U.S_COUNT:
            if (cps[0] - U.S_BASE) % PART_1_HANGUL_STEP == 0 or cps[0] == U.S_BASE + U.S_COUNT - 1:
                hangul.append((number, columns))
        else:
            other.append((number, columns))
    return sorted(hangul + other[::PART_1_OTHER_STEP])


def normalize_record(units, form: str, expected, files: tuple[str, ...], note: str | None = None) -> dict[str, str]:
    record = {
        "op": "String.prototype.normalize",
        "in": f"{sv(units)} str:\"{form}\"",
        "out": text_out(expected),
        "prov": prov(NORMALIZE, files),
        "script": f"{lit(units)}.normalize(\"{form}\")",
    }
    if note:
        record["note"] = note
    return record


N_CELLS = [
    ("N1", [0xD800]), ("N1", [0x61, 0xDC00, 0x301]),
    ("N2", [0x105C9]), ("N2", [0x11383]), ("N2", [0x1611E, 0x1611E]), ("N2", [0x105D2, 0x307]),
    ("N4", [0xE9]), ("N4", [0x65, 0x301]), ("N4", [0xFB01]), ("N4", [0xD4DB]), ("N4", [0x1111, 0x1171, 0x11B6]),
    ("N4", [0x344]), ("N4", [0x1E9B, 0x323]), ("N4", [0x71, 0x307, 0x323]), ("N4", [0xFDFA]), ("N4", [0x1D15E]),
    ("N4", [0xF73]), ("N4", [0x16D67]),
]
LONE_SURROGATE_TEXTS = [[0xD800], [0xDBFF], [0xDC00], [0xDFFF], [0xDC00, 0xD800], [0x61, 0x301, 0xD800],
                        [0xD800, 0x301], [0x1100, 0xD800, 0x1161], [0xD83D, 0xDE00], [0xD800, 0xD800, 0xDC00],
                        [0x65, 0xDBFF, 0x301, 0x323]]


def normalization(corpus: Corpus, db: U.UCD) -> None:
    file = "normalize"
    parts = normalization_test_lines(db)
    for part in (0, 1, 2, 3, 4, 5):
        lines = part_1_sample(parts[1]) if part == 1 else parts[part]
        for number, columns in lines:
            for k, form in enumerate(FORMS):
                corpus.add(file, f"normalize-part{part}", normalize_record(
                    columns[0], form, columns[k + 1], ("NormalizationTest.txt",) + NORMALIZATION_DATA,
                    f"NormalizationTest.txt line {number}, part {part}: c1 to {form} is c{k + 2}"))
    for row, cps in N_CELLS:
        units = units_of(cps)
        for form in FORMS:
            corpus.add(file, "normalize-cells", normalize_record(
                units, form, db.normalize(units, form), NORMALIZATION_DATA, f"correspondence row {row}"))
    for units in LONE_SURROGATE_TEXTS:
        for form in FORMS:
            corpus.add(file, "normalize-lone-surrogate", normalize_record(
                units, form, db.normalize(units, form), NORMALIZATION_DATA,
                "a lone surrogate is a code point of its own, of class 0, without a decomposition, so it passes "
                "through unchanged (correspondence row N1)"))
    # Row N1's collator: localeCompare's comparison of a lone surrogate, whose decomposition moves to the owner,
    # returns a Number and throws nothing (ECMA-262's clause and ECMA-402's both return a Number).
    for units in ([0xD800], [0x61, 0xDC00, 0x301]):
        corpus.add(file, "normalize-collator", {
            "op": "String.prototype.localeCompare of a lone surrogate, the type of its result",
            "in": f"{sv(units)} str:\"a\"",
            "out": str_out("number"),
            "prov": prov(("sec-string.prototype.localecompare",), ()),
            "script": f"typeof {lit(units)}.localeCompare(\"a\")",
            "note": ("a lone surrogate is a code point the collator's decomposition passes through; the result is a "
                     "Number, whatever its sign (correspondence row N1)"),
        })
    # The form argument (N5): checked before the text; undefined is NFC.
    form_cases = [
        ("\"a\".normalize(\"nfc\")", "str:\"a\" str:\"nfc\"", "error: RangeError"),
        ("\"a\".normalize(\"NFC \")", "str:\"a\" str:\"NFC \"", "error: RangeError"),
        ("\"a\".normalize(\"\")", "str:\"a\" str:\"\"", "error: RangeError"),
        ("\"a\".normalize(null)", "str:\"a\" null", "error: RangeError"),
        ("\"\\uD800\".normalize(\"nfd\")", "str:\"\\uD800\" str:\"nfd\"", "error: RangeError"),
        ("\"e\\u0301\".normalize()", "str:\"e\\u0301\"", text_out(db.normalize([0x65, 0x301], "NFC"))),
        ("\"e\\u0301\".normalize(undefined)", "str:\"e\\u0301\" undefined", text_out(db.normalize([0x65, 0x301], "NFC"))),
        ("\"\\u00E9\".normalize({ toString: function () { return \"NFD\"; } })", "str:\"\\u00E9\" str:\"NFD\"",
         text_out(db.normalize([0xE9], "NFD"))),
    ]
    for script, inputs, out in form_cases:
        corpus.add(file, "normalize-form", {
            "op": "String.prototype.normalize, the form argument",
            "in": inputs,
            "out": out,
            "prov": prov(NORMALIZE, NORMALIZATION_DATA),
            "script": script,
            "note": "the form is converted and checked before the text is normalized; undefined is NFC (correspondence row N5)",
        })
    # The ceiling (N6): a result of 2^24 code units is built whatever the decomposition's length; one past it is
    # the engine's limit.
    ceiling_cases = [
        ([0xE9], CEILING, [], "NFC", "an NFC result of 2^24 code units whose decomposition is twice as long (correspondence row N6)"),
        ([0xE9], CEILING // 2, [], "NFD", "an NFD result of 2^24 code units"),
        ([0xE9], CEILING // 2, [0x61], "NFD", None),
    ]
    for piece, count, suffix, form, note in ceiling_cases:
        length, _ = U.repeat_result(db, "normalization", form, piece, count, suffix)
        source = f"{lit(piece)}.repeat({count})" + (f" + {lit(suffix)}" if suffix else "")
        record = {
            "op": "String.prototype.normalize, the result's length",
            "in": f"{sv(piece)} int:{count} {sv(suffix)} str:\"{form}\"",
            "out": number_out(float(length)),
            "prov": prov(NORMALIZE, NORMALIZATION_DATA),
            "script": f"({source}).normalize(\"{form}\").length",
        }
        if length > CEILING:
            record["class"] = "impl-defined"
            record["per-engine.broiler-vm"] = "error: RangeError"
            record["note"] = f"one code unit past 2^24. {VM_CEILING_POLICY}; Broiler.JS's ceiling is above it"
        else:
            record["note"] = note
        corpus.add(file, "normalize-ceiling", record)


# --------------------------------------------------------------------------------------------------
# Casing.

UPPER = ("sec-string.prototype.touppercase",)
LOWER = ("sec-string.prototype.tolowercase",)
LOCALE_UPPER = ("sec-string.prototype.tolocaleuppercase",)
LOCALE_LOWER = ("sec-string.prototype.tolocalelowercase",)
# Upper case reads the mappings; lower case reads Cased and Case_Ignorable too (Final_Sigma); the languages'
# conditions read Soft_Dotted (PropList.txt) and the combining classes (UnicodeData.txt).
UPPER_DATA = ("UnicodeData.txt", "SpecialCasing.txt")
LOWER_DATA = ("UnicodeData.txt", "SpecialCasing.txt", "DerivedCoreProperties.txt")
LOCALE_CASING_DATA = ("UnicodeData.txt", "SpecialCasing.txt", "DerivedCoreProperties.txt", "PropList.txt")
TRANSFORM_CASE = ("ECMA-262's clause defers to ECMA-402's TransformCase, which takes the first requested locale, its "
                  "Unicode extension removed, matches it by prefix against the languages the UCD has language-sensitive "
                  "mappings for, and maps by that language: SpecialCasing.txt's conditional lines for tr, az and lt, "
                  "else the default mapping. Normative, as both engines' plans read it (JSD-0074's case-mapping section, "
                  "row C9; Broiler.JS's record of SJL-7.1), both engine-level runners' realms having Intl")


def case_record(units, upper: bool, tag: str | None, db: U.UCD, note: str | None = None) -> dict[str, str]:
    language = "default" if tag is None else U.parse_language(tag)
    expected = db.to_upper(units, language) if upper else db.to_lower(units, language)
    if tag is None:
        method = "toUpperCase" if upper else "toLowerCase"
        record = {
            "op": f"String.prototype.{method}",
            "in": sv(units),
            "out": text_out(expected),
            "prov": prov(UPPER, UPPER_DATA) if upper else prov(LOWER, LOWER_DATA),
            "script": f"{lit(units)}.{method}()",
        }
    else:
        method = "toLocaleUpperCase" if upper else "toLocaleLowerCase"
        record = {
            "op": f"String.prototype.{method}",
            "in": f"{sv(units)} {string_value(tag)}",
            "out": text_out(expected),
            "prov": prov(LOCALE_UPPER if upper else LOCALE_LOWER, LOCALE_CASING_DATA),
            "script": f"{lit(units)}.{method}({lit_str(tag)})",
        }
    if note:
        record["note"] = note
    return record


SIGMA_BEFORE = [[], [0x391], [0x391, 0x27], [0x27], [0x31], [0xAA], [0xBA], [0x2B0], [0x391, 0x345], [0x345],
                [0xD800], [0x391, 0x3A3], [0x391, 0x20], [0x1D400]]
SIGMA_AFTER = [[], [0x391], [0x27, 0x391], [0x27], [0x31], [0x2B0], [0x345], [0x345, 0x391], [0xDC00], [0x20, 0x392],
               [0x2E], [0x1D400]]

# The inputs of SpecialCasing.txt's conditional lines and of the language's unconditional ones, each where the
# condition holds and where it does not; every one goes through every tag, so each line is seen in its own
# language, in the other languages and in the default.
SPECIAL_CASING_INPUTS = [
    # Final_Sigma (every language).
    [0x391, 0x3A3], [0x391, 0x3A3, 0x391], [0x3A3],
    # tr and az: U+0130, U+0307 After_I, U+0049 Not_Before_Dot, U+0069.
    [0x130], [0x49, 0x307], [0x49, 0x316, 0x307], [0x49, 0x301, 0x307], [0x41, 0x307], [0x49], [0x49, 0x41],
    [0x69], [0x131], [0x69, 0x307],
    # lt: U+0307 After_Soft_Dotted, U+0049, U+004A and U+012E More_Above, U+00CC, U+00CD and U+0128.
    [0x6A, 0x307], [0x69, 0x316, 0x307], [0x69, 0x301, 0x307], [0x61, 0x307], [0x49, 0x300], [0x49, 0x316, 0x300],
    [0x49, 0x903, 0x300], [0x4A, 0x301], [0x4A], [0x12E, 0x303], [0x12E], [0xCC], [0xCD], [0x128],
    [0x1DF1A, 0x307], [0x1E04C, 0x307], [0x1E04D, 0x307], [0x1E068, 0x307],
]
CASING_TAGS = ("tr", "az", "lt", "en")

C_CELLS = [
    ("C1", [0xAA, 0x3A3], False), ("C1", [0xBA, 0x3A3], False),
    ("C2", [0x391, 0x3A3, 0x27, 0x391], False), ("C2", [0x391, 0x3A3, 0x2E, 0x391], False),
    ("C2", [0x391, 0x3A3, 0xB7, 0x391], False), ("C2", [0x391, 0x3A3, 0x2B0], False),
] + [("C3", [cp], upper) for cp in (0x1C89, 0xA7CB, 0xA7CE, 0xA7CF, 0xA7DC, 0x19B, 0x10D50, 0xDF, 0x130, 0x1C5, 0x149,
                                    0x1F80, 0x10400, 0x1E030) for upper in (True, False)]
C_LOCALE_CELLS = [
    ("C4", [0xA7CB], False, "tr"), ("C4", [0xA7CE], False, "tr"), ("C4", [0xA7CB], False, "az"),
    ("C4", [0xA7CE], False, "az"),
    ("C5", [0x1DF1A, 0x307], True, "lt"),
    ("C6", [0x49, 0x903, 0x300], False, "lt"), ("C6", [0x49, 0x31B, 0x300], False, "lt"),
    ("C6", [0x49, 0x93C, 0x300], False, "lt"), ("C6", [0x49, 0x315, 0x300], False, "lt"),
]
CASING_LONE_SURROGATES = [([0xD800, 0x61], True), ([0xDC00, 0x3A3], False), ([0xD800], True), ([0x61, 0xDBFF], True),
                          ([0xDFFF, 0xD800], False), ([0x391, 0xD800, 0x3A3], False), ([0x391, 0x3A3, 0xD800], False),
                          ([0xDC00, 0x3B1], True), ([0xD801, 0xDC28, 0xDC00], True), ([0xDBFF, 0xD801, 0xDC00], False)]


def casing(corpus: Corpus, db: U.UCD) -> None:
    file = "casing"
    for upper, group in ((True, "casing-upper"), (False, "casing-lower")):
        for cp in range(U.CODE_POINTS):
            if 0xD800 <= cp <= 0xDFFF:
                continue
            if db.change_case([cp], upper) != [cp]:
                corpus.add(file, group, case_record(units_of([cp]), upper, None, db))
    for before in SIGMA_BEFORE:
        for after in SIGMA_AFTER:
            units = units_of(before) + [0x3A3] + units_of(after)
            corpus.add(file, "casing-final-sigma", case_record(
                units, False, None, db, "Final_Sigma: a cased letter and case-ignorables before, and no case-ignorables "
                "then a cased letter after; the scans are possessive (section 3.13), and a lone surrogate is neither"))
    for cps in SPECIAL_CASING_INPUTS:
        for tag in CASING_TAGS:
            for upper in (True, False):
                corpus.add(file, "casing-special", case_record(units_of(cps), upper, tag, db, TRANSFORM_CASE))
    for row, cps, upper in C_CELLS:
        corpus.add(file, "casing-cells", case_record(units_of(cps), upper, None, db, f"correspondence row {row}"))
    for row, cps, upper, tag in C_LOCALE_CELLS:
        corpus.add(file, "casing-cells", case_record(units_of(cps), upper, tag, db,
                                                     f"correspondence row {row}. {TRANSFORM_CASE}"))
    # The locale argument (C7 to C9): CanonicalizeLocaleList, its first element, the extension removed.
    dotted_capital_i = text_out([0x130])
    locale_cases = [
        ("C7", "[\"tr\", \"en\"]", "str:\"i\" str:\"tr\" str:\"en\"", dotted_capital_i),
        ("C7", "[\"en\", \"tr\"]", "str:\"i\" str:\"en\" str:\"tr\"", text_out([0x49])),
        ("C8", "null", "str:\"i\" null", "error: TypeError"),
        ("C9", "\"tr\"", "str:\"i\" str:\"tr\"", dotted_capital_i),
        ("C9", "\"az\"", "str:\"i\" str:\"az\"", dotted_capital_i),
        ("C9", "\"TR\"", "str:\"i\" str:\"TR\"", dotted_capital_i),
        ("C9", "\"tr-u-co-search\"", "str:\"i\" str:\"tr-u-co-search\"", dotted_capital_i),
        ("C9", "\"tr-TR\"", "str:\"i\" str:\"tr-TR\"", dotted_capital_i),
        ("C9", "\"x!\"", "str:\"i\" str:\"x!\"", "error: RangeError"),
    ]
    for row, argument, inputs, out in locale_cases:
        corpus.add(file, "casing-locale", {
            "op": "String.prototype.toLocaleUpperCase, " + ("a list of locales" if argument.startswith("[") else "the locale argument"),
            "in": inputs,
            "out": out,
            "prov": prov(LOCALE_UPPER, LOCALE_CASING_DATA),
            "script": f"\"i\".toLocaleUpperCase({argument})",
            "note": (f"correspondence row {row}: CanonicalizeLocaleList throws a TypeError for null and a RangeError "
                     "for a malformed tag. " + TRANSFORM_CASE),
        })
    for cps, upper in CASING_LONE_SURROGATES:
        corpus.add(file, "casing-lone-surrogate", case_record(
            units_of(cps), upper, None, db, "a lone surrogate maps to itself and is neither cased nor case-ignorable "
            "(correspondence row C11)"))
    # The ceiling (C12): the whole result's length is checked, in the language pass too.
    ceiling_cases = [
        ([0xDF], CEILING // 2, [], ("upper", "default"), None),
        ([0xDF], CEILING // 2, [0x61], ("upper", "default"), None),
        ([0x130], CEILING // 2, [], ("lower", "lithuanian"), "lt"),
        ([0x130], CEILING // 2, [0x49], ("lower", "lithuanian"), "lt"),
    ]
    for piece, count, suffix, operation, tag in ceiling_cases:
        length, _ = U.repeat_result(db, "casing", operation, piece, count, suffix)
        source = f"{lit(piece)}.repeat({count})" + (f" + {lit(suffix)}" if suffix else "")
        upper = operation[0] == "upper"
        method = ("toLocaleUpperCase" if upper else "toLocaleLowerCase") if tag else ("toUpperCase" if upper else "toLowerCase")
        record = {
            "op": f"String.prototype.{method}, the result's length",
            "in": f"{sv(piece)} int:{count} {sv(suffix)}" + (f" {string_value(tag)}" if tag else ""),
            "out": number_out(float(length)),
            "prov": prov(LOCALE_LOWER, LOCALE_CASING_DATA) if tag else prov(UPPER, UPPER_DATA),
            "script": f"({source}).{method}({lit_str(tag) if tag else ''}).length",
        }
        if length > CEILING:
            record["class"] = "impl-defined"
            record["per-engine.broiler-vm"] = "error: RangeError"
            record["note"] = (f"one code unit past 2^24 (correspondence row C12). {VM_CEILING_POLICY}; Broiler.JS's "
                              "ceiling is above it")
        else:
            record["note"] = "a result of exactly 2^24 code units (correspondence row C12)"
        corpus.add(file, "casing-ceiling", record)
    case_folding(corpus, db)


# Case folding, through RegExp's ignoreCase (correspondence row F1; row F2, the class [a-z] under i without u, is
# SJL-11's and has no vector). Under u or v, Canonicalize is simple case folding (CaseFolding.txt, status C and
# S); without them, it is the one-unit full upper-case mapping, kept unless it is longer or leaves a unit at or
# above 128 for one below it.
FOLDING = ("sec-runtime-semantics-canonicalize-ch", "sec-runtime-semantics-charactersetmatcher-abstract-operation",
           "sec-regexp.prototype.test")
F1_CELLS = [
    ("\\w", "iu", 0x17F), ("[a-z]", "iu", 0x212A), ("\\u0390", "iu", 0x1FD3), ("\\u00DF", "i", 0x1E9E),
    ("\\u00DF", "iu", 0x1E9E), ("\\u{10400}", "iu", 0x10428), ("\\u03C3", "i", 0x3C2),
]


def folding_matches(db: U.UCD, members: list[int], flags: str, cp: int) -> bool:
    """CharacterSetMatcher with ignoreCase: some member canonicalizes to what cp canonicalizes to."""
    canonicalize = db.fold if "u" in flags or "v" in flags else db.canonicalize_without_u
    return any(canonicalize(m) == canonicalize(cp) for m in members)


def case_folding(corpus: Corpus, db: U.UCD) -> None:
    file = "casing"
    # Every orbit of more than one code point: its first member as the pattern, against every member and the
    # first code point after the orbit that is not in it; under u, and under v too for the orbits of three or four.
    for orbit in sorted(set(db.orbits.values())):
        pattern = orbit[0]
        outside = next(c for c in range(orbit[-1] + 1, U.CODE_POINTS) if db.fold(c) != db.fold(pattern))
        tested = list(orbit) + [outside]
        for flags in ("iu", "iv") if len(orbit) > 2 else ("iu",):
            tests = ", ".join(f"r.test({lit(units_of([c]))})" for c in tested)
            corpus.add(file, "casing-folding", {
                "op": "RegExp ignoreCase, simple case folding",
                "in": f"int:{pattern} {string_value(flags)} " + " ".join(f"int:{c}" for c in tested),
                "out": str_out(",".join("true" if db.fold(c) == db.fold(pattern) else "false" for c in tested)),
                "prov": prov(FOLDING, ("CaseFolding.txt",)),
                "script": f"var r = /^{escape(pattern)}$/{flags}; [{tests}].join()",
                "note": (f"the orbit {hex_cps(orbit)} and {hex_cps([outside])}: under u or v, Canonicalize is simple "
                         "case folding, so a code point matches every member of its orbit and nothing else"),
            })
    for pattern, flags, cp in F1_CELLS:
        if pattern == "\\w":
            members = [c for c in range(0x80) if chr(c).isalnum() or c == 0x5F]
        elif pattern == "[a-z]":
            members = list(range(0x61, 0x7B))
        else:
            members = [int(pattern.strip("\\u{}"), 16)]
        corpus.add(file, "casing-folding-cells", {
            "op": "RegExp ignoreCase" + (", simple case folding" if "u" in flags else ", Canonicalize without u or v"),
            "in": f"{string_value(pattern)} {string_value(flags)} int:{cp}",
            "out": bool_out(folding_matches(db, members, flags, cp)),
            "prov": prov(FOLDING + (("sec-wordcharacters",) if pattern == "\\w" else ()),
                         ("CaseFolding.txt",) if "u" in flags else UPPER_DATA),
            "script": f"/{pattern}/{flags}.test({lit(units_of([cp]))})",
            "note": "correspondence row F1",
        })


# --------------------------------------------------------------------------------------------------
# Properties: \p{...} acceptance and membership.

EARLY_ERRORS = "sec-patterns-static-semantics-early-errors"
MATCH_PROPERTY = "sec-runtime-semantics-unicodematchproperty-p"
MATCH_VALUE = "sec-runtime-semantics-unicodematchpropertyvalue-p-v"
MEMBERSHIP = ("sec-compiletocharset", MATCH_PROPERTY, MATCH_VALUE)


def matching(db: U.UCD, body: str) -> tuple[str, ...]:
    """The clauses that resolve an admitted body: UnicodeMatchProperty and UnicodeMatchPropertyValue for a
    Name=Value pair, UnicodeMatchPropertyValue for a lone General_Category value (CompileToCharSet tries it
    first), and UnicodeMatchProperty for any other lone name; a refused body is the early errors', which read
    the tables UnicodeMatchProperty's clause holds."""
    if not any(accepted(db, body, flag) for flag in "uv"):
        return (EARLY_ERRORS, MATCH_PROPERTY, MATCH_VALUE)
    if "=" in body:
        return (MATCH_PROPERTY, MATCH_VALUE)
    return (MATCH_VALUE,) if body in db.values_of["General_Category"] else (MATCH_PROPERTY,)


STRINGS = ("sec-compiletocharset", "sec-static-semantics-maycontainstrings", "sec-runtime-semantics-unicodematchproperty-p")
NAME_DATA = ("PropertyAliases.txt", "PropertyValueAliases.txt")
BINARY_FILES = ("PropList.txt", "DerivedCoreProperties.txt", "DerivedNormalizationProps.txt",
                "DerivedBinaryProperties.txt", "emoji-data.txt")
SAFE_NAME = frozenset("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_.- =&")


def accepted(db: U.UCD, body: str, flag: str) -> bool:
    """Whether \\p{body} is admitted under the flag: a lone name, or one Name=Value pair."""
    if "=" not in body:
        return db.resolve_lone(body, unicode_sets=flag == "v") != U.REFUSED
    prop, _, value = body.partition("=")
    return "=" not in value and db.resolve_pair(prop, value) != U.REFUSED


def acceptance_record(db: U.UCD, body: str, flag: str, files: tuple[str, ...], note: str | None = None,
                      negated: bool = False) -> dict[str, str]:
    escape_letter = "P" if negated else "p"
    ok = accepted(db, body, flag) and not (negated and body in db.ecma_strings)
    clauses = matching(db, body)
    if not ok and EARLY_ERRORS not in clauses:
        clauses = (EARLY_ERRORS,) + clauses
    if body in db.ecma_strings:
        clauses += ("sec-static-semantics-maycontainstrings",)
    record = {
        "op": "RegExp \\" + escape_letter + "{...}, acceptance",
        "in": f"{string_value(body)} {string_value(flag)}",
        "out": bool_out(True) if ok else "error: SyntaxError",
        "prov": prov(clauses, files),
        "script": f"/\\{escape_letter}{{{body}}}/{flag} instanceof RegExp",
    }
    if note:
        record["note"] = note
    return record


def membership_record(db: U.UCD, expression: str, flag: str, cps: list[int], expected: list[bool],
                      files: tuple[str, ...], note: str | None = None, negated: bool = False) -> dict[str, str]:
    escape_letter = "P" if negated else "p"
    tests = ", ".join(f"r.test({lit(units_of([c]))})" for c in cps)
    record = {
        "op": "RegExp \\" + escape_letter + "{...}, membership of code points",
        "in": f"{string_value(expression)} {string_value(flag)} " + " ".join(f"int:{c}" for c in cps),
        "out": str_out(",".join("true" if e else "false" for e in expected)),
        "prov": prov(("sec-compiletocharset",) + matching(db, expression), files),
        "script": f"var r = /^\\{escape_letter}{{{expression}}}$/{flag}; [{tests}].join()",
    }
    if note:
        record["note"] = note
    return record


def property_files(db: U.UCD) -> dict[str, tuple[str, ...]]:
    """The UCD file each set's members come from, by its resolution's name."""
    out: dict[str, tuple[str, ...]] = {}
    for name in BINARY_FILES:
        for rec in U.records(db.pins.read(name)):
            if len(rec) == 2:
                out.setdefault(rec[1], (name,))
    out["Any"] = ()
    out["ASCII"] = ()
    out["Assigned"] = ("UnicodeData.txt",)
    return out


def files_of_set(name: str, binary_files: dict[str, tuple[str, ...]]) -> tuple[str, ...]:
    if name.startswith("General_Category="):
        return ("UnicodeData.txt", "PropertyValueAliases.txt")
    if name.startswith("Script="):
        return ("Scripts.txt", "PropertyValueAliases.txt")
    if name.startswith("Script_Extensions="):
        return ("ScriptExtensions.txt", "Scripts.txt", "PropertyValueAliases.txt")
    return binary_files[name]


def string_files(name: str) -> tuple[str, ...]:
    if name == "RGI_Emoji":
        return ("emoji-sequences.txt", "emoji-zwj-sequences.txt")
    if name == "RGI_Emoji_ZWJ_Sequence":
        return ("emoji-zwj-sequences.txt",)
    return ("emoji-sequences.txt",)


def admitted_names(db: U.UCD) -> tuple[list[str], list[tuple[str, str]]]:
    """Every lone name (General_Category's values and aliases, then the binary properties and aliases of
    ECMA-262's table) and every Name=Value pair the tables and PropertyValueAliases.txt admit."""
    lone = []
    for rec in db.value_aliases["gc"]:
        lone.extend(rec)
    lone.extend(db.ecma_binary)
    pairs = []
    for prop in db.ecma_nonbinary:
        canonical = db.ecma_nonbinary[prop]
        key = "gc" if canonical == "General_Category" else "sc"
        for rec in db.value_aliases[key]:
            for alias in rec:
                pairs.append((prop, alias))
    return list(dict.fromkeys(lone)), list(dict.fromkeys(pairs))


def mutations(name: str) -> list[tuple[str, str]]:
    """The refused set's changes to an admitted name: its case changed either way, its underscores removed, and
    an Is before it."""
    return [(name.lower(), "in lower case"), (name.upper(), "in upper case"),
            (name.replace("_", ""), "without its underscores"), ("Is" + name, "with Is before it")]


def refused_bodies(db: U.UCD) -> list[tuple[str, str, str]]:
    """(body, flag, why) for the refused set; each is checked to be refused under the flag it is used with, and
    a change that leaves the name admitted, or unchanged, is dropped."""
    lone, _ = admitted_names(db)
    out: list[tuple[str, str, str]] = []
    seen: set[tuple[str, str]] = set()

    def add(body: str, flags: str, why: str) -> None:
        if not set(body) <= SAFE_NAME:
            return
        for flag in flags:
            if (body, flag) in seen or accepted(db, body, flag):
                continue
            seen.add((body, flag))
            out.append((body, flag, why))

    # Each lone name changed four ways under u, and in lower case under v as well.
    for name in lone:
        for mutated, how in mutations(name):
            add(mutated, "uv" if how == "in lower case" else "u", f"the lone name {name} {how}")
    # Each non-binary property's name, and each General_Category value, changed four and three ways; each
    # Script value's long name changed three ways, and its short alias in lower case.
    for prop in db.ecma_nonbinary:
        value = "Lu" if db.ecma_nonbinary[prop] == "General_Category" else "Latn"
        for mutated, how in mutations(prop):
            add(f"{mutated}={value}", "u", f"the property name {prop} {how}")
    for rec in db.value_aliases["gc"]:
        for alias in rec:
            for mutated, how in mutations(alias)[:3]:
                add(f"General_Category={mutated}", "u", f"the General_Category value {alias} {how}")
    for rec in db.value_aliases["sc"]:
        for mutated, how in mutations(rec[1])[:3]:
            add(f"Script={mutated}", "u", f"the Script value {rec[1]} {how}")
        add(f"Script={rec[0].lower()}", "u", f"the Script value {rec[0]} in lower case")
    # A value under the wrong property, a lone property, a script alone.
    for body in ("sc=Lu", "Script=Letter", "scx=Uppercase_Letter", "gc=Latin", "gc=Latn", "General_Category=Greek",
                 "General_Category", "gc", "Script", "sc", "Script_Extensions", "scx", "Latin", "Latn", "Greek",
                 "Katakana_Or_Hiragana", "Hrkt", "L&", "LC=Y", "Lu=Y", "Letter=Yes"):
        add(body, "uv", "a value under a property that does not have it, or a non-binary property or a value alone")
    # A binary property, or a property of strings, given a value.
    for canonical in sorted(set(db.ecma_binary.values())):
        add(f"{canonical}=Yes", "u", "a binary property takes no value")
    for name in db.ecma_strings:
        add(f"{name}=Yes", "v", "a property of strings takes no value")
    # Every property PropertyAliases.txt names that the tables do not admit, alone and with a value.
    admitted = set(db.ecma_binary) | set(db.ecma_nonbinary) | set(db.ecma_strings)
    for rec in db.property_aliases:
        if any(alias in admitted for alias in rec):
            continue
        for alias in rec:
            add(alias, "u", "a property of PropertyAliases.txt that ECMA-262's tables do not admit")
        values = db.value_aliases.get(rec[0])
        if values:
            add(f"{rec[0]}={values[0][0]}", "u", "a property of PropertyAliases.txt that ECMA-262's tables do not admit, with a value")
            add(f"{rec[1]}={values[0][1]}", "u", "a property of PropertyAliases.txt that ECMA-262's tables do not admit, with a value")
    for body in ("Age=1.1", "Age=V1_1", "age=17.0", "Block=Basic_Latin", "blk=ASCII", "In_Basic_Latin", "InBasicLatin",
                 "IsLatin", "Is_Latin", "InLatin", "Script=IsLatin"):
        add(body, "uv", "an Age, a Block or a prefixed name that ECMA-262 does not admit")
    # Malformed bodies.
    for body in ("", "=Lu", "gc=", "gc==Lu", "gc=Lu=Lu", " Lu", "Lu ", "gc =Lu", "gc= Lu", "General Category=Lu",
                 "General-Category=Lu", "Script_Extensions=Greek Latin"):
        add(body, "uv", "a malformed property body")
    return out


def selected_ranges(ranges: list[tuple[int, int]]) -> list[tuple[int, int]]:
    n = len(ranges)
    if n <= MEMBERSHIP_RANGES:
        return list(ranges)
    picks = sorted({round(k * (n - 1) / (MEMBERSHIP_RANGES - 1)) for k in range(MEMBERSHIP_RANGES)})
    return [ranges[i] for i in picks]


def edges(first: int, last: int) -> list[int]:
    points = [first - 1, first, last, last + 1] if first != last else [first - 1, first, first + 1]
    return [p for p in points if 0 <= p < U.CODE_POINTS]


# The code points an empty set's membership vectors test: Hiragana, Katakana, the prolonged sound mark and a
# halfwidth Katakana letter, the code points Katakana_Or_Hiragana would name if it had any.
EMPTY_SET_PROBES = [0x3042, 0x30A2, 0x30FC, 0xFF71]


P_CELLS = [
    ("P1", "letter", "u"), ("P1", "Script_Extensions=greek", "u"), ("P1", "GC=Lu", "u"), ("P1", "lu", "u"),
    ("P1", "rgiemoji", "v"), ("P2", "sc=Hrkt", "u"), ("P2", "Script=Katakana_Or_Hiragana", "u"),
    ("P3", "IsLatin", "u"), ("P6", "Lu", "u"), ("P6", "General_Category=Lu", "u"), ("P6", "gc=LC", "u"),
    ("P6", "Lowercase_Letter", "u"), ("P6", "Any", "u"), ("P6", "Script=Zzzz", "u"),
    ("P6", "Script_Extensions=Unknown", "u"), ("P6", "scx=Grek", "u"), ("P6", "scx=Hira", "u"), ("P6", "ID_Start", "u"),
    ("P6", "ASCII=Y", "u"), ("S1", "RGI_Emoji", "u"), ("S2", "rgiemoji", "v"),
]
P_MEMBERSHIP_CELLS = [
    ("P2", "sc=Hrkt", "u", [0x3042, 0x30A2, 0x30FC]), ("P2", "Script=Katakana_Or_Hiragana", "u", [0x3042, 0x30A2]),
    ("P6", "Lu", "u", [0x41, 0x61]), ("P6", "General_Category=Lu", "u", [0x41, 0x61]), ("P6", "gc=LC", "u", [0x41, 0x61, 0x1C5, 0x2B0]),
    ("P6", "Lowercase_Letter", "u", [0x61, 0x41]), ("P6", "Any", "u", [0x0, 0xD800, 0x10FFFF]),
    ("P6", "Script=Zzzz", "u", [0x378, 0x41]), ("P6", "Script_Extensions=Unknown", "u", [0x378, 0x41]),
    ("P6", "scx=Grek", "u", [0x342, 0x3B1, 0x41]), ("P6", "scx=Hira", "u", [0x30FC, 0x3042, 0x30A2]),
    ("P6", "ID_Start", "u", [0x2118, 0x2E2F]),
]
SET_OPERATIONS = [
    ("P4", "[\\p{gc=Lu}--\\p{Script=Latin}]", [0x41, 0x391, 0x3B1, 0x410]),
    ("P4", "[\\p{Lu}&&\\p{Script=Greek}]", [0x41, 0x391, 0x3B1]),
    ("P4", "[\\p{General_Category=Letter}--[a-z]]", [0x61, 0x7A, 0x41, 0xE9]),
    ("P4", "[\\p{Script_Extensions=Hira}&&\\p{Script=Common}]", [0x30FC, 0x3042, 0x41]),
    ("P4", "[\\p{Nd}--[0-9]]", [0x30, 0x39, 0x660, 0x1D7CE]),
    ("P4", "[\\p{ASCII}&&\\p{gc=P}]", [0x21, 0x41, 0xA1]),
]


def set_operation_expected(db: U.UCD, pattern: str, cp: int) -> bool:
    sets = db.sets()
    inside = lambda name: U.ranges_contain(sets[name], cp)  # noqa: E731
    if pattern == "[\\p{gc=Lu}--\\p{Script=Latin}]":
        return inside("General_Category=Uppercase_Letter") and not inside("Script=Latin")
    if pattern == "[\\p{Lu}&&\\p{Script=Greek}]":
        return inside("General_Category=Uppercase_Letter") and inside("Script=Greek")
    if pattern == "[\\p{General_Category=Letter}--[a-z]]":
        return inside("General_Category=Letter") and not 0x61 <= cp <= 0x7A
    if pattern == "[\\p{Script_Extensions=Hira}&&\\p{Script=Common}]":
        return inside("Script_Extensions=Hiragana") and inside("Script=Common")
    if pattern == "[\\p{Nd}--[0-9]]":
        return inside("General_Category=Decimal_Number") and not 0x30 <= cp <= 0x39
    if pattern == "[\\p{ASCII}&&\\p{gc=P}]":
        return inside("ASCII") and inside("General_Category=Punctuation")
    raise AssertionError(pattern)


def properties(corpus: Corpus, db: U.UCD) -> None:
    file = "properties"
    lone, pairs = admitted_names(db)
    # Acceptance of every admitted name, under u and v.
    for name in lone:
        for flag in ("u", "v"):
            corpus.add(file, "property-accepted", acceptance_record(db, name, flag, NAME_DATA))
    # Every pair under u; under v, every value with the property's canonical name.
    for prop, value in pairs:
        for flag in ("u", "v") if prop in ("General_Category", "Script", "Script_Extensions") else ("u",):
            corpus.add(file, "property-accepted", acceptance_record(db, f"{prop}={value}", flag, NAME_DATA))
    for name in db.ecma_strings:
        for flag in ("v", "u"):
            corpus.add(file, "property-accepted", acceptance_record(
                db, name, flag, NAME_DATA + string_files(name),
                "a property of strings is admitted under v alone (UnicodeMatchProperty, MayContainStrings)"))
        corpus.add(file, "property-accepted", acceptance_record(
            db, name, "v", NAME_DATA + string_files(name), "\\P{...} of a property of strings is an early error", negated=True))
    # The refused set.
    for body, flag, why in refused_bodies(db):
        corpus.add(file, "property-refused", acceptance_record(
            db, body, flag, NAME_DATA, f"{why}: ECMA-262 admits only its tables' names and values, compared exactly"))
    # Membership at range edges.
    sets = db.sets()
    binary_files = property_files(db)
    names = [n for n in sets if n.startswith("General_Category=")]
    names += sorted(set(db.ecma_binary.values()), key=str)
    names += [n for n in sets if n.startswith("Script=")] + [n for n in sets if n.startswith("Script_Extensions=")]
    for name in names:
        ranges = sets[name]
        files = files_of_set(name, binary_files)
        if not ranges:
            probes = EMPTY_SET_PROBES
            for flag in ("u", "v"):
                corpus.add(file, "property-membership", membership_record(
                    db, name, flag, probes, [False] * len(probes), files,
                    "an empty set: no code point has this value (Katakana_Or_Hiragana is a value of Script with no "
                    "code point in Scripts.txt or ScriptExtensions.txt)"))
            continue
        chosen = selected_ranges(ranges)
        for index, (first, last) in enumerate(chosen):
            cps = edges(first, last)
            expected = [U.ranges_contain(ranges, c) for c in cps]
            flags = ("u", "v") if index in (0, len(chosen) - 1) else ("u",)
            for flag in flags:
                corpus.add(file, "property-membership", membership_record(
                    db, name, flag, cps, expected, files,
                    f"the range {first:04X}..{last:04X}, {len(ranges)} ranges in all: one before, the first, the last, one after"))
    for row, body, flag in P_CELLS:
        files = NAME_DATA + (string_files(body) if body in db.ecma_strings else ())
        corpus.add(file, "property-cells", acceptance_record(db, body, flag, files, f"correspondence row {row}"))
    corpus.add(file, "property-cells", acceptance_record(db, "RGI_Emoji", "v", NAME_DATA + string_files("RGI_Emoji"),
                                                         "correspondence row S1", negated=True))
    for row, body, flag, cps in P_MEMBERSHIP_CELLS:
        prop, _, value = body.partition("=")
        name = db.resolve_pair(prop, value) if value else db.resolve_lone(body)
        expected = [U.ranges_contain(sets[name], c) for c in cps]
        corpus.add(file, "property-cells", membership_record(db, body, flag, cps, expected, files_of_set(name, binary_files),
                                                             f"correspondence row {row}"))
    # Set operations under v with General_Category inside them (P4).
    for row, pattern, cps in SET_OPERATIONS:
        expected = [set_operation_expected(db, pattern, c) for c in cps]
        tests = ", ".join(f"r.test({lit(units_of([c]))})" for c in cps)
        corpus.add(file, "property-set-operation", {
            "op": "RegExp v-mode set operation, membership of code points",
            "in": f"{string_value(pattern)} str:\"v\" " + " ".join(f"int:{c}" for c in cps),
            "out": str_out(",".join("true" if e else "false" for e in expected)),
            "prov": prov(MEMBERSHIP, ("UnicodeData.txt", "Scripts.txt", "ScriptExtensions.txt", "PropertyValueAliases.txt")),
            "script": f"var r = /^{pattern}$/v; [{tests}].join()",
            "note": f"General_Category and Script inside a v-mode set operation (correspondence row {row})",
        })
    # Properties of strings: members and near-misses, under v.
    for name in db.ecma_strings:
        members = sorted(db.string_members[name])
        picks = sorted({round(k * (len(members) - 1) / 9) for k in range(10)}) if members else []
        for i in picks:
            member = list(members[i])
            candidates = [member]
            if len(member) > 1:
                candidates.append(member[:-1])
                candidates.append(member + [0xFE0F])
            for cps in candidates:
                units = units_of(cps)
                expected = db.in_string_property(units, name)
                corpus.add(file, "property-strings", {
                    "op": "RegExp \\p{...} of strings, membership of a string",
                    "in": f"{string_value(name)} str:\"v\" {sv(units)}",
                    "out": bool_out(expected),
                    "prov": prov(STRINGS + ("sec-regexp.prototype.test",), string_files(name)),
                    "script": f"/^\\p{{{name}}}$/v.test({lit(units)})",
                    "note": f"{hex_cps(cps)}: {'a member' if expected else 'not a member'} of {name}",
                })
    for row, name, cps in (("S1", "RGI_Emoji", [0x1F44D, 0x1F3FD]), ("S1", "Basic_Emoji", [0x231A]),
                           ("S1", "RGI_Emoji", [0x1F468, 0x200D, 0x2764, 0xFE0F, 0x200D, 0x1F468]),
                           ("S1", "RGI_Emoji", [0x1F468, 0x200D, 0x2764, 0x200D, 0x1F468])):
        units = units_of(cps)
        corpus.add(file, "property-cells", {
            "op": "RegExp \\p{...} of strings, membership of a string",
            "in": f"{string_value(name)} str:\"v\" {sv(units)}",
            "out": bool_out(db.in_string_property(units, name)),
            "prov": prov(STRINGS + ("sec-regexp.prototype.test",), string_files(name)),
            "script": f"/^\\p{{{name}}}$/v.test({lit(units)})",
            "note": f"correspondence row {row}",
        })
    for name in ("RGI_Emoji", "Basic_Emoji"):
        corpus.add(file, "property-strings", {
            "op": "RegExp \\p{...} of strings in a negated class, acceptance",
            "in": f"{string_value(name)} str:\"v\"",
            "out": "error: SyntaxError",
            "prov": prov(("sec-patterns-static-semantics-early-errors", "sec-static-semantics-maycontainstrings"), ()),
            "script": f"/[^\\p{{{name}}}]/v instanceof RegExp",
            "note": "a negated class that may contain strings is an early error",
        })


# --------------------------------------------------------------------------------------------------
# Identifiers and white space.

IDENTIFIER = ("sec-identifier-names", "sec-identifier-names-static-semantics-early-errors", "sec-identifiercodepoint")
IDENTIFIER_DATA = ("DerivedCoreProperties.txt", "PropList.txt")
GROUP_NAME = ("sec-patterns-static-semantics-early-errors", "sec-regexpidentifiercodepoint", "sec-identifier-names")


def identifier_record(db: U.UCD, cp: int, position: str, note: str | None = None) -> dict[str, str]:
    sets = db.sets()
    if position == "start":
        ok = U.ranges_contain(sets["lexical:IdentifierStartChar"], cp)
        script = f"var {escape(cp)}; true"
        op = "IdentifierStart, an escaped code point"
    else:
        ok = U.ranges_contain(sets["lexical:IdentifierPartChar"], cp)
        script = f"var a{escape(cp)}; true"
        op = "IdentifierPart, an escaped code point"
    record = {
        "op": op,
        "in": f"int:{cp}",
        "out": bool_out(True) if ok else "error: SyntaxError",
        "prov": prov(IDENTIFIER, IDENTIFIER_DATA),
        "script": script,
    }
    if note:
        record["note"] = note
    return record


SPECIAL_IDENTIFIER_POINTS = [
    (0x24, "$"), (0x5F, "_"), (0x200C, "ZWNJ"), (0x200D, "ZWJ"), (0x2E2F, "row I1: Lm and Pattern_Syntax"),
    (0x0870, "row I2: Unicode 14"), (0x16D40, "row I2: Unicode 16"), (0x11B60, "row I2: Unicode 17"),
    (0x2118, "row I2: Other_ID_Start"), (0x1885, "row I2: Other_ID_Start"), (0x00B7, "row I2: Other_ID_Continue"),
    (0x3164, "Lo, Other_Default_Ignorable_Code_Point"), (0x30, "a digit"), (0xD800, "a lone surrogate"),
    (0xDC00, "a lone surrogate"), (0x10FFFF, "the last code point"), (0x0, "the first code point"),
]

WHITE_SPACE_CANDIDATES = [0x9, 0xB, 0xC, 0x20, 0xA0, 0x1680] + list(range(0x2000, 0x200B)) + [0x202F, 0x205F, 0x3000, 0xFEFF] + [
    0x85, 0x180E, 0x200B, 0x200C, 0x200D, 0x2060, 0xFFFE, 0xAD, 0x1C, 0x1D, 0x1E, 0x1F, 0x8, 0xE, 0x3164, 0xFFA0]

SYNTAX_CHARACTERS = frozenset(ord(c) for c in "^$\\.*+?()[]{}|")
CONTROL_ESCAPES = {0x09: "t", 0x0A: "n", 0x0B: "v", 0x0C: "f", 0x0D: "r"}
OTHER_PUNCTUATORS = frozenset(ord(c) for c in ",-=<>#&!%:;@~'`\"")


def regexp_escape(db: U.UCD, units) -> list[int]:
    """RegExp.escape and EncodeForRegExpEscape (sec-regexp.escape, sec-encodeforregexpescape), as the pinned
    edition writes them, over the reference's WhiteSpace and LineTerminator sets."""
    sets = db.sets()
    out: list[int] = []
    for cp in U.code_points(units):
        if not out and (0x30 <= cp <= 0x39 or 0x41 <= cp <= 0x5A or 0x61 <= cp <= 0x7A):
            piece = f"\\x{cp:02x}"
        elif cp in SYNTAX_CHARACTERS or cp == 0x2F:
            piece = "\\" + chr(cp)
        elif cp in CONTROL_ESCAPES:
            piece = "\\" + CONTROL_ESCAPES[cp]
        elif (cp in OTHER_PUNCTUATORS or U.ranges_contain(sets["lexical:WhiteSpace"], cp)
              or U.ranges_contain(sets["lexical:LineTerminator"], cp) or 0xD800 <= cp <= 0xDFFF):
            piece = f"\\x{cp:02x}" if cp <= 0xFF else "".join(f"\\u{u:04x}" for u in U.code_units([cp]))
        else:
            out.extend(U.code_units([cp]))
            continue
        out.extend(ord(c) for c in piece)
    return out


def is_white_space(db: U.UCD, cp: int) -> bool:
    return U.ranges_contain(db.sets()["lexical:WhiteSpace"], cp)


def is_white_space_or_line_terminator(db: U.UCD, cp: int) -> bool:
    sets = db.sets()
    return U.ranges_contain(sets["lexical:WhiteSpace"], cp) or U.ranges_contain(sets["lexical:LineTerminator"], cp)


def trim(db: U.UCD, units) -> list[int]:
    """TrimString (sec-trimstring) with both ends: the white space and line terminators at each end removed."""
    i, j = 0, len(units)
    while i < j and is_white_space_or_line_terminator(db, units[i]):
        i += 1
    while j > i and is_white_space_or_line_terminator(db, units[j - 1]):
        j -= 1
    return list(units[i:j])


def white_space(corpus: Corpus, db: U.UCD) -> None:
    file = "identifiers"
    data = ("UnicodeData.txt",)
    for cp in WHITE_SPACE_CANDIDATES:
        ws = is_white_space(db, cp)
        name = f"U+{cp:04X}, {'WhiteSpace' if ws else 'not WhiteSpace'}"
        source = units_of([0x31, cp, 0x2B, 0x31])
        corpus.add(file, "white-space", {
            "op": "WhiteSpace in source text",
            "in": f"int:{cp}",
            "out": number_out(2.0) if ws else "error: SyntaxError",
            "prov": prov(("sec-white-space", "sec-tokens", "sec-performeval"), data),
            "script": f"(0, eval)({lit(source)})",
            "note": (f"{name}: 1, the code point and +1 as source text, through indirect eval, the one way an ASCII "
                     "script can put the code point in source text (correspondence rows W1 and W3)"),
        })
        for flag in ("", "u"):
            corpus.add(file, "white-space", {
                "op": "WhiteSpace, CharacterClassEscape s" + (" under u" if flag else ""),
                "in": f"int:{cp}",
                "out": bool_out(is_white_space_or_line_terminator(db, cp)),
                "prov": prov(("sec-compiletocharset", "sec-white-space", "sec-regexp.prototype.test"), data),
                "script": f"/^\\s$/{flag}.test({lit([cp])})",
                "note": f"{name}: \\s is WhiteSpace and LineTerminator (correspondence row W3)",
            })
        units = [cp, 0x78, cp]
        corpus.add(file, "white-space", {
            "op": "WhiteSpace, String.prototype.trim",
            "in": sv(units),
            "out": text_out(trim(db, units)),
            "prov": prov(("sec-trimstring", "sec-string.prototype.trim", "sec-white-space"), data),
            "script": f"{lit(units)}.trim()",
            "note": f"{name} (correspondence row W2)",
        })
        corpus.add(file, "white-space", {
            "op": "WhiteSpace, RegExp.escape",
            "in": sv([cp]),
            "out": text_out(regexp_escape(db, [cp])),
            "prov": prov(("sec-regexp.escape", "sec-encodeforregexpescape", "sec-white-space"), data),
            "script": f"RegExp.escape({lit([cp])})",
            "note": f"{name}: EncodeForRegExpEscape escapes WhiteSpace and LineTerminator (correspondence rows W1 and W4)",
        })
    # The W cells as the correspondence writes them.
    cells = [
        ("W1", "RegExp.escape", [0x20, 0x85, 0x3000]),
        ("W4", "RegExp.escape", [0x20, 0xFEFF, 0xA0]),
        ("W2", "trim", [0x85]), ("W2", "trim", [0x180E]), ("W2", "trim", [0xFEFF, 0x3000, 0x20, 0x20, 0x78]),
    ]
    for row, method, units in cells:
        if method == "trim":
            corpus.add(file, "white-space-cells", {
                "op": "WhiteSpace, String.prototype.trim",
                "in": sv(units),
                "out": text_out(trim(db, units)),
                "prov": prov(("sec-trimstring", "sec-string.prototype.trim", "sec-white-space"), data),
                "script": f"{lit(units)}.trim()",
                "note": f"correspondence row {row}",
            })
        else:
            corpus.add(file, "white-space-cells", {
                "op": "WhiteSpace, RegExp.escape",
                "in": sv(units),
                "out": text_out(regexp_escape(db, units)),
                "prov": prov(("sec-regexp.escape", "sec-encodeforregexpescape", "sec-white-space"), data),
                "script": f"RegExp.escape({lit(units)})",
                "note": f"correspondence row {row}",
            })
    for row, function, cp in (("W2", "Number", 0x85), ("W2", "parseInt", 0x180E), ("W2", "Number", 0xFEFF),
                              ("W2", "parseInt", 0x3000)):
        units = [cp, 0x31]
        value = 1.0 if is_white_space_or_line_terminator(db, cp) else math.nan
        corpus.add(file, "white-space-cells", {
            "op": f"WhiteSpace, {function} of a string",
            "in": sv(units),
            "out": number_out(value),
            "prov": prov(("sec-stringtonumber" if function == "Number" else "sec-parseint-string-radix", "sec-white-space"), data),
            "script": f"{function}({lit(units)})",
            "note": f"correspondence row {row}: StrWhiteSpaceChar is WhiteSpace and LineTerminator",
        })


def identifiers(corpus: Corpus, db: U.UCD) -> None:
    file = "identifiers"
    seen: set[int] = set()
    for first, last in db.binary["ID_Start"]:
        for cp in edges(first, last):
            if cp not in seen:
                seen.add(cp)
                corpus.add(file, "identifier-start", identifier_record(
                    db, cp, "start", f"an edge of the ID_Start range {first:04X}..{last:04X}"))
    seen.clear()
    for first, last in db.binary["ID_Continue"]:
        for cp in edges(first, last):
            if cp not in seen:
                seen.add(cp)
                corpus.add(file, "identifier-part", identifier_record(
                    db, cp, "part", f"an edge of the ID_Continue range {first:04X}..{last:04X}"))
    specials = list(SPECIAL_IDENTIFIER_POINTS)
    for name in ("Other_ID_Start", "Other_ID_Continue"):
        for first, last in db.binary[name]:
            specials += [(cp, name) for cp in range(first, last + 1)]
    letters = ("Lu", "Ll", "Lt", "Lm", "Lo", "Nl", "Mn", "Mc", "Nd", "Pc")
    specials += [(cp, "Pattern_Syntax, of a letter's, a mark's, a digit's or a connector's category")
                 for first, last in db.binary["Pattern_Syntax"] for cp in range(first, last + 1)
                 if db.general_category(cp) in letters]
    done: set[int] = set()
    for cp, why in specials:
        if cp in done:
            continue
        done.add(cp)
        for position in ("start", "part"):
            corpus.add(file, "identifier-special", identifier_record(db, cp, position, why))
    # Group names (row I3): RegExpIdentifierName takes the identifier's code points.
    sets = db.sets()
    for cp in (0x2E2F, 0x0870, 0x16D40, 0x11B60, 0x2118, 0x00B7, 0x200C, 0x24, 0x5F, 0x3164):
        for position in ("start", "part"):
            name = escape(cp) + "x" if position == "start" else "x" + escape(cp)
            key = "lexical:RegExpIdentifierStart" if position == "start" else "lexical:RegExpIdentifierPart"
            ok = U.ranges_contain(sets[key], cp)
            for flag in ("", "u"):
                corpus.add(file, "identifier-group-name", {
                    "op": f"RegExpIdentifier{'Start' if position == 'start' else 'Part'}, an escaped code point in a group name"
                          + (" under u" if flag else ""),
                    "in": f"int:{cp}",
                    "out": bool_out(True) if ok else "error: SyntaxError",
                    "prov": prov(GROUP_NAME, IDENTIFIER_DATA),
                    "script": f"/(?<{name}>.)/{flag} instanceof RegExp",
                    "note": "a group name takes the code points an identifier takes (correspondence row I3)",
                })
    white_space(corpus, db)


# --------------------------------------------------------------------------------------------------
# Intl.Segmenter.

BREAK_TESTS = (("grapheme", "GraphemeBreakTest.txt", "GraphemeBreakProperty.txt"),
               ("word", "WordBreakTest.txt", "WordBreakProperty.txt"),
               ("sentence", "SentenceBreakTest.txt", "SentenceBreakProperty.txt"))
BREAK_DATA = {"grapheme": ("GraphemeBreakProperty.txt", "emoji-data.txt", "DerivedCoreProperties.txt"),
              "word": ("WordBreakProperty.txt", "emoji-data.txt"),
              "sentence": ("SentenceBreakProperty.txt",)}


def break_cases(db: U.UCD, name: str):
    for number, line in enumerate(db.pins.read(name).split("\n"), 1):
        data = line.split("#", 1)[0].strip()
        if not data:
            continue
        cps, expected = [], []
        for token in data.split():
            if token == "\u00F7":
                expected.append(len(cps))
            elif token != "\u00D7":
                cps.append(int(token, 16))
        yield number, cps, expected


def segment_indices(units, boundaries: list[int]) -> str:
    return ",".join(str(b) for b in boundaries[:-1]) if units else ""


def segment_script(units, granularity: str, what: str, locale: str = "en") -> str:
    mapping = ("function (s) { return s.index; }).join()" if what == "index"
               else "function (s) { return s.isWordLike ? \"Y\" : \"N\"; }).join(\"\")")
    return (f"Array.from(new Intl.Segmenter(\"{locale}\", {{ granularity: \"{granularity}\" }}).segment({lit(units)}), "
            + mapping)


def segment_record(units, granularity: str, boundaries: list[int], files: tuple[str, ...], note: str,
                   locale: str = "en") -> dict[str, str]:
    expected = str_out(segment_indices(units, boundaries))
    return {
        "op": f"Intl.Segmenter {granularity} segments, their indices" + ("" if locale == "en" else ", a locale"),
        "in": f"{sv(units)} {string_value(granularity)}" + ("" if locale == "en" else f" {string_value(locale)}"),
        "out": expected,
        "class": "impl-defined",
        "per-engine.broiler-js": expected,
        "per-engine.broiler-vm": expected,
        "prov": " ".join([GENERATOR, ucd(*files)]),
        "script": segment_script(units, granularity, "index", locale),
        "note": note,
    }


def word_like_record(db: U.UCD, units, files: tuple[str, ...], note: str) -> dict[str, str]:
    expected = str_out(db.word_status(units))
    return {
        "op": "Intl.Segmenter word segments, isWordLike",
        "in": f"{sv(units)} str:\"word\"",
        "out": expected,
        "class": "impl-defined",
        "per-engine.broiler-js": expected,
        "per-engine.broiler-vm": expected,
        "prov": " ".join([GENERATOR, ucd(*files)]),
        "script": segment_script(units, "word", "word-like"),
        "note": note,
    }


G_CELLS = [
    ("G1", "sentence", "One. Two? Three! Mr. Smith"), ("G1", "sentence", "Hello world. \u4E16\u754C\u3002\u7D42\u308F\u308A"),
    ("G2", "word", "can't"), ("G2", "word", "a_b c.d 3,000"), ("G2", "word", "\u65E5\u672C\u8A9E\u306E\u30C6\u30AD\u30B9\u30C8"),
    ("G3", "grapheme", "e\u0301\U0001F44D\U0001F3FD\U0001F1E9\U0001F1EA"), ("G3", "grapheme", "\u1112\u1161\u11AB\uAD6D\uC5B4"),
    ("G3", "grapheme", "\r\n"), ("G3", "grapheme", "\uD800a"), ("G3", "word", "\uD800a"), ("G3", "sentence", "\uD800a"),
    ("G4", "word", "_"), ("G4", "word", "a_b"), ("G4", "word", "3,000 \u3042 \u30C6 \u4E16 ! a"),
]
# Row G6: the locale is resolved without effect, so a locale with a tailoring or a dictionary elsewhere segments
# as the default does.
G6_CELLS = [
    ("ja", "word", "\u65E5\u672C\u8A9E\u306E\u30C6\u30AD\u30B9\u30C8"), ("th", "word", "\u0E20\u0E32\u0E29\u0E32\u0E44\u0E17\u0E22"),
    ("de", "sentence", "Herr Dr. M\u00FCller kam. Er ging."), ("tr", "word", "a_b c.d 3,000"),
]


def segmenter(corpus: Corpus, db: U.UCD) -> None:
    file = "segmenter"
    functions = {"grapheme": db.grapheme_breaks, "word": db.word_breaks, "sentence": db.sentence_breaks}
    for granularity, test_file, _ in BREAK_TESTS:
        files = (test_file,) + BREAK_DATA[granularity]
        for number, cps, expected in break_cases(db, test_file):
            if functions[granularity](cps) != expected:
                raise Refused(f"{test_file} line {number}: the reference disagrees")
            units = U.code_units(cps)
            boundaries = [len(U.code_units(cps[:i])) for i in expected]
            corpus.add(file, f"segmenter-{granularity}", segment_record(
                units, granularity, boundaries, files, f"{test_file} line {number}. {SEGMENTER_PLAN}"))
            if granularity == "word":
                corpus.add(file, "segmenter-word-like", word_like_record(
                    db, units, ("WordBreakProperty.txt", "PropList.txt", "Scripts.txt"),
                    f"{test_file} line {number}'s segments. {WORD_LIKE_PLAN}"))
    for row, granularity, value in G_CELLS:
        units = U.code_units([ord(c) for c in value])
        boundaries = {"grapheme": db.grapheme_boundaries, "word": db.word_boundaries,
                      "sentence": db.sentence_boundaries}[granularity](units)
        corpus.add(file, "segmenter-cells", segment_record(
            units, granularity, boundaries, BREAK_DATA[granularity], f"correspondence row {row}. {SEGMENTER_PLAN}"))
        if granularity == "word":
            corpus.add(file, "segmenter-cells", word_like_record(
                db, units, ("WordBreakProperty.txt", "PropList.txt", "Scripts.txt"),
                f"correspondence row {'G4' if row == 'G4' else row + ' and G4'}. {WORD_LIKE_PLAN}"))
    for locale, granularity, value in G6_CELLS:
        units = U.code_units([ord(c) for c in value])
        boundaries = {"word": db.word_boundaries, "sentence": db.sentence_boundaries}[granularity](units)
        corpus.add(file, "segmenter-cells", segment_record(
            units, granularity, boundaries, BREAK_DATA[granularity],
            f"correspondence row G6: the locale {locale} has no effect. {SEGMENTER_PLAN}", locale))


# --------------------------------------------------------------------------------------------------


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--ucd", required=True, type=Path,
                        help="the directory of the pinned Unicode data (normally Broiler.Unicode's data/unicode/pins/)")
    parser.add_argument("--out", required=True, type=Path)
    arguments = parser.parse_args(argv)
    started = time.perf_counter()
    try:
        pins = U.Pins(U.PIN_FILE, arguments.ucd)
        if pins.version != UCD_VERSION:
            raise U.PinError(f"the pin names Unicode {pins.version}, not {UCD_VERSION}")
        for name in sorted(pins.entries):
            pins.read_bytes(name)  # every pinned file, checked before any is used
        db = U.UCD(pins)
        corpus = Corpus()
        normalization(corpus, db)
        casing(corpus, db)
        properties(corpus, db)
        identifiers(corpus, db)
        segmenter(corpus, db)
    except (U.PinError, Refused) as error:
        print(f"refused: {error}", file=sys.stderr)
        return 2
    corpus.classify(DIVERGENCES)
    corpus.write(arguments.out)
    for name, records in sorted(corpus.files.items()):
        classes = defaultdict(int)
        for record in records:
            classes[record["class"]] += 1
        print(f"{name}.vectors: {len(records)} vectors ({', '.join(f'{n} {c}' for c, n in sorted(classes.items()))})")
    total = sum(len(records) for records in corpus.files.values())
    print(f"{total} vectors in {len(corpus.files)} files, {time.perf_counter() - started:.1f} s")
    return 0


if __name__ == "__main__":
    sys.exit(main())
