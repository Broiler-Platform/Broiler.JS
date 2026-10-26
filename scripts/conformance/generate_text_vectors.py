#!/usr/bin/env python3
"""Generate the text area of the shared conformance corpus.

    python3 scripts/conformance/generate_text_vectors.py --out conformance/vectors/text

Every expected value comes from `textref.py`, the corpus's independent reference computation of the
text algorithms, written from the specification's text; no expected value is taken from either engine.
The populations are milestone SJL-5.2's: TrimString over the white space and the characters an engine
may mistake for it; IsStringWellFormedUnicode and toWellFormed over lone and paired surrogates, with
CodePointAt and AdvanceStringIndex; StringPad at the length policy's edge and in step order, and the
relative-index normalisation through the built-ins that apply it; replacement templates with every
`$` form against string and regular-expression patterns; the URI codecs over every escape, every
malformed UTF-8 sequence and every surrogate case, with escape and unescape; the base64 and hex codecs
in both alphabets and every last-chunk mode, partial writes into short destinations and the option
readers; and JSON's lexical grammar, its quoting, its gap and its line ending.

The output is deterministic: the same inputs give the same bytes. Vector identifiers are stable and
are never reused; a later revision appends.
"""

from __future__ import annotations

import argparse
import itertools
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import numref as R  # noqa: E402
import textref as T  # noqa: E402
from generate_number_vectors import f64, js_string_literal, splitmix64, string_value  # noqa: E402

# The corpus revision this generator writes, and the revision its vectors were added in. Revision 11
# widened the schema's clause ids, which four of this area's citations need, and changed no vector;
# revision 12 classified both engines' first runs on the area (text-divergences.txt), and revision 13
# retired every field the two engines' cut-overs corrected. Revisions 14 to 17 added, classified and
# retired the Date area's divergences (generate_date_vectors.py) and changed nothing here, and revision 18
# added the Unicode area (generate_unicode_vectors.py) and changed nothing here either, nor did revision 19,
# which classified its divergences, nor revision 20, which retired them, nor revision 21, which added the
# time-zone area (generate_time_zone_vectors.py), nor revision 22, which classified its divergences, nor revision
# 23, which retired them, nor revision 24, which added the Temporal area (generate_temporal_vectors.py) and widened
# the schema's provenance, nor revision 25, which classified its differences, nor revision 26, which retired them.
CORPUS_REVISION = 27
FIRST_REVISION = 10
GENERATOR = "ref:textref@1"
SPEC = "spec:es2026@0248456c"
ENGINES = ("broiler-js", "broiler-vm")
DIVERGENCES = Path(__file__).resolve().parent / "text-divergences.txt"

# Broiler.VM's string ceiling (JsRealm.String's StringLengthCeiling, 2^24 code units), which JSD-0070
# decision 4 keeps as this family's policy: past it, padStart and padEnd refuse with RangeError.
VM_STRING_CEILING = 1 << 24
VM_CEILING_POLICY = ("Broiler.VM's string ceiling (StringLengthCeiling, 2^24 code units), its policy under "
                     "JSD-0070 decision 4: a string past it is refused with RangeError")


def clause(*ids: str) -> str:
    return " ".join(f"{SPEC}#{i}" for i in ids)


def prov(*ids: str) -> str:
    return f"{clause(*ids)} {GENERATOR}"


def text_out(value: str) -> str:
    return f"value: {string_value(value)}"


def number_out(value: int | float) -> str:
    return f"value: {f64(R.from_fraction(R.Fraction(value)) if value == value else R.CANONICAL_NAN)}"


def int_out(value: int) -> str:
    bits = R.from_fraction(R.Fraction(abs(value)))
    if value < 0:
        bits |= R.SIGN_BIT
    return f"value: {f64(bits)}"


def bool_out(value: bool) -> str:
    return f"value: bool:{'true' if value else 'false'}"


def texts_out(values: list[str]) -> str:
    """Every admitted text, as the alternatives of section 4 of the schema."""
    return " | ".join(text_out(v) for v in values)


def error_out(name: str) -> str:
    return f"error: {name}"


def lit(s: str) -> str:
    return js_string_literal(s)


# --------------------------------------------------------------------------------------------------
# Records.


class Corpus:
    def __init__(self) -> None:
        self.files: dict[str, list[dict[str, str]]] = {}
        self.ids: set[str] = set()
        self.counters: dict[str, int] = {}

    def add(self, file: str, group: str, record: dict[str, str], since: int = FIRST_REVISION) -> None:
        self.counters[group] = self.counters.get(group, 0) + 1
        vector_id = f"text/{group}/{self.counters[group]:04d}"
        assert vector_id not in self.ids
        self.ids.add(vector_id)
        full = {"id": vector_id, "area": "text"}
        full.update(record)
        full.setdefault("class", "normative")
        full["since"] = str(since)
        assert "\n" not in full["script"], full["script"]
        assert all(ord(ch) < 0x80 for ch in full["script"]), full["script"]
        self.files.setdefault(file, []).append(full)

    def classify(self, path: Path) -> None:
        """Applies text-divergences.txt, as generate_numerics_vectors.py applies its own file."""
        if not path.exists():
            return
        reasons: dict[str, str] = {}
        by_id = {record["id"]: record for records in self.files.values() for record in records}
        for line_number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
            if not line or line.startswith("#"):
                continue
            if line.startswith("reason "):
                key, _, text = line[len("reason "):].partition(": ")
                if not text or key in reasons:
                    raise SystemExit(f"{path.name}:{line_number}: a malformed or repeated reason")
                reasons[key] = text
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
                "# Generated by scripts/conformance/generate_text_vectors.py; do not edit by hand.",
                "# Expected values: scripts/conformance/textref.py, written from the specification's text.",
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
# Whitespace and UTF-16.

