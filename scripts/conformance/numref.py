"""Reference computations for the number-conversion vectors, in exact arithmetic.

Every expected value in the number area of the shared conformance corpus is computed here, from the
algorithms of ECMA-262 as the corpus pins them (`conformance/vectors/sources/ecma262.pin`), over
Python's unbounded integers and `fractions.Fraction`. Nothing here calls a floating-point operation
to obtain a result: a double is read and written only as a bit pattern, and every rounding is done on
exact rationals. The module's tests cross-check the results against CPython's own correctly rounded
conversions, which are a second implementation independent of both engines.

Where the specification admits more than one result, a function returns the whole admissible set:
the least significant digit of Number::toString is "not necessarily uniquely determined", and
RoundMVResult lets an implementation round a value of more than 20 significant digits either of two
ways. The recommended choice, where the specification names one, is returned beside the set.
"""

from __future__ import annotations

from dataclasses import dataclass
from fractions import Fraction

# --------------------------------------------------------------------------------------------------
# Doubles as bit patterns.

EXPONENT_BIAS = 1075  # value = mantissa * 2 ** (exponent_field - 1075) for normal numbers
MANTISSA_BITS = 52
SIGN_BIT = 1 << 63
EXPONENT_MASK = 0x7FF << MANTISSA_BITS
FRACTION_MASK = (1 << MANTISSA_BITS) - 1
POSITIVE_INFINITY = 0x7FF0000000000000
NEGATIVE_INFINITY = 0xFFF0000000000000
CANONICAL_NAN = 0x7FF8000000000000
NEGATIVE_ZERO = SIGN_BIT
MAX_FINITE = 0x7FEFFFFFFFFFFFFF
SMALLEST_SUBNORMAL = 0x0000000000000001
LARGEST_SUBNORMAL = 0x000FFFFFFFFFFFFF
SMALLEST_NORMAL = 0x0010000000000000


def is_nan(bits: int) -> bool:
    return (bits & EXPONENT_MASK) == EXPONENT_MASK and (bits & FRACTION_MASK) != 0


def is_infinite(bits: int) -> bool:
    return (bits & ~SIGN_BIT) == POSITIVE_INFINITY


def is_finite(bits: int) -> bool:
    return (bits & EXPONENT_MASK) != EXPONENT_MASK


def is_negative(bits: int) -> bool:
    return bits & SIGN_BIT != 0


def is_zero(bits: int) -> bool:
    return bits & ~SIGN_BIT == 0


def to_fraction(bits: int) -> Fraction:
    """The exact value of a finite double."""
    if not is_finite(bits):
        raise ValueError("not finite")
    field = (bits & EXPONENT_MASK) >> MANTISSA_BITS
    fraction = bits & FRACTION_MASK
    if field == 0:
        magnitude = Fraction(fraction) / (1 << 1074)
    else:
        mantissa = fraction | (1 << MANTISSA_BITS)
        exponent = field - EXPONENT_BIAS
        magnitude = Fraction(mantissa * (1 << exponent)) if exponent >= 0 else Fraction(mantissa, 1 << -exponent)
    return -magnitude if is_negative(bits) else magnitude


def _round_half_even(numerator: int, denominator: int) -> int:
    """The integer nearest numerator / denominator, ties to even; both positive."""
    quotient, remainder = divmod(numerator, denominator)
    twice = 2 * remainder
    if twice > denominator or (twice == denominator and quotient % 2 == 1):
        quotient += 1
    return quotient


