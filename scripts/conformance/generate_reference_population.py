#!/usr/bin/env python3
"""Generate the input population of bundle sjl-3-001's reference-arm comparison.

    python3 scripts/conformance/generate_reference_population.py --out <directory> [--jobs N]

The population is the one docs/evidence/sjl-3-001/reference-arms.md declares, class by class (D1 to
D6, T1, T2, P1 to P5, L1 and K1), with the seeds it names. The comparison in
Broiler.ECMAScript.Algorithms.Tests reads what this writes; nothing here is an expected value, and
the comparison judges no input by it.

Every class is one gzip file of one input per line, and manifest.json records each file's line
count and the SHA-256 of its uncompressed text, so a bundle can name the population it ran. A double
is its IEEE 754 bit pattern in 16 hexadecimal digits; a string is written with every character
outside '!' to '~', and the backslash, as \\uXXXX, so whitespace, line terminators and lone
surrogates survive a line-oriented file.

The output is deterministic: the same arguments give the same bytes (gzip's header carries no time).
"""

from __future__ import annotations

import argparse
import gzip
import hashlib
import json
import multiprocessing
import re
import sys
from fractions import Fraction
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import numref as R  # noqa: E402
from generate_number_vectors import splitmix64  # noqa: E402

RULE = "docs/evidence/sjl-3-001/reference-arms.md"
SEED_D6 = 0x534A4C332D303031  # "SJL3-001"
SEED_T1 = 0x534A4C332D303032  # "SJL3-002"
SEED_T2 = 0x534A4C332D303033  # "SJL3-003"
D6_COUNT = 1 << 20
P2_FROM_D6 = 1 << 14
P2_LENGTHS = (17, 18, 19, 20, 25, 30, 40, 50, 100, 200, 400, 800)
TIES_PER_COUNT = 64


# --------------------------------------------------------------------------------------------------
# Writing.

def escape(text: str) -> str:
    return "".join(c if "!" <= c <= "~" and c != "\\" else f"\\u{ord(c):04X}" for c in text)