TRIM_CANDIDATES = sorted(T.STR_WHITE_SPACE | {"\u0085", "\u180E", "\u200B", "\u001C", "\u001F", "\uFFFE",
                                              "\u00AD", "\u2060", "a"})
SURROGATE_ALPHABET = ["a", "\uD800", "\uDBFF", "\uDC00", "\uDFFF", "\uD83D\uDE00"]
TRIM_METHODS = (("trim", "start+end"), ("trimStart", "start"), ("trimEnd", "end"))


def whitespace_and_utf16(corpus: Corpus) -> None:
    file = "utf16"
    for ch in TRIM_CANDIDATES:
        for run in (1, 2):
            s = ch * run + "x" + ch * run
            for method, where in TRIM_METHODS:
                corpus.add(file, "trim", {
                    "op": f"TrimString {where}",
                    "in": string_value(s),
                    "out": text_out(T.trim_string(s, where)),
                    "prov": prov("sec-trimstring", "sec-string.prototype." + method.lower()),
                    "script": f"{lit(s)}.{method}()",
                })
    all_white = "".join(sorted(T.STR_WHITE_SPACE))
    for method, where in TRIM_METHODS:
        s = all_white + "a b" + all_white
        corpus.add(file, "trim", {
            "op": f"TrimString {where}",
            "in": string_value(s),
            "out": text_out(T.trim_string(s, where)),
            "prov": prov("sec-trimstring"),
            "script": f"{lit(s)}.{method}()",
        })

    strings = [""]
    for length in (1, 2, 3):
        strings.extend("".join(p) for p in itertools.product(SURROGATE_ALPHABET, repeat=length))
    for s in strings:
        corpus.add(file, "is-well-formed", {
            "op": "IsStringWellFormedUnicode",
            "in": string_value(s),
            "out": bool_out(T.is_string_well_formed_unicode(s)),
            "prov": prov("sec-isstringwellformedunicode", "sec-string.prototype.iswellformed"),
            "script": f"{lit(s)}.isWellFormed()",
        })
        corpus.add(file, "to-well-formed", {
            "op": "toWellFormed",
            "in": string_value(s),
            "out": text_out(T.to_well_formed(s)),
            "prov": prov("sec-string.prototype.towellformed"),
            "script": f"{lit(s)}.toWellFormed()",
        })
    for s in strings:
        if not s:
            continue
        for position in range(len(s)):
            record = T.code_point_at(s, position)
            corpus.add(file, "code-point-at", {
                "op": "CodePointAt code point",
                "in": f"{string_value(s)} int:{position}",
                "out": int_out(record.code_point),
                "prov": prov("sec-codepointat", "sec-string.prototype.codepointat"),
                "script": f"{lit(s)}.codePointAt({position})",
            })
        pieces = []
        position = 0
        while position < len(s):
            record = T.code_point_at(s, position)
            pieces.append(s[position:position + record.code_unit_count])
            position += record.code_unit_count
        corpus.add(file, "string-iterator", {
            "op": "StringToCodePoints joined by |",
            "in": string_value(s),
            "out": text_out("|".join(pieces)),
            "prov": prov("sec-%stringiteratorprototype%.next", "sec-codepointat"),
            "script": f"[...{lit(s)}].join(\"|\")",
        })
        for unicode in (False, True):
            boundaries = [0]
            index = 0
            while index < len(s):
                index = T.advance_string_index(s, index, unicode)
                boundaries.append(index)
            marked = "|" + "|".join(s[a:b] for a, b in zip(boundaries, boundaries[1:])) + "|"
            corpus.add(file, "advance-string-index", {
                "op": "AdvanceStringIndex walk" + (" unicode" if unicode else ""),
                "in": f"{string_value(s)} bool:{'true' if unicode else 'false'}",
                "out": text_out(marked),
                "prov": prov("sec-advancestringindex", "sec-regexp.prototype-%symbol.replace%"),
                "script": f"{lit(s)}.replace(/(?:)/g{'u' if unicode else ''}, \"|\")",
            })


# --------------------------------------------------------------------------------------------------
# Padding and indices.

FILLERS = ["", " ", "ab", "\uD800", "\uDC00\uD800", "0123456789"]


