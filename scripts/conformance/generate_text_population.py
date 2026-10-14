#!/usr/bin/env python3
"""Generate the input population of bundle sjl-5-001's reference-arm comparison.

    python3 scripts/conformance/generate_text_population.py --out <directory>

The population is the one docs/evidence/sjl-5-001/reference-arms.md declares, class by class (T1,
P1, S1, U1, B1 and J1), with the seeds it names, as its revision of 2026-10-14
(docs/evidence/sjl-5-001/reference-arms-revised-2026-10-14.md) reads the sets the rule leaves open
and the parts it bounds. The comparison in Broiler.ECMAScript.Algorithms.Tests reads what this
writes; nothing here is an expected value, and the comparison judges no input by it.

Every class is one or more gzip files of one input per line, and manifest.json records each file's
line count and the SHA-256 of its uncompressed text, so a bundle can name the population it ran:

- a string is written with every character outside '!' to '~', and the backslash, as \\uXXXX, so
  that a line holds no space, and the empty string is the empty line;
- a code point is its value in upper-case hexadecimal; a byte array is its bytes as two lower-case
  hexadecimal digits each, and the empty array is "-";
- a line of several fields separates them with one space.

**A draw** is one output of SplitMix64 (the vector generator's `splitmix64`, which
tests/test_reference_population.py tests against the published reference outputs). A choice among n
things takes the draw modulo n; a choice that needs a second value takes it from the draw shifted right
by 16 bits, modulo its own range. Each drawn set below states its draws in that form.

Products the comparison takes over these classes - a string at every index, a template at every
capture count, a text in every alphabet and mode into every destination, a document at every depth
limit - are built by the comparison, because they would be gigabytes as text.

The output is deterministic: the same arguments give the same bytes (gzip's header carries no time).
"""

from __future__ import annotations

import argparse
import gzip
import hashlib
import itertools
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import numref as R  # noqa: E402
from generate_number_vectors import splitmix64  # noqa: E402

RULE = "docs/evidence/sjl-5-001/reference-arms.md"
REVISION = "docs/evidence/sjl-5-001/reference-arms-revised-2026-10-14.md"

SEED_T1 = 0x534A4C352D303031  # "SJL5-001"
SEED_S1 = 0x534A4C352D303032
SEED_U1 = 0x534A4C352D303033
SEED_B1 = 0x534A4C352D303034
SEED_B1_HEX = 0x534A4C352D303035
SEED_B1_BYTES = 0x534A4C352D303036
SEED_J1 = 0x534A4C352D303037
SEED_J1_NUMBERS = 0x534A4C352D303038

T1_DRAWS = 1 << 16
S1_DRAWS = 1 << 14
U1_DRAWS = 1 << 16
B1_DRAWS = 1 << 14
B1_HEX_DRAWS = 1 << 12
B1_BYTE_DRAWS = 1 << 12
J1_DRAWS = 1 << 14
J1_NUMBER_DRAWS = 1 << 12

ELEVEN_UNITS = ["a", "\u0020", "\u00A0", "\u0085", "\u180E", "\uFEFF", "\u2028", "\uD800", "\uDBFF", "\uDC00", "\uDFFF"]
WHITE_SPACE = sorted(chr(c) for c in R.STR_WHITE_SPACE) + ["\u0085", "\u180E", "\u200B"]
TEMPLATE_CHARACTERS = ["$", "&", "`", "'", "0", "1", "2", "9", "<", ">", "a"]
GROUP_NAMES = ["a", "b", "xyz"]
BASE64_CHARACTERS = ["A", "Z", "a", "z", "0", "9", "+", "/", "-", "_", "=", "\u0020", "\u000A", "@"]
HEX_CHARACTERS = ["0", "9", "a", "f", "A", "F", "g", "Z", "\u0020"]
JSON_CHARACTERS = ["{", "}", "[", "]", ",", ":", "\"", "\\", "0", "1", "-", ".", "e", "t", "n", "u", "a", "\u0020"]
CONTINUATIONS = [0x00, 0x7F, 0x80, 0x8F, 0x90, 0x9F, 0xA0, 0xBF, 0xC0, 0xFF]
URI_RESERVED = ";/?:@&=+$,#"
URI_UNRESERVED = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.!~*'()"
BASE64_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"