def hex_bits(bits: int) -> str:
    return f"{bits:016X}"


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
        manifest = {"rule": RULE, "generator": "scripts/conformance/generate_reference_population.py",
                    "reference": "scripts/conformance/numref.py",
                    "seeds": {"D6": hex(SEED_D6), "T1": hex(SEED_T1), "T2": hex(SEED_T2)},
                    "classes": self.classes}
        (self.out / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")


# --------------------------------------------------------------------------------------------------
# The doubles.

def power_of_two(exponent: int) -> int:
    return R.from_fraction(Fraction(2) ** exponent)


def power_of_ten(exponent: int) -> int:
    return R.from_fraction(Fraction(10) ** exponent)


def around(bits: int, count: int) -> list[int]:
    """A positive double with the `count` doubles on each side of it."""
    return [bits + k for k in range(-count, count + 1) if 0 <= bits + k <= R.POSITIVE_INFINITY]


def d1() -> list[int]:
    positive = [0, R.SMALLEST_SUBNORMAL, R.LARGEST_SUBNORMAL, R.SMALLEST_NORMAL, R.MAX_FINITE, R.POSITIVE_INFINITY]
    return positive + [b | R.SIGN_BIT for b in positive] + [R.CANONICAL_NAN]


def d2() -> list[int]:
    return [b for e in range(-1074, 1024) for b in around(power_of_two(e), 1)]


def d3() -> list[int]:
    return [b for e in range(-323, 309) for b in around(power_of_ten(e), 1)]


def d4() -> list[int]:
    return around(R.string_to_number("1e21")[0], 8) + around(R.string_to_number("1e-7")[0], 8)


def d5() -> list[int]:
    return around(power_of_two(53), 64) + around(power_of_two(64), 64)


def d6() -> list[int]:
    out = []
    for draw in splitmix64(SEED_D6):
        if R.is_nan(draw):
            continue
        out.append(draw)
        if len(out) == D6_COUNT:
            return out
    raise AssertionError


def t1() -> list[int]:
    """For every f from 0 to 100, 64 doubles a / 2**(f+1) with a odd and below 2**53."""
    out = []
    stream = splitmix64(SEED_T1)
    for f in range(0, 101):
        for _ in range(TIES_PER_COUNT):
            a = (next(stream) >> 11) | 1
            value = Fraction(a, 2 ** (f + 1))
            assert value < 10 ** 21
            bits = R.from_fraction(value)
            assert R.to_fraction(bits) == value
            out.append(bits)
    return out


def significant_digits(value: Fraction) -> str:
    """The significant digits of a positive dyadic rational's exact decimal expansion."""
    k = 0
    while value.denominator != 1:
        value *= 10
        k += 1
    return str(value.numerator).rstrip("0")


def t2() -> list[int]:
    """For every p from 1 to 100, at most 64 doubles whose exact expansion has p + 1 significant
    digits ending in 5: integers below 2**53 for the short ones, dyadic fractions for every p."""
    out = []
    stream = splitmix64(SEED_T2)
    for p in range(1, 101):
        digits = p + 1
        found: list[int] = []
        attempts = 0
        while len(found) < TIES_PER_COUNT and attempts < 64 * TIES_PER_COUNT:
            attempts += 1
            draw = next(stream)
            if attempts % 2 == 1 and digits <= 16:
                # An integer of `digits` digits ending in 5.
                head = 10 ** (digits - 2) + draw % (9 * 10 ** (digits - 2))
                value = Fraction(head * 10 + 5)
                if value >= 2 ** 53:
                    continue
            else:
                # a / 2**k, a odd below 2**53: its expansion has len(a * 5**k) significant digits.
                a = (draw >> 11) | 1
                k = 1
                while len(str(a * 5 ** k)) < digits:
                    k += 1
                if len(str(a * 5 ** k)) != digits:
                    continue
                value = Fraction(a, 2 ** k)
            bits = R.from_fraction(value)
            if R.to_fraction(bits) != value:
                continue
            shown = significant_digits(value)
            assert len(shown) == digits and shown.endswith("5"), (p, value)
            found.append(bits)
        out += found
    return out


# --------------------------------------------------------------------------------------------------
# The strings.

def shortest_strings(bits: int) -> tuple[str, str]:
    """The shortest round-trip digits of |x| as numref chooses them, positional and with an exponent."""
    magnitude = bits & ~R.SIGN_BIT
    if R.is_zero(magnitude):
        return "0", "0e+0"
    result = R.shortest(magnitude, 10)
    n, s = result.recommended
    return R._format_positional(s, result.k, n, 10), R._format_exponent(s, result.k, n)


def p1_for(bits_chunk: list[int]) -> list[tuple[str, str]]:
    return [shortest_strings(b) for b in bits_chunk]


def chunked(items: list[int], size: int) -> list[list[int]]:
    return [items[i:i + size] for i in range(0, len(items), size)]


def exponent_form(integer: int, exponent: int, negative: bool) -> str:
    """integer * 10**exponent as d.ddde±x."""
    digits = str(integer)
    leading = exponent + len(digits) - 1
    body = digits[0] + ("." + digits[1:] if len(digits) > 1 else "")
    return ("-" if negative else "") + body + "e" + ("+" if leading >= 0 else "-") + str(abs(leading))


def p2_for(bits: int) -> list[str]:
    """The midpoint above a finite double, written exactly, truncated, and one unit either side."""
    x = R.to_fraction(bits)
    if bits == R.MAX_FINITE:
        above = Fraction(1 << 1024)  # the overflow threshold's other side, as numref's interval has it
    elif bits == R.NEGATIVE_ZERO:
        above = R.to_fraction(R.SMALLEST_SUBNORMAL)
    elif R.is_negative(bits):
        above = R.to_fraction(bits - 1)  # towards +infinity, a negative double's pattern decreases
    else:
        above = R.to_fraction(R.next_up(bits))
    midpoint = (x + above) / 2
    negative = midpoint < 0
    magnitude = abs(midpoint)
    # magnitude = whole * 10**-k exactly.
    k = 0
    scaled = magnitude
    while scaled.denominator != 1:
        scaled *= 10
        k += 1
    whole = scaled.numerator
    while whole % 10 == 0 and whole:
        whole //= 10
        k -= 1
    out = []
    digits = str(whole)
    for length in P2_LENGTHS:
        if len(digits) >= length:
            truncated = int(digits[:length])
            exponent = -k + (len(digits) - length)
        else:
            truncated = int(digits + "0" * (length - len(digits)))
            exponent = -k - (length - len(digits))
        for delta in (0, 1, -1):
            value = truncated + delta
            if value > 0:
                out.append(exponent_form(value, exponent, negative))
    out.append(exponent_form(whole, -k, negative))
    return out


def p2_chunk(bits_chunk: list[int]) -> list[list[str]]:
    return [p2_for(b) for b in bits_chunk]


def p3_integers() -> list[int]:
    values = [2 ** e + k for e in list(range(52, 57)) + list(range(63, 67)) for k in range(-64, 65)]
    for e in range(53, 1024):
        half = 2 ** e + 2 ** (e - 53)
        values += [half - 1, half, half + 1]
    return values


def p3(integers: list[int]) -> list[str]:
    out = []
    for value in integers:
        out += ["0b" + R._digits(value, 2), "0o" + R._digits(value, 8), "0x" + R._digits(value, 16)]
        out += [R._digits(value, radix) for radix in range(2, 37)]
    return out


def p4() -> list[str]:
    out = []
    for code in range(0x10000):
        c = chr(code)
        out += [c, c + "1", "1" + c, c + "1" + c]
    return out


def p5() -> list[str]:
    out = []
    for prefix in ("0x", "0o", "0b"):
        beyond = {"0x": "g", "0o": "8", "0b": "2"}[prefix]
        for p in (prefix, prefix.upper()):
            out += [p, p + "_1", p + "1_", p + beyond, p + "1" + beyond, "+" + p + "1", "-" + p + "1", p + "_"]
    for word in ("Infinity", "infinity", "INFINITY", "InFiNiTy", "Infinit", "Infinityx", "Inf", "infinite"):
        out += [word, "+" + word, "-" + word]
    out += ["", ".", ".5", "5.", "e5", "1e", "1e+", "1e-", "+.", "-.", ".e1", "+", "-", "++1", "--1", "+-1", "E5", "1E5",
            "1.e5", ".5e-5", "5.e+5"]
    for width in (400, 1000, 100000):
        for exponent in ("1" + "0" * (width - 1), "9" * width):
            for mantissa in ("1", "0", "1.5", "0.0"):
                for sign in ("", "+", "-"):
                    out.append(mantissa + "e" + sign + exponent)
    out += ["00", "007", "0001.5", "000e5", "-000", "+000", "00x1", "0x0001", "0o0007", "0b0001", "08", "09", "010",
            "0.0000", "-0", "+0", "000000000000000000000000000001"]
    out += ["1_000", "1_0.5", "0x1_0", "0b1_0", "0o1_0", "1e1_0", "_1", "1_", "1__0", "0_1", ".5_5"]
    return out


LITERAL = re.compile(
    r"(?:(?:0|[1-9][0-9]*)(?:\.[0-9]*)?(?:[eE][+-]?[0-9]+)?"
    r"|\.[0-9]+(?:[eE][+-]?[0-9]+)?"
    r"|0[xX][0-9a-fA-F]+|0[oO][0-7]+|0[bB][01]+"
    r"|0[0-7]+"
    r"|0[0-9]*[89][0-9]*(?:\.[0-9]*)?(?:[eE][+-]?[0-9]+)?)\Z")


def l1(strings: list[str], d5_bits: list[int], integers: list[int]) -> list[str]:
    out = [s for s in strings if LITERAL.match(s)]
    out += ["010", "0777", "00", "08", "09", "08.5", "09e1", "0089", "019", "07.5", "07e1"]
    out += ["1_000", "1_000.000_1", "0x1_F", "0b1_0", "0o1_7", "1e1_0", ".1_1", "0.0_1", "1_0e-1_0", "1__0", "1_",
            "_1", "0_1", "0x_1", "1_.5", "1._5", "1e_1", "1e+_1", "0b_1", "0o_1", "01_0", "08_1", "0x1__1", "1_n",
            "1_0n", "0x1_0n"]
    values = [int(R.to_fraction(b)) for b in d5_bits] + integers
    for value in values:
        out += [str(value) + "n", "0b" + R._digits(value, 2) + "n", "0o" + R._digits(value, 8) + "n",
                "0x" + R._digits(value, 16) + "n"]
    out += ["0n", "01n", "08n", "1.5n", "1e3n", ".5n", "0x0n", "0b0n", "0o0n", "0xFFFFFFFFFFFFFFFFn",
            "0x10000000000000000n", "18446744073709551615n", "18446744073709551616n"]
    return out


def k1(bits: list[int]) -> list[str]:
    out = [R.number_to_string(b, 10).recommended for b in bits]
    out += ["0", "1", "4294967294", "4294967295", "4294967296", "-0", "01", "1.0", "1e3", " 1", "+1", "-1", "Infinity",
            "-Infinity", "NaN"]
    return out


# --------------------------------------------------------------------------------------------------

def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--jobs", type=int, default=multiprocessing.cpu_count())
    args = parser.parse_args(argv)
    args.out.mkdir(parents=True, exist_ok=True)
    writer = Writer(args.out)

    doubles = {"D1": d1(), "D2": d2(), "D3": d3(), "D4": d4(), "D5": d5(), "D6": d6()}
    what = {"D1": "special values at both signs, and NaN", "D2": "powers of two with their neighbours",
            "D3": "doubles nearest the powers of ten with their neighbours",
            "D4": "the doubles nearest 1e21 and 1e-7 with eight on each side",
            "D5": "64 doubles on each side of 2^53 and 2^64", "D6": f"{D6_COUNT} SplitMix64 doubles, seed {hex(SEED_D6)}"}
    for name, bits in doubles.items():
        writer.write(name, [hex_bits(b) for b in bits], what[name])
    writer.write("T1", [hex_bits(b) for b in t1()], f"toFixed ties, seed {hex(SEED_T1)}")
    writer.write("T2", [hex_bits(b) for b in t2()], f"toExponential and toPrecision ties, seed {hex(SEED_T2)}")

    finite = [b for name in ("D1", "D2", "D3", "D4", "D5", "D6") for b in doubles[name] if R.is_finite(b)]
    with multiprocessing.Pool(args.jobs) as pool:
        pairs = [pair for chunk in pool.map(p1_for, chunked(finite, 4096)) for pair in chunk]
        p1 = []
        for positional, exponent in pairs:
            for text in (positional, exponent):
                p1 += [text, "+" + text, "-" + text]
        writer.write("P1", [escape(s) for s in p1], "the shortest strings of D1 to D6's finite doubles, both notations, three signs")

        boundary = doubles["D2"] + doubles["D3"] + [b for b in doubles["D6"] if R.is_finite(b)][:P2_FROM_D6]
        p2 = [s for chunk in pool.map(p2_chunk, chunked(boundary, 256)) for group in chunk for s in group]
    writer.write("P2", [escape(s) for s in p2], "rounding boundaries: midpoints, truncated and one unit either side")

    integers = p3_integers()
    p3_strings = p3(integers)
    writer.write("P3", [escape(s) for s in p3_strings], "integers around 2^53 and 2^64, and halfway cases, in every radix")
    writer.write("P4", [escape(s) for s in p4()], "every UTF-16 code unit alone, before, after and around \"1\"")
    writer.write("P5", [escape(s) for s in p5()], "the grammar's edges")
    writer.write("L1", [escape(s) for s in l1(p1 + p2 + p3_strings, doubles["D5"], integers)],
                 "literal texts: the admitted strings of P1 to P3, legacy, separator and BigInt forms")
    writer.write("K1", [escape(s) for s in k1([b for n in ("D1", "D2", "D3", "D4", "D5") for b in doubles[n]])],
                 "keys: the canonical numeric strings of D1 to D5, and the listed edges")
    writer.manifest()
    return 0


if __name__ == "__main__":
    sys.exit(main())