def from_fraction(value: Fraction, negative_zero: bool = False) -> int:
    """𝔽(value): the double nearest the exact value, ties to even, overflowing to an infinity.

    `negative_zero` selects -0 when the value is zero (the caller decides what a zero means).
    """
    if value == 0:
        return NEGATIVE_ZERO if negative_zero else 0
    sign = SIGN_BIT if value < 0 else 0
    magnitude = -value if value < 0 else value
    numerator, denominator = magnitude.numerator, magnitude.denominator
    # floor(log2(magnitude)), exactly.
    log2 = numerator.bit_length() - denominator.bit_length()
    if (numerator << max(0, -log2)) < (denominator << max(0, log2)):
        log2 -= 1
    exponent = max(log2 - MANTISSA_BITS, -1074)
    # mantissa = round(magnitude / 2 ** exponent)
    if exponent >= 0:
        mantissa = _round_half_even(numerator, denominator << exponent)
    else:
        mantissa = _round_half_even(numerator << -exponent, denominator)
    if mantissa == 1 << (MANTISSA_BITS + 1):
        mantissa >>= 1
        exponent += 1
    if mantissa >= 1 << MANTISSA_BITS:
        field = exponent + EXPONENT_BIAS
        if field >= 0x7FF:
            return sign | POSITIVE_INFINITY
        return sign | (field << MANTISSA_BITS) | (mantissa - (1 << MANTISSA_BITS))
    # Subnormal: exponent is -1074.
    return sign | mantissa


def next_up(bits: int) -> int:
    """The next double towards +infinity, for a finite non-negative double."""
    return bits + 1


def next_down(bits: int) -> int:
    """The next double towards zero, for a finite positive double."""
    return bits - 1


# --------------------------------------------------------------------------------------------------
# Number::toString(x, radix): the shortest digits that round-trip.

DIGITS = "0123456789abcdefghijklmnopqrstuvwxyz"


@dataclass(frozen=True)
class Shortest:
    """The least k of Number::toString, every admissible (n, s) pair for it, and the recommended pair.

    For all but the smallest subnormals in a large radix, every pair shares one n.
    """

    k: int
    admissible: tuple[tuple[int, int], ...]
    recommended: tuple[int, int]


def _acceptance(bits: int) -> tuple[Fraction, Fraction, bool]:
    """The interval of reals that round to the positive finite double `bits`, and whether its ends
    belong to it (they do when the double's significand is even, because a tie rounds to even)."""
    x = to_fraction(bits)
    below = to_fraction(next_down(bits)) if bits > SMALLEST_SUBNORMAL else Fraction(0)
    if bits == MAX_FINITE:
        above = Fraction(1 << 1024)  # the overflow threshold's other side, 2 ** 1024
    else:
        above = to_fraction(next_up(bits))
    low = (below + x) / 2
    high = (x + above) / 2
    inclusive = bits % 2 == 0
    return low, high, inclusive