def padding_and_indices(corpus: Corpus) -> None:
    file = "pad-index"
    for s in ("", "a", "abc", "\uD83D\uDE00"):
        for target in (0, 1, 2, 3, 4, 5, 7, 10, 23):
            for filler in FILLERS + [None]:
                for method, placement in (("padStart", "start"), ("padEnd", "end")):
                    fill = " " if filler is None else filler
                    args = f"{target}" + ("" if filler is None else f", {lit(filler)}")
                    corpus.add(file, "pad", {
                        "op": f"StringPad {placement}",
                        "in": f"{string_value(s)} int:{target} {string_value(fill)}",
                        "out": text_out(T.string_pad(s, target, fill, placement)),
                        "prov": prov("sec-stringpad", "sec-stringpaddingbuiltinsimpl"),
                        "script": f"{lit(s)}.{method}({args})",
                    })
    # The length policy's edge: Broiler.VM refuses past its ceiling (impl-defined); at the ceiling
    # every engine builds the string.
    for method in ("padStart", "padEnd"):
        corpus.add(file, "pad-ceiling", {
            "op": f"StringPad length at {VM_STRING_CEILING}",
            "in": f"int:{VM_STRING_CEILING}",
            "out": int_out(VM_STRING_CEILING),
            "prov": prov("sec-stringpad"),
            "script": f"\"x\".{method}({VM_STRING_CEILING}).length",
        })
        corpus.add(file, "pad-ceiling", {
            "op": f"StringPad length at {VM_STRING_CEILING + 1}",
            "in": f"int:{VM_STRING_CEILING + 1}",
            "out": int_out(VM_STRING_CEILING + 1),
            "class": "impl-defined",
            "per-engine.broiler-vm": error_out("RangeError"),
            "prov": prov("sec-stringpad"),
            "note": f"A string past an engine's own limit may be refused. {VM_CEILING_POLICY}.",
            "script": f"\"x\".{method}({VM_STRING_CEILING + 1}).length",
        })
        # Step order: the filler is converted before StringPad looks at the length (P1), and an empty
        # filler returns the string whatever the length (P2).
        corpus.add(file, "pad-order", {
            "op": "StringPaddingBuiltinsImpl converts the filler first",
            "in": f"int:{VM_STRING_CEILING + 1}",
            "out": int_out(1),
            "prov": prov("sec-stringpaddingbuiltinsimpl"),
            "script": (f"(function () {{ var c = 0; try {{ \"x\".{method}({VM_STRING_CEILING + 1}, "
                       "{ toString: function () { c++; return \"\"; } }); } catch (e) { } return c; })()"),
        })
        corpus.add(file, "pad-order", {
            "op": "StringPad with an empty filler",
            "in": f"int:{VM_STRING_CEILING + 1} str:\"\"",
            "out": text_out("x"),
            "prov": prov("sec-stringpad"),
            "script": f"\"x\".{method}({VM_STRING_CEILING + 1}, \"\")",
        })

    word = "abcdefg"
    relatives = ["-Infinity", "-(2**53)"] + [str(r) for r in range(-9, 10)] + ["2**53", "Infinity"]
    values = {"-Infinity": float("-inf"), "-(2**53)": -(2 ** 53), "2**53": 2 ** 53, "Infinity": float("inf")}
    def typed(r: str) -> str:
        """The relative index as a typed value: int: for an integer the notation holds, else its bits."""
        value = values.get(r, int(r) if r.lstrip("-").isdigit() else None)
        if value in (float("inf"), float("-inf")):
            return f64(0x7FF0000000000000 | (R.SIGN_BIT if value < 0 else 0))
        if abs(value) >= 2 ** 53:
            bits = R.from_fraction(R.Fraction(abs(value)))
            return f64(bits | (R.SIGN_BIT if value < 0 else 0))
        return f"int:{value}"

    for r in relatives:
        rel = values.get(r, int(r) if r.lstrip("-").isdigit() else None)
        start = T.relative_index(rel, len(word))
        corpus.add(file, "relative-index", {
            "op": "relative index",
            "in": f"int:{len(word)} {typed(r)}",
            "out": text_out(word[start:]),
            "prov": prov("sec-string.prototype.slice"),
            "script": f"{lit(word)}.slice({r})",
        })
        index = rel if rel not in (float("inf"), float("-inf")) else None
        at_value = None
        if index is not None and -len(word) <= index < len(word):
            at_value = word[index]
        corpus.add(file, "relative-index", {
            "op": "relative index for at",
            "in": f"int:{len(word)} {typed(r)}",
            "out": "value: undefined" if at_value is None else text_out(at_value),
            "prov": prov("sec-string.prototype.at"),
            "script": f"{lit(word)}.at({r})",
        })
        for kind in ("[1, 2, 3, 4, 5, 6, 7]", "new Int8Array([1, 2, 3, 4, 5, 6, 7])"):
            corpus.add(file, "relative-index", {
                "op": "relative index for slice of a list",
                "in": f"int:7 {typed(r)}",
                "out": text_out(",".join(str(v) for v in [1, 2, 3, 4, 5, 6, 7][start:])),
                "prov": prov("sec-array.prototype.slice" if kind.startswith("[") else "sec-%typedarray%.prototype.slice"),
                "script": f"Array.prototype.join.call({kind}.slice({r}), \",\")",
            })
    # %TypedArray%.prototype.lastIndexOf reads fromIndex whenever a second argument is passed (P5).
    for extra in ("", ", \"extra\""):
        corpus.add(file, "last-index-of", {
            "op": "lastIndexOf with fromIndex",
            "in": "int:1",
            "out": int_out(0),
            "prov": clause("sec-%typedarray%.prototype.lastindexof") + " " + GENERATOR,
            "script": f"new Int8Array([1, 2, 1]).lastIndexOf(1, 1{extra})",
        })