# --------------------------------------------------------------------------------------------------
# Writing.

def escape(text: str) -> str:
    return "".join(c if "!" <= c <= "~" and c != "\\" else f"\\u{ord(c):04X}" for c in text)


def byte_line(data: bytes) -> str:
    return data.hex() if data else "-"


class Writer:
    def __init__(self, out: Path) -> None:
        self.out = out
        self.classes: dict[str, dict[str, object]] = {}

    def write(self, name: str, lines: list[str], what: str) -> None:
        text = "".join(line + "\n" for line in lines)
        data = text.encode("utf-8")
        path = self.out / f"{name}.txt.gz"
        with open(path, "wb") as raw:
            with gzip.GzipFile(filename="", mode="wb", fileobj=raw, mtime=0, compresslevel=6) as stream:
                stream.write(data)
        self.classes[name] = {"file": path.name, "count": len(lines), "sha256": hashlib.sha256(data).hexdigest(),
                              "what": what}
        print(f"{name}: {len(lines)}", file=sys.stderr)

    def manifest(self) -> None:
        seeds = {name: hex(value) for name, value in globals().items() if name.startswith("SEED_")}
        manifest = {"rule": RULE, "revision": REVISION,
                    "generator": "scripts/conformance/generate_text_population.py",
                    "seeds": seeds, "classes": self.classes}
        (self.out / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")


def strings_over(alphabet: list[str], lengths: range) -> list[str]:
    return ["".join(p) for n in lengths for p in itertools.product(alphabet, repeat=n)]


def unique(values: list[str]) -> list[str]:
    seen: set[str] = set()
    out = []
    for value in values:
        if value not in seen:
            seen.add(value)
            out.append(value)
    return out


# --------------------------------------------------------------------------------------------------
# T1: strings.

def drawn_unit(draw: int) -> str:
    """One element of a drawn string: ASCII, white space, the rest of the BMP, an astral pair or a lone surrogate."""
    kind, value = draw % 5, draw >> 16
    if kind == 0:
        return chr(0x20 + value % 95)
    if kind == 1:
        return WHITE_SPACE[value % len(WHITE_SPACE)]
    if kind == 2:
        code = 0x80 + value % (0x10000 - 0x80 - 0x800)
        return chr(code + 0x800 if code >= 0xD800 else code)
    if kind == 3:
        code = 0x10000 + value % 0x100000
        return chr(0xD800 + ((code - 0x10000) >> 10)) + chr(0xDC00 + ((code - 0x10000) & 0x3FF))
    return chr(0xD800 + value % 0x800)


def t1_draws() -> list[str]:
    """2^16 strings: a draw gives the length, 0 to 64 code units, and each element is a draw; an astral
    pair that would pass the length is not taken, and the next draw is."""
    stream = splitmix64(SEED_T1)
    out = []
    for _ in range(T1_DRAWS):
        length = next(stream) % 65
        text = ""
        while len(text) < length:
            element = drawn_unit(next(stream))
            if len(text) + len(element) <= length:
                text += element
        out.append(text)
    return out


def t1_white_space() -> list[str]:
    out = []
    for c in WHITE_SPACE:
        for run in range(1, 4):
            pad = c * run
            out += [pad + "ab", "ab" + pad, pad + "ab" + pad]
    return out


# --------------------------------------------------------------------------------------------------
# P1: padding and indices.

P1_SHORT = ["", "a", "ab", "abc", "abcd", "abcde"]
P1_FILLERS = ["", " ", "ab", "\uD800", "\uDC00\uD800", "0123456789"]
P1_MAXIMA = [0, 1, 16, 1 << 24, 1 << 30, (1 << 31) - 1]
P1_LENGTHS = [0, 1, 2, 7, 1 << 32, (1 << 53) - 1]


def p1_relative() -> list[str]:
    """length relative: the relative value is "-Infinity", "Infinity" or a decimal integer. Every integer
    from -(length + 2) to length + 2 for the short lengths; for 2^32 and 2^53 - 1, the integers within two
    of -length, of zero and of length, as the revision reads it."""
    out = []
    for length in P1_LENGTHS:
        if length <= 7:
            integers = list(range(-(length + 2), length + 3))
        else:
            integers = sorted(set(list(range(-length - 2, -length + 3)) + list(range(-2, 3)) + list(range(length - 2, length + 3))))
        values = ["-Infinity", str(-(1 << 53))] + [str(i) for i in integers] + [str(1 << 53), "Infinity"]
        out += [f"{length} {v}" for v in unique(values)]
    return out


# --------------------------------------------------------------------------------------------------
# S1: templates.

def s1_draws() -> list[str]:
    """2^14 templates: a draw gives the length, 5 to 40; each element is a draw over the eleven characters
    and the three group names, and the template is cut at its length."""
    stream = splitmix64(SEED_S1)
    tokens = TEMPLATE_CHARACTERS + GROUP_NAMES
    out = []
    for _ in range(S1_DRAWS):
        length = 5 + next(stream) % 36
        text = ""
        while len(text) < length:
            text += tokens[next(stream) % len(tokens)]
        out.append(text[:length])
    return out


# --------------------------------------------------------------------------------------------------
# U1: URI.

def octet(value: int, lower: bool = False) -> str:
    text = f"%{value:02X}"
    return text.lower() if lower else text


def u1_decode() -> list[str]:
    out = []
    for value in range(256):
        out += [octet(value), octet(value, True)]
    for length in (2, 3, 4):
        for lead in range(0x80, 0x100):
            for rest in itertools.product(CONTINUATIONS, repeat=length - 1):
                octets = [lead, *rest]
                text = "".join(octet(o) for o in octets)
                out.append(text)
                out += [text[:3 * k] for k in range(1, length)]
    for c in URI_RESERVED + URI_UNRESERVED:
        out += [c, octet(ord(c)), octet(ord(c), True)]
    out += ["%", "%A", "%f", "%G", "%z", "%AF", "%af", "%AG", "%Gf", "%GZ", "%zz", "a%", "a%A", "a%G"]
    return unique(out)


def u1_unescape() -> list[str]:
    out = []
    for value in range(0x10000):
        upper = f"%u{value:04X}"
        out.append(upper)
        if upper.lower()[2:] != upper[2:]:
            out.append("%u" + f"{value:04x}")
    out += ["%u", "%u1", "%u12", "%u123", "%uG123", "%u12G4", "%U1234", "%u12345", "a%u1"]
    return out


def u1_draws() -> list[str]:
    """2^16 strings: a draw gives the element count, 0 to 24; each element is a draw: raw ASCII, a raw
    unit of the BMP, an astral pair, a lone surrogate, a %XX escape, a UTF-8 sequence's escapes, a
    %uXXXX escape, or a malformed escape."""
    stream = splitmix64(SEED_U1)
    out = []
    for _ in range(U1_DRAWS):
        count = next(stream) % 25
        parts = []
        for _ in range(count):
            draw = next(stream)
            kind, value = draw % 8, draw >> 16
            if kind == 0:
                parts.append(chr(0x20 + value % 95))
            elif kind == 1:
                code = 0x80 + value % (0x10000 - 0x80 - 0x800)
                parts.append(chr(code + 0x800 if code >= 0xD800 else code))
            elif kind == 2:
                code = value % 0x100000
                parts.append(chr(0xD800 + (code >> 10)) + chr(0xDC00 + (code & 0x3FF)))
            elif kind == 3:
                parts.append(chr(0xD800 + value % 0x800))
            elif kind == 4:
                parts.append(octet(value % 256, bool(value & 0x100)))
            elif kind == 5:
                code = value % 0x110000
                if 0xD800 <= code <= 0xDFFF:
                    code += 0x800
                parts.append("".join(octet(b) for b in chr(code % 0x110000).encode("utf-8", "surrogatepass")))
            elif kind == 6:
                parts.append(f"%u{value % 0x10000:04X}")
            else:
                parts.append(["%", "%Z", "%5", "%u12", "%uZZZZ", "%%", "%E2%82", "%C0%80"][value % 8])
        out.append("".join(parts))
    return out


# --------------------------------------------------------------------------------------------------
# B1: base64 and hex.

def b1_draws() -> list[str]:
    """2^14 strings of 5 to 120 code units: a draw gives the byte count and each byte; they are encoded
    in an alphabet a draw chooses, padded or not; white space is put in at drawn places; and most are
    then mutated by one character: one of the fourteen inserted, one deleted, or one replaced."""
    stream = splitmix64(SEED_B1)
    url = BASE64_ALPHABET[:62] + "-_"
    out = []
    while len(out) < B1_DRAWS:
        count = next(stream) % 89
        data = bytes(next(stream) % 256 for _ in range(count))
        alphabet = BASE64_ALPHABET if next(stream) % 2 == 0 else url
        text = ""
        for start in range(0, len(data), 3):
            group = data[start:start + 3]
            value = int.from_bytes(group + bytes(3 - len(group)), "big")
            text += "".join(alphabet[(value >> s) & 0x3F] for s in (18, 12, 6, 0))[:len(group) + 1]
            if len(group) < 3 and next(stream) % 2 == 0:
                text += "=" * (3 - len(group))
        for _ in range(next(stream) % 4):
            at = next(stream) % (len(text) + 1)
            text = text[:at] + [" ", "\n", "\t", "\r", "\f"][next(stream) % 5] + text[at:]
        mutation = next(stream) % 4
        if mutation and text:
            at = next(stream) % len(text)
            c = BASE64_CHARACTERS[next(stream) % len(BASE64_CHARACTERS)]
            text = (text[:at] + c + text[at:]) if mutation == 1 else (text[:at] + text[at + 1:]) if mutation == 2 else (text[:at] + c + text[at + 1:])
        if 5 <= len(text) <= 120:
            out.append(text)
    return out


def b1_hex_draws() -> list[str]:
    """2^12 strings of 5 to 64 code units: a draw gives the length, and each character is a draw, mostly
    hexadecimal digits of either case, sometimes one of the nine."""
    stream = splitmix64(SEED_B1_HEX)
    digits = "0123456789abcdefABCDEF"
    out = []
    for _ in range(B1_HEX_DRAWS):
        length = 5 + next(stream) % 60
        text = ""
        for _ in range(length):
            draw = next(stream)
            text += HEX_CHARACTERS[(draw >> 16) % len(HEX_CHARACTERS)] if draw % 16 == 0 else digits[(draw >> 16) % len(digits)]
        out.append(text)
    return out


def b1_bytes() -> list[str]:
    out = [byte_line(b"")]
    out += [byte_line(bytes([a])) for a in range(256)]
    out += [byte_line(bytes([a, b])) for a in range(256) for b in range(256)]
    stream = splitmix64(SEED_B1_BYTES)
    for _ in range(B1_BYTE_DRAWS):
        length = 3 + next(stream) % 62
        out.append(byte_line(bytes(next(stream) % 256 for _ in range(length))))
    return out


# --------------------------------------------------------------------------------------------------
# J1: JSON.

def j1_document(stream, depth: int) -> str:
    """A document nesting to exactly depth containers: a chain of arrays and objects, each level with a
    drawn number of drawn primitive siblings before and after the next level."""
    def primitive() -> str:
        draw = next(stream)
        kind, value = draw % 6, draw >> 16
        return ["null", "true", "false", str(value % 1000 - 500), f"\"s{value % 100}\"", f"{value % 100}.5e{value % 7 - 3}"][kind]

    opening, closing = [], []
    for level in range(depth):
        is_object = next(stream) % 2 == 0
        before = [primitive() for _ in range(next(stream) % 3)]
        after = [primitive() for _ in range(next(stream) % 2)]
        if is_object:
            opening.append("{" + "".join(f"\"b{i}\":{p}," for i, p in enumerate(before)) + f"\"k{level}\":")
            closing.append("".join(f",\"a{i}\":{p}" for i, p in enumerate(after)) + "}")
        else:
            opening.append("[" + "".join(p + "," for p in before))
            closing.append("".join("," + p for p in after) + "]")
    return "".join(opening) + primitive() + "".join(reversed(closing))


def j1_draws() -> list[str]:
    """2^14 documents: a draw gives the depth, 1 to 600, and the document is drawn as j1_document says;
    each is followed by its mutation: a draw chooses an insertion, a deletion or a replacement, a draw
    its place, and a draw the character, one of the eighteen."""
    stream = splitmix64(SEED_J1)
    out = []
    for _ in range(J1_DRAWS):
        depth = 1 + next(stream) % 600
        document = j1_document(stream, depth)
        kind = next(stream) % 3
        at = next(stream) % len(document)
        c = JSON_CHARACTERS[next(stream) % len(JSON_CHARACTERS)]
        mutated = (document[:at] + c + document[at:]) if kind == 0 else (document[:at] + document[at + 1:]) if kind == 1 else (document[:at] + c + document[at + 1:])
        out += [document, mutated]
    return out


def j1_escapes() -> list[str]:
    out = ["\"\\\"\"", "\"\\\\\"", "\"\\/\"", "\"\\b\"", "\"\\f\"", "\"\\n\"", "\"\\r\"", "\"\\t\"", "\"\\x\"", "\"\\u\"", "\"\\u12\"", "\"\\u12G4\"", "\"\\"]
    for value in range(0x10000):
        upper = f"\"\\u{value:04X}\""
        out.append(upper)
        lower = f"\"\\u{value:04x}\""
        if lower != upper:
            out.append(lower)
        out.append("\"" + chr(value) + "\"")
    return out


def j1_numbers() -> list[str]:
    """2^12 numbers: a draw gives the sign, the integer digits (1 to 20, without a leading zero), the
    fraction digits (0 to 20) and the exponent (-400 to 400); each is written with no exponent when the
    exponent is zero, and otherwise in every spelling: e, E, e+, E+ (for a positive exponent) and e-, E-."""
    stream = splitmix64(SEED_J1_NUMBERS)
    out = ["1e400", "-1e400", "1e-400", "-0", "0", "-0.0", "0e0", "1E+2", "1e-2"]
    for _ in range(J1_NUMBER_DRAWS):
        negative = next(stream) % 2 == 1
        integer = str(1 + next(stream) % 9) + "".join(str(next(stream) % 10) for _ in range(next(stream) % 20))
        fraction = "".join(str(next(stream) % 10) for _ in range(next(stream) % 21))
        exponent = next(stream) % 801 - 400
        mantissa = ("-" if negative else "") + integer + ("." + fraction if fraction else "")
        if exponent == 0:
            out.append(mantissa)
        elif exponent > 0:
            out += [f"{mantissa}{e}{exponent}" for e in ("e", "E", "e+", "E+")]
        else:
            out += [f"{mantissa}{e}{exponent}" for e in ("e", "E")]
    return out


def j1_gaps() -> list[str]:
    """The gap's inputs: "n <Number>" for the numbers, "s <String>" for strings of every length from 0 to 12."""
    numbers = ["-Infinity", "-1", "0", "0.5", "1", "9", "10", "11", str(1 << 53), "Infinity"]
    text = "0123456789\tB"
    return [f"n {n}" for n in numbers] + ["s " + escape(text[:k]) for k in range(13)]


def j1_nested() -> list[str]:
    out = []
    for depth in (63, 64, 65, 511, 512, 513, 1023, 1024, 1025):
        out.append("[" * depth + "1" + "]" * depth)
        out.append("{\"k\":" * depth + "1" + "}" * depth)
    return out


# --------------------------------------------------------------------------------------------------

def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--out", required=True, type=Path)
    args = parser.parse_args(argv)
    args.out.mkdir(parents=True, exist_ok=True)
    w = Writer(args.out)

    w.write("T1U", [escape(chr(c)) for c in range(0x10000)], "every code unit as a one-unit string")
    w.write("T1E", [escape(s) for s in strings_over(ELEVEN_UNITS, range(1, 5))], "every string of length 1 to 4 over the eleven units")
    w.write("T1W", [escape(s) for s in t1_white_space()], "every white-space character and U+0085, U+180E, U+200B at either end and both of \"ab\", in runs of 1 to 3")
    w.write("T1R", [escape(s) for s in t1_draws()], f"{T1_DRAWS} strings drawn with seed {hex(SEED_T1)}")
    w.write("P1S", [escape(s) for s in P1_SHORT], "the strings of lengths 0 to 5")
    w.write("P1F", [escape(s) for s in P1_FILLERS], "the fillers")
    w.write("P1M", [str(m) for m in P1_MAXIMA], "the maximum lengths")
    w.write("P1X", p1_relative(), "length relative: the relative-index inputs")
    w.write("S1E", [escape(s) for s in strings_over(TEMPLATE_CHARACTERS, range(0, 5))], "every template of length 0 to 4 over the eleven characters")
    w.write("S1R", [escape(s) for s in s1_draws()], f"{S1_DRAWS} templates drawn with seed {hex(SEED_S1)}")
    w.write("U1C", [f"{c:X}" for c in range(0x110000)], "every code point, a lone surrogate as one unit")
    w.write("U1D", [escape(s) for s in u1_decode()], "the decode inputs")
    w.write("U1N", [escape(s) for s in u1_unescape()], "every %uXXXX in both letter cases, and short ones")
    w.write("U1R", [escape(s) for s in u1_draws()], f"{U1_DRAWS} strings drawn with seed {hex(SEED_U1)}")
    w.write("B1E", [escape(s) for s in strings_over(BASE64_CHARACTERS, range(0, 5))], "every string of length 0 to 4 over the fourteen characters")
    w.write("B1R", [escape(s) for s in b1_draws()], f"{B1_DRAWS} strings drawn with seed {hex(SEED_B1)}")
    w.write("B1H", [escape(s) for s in strings_over(HEX_CHARACTERS, range(0, 5))], "every string of length 0 to 4 over the nine hex characters")
    w.write("B1Q", [escape(s) for s in b1_hex_draws()], f"{B1_HEX_DRAWS} hex strings drawn with seed {hex(SEED_B1_HEX)}")
    w.write("B1Y", b1_bytes(), f"every byte array of length 0 to 2, then {B1_BYTE_DRAWS} drawn with seed {hex(SEED_B1_BYTES)}")
    w.write("J1E", [escape(s) for s in strings_over(JSON_CHARACTERS, range(0, 4))], "every text of length 0 to 3 over the eighteen characters")
    w.write("J1R", [escape(s) for s in j1_draws()], f"{J1_DRAWS} documents drawn with seed {hex(SEED_J1)}, each followed by its mutation")
    w.write("J1S", [escape(s) for s in j1_escapes()], "every escape, and every code unit escaped in both letter cases and raw")
    w.write("J1N", [escape(s) for s in j1_numbers()], f"the fixed numbers, then {J1_NUMBER_DRAWS} drawn with seed {hex(SEED_J1_NUMBERS)}")
    w.write("J1D", [escape(s) for s in j1_nested()], "arrays and objects nested 63 to 1025 deep")
    w.write("J1G", j1_gaps(), "the gap's numbers and strings")
    w.manifest()
    return 0


if __name__ == "__main__":
    sys.exit(main())