def _ceil_div(a: Fraction, b: Fraction) -> int:
    q = a / b
    return -((-q.numerator) // q.denominator)


def _floor_div(a: Fraction, b: Fraction) -> int:
    q = a / b
    return q.numerator // q.denominator


def _ilog(value: Fraction, radix: int) -> int:
    """floor(log_radix(value)) for a positive rational, exactly."""
    exponent = 0
    if value >= 1:
        power = Fraction(radix)
        while power <= value:
            power *= radix
            exponent += 1
        return exponent
    power = Fraction(1)
    while power > value:
        power /= radix
        exponent -= 1
    return exponent


def shortest(bits: int, radix: int) -> Shortest:
    """Step 5 of Number::toString for a positive finite double: the least k, and every (n, s) with
    radix**(k-1) <= s < radix**k such that 𝔽(s * radix**(n-k)) is x."""
    if not (is_finite(bits) and not is_negative(bits) and not is_zero(bits)):
        raise ValueError("a positive finite double is required")
    x = to_fraction(bits)
    low, high, inclusive = _acceptance(bits)
    n0 = _ilog(x, radix) + 1
    k = 1
    while True:
        found: list[tuple[int, int]] = []
        for n in (n0 - 1, n0, n0 + 1):
            scale = Fraction(radix) ** (n - k)
            lo = _ceil_div(low, scale)
            hi = _floor_div(high, scale)
            if not inclusive:
                if Fraction(lo) * scale == low:
                    lo += 1
                if Fraction(hi) * scale == high:
                    hi -= 1
            lo = max(lo, radix ** (k - 1))
            hi = min(hi, radix ** k - 1)
            for s in range(lo, hi + 1):
                found.append((n, s))
        if found:
            admissible = tuple(sorted(set(found)))

            def distance(pair: tuple[int, int]) -> tuple[Fraction, int]:
                n, s = pair
                return (abs(Fraction(s) * Fraction(radix) ** (n - k) - x), s % 2)

            recommended = min(admissible, key=distance)
            return Shortest(k=k, admissible=admissible, recommended=recommended)
        k += 1


def _digits(value: int, radix: int) -> str:
    if value == 0:
        return "0"
    out = []
    while value:
        value, digit = divmod(value, radix)
        out.append(DIGITS[digit])
    return "".join(reversed(out))


def _format_positional(s: int, k: int, n: int, radix: int) -> str:
    digits = _digits(s, radix)
    assert len(digits) == k
    if n >= k:
        return digits + "0" * (n - k)
    if n > 0:
        return digits[:n] + "." + digits[n:]
    return "0." + "0" * (-n) + digits


def _format_exponent(s: int, k: int, n: int) -> str:
    digits = _digits(s, 10)
    sign = "-" if n < 0 else "+"
    exponent = str(abs(n - 1))
    if k == 1:
        return digits + "e" + sign + exponent
    return digits[0] + "." + digits[1:] + "e" + sign + exponent


def number_to_string_from(s: int, k: int, n: int, radix: int) -> str:
    """Steps 6 to 12 of Number::toString, for a positive x whose step 5 chose s."""
    if radix != 10 or -5 <= n <= 21:
        return _format_positional(s, k, n, radix)
    return _format_exponent(s, k, n)


@dataclass(frozen=True)
class Admissible:
    """Every result the specification admits, and the one it recommends (when it names one)."""

    values: tuple[str, ...]
    recommended: str


def number_to_string(bits: int, radix: int = 10) -> Admissible:
    """Number::toString(x, radix) for any double."""
    if is_nan(bits):
        return Admissible(("NaN",), "NaN")
    if is_zero(bits):
        return Admissible(("0",), "0")
    if is_negative(bits):
        inner = number_to_string(bits & ~SIGN_BIT, radix)
        return Admissible(tuple("-" + v for v in inner.values), "-" + inner.recommended)
    if is_infinite(bits):
        return Admissible(("Infinity",), "Infinity")
    result = shortest(bits, radix)
    values = tuple(number_to_string_from(s, result.k, n, radix) for n, s in result.admissible)
    n, s = result.recommended
    return Admissible(values, number_to_string_from(s, result.k, n, radix))


# --------------------------------------------------------------------------------------------------
# toFixed, toExponential and toPrecision: exact digit generation with the specification's tie rule.

def _round_half_up(numerator: int, denominator: int) -> int:
    """The integer nearest a positive rational; of two equally near, the larger."""
    quotient, remainder = divmod(numerator, denominator)
    if 2 * remainder >= denominator:
        quotient += 1
    return quotient


def to_fixed(bits: int, fraction_digits: int) -> str:
    """Number.prototype.toFixed after its argument checks: f in 0..100."""
    if not is_finite(bits):
        return number_to_string(bits).recommended
    x = to_fraction(bits)
    prefix = ""
    if x < 0:
        prefix, x = "-", -x
    if x >= 10 ** 21:
        return prefix + number_to_string(from_fraction(x)).recommended
    scaled = x * 10 ** fraction_digits
    n = _round_half_up(scaled.numerator, scaled.denominator)
    m = "0" if n == 0 else str(n)
    if fraction_digits != 0:
        k = len(m)
        if k <= fraction_digits:
            m = "0" * (fraction_digits + 1 - k) + m
            k = fraction_digits + 1
        m = m[: k - fraction_digits] + "." + m[k - fraction_digits:]
    return prefix + m


def _exponent_and_digits(x: Fraction, digits: int) -> tuple[int, int]:
    """e and n with 10**(digits-1) <= n < 10**digits and n * 10**(e-digits+1) as close to x as
    possible, the larger on a tie; x positive."""
    e = _ilog(x, 10)
    scaled = x / Fraction(10) ** (e - digits + 1)
    n = _round_half_up(scaled.numerator, scaled.denominator)
    if n == 10 ** digits:
        n //= 10
        e += 1
    return e, n


def to_exponential(bits: int, fraction_digits: int | None) -> Admissible:
    """Number.prototype.toExponential after its argument checks; None is an undefined argument."""
    if not is_finite(bits):
        value = number_to_string(bits).recommended
        return Admissible((value,), value)
    x = to_fraction(bits)
    prefix = ""
    if x < 0:
        prefix, x = "-", -x

    def render(m: str, f: int, e: int) -> str:
        if f != 0:
            m = m[0] + "." + m[1:]
        if e == 0:
            c, d = "+", "0"
        elif e > 0:
            c, d = "+", str(e)
        else:
            c, d = "-", str(-e)
        return prefix + m + "e" + c + d

    if x == 0:
        f = fraction_digits or 0
        value = render("0" * (f + 1), f, 0)
        return Admissible((value,), value)
    if fraction_digits is not None:
        e, n = _exponent_and_digits(x, fraction_digits + 1)
        value = render(str(n), fraction_digits, e)
        return Admissible((value,), value)
    result = shortest(from_fraction(x), 10)
    ff = result.k - 1
    values = tuple(render(str(s), ff, n - 1) for n, s in result.admissible)
    n, s = result.recommended
    return Admissible(values, render(str(s), ff, n - 1))


def to_precision(bits: int, precision: int) -> str:
    """Number.prototype.toPrecision after its argument checks: p in 1..100."""
    if not is_finite(bits):
        return number_to_string(bits).recommended
    x = to_fraction(bits)
    prefix = ""
    if x < 0:
        prefix, x = "-", -x
    p = precision
    if x == 0:
        m = "0" * p
        e = 0
    else:
        e, n = _exponent_and_digits(x, p)
        m = str(n)
        if e < -6 or e >= p:
            if p != 1:
                m = m[0] + "." + m[1:]
            c = "+" if e > 0 else "-"
            return prefix + m + "e" + c + str(abs(e))
    if e == p - 1:
        return prefix + m
    if e >= 0:
        return prefix + m[: e + 1] + "." + m[e + 1:]
    return prefix + "0." + "0" * (-(e + 1)) + m


# --------------------------------------------------------------------------------------------------
# White space and line terminators.
#
# A JavaScript string is modelled as a Python str whose characters are UTF-16 code units: an astral
# character is written as its surrogate pair, and a lone surrogate is a character of its own.

# Space_Separator (Zs) as the corpus's pinned UCD derives it: the White_Space lines of PropList.txt
# whose general category is Zs. `zs_from_proplist` re-derives it from the pinned file, and the
# generator refuses to run when the two disagree.
SPACE_SEPARATORS = frozenset([0x0020, 0x00A0, 0x1680, *range(0x2000, 0x200B), 0x202F, 0x205F, 0x3000])
WHITE_SPACE = frozenset([0x0009, 0x000B, 0x000C, 0xFEFF]) | SPACE_SEPARATORS
LINE_TERMINATORS = frozenset([0x000A, 0x000D, 0x2028, 0x2029])
STR_WHITE_SPACE = WHITE_SPACE | LINE_TERMINATORS


def zs_from_proplist(path: str) -> frozenset[int]:
    """The Zs members of White_Space, read from a UCD PropList.txt."""
    found: set[int] = set()
    with open(path, encoding="utf-8") as handle:
        for line in handle:
            body, _, comment = line.partition("#")
            fields = [field.strip() for field in body.split(";")]
            if len(fields) != 2 or fields[1] != "White_Space":
                continue
            if comment.split()[0] != "Zs":
                continue
            first, _, last = fields[0].partition("..")
            found.update(range(int(first, 16), int(last or first, 16) + 1))
    return frozenset(found)


def _trim(s: str, start: bool = True, end: bool = True) -> str:
    i, j = 0, len(s)
    if start:
        while i < j and ord(s[i]) in STR_WHITE_SPACE:
            i += 1
    if end:
        while j > i and ord(s[j - 1]) in STR_WHITE_SPACE:
            j -= 1
    return s[i:j]


# --------------------------------------------------------------------------------------------------
# Decimal values and RoundMVResult.

def _decimal_to_double(a: int, e: int, negative: bool = False) -> int:
    """𝔽(a * 10**e) for a non-negative integer a, with shortcuts far outside the double range."""
    if a == 0:
        return NEGATIVE_ZERO if negative else 0
    length = len(str(a))
    if length + e > 310:
        bits = POSITIVE_INFINITY
    elif length + e < -326:
        bits = 0
    elif e >= 0:
        bits = from_fraction(Fraction(a * 10 ** e))
    else:
        bits = from_fraction(Fraction(a, 10 ** -e))
    if negative:
        bits |= SIGN_BIT
    return bits


def _strip_trailing_zeros(a: int, e: int) -> tuple[int, int]:
    if a == 0:
        return 0, 0
    while a % 10 == 0:
        a //= 10
        e += 1
    return a, e


def round_mv_result(a: int, e: int, negative: bool = False) -> tuple[int, ...]:
    """Every Number RoundMVResult may return for the value a * 10**e (a >= 0), negated afterwards
    when `negative`: one value at 20 or fewer significant digits, otherwise 𝔽 of option1 and of
    option2 (which may coincide)."""
    a, e = _strip_trailing_zeros(a, e)
    digits = len(str(a)) if a else 0
    if digits <= 20:
        return (_decimal_to_double(a, e, negative),)
    drop = digits - 20
    head = a // 10 ** drop
    options = {_decimal_to_double(head, e + drop, negative), _decimal_to_double(head + 1, e + drop, negative)}
    return tuple(sorted(options))


# --------------------------------------------------------------------------------------------------
# StringToNumber, parseFloat and parseInt.

_ASCII_DIGITS = "0123456789"


def _scan_digits(s: str, i: int, alphabet: str = _ASCII_DIGITS) -> int:
    while i < len(s) and s[i] in alphabet:
        i += 1
    return i


@dataclass(frozen=True)
class DecimalMatch:
    end: int
    negative: bool
    infinite: bool
    a: int = 0
    e: int = 0


def match_str_decimal_literal(s: str, i: int = 0) -> DecimalMatch | None:
    """The longest StrDecimalLiteral starting at i, or None."""
    j = i
    negative = False
    if j < len(s) and s[j] in "+-":
        negative = s[j] == "-"
        j += 1
    if s.startswith("Infinity", j):
        return DecimalMatch(j + 8, negative, True)
    int_end = _scan_digits(s, j)
    integer = s[j:int_end]
    fraction = ""
    k = int_end
    if k < len(s) and s[k] == ".":
        frac_end = _scan_digits(s, k + 1)
        candidate = s[k + 1:frac_end]
        if integer or candidate:
            fraction = candidate
            k = frac_end
    if not integer and not fraction:
        return None
    exponent = 0
    if k < len(s) and s[k] in "eE":
        m = k + 1
        sign = 1
        if m < len(s) and s[m] in "+-":
            sign = -1 if s[m] == "-" else 1
            m += 1
        exp_end = _scan_digits(s, m)
        if exp_end > m:
            exponent = sign * int(s[m:exp_end])
            k = exp_end
    a = int(integer + fraction) if integer + fraction else 0
    return DecimalMatch(k, negative, False, a, exponent - len(fraction))


_NON_DECIMAL = {"b": (2, "01"), "o": (8, "01234567"), "x": (16, "0123456789abcdefABCDEF")}


def string_to_number(s: str) -> tuple[int, ...]:
    """Every Number StringToNumber(s) may return."""
    t = _trim(s)
    if t == "":
        return (0,)
    if len(t) > 2 and t[0] == "0" and t[1].lower() in _NON_DECIMAL:
        radix, alphabet = _NON_DECIMAL[t[1].lower()]
        if _scan_digits(t, 2, alphabet) == len(t):
            return (from_fraction(Fraction(int(t[2:], radix))),)
        return (CANONICAL_NAN,)
    match = match_str_decimal_literal(t)
    if match is None or match.end != len(t):
        return (CANONICAL_NAN,)
    if match.infinite:
        return (NEGATIVE_INFINITY if match.negative else POSITIVE_INFINITY,)
    return round_mv_result(match.a, match.e, match.negative)


def parse_float(s: str) -> tuple[int, ...]:
    """Every Number parseFloat(s) may return."""
    t = _trim(s, end=False)
    match = match_str_decimal_literal(t)
    if match is None:
        return (CANONICAL_NAN,)
    if match.infinite:
        return (NEGATIVE_INFINITY if match.negative else POSITIVE_INFINITY,)
    return round_mv_result(match.a, match.e, match.negative)


def to_int32(value: int) -> int:
    value %= 1 << 32
    return value - (1 << 32) if value >= 1 << 31 else value


_RADIX_DIGITS = "0123456789abcdefghijklmnopqrstuvwxyz"


@dataclass(frozen=True)
class ParseIntResult:
    values: tuple[int, ...]
    # True where R is not 2, 4, 8, 10, 16 or 32: the specification lets the result be an
    # implementation-approximated integer, so no single value is normative.
    approximated: bool


def parse_int(s: str, radix: int | None) -> ParseIntResult:
    """Every Number parseInt(s, radix) may return; radix None is undefined."""
    t = _trim(s, end=False)
    negative = False
    if t and t[0] == "-":
        negative = True
    if t and t[0] in "+-":
        t = t[1:]
    r = to_int32(radix) if radix is not None else 0
    strip_prefix = True
    if r != 0:
        if r < 2 or r > 36:
            return ParseIntResult((CANONICAL_NAN,), False)
        if r != 16:
            strip_prefix = False
    else:
        r = 10
    if strip_prefix and len(t) >= 2 and t[:2] in ("0x", "0X"):
        t = t[2:]
        r = 16
    end = 0
    while end < len(t) and t[end].lower() in _RADIX_DIGITS[:r]:
        end += 1
    z = t[:end]
    if z == "":
        return ParseIntResult((CANONICAL_NAN,), False)
    exact = int(z, r)

    def number(value: int) -> int:
        if value == 0:
            return NEGATIVE_ZERO if negative else 0
        if value.bit_length() > 1100:
            return (SIGN_BIT if negative else 0) | POSITIVE_INFINITY
        bits = from_fraction(Fraction(value))
        return bits | SIGN_BIT if negative else bits

    values = {number(exact)}
    if r == 10:
        stripped = z.lstrip("0").rstrip("0")
        if len(stripped) > 20:
            trailing = len(z.lstrip("0")) - 20
            values.add(number(exact // 10 ** trailing * 10 ** trailing))
    return ParseIntResult(tuple(sorted(values)), r not in (2, 4, 8, 10, 16, 32))


# --------------------------------------------------------------------------------------------------
# NumericLiteral values.

@dataclass(frozen=True)
class LiteralResult:
    """('number', admissible doubles) | ('bigint', value) | ('error', 'SyntaxError')."""

    kind: str
    numbers: tuple[int, ...] = ()
    bigint: int = 0


_SYNTAX_ERROR = LiteralResult("error")


def _digits_with_separators(text: str, i: int, alphabet: str) -> tuple[str, int] | None:
    """Digits[+Sep] at i: digit ( '_'? digit )*. Returns the digits without separators and the
    end, or None when no digit is there or a separator is misplaced."""
    if i >= len(text) or text[i] not in alphabet:
        return None
    out = [text[i]]
    j = i + 1
    while j < len(text):
        if text[j] in alphabet:
            out.append(text[j])
            j += 1
        elif text[j] == "_" and j + 1 < len(text) and text[j + 1] in alphabet:
            out.append(text[j + 1])
            j += 2
        elif text[j] == "_":
            return None
        else:
            break
    return "".join(out), j


def numeric_literal(text: str, strict: bool = False) -> LiteralResult:
    """The value of a script consisting of exactly one NumericLiteral, or its early error.

    A literal followed by anything (an IdentifierStart, a digit, a separator or another literal) is
    a SyntaxError, which is what such a script is.
    """
    if text[:1] == "0" and text[1:2].lower() in _NON_DECIMAL:
        radix, alphabet = _NON_DECIMAL[text[1].lower()]
        found = _digits_with_separators(text, 2, alphabet)
        if found is None:
            return _SYNTAX_ERROR
        digits, end = found
        value = int(digits, radix)
        if end == len(text):
            return LiteralResult("number", (from_fraction(Fraction(value)),))
        if text[end:] == "n":
            return LiteralResult("bigint", bigint=value)
        return _SYNTAX_ERROR
    if text[:1] == "0" and text[1:2].isdigit():
        end = _scan_digits(text, 1)
        body = text[1:end]
        if all(c in "01234567" for c in body):
            # LegacyOctalIntegerLiteral: nothing may follow it.
            if strict or end != len(text):
                return _SYNTAX_ERROR
            return LiteralResult("number", (from_fraction(Fraction(int(body, 8))),))
        # NonOctalDecimalIntegerLiteral, a DecimalIntegerLiteral without separators.
        if strict:
            return _SYNTAX_ERROR
        return _decimal_tail(text, end, int(text[:end]), allow_bigint=False)
    if text[:1] == "0":
        return _decimal_tail(text, 1, 0, allow_bigint=True)
    if text[:1] in "123456789":
        found = _digits_with_separators(text, 0, _ASCII_DIGITS)
        if found is None:
            return _SYNTAX_ERROR
        digits, end = found
        return _decimal_tail(text, end, int(digits), allow_bigint=True)
    if text[:1] == ".":
        found = _digits_with_separators(text, 1, _ASCII_DIGITS)
        if found is None:
            return _SYNTAX_ERROR
        digits, end = found
        return _exponent_tail(text, end, int(digits), -len(digits))
    return _SYNTAX_ERROR


def _decimal_tail(text: str, i: int, integer: int, allow_bigint: bool) -> LiteralResult:
    """After a DecimalIntegerLiteral: an optional fraction, exponent or BigInt suffix."""
    if i == len(text):
        return LiteralResult("number", round_mv_result(integer, 0))
    if text[i] == "n":
        if allow_bigint and i + 1 == len(text):
            return LiteralResult("bigint", bigint=integer)
        return _SYNTAX_ERROR
    if text[i] == ".":
        if i + 1 < len(text) and text[i + 1] in _ASCII_DIGITS:
            found = _digits_with_separators(text, i + 1, _ASCII_DIGITS)
            if found is None:
                return _SYNTAX_ERROR
            digits, end = found
            return _exponent_tail(text, end, integer * 10 ** len(digits) + int(digits), -len(digits))
        return _exponent_tail(text, i + 1, integer, 0)
    return _exponent_tail(text, i, integer, 0)


def _exponent_tail(text: str, i: int, a: int, e: int) -> LiteralResult:
    if i < len(text) and text[i] in "eE":
        j = i + 1
        sign = 1
        if j < len(text) and text[j] in "+-":
            sign = -1 if text[j] == "-" else 1
            j += 1
        found = _digits_with_separators(text, j, _ASCII_DIGITS)
        if found is None:
            return _SYNTAX_ERROR
        digits, i = found
        e += sign * int(digits)
    if i != len(text):
        return _SYNTAX_ERROR
    return LiteralResult("number", round_mv_result(a, e))


# --------------------------------------------------------------------------------------------------
# Property keys.

def canonical_numeric_index(s: str) -> int | None:
    """CanonicalNumericIndexString(s) as a double, or None for undefined. Defined here only for
    strings whose ToString of their StringToNumber has a single admissible value."""
    if s == "-0":
        return NEGATIVE_ZERO
    numbers = string_to_number(s)
    if len(numbers) != 1:
        raise ValueError("StringToNumber is not unique for this input")
    n = numbers[0]
    rendered = number_to_string(n)
    if len(rendered.values) != 1:
        raise ValueError("ToString is not unique for this input")
    return n if rendered.values[0] == s else None


def is_array_index(s: str) -> bool:
    n = canonical_numeric_index(s)
    if n is None or is_nan(n) or not is_finite(n) or n == NEGATIVE_ZERO:
        return False
    value = to_fraction(n)
    return value.denominator == 1 and 0 <= value <= 2 ** 32 - 2