# --------------------------------------------------------------------------------------------------
# Substitution templates.

TEMPLATE_ALPHABET = ["$", "&", "`", "'", "0", "1", "9", "<", ">", "a"]


def substitution(corpus: Corpus) -> None:
    file = "substitution"
    templates = [""]
    for length in (1, 2, 3):
        templates.extend("".join(p) for p in itertools.product(TEMPLATE_ALPHABET, repeat=length))
    templates.extend(["$<a>", "$<b>", "$<c>", "$<>", "$<a", "$10", "$11", "$01", "$00", "$99", "$1$2$3"])
    # The optional group does not take part in the match: after "b" comes "d", so (c)? is undefined.
    subject = "xbdy"
    configurations = [
        ("string", "\"b\"", 1, [], None),
        ("regexp", "/(b)(c)?/", 1, ["b", None], None),
        ("named", "/(?<a>b)(?<b>c)?/", 1, ["b", None], {"a": "b", "b": None}),
    ]
    for template in templates:
        for kind, pattern, position, captures, named in configurations:
            matched = "b"
            expected = subject[:position] + T.get_substitution(matched, subject, position, captures, named, template) \
                + subject[position + len(matched):]
            corpus.add(file, "get-substitution", {
                "op": f"GetSubstitution {kind}",
                "in": f"{string_value(subject)} {string_value(template)}",
                "out": text_out(expected),
                "prov": prov("sec-getsubstitution"),
                "script": f"{lit(subject)}.replace({pattern}, {lit(template)})",
            })
    # The string path of replaceAll converts the replacement before it searches (S1).
    corpus.add(file, "replace-order", {
        "op": "replaceAll converts the replacement first",
        "in": "str:\"abc\" str:\"x\"",
        "out": int_out(1),
        "prov": prov("sec-string.prototype.replaceall"),
        "script": ("(function () { var c = 0; \"abc\".replaceAll(\"x\", "
                   "{ toString: function () { c++; return \"\"; } }); return c; })()"),
    })
    # A named group's value is converted with ToString, so Symbol.toPrimitive runs (S2).
    corpus.add(file, "named-to-string", {
        "op": "GetSubstitution converts a named capture with ToString",
        "in": "str:\"ab\" str:\"$<x>\"",
        "out": text_out("Pb"),
        "prov": prov("sec-getsubstitution", "sec-regexp.prototype-%symbol.replace%"),
        "script": ("(function () { var re = /a/; re.exec = function () { var r = [\"a\"]; r.index = 0; "
                   "r.groups = { x: { [Symbol.toPrimitive]: function () { return \"P\"; }, "
                   "toString: function () { return \"T\"; } } }; return r; }; return \"ab\".replace(re, \"$<x>\"); })()"),
    })
    # A RegExp whose Symbol.replace is absent is searched for as its text (S3).
    for absent in ("undefined", "null"):
        corpus.add(file, "absent-replace", {
            "op": "replace with a search value whose Symbol.replace is " + absent,
            "in": "str:\"a/a/g\" str:\"x\"",
            "out": text_out("ax"),
            "prov": prov("sec-string.prototype.replace"),
            "script": (f"(function () {{ var re = /a/g; re[Symbol.replace] = {absent}; "
                       "return \"a/a/g\".replace(re, \"x\"); })()"),
        })


# --------------------------------------------------------------------------------------------------
# URI.


def uri(corpus: Corpus) -> None:
    file = "uri"
    points = list(range(0x00, 0x80)) + [0x80, 0xA0, 0xE9, 0xFF, 0x100, 0x7FF, 0x800, 0x20AC, 0xD7FF, 0xE000,
                                        0xFFFD, 0xFFFF, 0x10000, 0x1F600, 0x10FFFF]
    samples = [T.units(chr(p)) for p in points] + ["\uD800", "\uDBFF", "\uDC00", "\uDFFF", "\uD800a",
                                                   "a\uDC00", "\uDC00\uD800", "\uD800\uD800\uDC00"]
    for s in samples:
        for name, extra in (("encodeURIComponent", ""), ("encodeURI", T.URI_RESERVED + "#")):
            result = T.encode(s, extra)
            corpus.add(file, "encode", {
                "op": f"Encode {name}",
                "in": string_value(s),
                "out": error_out(result.name) if isinstance(result, T.Error) else text_out(result),
                "prov": prov("sec-encode", "sec-encodeuricomponent-uricomponent" if name == "encodeURIComponent"
                             else "sec-encodeuri-uri"),
                "script": f"{name}({lit(s)})",
            })
    escapes = []
    for b in range(256):
        escapes.append("%%%02X" % b)
        if any(ch in "abcdef" for ch in "%02x" % b):
            escapes.append("%%%02x" % b)
    malformed = ["%", "%4", "%G0", "%4G", "%C3", "%C3%", "%C3%A", "%C3A9", "%C3%29", "%C0%80", "%C1%BF",
                 "%E0%80%80", "%E0%9F%BF", "%ED%A0%80", "%ED%BF%BF", "%EF%BF%BF", "%F0%80%80%80",
                 "%F4%8F%BF%BF", "%F4%90%80%80", "%F8%88%80%80%80", "%FF", "%80", "%BF", "%E2%82", "%E2%82%AC",
                 "%F0%9F%98%80", "%F0%9F%98", "a%2", "%%41", "%25%41", "%3B%2F%3F%3A%40%26%3D%2B%24%2C%23"]
    for s in escapes + malformed:
        for name, preserve in (("decodeURIComponent", ""), ("decodeURI", T.PRESERVE_URI)):
            result = T.decode(s, preserve)
            corpus.add(file, "decode", {
                "op": f"Decode {name}",
                "in": string_value(s),
                "out": error_out(result.name) if isinstance(result, T.Error) else text_out(result),
                "prov": prov("sec-decode", "sec-decodeuricomponent-encodeduricomponent" if name == "decodeURIComponent"
                             else "sec-decodeuri-encodeduri"),
                "script": f"{name}({lit(s)})",
            })
    for unit in list(range(0, 0x100)) + [0x100, 0x7FF, 0x20AC, 0xD800, 0xDC00, 0xFFFF]:
        s = chr(unit)
        corpus.add(file, "escape", {
            "op": "escape",
            "in": string_value(s),
            "out": text_out(T.escape(s)),
            "prov": prov("sec-escape-string"),
            "script": f"escape({lit(s)})",
        })
    for s in ["%41", "%4", "%", "%zz", "%u0041", "%u00e9", "%uD800", "%u12", "%u123", "%u123g", "%U0041",
              "%u0041x", "%%41", "%x41", "a%41b%u0042c", "%uDC00%uD800", "%+1", "%u+123", "%u-123"]:
        corpus.add(file, "unescape", {
            "op": "unescape",
            "in": string_value(s),
            "out": text_out(T.unescape(s)),
            "prov": prov("sec-unescape-string"),
            "script": f"unescape({lit(s)})",
        })


# --------------------------------------------------------------------------------------------------
# Base64 and hex.

BASE64_CHARACTERS = ["A", "Z", "a", "z", "0", "9", "+", "/", "-", "_", "=", " ", "\n", "@"]
MODES = ("loose", "strict", "stop-before-partial")


def bytes_text(values: list[int]) -> str:
    return ",".join(str(v) for v in values)


def base64_and_hex(corpus: Corpus) -> None:
    file = "base64-hex"
    # Every input of up to three characters over ten of them, and longer inputs drawn from all fourteen.
    inputs = [""]
    for length in (1, 2, 3):
        inputs.extend("".join(p) for p in itertools.product(BASE64_CHARACTERS[:8] + ["=", " "], repeat=length))
    draws = splitmix64(0x534A4C352D303033)
    for _ in range(200):
        length = 4 + next(draws) % 5
        inputs.append("".join(BASE64_CHARACTERS[next(draws) % len(BASE64_CHARACTERS)] for _ in range(length)))
    inputs.extend(["aGVsbG8=", "aGVsbG8", "aGVsbA==", "aGVsbA=", "aGVsbA", "aGVs bG8=", "aGVs\nbG8=", "aGVsbB==",
                   "aGVsbG9=", "YQ==", "YR==", "YQ", "Y", "YQ=", "YQ===", "=", "==", "YQ== ", "YQ==a", "_-8=", "+/8="])
    for s in inputs:
        for alphabet in ("base64", "base64url"):
            for mode in MODES:
                result = T.from_base64(s, alphabet, mode)
                corpus.add(file, "from-base64", {
                    "op": f"FromBase64 {alphabet} {mode}",
                    "in": string_value(s),
                    "out": error_out(result.error) if result.error else text_out(bytes_text(result.bytes)),
                    "prov": prov("sec-frombase64", "sec-uint8array.frombase64"),
                    "script": (f"Array.prototype.join.call(Uint8Array.fromBase64({lit(s)}, "
                               f"{{ alphabet: \"{alphabet}\", lastChunkHandling: \"{mode}\" }}), \",\")"),
                })
    for s in ["aGVsbG8=", "aGVsbA==", "aGVsbG8gd29ybGQ=", "YQ==", "YWI=", "YWJj", "YWJjZA==", "aGVsbG8@"]:
        for length in (0, 1, 2, 3, 4, 5, 8):
            for mode in MODES:
                result = T.from_base64(s, "base64", mode, length)
                written = result.bytes + [0] * (length - len(result.bytes))
                expected = (f"{result.error}/{bytes_text(written)}" if result.error
                            else f"{result.read}/{len(result.bytes)}/{bytes_text(written)}")
                corpus.add(file, "set-from-base64", {
                    "op": f"FromBase64 base64 {mode} into a destination",
                    "in": f"{string_value(s)} int:{length}",
                    "out": text_out(expected),
                    "prov": prov("sec-frombase64", "sec-uint8array.prototype.setfrombase64"),
                    "script": (f"(function () {{ var t = new Uint8Array({length}); try {{ var r = t.setFromBase64({lit(s)}, "
                               f"{{ lastChunkHandling: \"{mode}\" }}); return r.read + \"/\" + r.written + \"/\" + "
                               "Array.prototype.join.call(t, \",\"); } catch (e) { return e.name + \"/\" + "
                               "Array.prototype.join.call(t, \",\"); } })()"),
                })
    hex_inputs = [""]
    for length in (1, 2, 3, 4):
        hex_inputs.extend("".join(p) for p in itertools.product(["0", "9", "a", "F", "g", " "], repeat=length))
    hex_inputs.extend(["cafe", "CAFE", "aaZZ", "aaZZbb", "0", "000", "ff00ff", "x0", "0x"])
    for s in hex_inputs:
        result = T.from_hex(s)
        corpus.add(file, "from-hex", {
            "op": "FromHex",
            "in": string_value(s),
            "out": error_out(result.error) if result.error else text_out(bytes_text(result.bytes)),
            "prov": prov("sec-fromhex", "sec-uint8array.fromhex"),
            "script": f"Array.prototype.join.call(Uint8Array.fromHex({lit(s)}), \",\")",
        })
    for s in ["aaZZ", "aabbcc", "aabbZZ", "aab", "zz", "", "00ff00ff"]:
        for length in (0, 1, 2, 3, 4):
            result = T.from_hex(s, length)
            written = result.bytes + [0] * (length - len(result.bytes))
            expected = (f"{result.error}/{bytes_text(written)}" if result.error
                        else f"{result.read}/{len(result.bytes)}/{bytes_text(written)}")
            corpus.add(file, "set-from-hex", {
                "op": "FromHex into a destination",
                "in": f"{string_value(s)} int:{length}",
                "out": text_out(expected),
                "prov": prov("sec-fromhex", "sec-uint8array.prototype.setfromhex"),
                "script": (f"(function () {{ var t = new Uint8Array({length}); try {{ var r = t.setFromHex({lit(s)}); "
                           "return r.read + \"/\" + r.written + \"/\" + Array.prototype.join.call(t, \",\"); } "
                           "catch (e) { return e.name + \"/\" + Array.prototype.join.call(t, \",\"); } })()"),
            })
    arrays = [[], [0], [255], [0, 1], [251, 255], [104, 101, 108, 108, 111], [255, 254, 253], [62, 63, 64]]
    draws = splitmix64(0x534A4C352D303032)
    for length in range(1, 40):
        arrays.append([next(draws) & 0xFF for _ in range(length)])
    for values in arrays:
        data = bytes(values)
        literal = "[" + ", ".join(str(v) for v in values) + "]"
        for alphabet in ("base64", "base64url"):
            for omit in (False, True):
                corpus.add(file, "to-base64", {
                    "op": f"ToBase64 {alphabet}" + (" omitPadding" if omit else ""),
                    "in": f"str:\"{bytes_text(values)}\"",
                    "out": text_out(T.to_base64(data, alphabet, omit)),
                    "prov": prov("sec-uint8array.prototype.tobase64"),
                    "script": (f"new Uint8Array({literal}).toBase64({{ alphabet: \"{alphabet}\", "
                               f"omitPadding: {'true' if omit else 'false'} }})"),
                })
        corpus.add(file, "to-hex", {
            "op": "ToHex",
            "in": f"str:\"{bytes_text(values)}\"",
            "out": text_out(T.to_hex(data)),
            "prov": prov("sec-uint8array.prototype.tohex"),
            "script": f"new Uint8Array({literal}).toHex()",
        })
    # The option readers and the receiver (B2 to B4).
    option_cases = [
        ("Uint8Array.fromBase64(\"\", 1)", "GetOptionsObject refuses a non-object", error_out("TypeError")),
        ("Uint8Array.fromBase64(\"\", null)", "GetOptionsObject refuses null", error_out("TypeError")),
        ("Uint8Array.fromBase64(\"\", { alphabet: null })", "an alphabet that is not a name", error_out("TypeError")),
        ("Uint8Array.fromBase64(\"\", { lastChunkHandling: null })", "a last-chunk handling that is not a name",
         error_out("TypeError")),
        ("Uint8Array.fromBase64(\"\", { alphabet: new String(\"base64\") })", "an alphabet that is not a String value",
         error_out("TypeError")),
        ("new Uint8Array(1).toBase64(1)", "toBase64's GetOptionsObject refuses a non-object", error_out("TypeError")),
        ("Uint8Array.fromBase64(1)", "fromBase64 refuses a non-String", error_out("TypeError")),
        ("Uint8Array.fromHex(1)", "fromHex refuses a non-String", error_out("TypeError")),
        ("(function () { var b = new ArrayBuffer(4); var t = new Uint8Array(b); b.transfer(); return t.toBase64(); })()",
         "toBase64 refuses a detached array", error_out("TypeError")),
        ("(function () { var b = new ArrayBuffer(4); var t = new Uint8Array(b); b.transfer(); return t.toHex(); })()",
         "toHex refuses a detached array", error_out("TypeError")),
        ("(function () { var b = new ArrayBuffer(4); var t = new Uint8Array(b); b.transfer(); var r = t.setFromBase64(\"\"); return r.read; })()",
         "setFromBase64 refuses a detached array", error_out("TypeError")),
        ("(function () { var b = new ArrayBuffer(4); var t = new Uint8Array(b); b.transfer(); var r = t.setFromHex(\"\"); return r.read; })()",
         "setFromHex refuses a detached array", error_out("TypeError")),
        ("Uint8Array.prototype.toHex.call(new Int8Array(1))", "ValidateUint8Array refuses another typed array",
         error_out("TypeError")),
    ]
    for script, op, out in option_cases:
        corpus.add(file, "options", {
            "op": op,
            "in": "undefined",
            "out": out,
            "prov": prov("sec-uint8array.frombase64", "sec-uint8array.prototype.tobase64"),
            "script": script,
        })


# --------------------------------------------------------------------------------------------------
# JSON.

JSON_ALPHABET = ["{", "}", "[", "]", ",", ":", "\"", "\\", "0", "1", "-", ".", "e", "t", "n", "u", "a", " "]


def json_parse_out(text: str) -> str:
    try:
        value = T.json_parse(text)
    except T.JsonSyntaxError:
        return error_out("SyntaxError")
    return texts_out(T.serialize(value, ""))


def single_length(texts: list[str]) -> int:
    assert len({len(t) for t in texts}) == 1, texts
    return len(texts[0])


def json_area(corpus: Corpus) -> None:
    file = "json"
    # Every text of up to two characters over the alphabet, and longer texts drawn from it.
    texts = [""]
    for length in (1, 2):
        texts.extend("".join(p) for p in itertools.product(JSON_ALPHABET, repeat=length))
    draws = splitmix64(0x534A4C352D303034)
    for _ in range(2000):
        length = 3 + next(draws) % 6
        texts.append("".join(JSON_ALPHABET[next(draws) % len(JSON_ALPHABET)] for _ in range(length)))
    texts.extend(["true", "false", "null", "tru", "nul", "-0", "0.0", "1e5", "1E+5", "1e-5", "-1.5e-10", "01", "1.",
                  ".1", "+1", "1e", "1e+", "0x10", "Infinity", "NaN", "\"\\ud800\"", "\"\\uDC00\"", "\"\\ud83d\\ude00\"",
                  "\"\uD800\"", "\"\uDC00x\"", "\"\\u00e9\\u20AC\"", "\"\\x41\"", "\"\\u12\"", "\"a\tb\"", "\"\\/\"",
                  "{\"a\":1,\"a\":2}", "{\"__proto__\":1}", "[1,]", "{\"a\":1,}", "[1 2]", " \t\n\r[ \t\n\r] \t\n\r",
                  "\u00a0[]", "[]\u2028", "\uFEFF[]", "{\"\":[]}", "[[[[[[[[[[1]]]]]]]]]]", "123456789012345678901234567890",
                  "1e400", "-1e400", "1e-400", "5e-324", "2.2250738585072014e-308", "1.7976931348623157e308",
                  "0.1", "-0.0", "1E400"])
    for text in texts:
        corpus.add(file, "parse", {
            "op": "JSON parse then serialize",
            "in": string_value(text),
            "out": json_parse_out(text),
            "prov": prov("sec-json.parse", "sec-json.stringify"),
            "script": f"JSON.stringify(JSON.parse({lit(text)}))",
        })
    for text, bits in (("1e400", R.from_fraction(R.Fraction(10) ** 400)), ("-1e400", R.from_fraction(R.Fraction(10) ** 400) | R.SIGN_BIT),
                       ("-0", R.SIGN_BIT), ("1e-400", 0)):
        corpus.add(file, "parse-number", {
            "op": "JSON parse a number",
            "in": string_value(text),
            "out": f"value: {f64(bits)}",
            "prov": prov("sec-json.parse"),
            "script": f"JSON.parse({lit(text)})",
        })
    for text in ("\"\\ud800\"", "\"\uD800\"", "\"\\udc00\"", "\"\uDC00\""):
        corpus.add(file, "parse-surrogate", {
            "op": "JSON parse a lone surrogate",
            "in": string_value(text),
            "out": text_out(T.json_parse(text).value),
            "prov": prov("sec-json.parse"),
            "script": f"JSON.parse({lit(text)})",
        })
    for depth in (1, 2, 32, 63, 64, 65, 66, 100, 256, 511, 512):
        for opener, closer, inner in (("[", "]", "1"), ("{\"k\":", "}", "1")):
            text_script = f"{lit(opener)}.repeat({depth}) + {lit(inner)} + {lit(closer)}.repeat({depth})"
            text = opener * depth + inner + closer * depth
            corpus.add(file, "parse-depth", {
                "op": f"JSON parse nested {depth} deep",
                "in": f"int:{depth} {string_value(opener)}",
                "out": int_out(single_length(T.serialize(T.json_parse(text), ""))),
                "prov": prov("sec-json.parse"),
                "script": f"JSON.stringify(JSON.parse({text_script})).length",
            })
    quote_inputs = [chr(c) for c in range(0x00, 0x21)] + ["\"", "\\", "/", "\u007F", "\u0080", "\u2028", "\u2029",
                                                           "\uD800", "\uDBFF", "\uDC00", "\uDFFF", "\uD83D\uDE00",
                                                           "\uDE00\uD83D", "a\uD800b", "\uFEFF", "\uFFFF"]
    for s in quote_inputs:
        corpus.add(file, "quote", {
            "op": "QuoteJSONString",
            "in": string_value(s),
            "out": text_out(T.quote_json_string(s)),
            "prov": prov("sec-quotejsonstring", "sec-json.stringify"),
            "script": f"JSON.stringify({lit(s)})",
        })
    value_text = "{\"a\":[1,{\"b\":[]},{}],\"c\":\"d\"}"
    parsed = T.json_parse(value_text)
    for space, gap in (("-1", ""), ("0", ""), ("0.5", ""), ("1", " "), ("1.9", " "), ("9", " " * 9), ("10", " " * 10),
                       ("11", " " * 10), ("Infinity", " " * 10), ("2**53", " " * 10), ("-Infinity", ""), ("NaN", ""),
                       ("new Number(3)", "   "), ("\"\"", ""), ("\"\\t\"", "\t"), ("\"ab\"", "ab"),
                       ("\"0123456789AB\"", "0123456789"), ("new String(\"xy\")", "xy"), ("true", ""), ("null", ""),
                       ("[2]", ""), ("{}", "")):
        corpus.add(file, "gap", {
            "op": "JSON stringify with a gap",
            "in": f"{string_value(value_text)} {string_value(space)}",
            "out": texts_out(T.serialize(parsed, gap)),
            "prov": prov("sec-json.stringify", "sec-serializejsonobject", "sec-serializejsonarray"),
            "script": f"JSON.stringify(JSON.parse({lit(value_text)}), null, {space})",
        })
    for space in ("1", "\"\\t\"", "\"--\""):
        corpus.add(file, "line-ending", {
            "op": "JSON stringify writes LF and no CR",
            "in": string_value(space),
            "out": int_out(-1),
            "prov": prov("sec-serializejsonobject", "sec-serializejsonarray"),
            "script": f"JSON.stringify({{ a: [1, {{ b: 2 }}] }}, null, {space}).indexOf(\"\\r\")",
        })
    raw_cases = [
        ("JSON.stringify(JSON.rawJSON(\"1e1000\"))", "rawJSON's text is written as it is", text_out("1e1000")),
        ("JSON.stringify({ a: JSON.rawJSON(\"\\\"x\\\"\") })", "rawJSON inside an object", text_out("{\"a\":\"x\"}")),
        ("Object.keys(JSON.rawJSON(\"1\")).join()", "rawJSON's property is enumerable", text_out("rawJSON")),
        ("JSON.isRawJSON(JSON.rawJSON(\"1\"))", "isRawJSON of a raw JSON object", bool_out(True)),
        ("JSON.isRawJSON(Object.freeze({ rawJSON: \"1\" }))", "isRawJSON of a look-alike", bool_out(False)),
        ("JSON.stringify(Object.freeze({ rawJSON: \"}{\" }))", "a look-alike is an ordinary object",
         text_out("{\"rawJSON\":\"}{\"}")),
        ("JSON.rawJSON(\"{}\")", "rawJSON refuses an object", error_out("SyntaxError")),
        ("JSON.rawJSON(\"[]\")", "rawJSON refuses an array", error_out("SyntaxError")),
        ("JSON.rawJSON(\" 1\")", "rawJSON refuses leading white space", error_out("SyntaxError")),
        ("JSON.rawJSON(\"1 \")", "rawJSON refuses trailing white space", error_out("SyntaxError")),
        ("JSON.rawJSON(\"\")", "rawJSON refuses an empty text", error_out("SyntaxError")),
        ("Object.isFrozen(JSON.rawJSON(\"1\"))", "rawJSON's object is frozen", bool_out(True)),
        ("Object.getPrototypeOf(JSON.rawJSON(\"1\"))", "rawJSON's object has a null prototype", "value: null"),
    ]
    for script, op, out in raw_cases:
        corpus.add(file, "raw-json", {
            "op": op,
            "in": "undefined",
            "out": out,
            "prov": prov("sec-json.rawjson", "sec-json.israwjson"),
            "script": script,
        })


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", required=True, type=Path)
    arguments = parser.parse_args(argv)
    corpus = Corpus()
    whitespace_and_utf16(corpus)
    padding_and_indices(corpus)
    substitution(corpus)
    uri(corpus)
    base64_and_hex(corpus)
    json_area(corpus)
    corpus.classify(DIVERGENCES)
    corpus.write(arguments.out)
    total = sum(len(records) for records in corpus.files.values())
    print(f"{total} vectors in {len(corpus.files)} files")
    return 0


if __name__ == "__main__":
    sys.exit(main())
