"""Reference computations for the numerics vectors, in exact arithmetic.

Every expected value in the numerics area of the shared conformance corpus is computed here, from
the algorithms of ECMA-262 as the corpus pins them (`conformance/vectors/sources/ecma262.pin`) and of
the Temporal proposal as both engines implement it, over Python's unbounded integers and
`fractions.Fraction`. A double, a binary32 and a binary16 are read and written only as bit patterns,
and every rounding is done on exact rationals.

Where the specification lets a result be "implementation-approximated" (`**` and `Math.pow` in
general, `Math.hypot`, `Math.cbrt`, `Math.expm1`, `Math.log1p`), the reference is the correctly
rounded result, and the generator also gives the faithful pair (the two doubles bracketing the exact
value) where an engine's plan commits it to a bound of one unit in the last place. The square and cube
roots are found by exact comparison with the midpoints between doubles. The exponential and the
logarithm are enclosed in an interval that `decimal`'s correctly rounded `exp` and `ln` bound, or that
a Taylor series with an exact remainder bound gives near zero, and the interval is narrowed until it
decides the rounding; nothing is ever taken from a floating-point operation.

The module's tests cross-check what CPython computes independently and exactly (its binary16 and
binary32 packing, `math.fsum`, `math.sqrt`), and the transcendental results against the C library to
within one unit in the last place, which is all that library promises.
"""

from __future__ import annotations

import decimal
from dataclasses import dataclass
from fractions import Fraction
from math import isqrt

import numref as R

# --------------------------------------------------------------------------------------------------
# Binary formats as bit patterns.


@dataclass(frozen=True)
class Format:
    name: str
    exponent_bits: int
    fraction_bits: int

    @property
    def bias(self) -> int:
        return (1 << (self.exponent_bits - 1)) - 1

    @property
    def sign_bit(self) -> int:
        return 1 << (self.exponent_bits + self.fraction_bits)

    @property
    def fraction_mask(self) -> int:
        return (1 << self.fraction_bits) - 1

    @property
    def exponent_mask(self) -> int:
        return ((1 << self.exponent_bits) - 1) << self.fraction_bits

    @property
    def infinity(self) -> int:
        return self.exponent_mask

    @property
    def quiet_nan(self) -> int:
        return self.exponent_mask | (1 << (self.fraction_bits - 1))

    @property
    def min_quantum(self) -> int:
        """The exponent of the least significant bit of a subnormal: 2 ** min_quantum is the smallest."""
        return 1 - self.bias - self.fraction_bits


BINARY16 = Format("binary16", 5, 10)
BINARY32 = Format("binary32", 8, 23)
BINARY64 = Format("binary64", 11, 52)


def _round_half_even(numerator: int, denominator: int) -> int:
    quotient, remainder = divmod(numerator, denominator)
    twice = 2 * remainder
    if twice > denominator or (twice == denominator and quotient % 2 == 1):
        quotient += 1
    return quotient


def _floor_log2(value: Fraction) -> int:
    numerator, denominator = value.numerator, value.denominator
    log2 = numerator.bit_length() - denominator.bit_length()
    if (numerator << max(0, -log2)) < (denominator << max(0, log2)):
        log2 -= 1
    return log2


def encode(value: Fraction, fmt: Format, negative_zero: bool = False) -> int:
    """The bits of the value of `fmt` nearest `value`, ties to even, overflowing to an infinity
    (IEEE 754-2019 roundTiesToEven, which ECMA-262 names for every conversion to a binary format)."""
    if value == 0:
        return fmt.sign_bit if negative_zero else 0
    sign = fmt.sign_bit if value < 0 else 0
    magnitude = -value if value < 0 else value
    numerator, denominator = magnitude.numerator, magnitude.denominator
    exponent = max(_floor_log2(magnitude) - fmt.fraction_bits, fmt.min_quantum)
    if exponent >= 0:
        mantissa = _round_half_even(numerator, denominator << exponent)
    else:
        mantissa = _round_half_even(numerator << -exponent, denominator)
    if mantissa == 1 << (fmt.fraction_bits + 1):
        mantissa >>= 1
        exponent += 1
    if mantissa >= 1 << fmt.fraction_bits:
        field = exponent - fmt.min_quantum + 1
        if field >= (1 << fmt.exponent_bits) - 1:
            return sign | fmt.infinity
        return sign | (field << fmt.fraction_bits) | (mantissa - (1 << fmt.fraction_bits))
    return sign | mantissa


def is_nan_in(bits: int, fmt: Format) -> bool:
    return (bits & fmt.exponent_mask) == fmt.exponent_mask and (bits & fmt.fraction_mask) != 0


def is_infinite_in(bits: int, fmt: Format) -> bool:
    return (bits & fmt.exponent_mask) == fmt.exponent_mask and (bits & fmt.fraction_mask) == 0


def decode(bits: int, fmt: Format) -> Fraction:
    """The exact value of finite `bits` of `fmt`."""
    if (bits & fmt.exponent_mask) == fmt.exponent_mask:
        raise ValueError("not finite")
    field = (bits & fmt.exponent_mask) >> fmt.fraction_bits
    fraction = bits & fmt.fraction_mask
    if field == 0:
        mantissa, exponent = fraction, fmt.min_quantum
    else:
        mantissa, exponent = fraction | (1 << fmt.fraction_bits), field - 1 + fmt.min_quantum
    magnitude = Fraction(mantissa) * (Fraction(2) ** exponent)
    return -magnitude if bits & fmt.sign_bit else magnitude


def convert(bits: int, source: Format, target: Format) -> int:
    """`bits` of `source` rounded to `target`, NaN to the target's canonical quiet NaN."""
    negative = bool(bits & source.sign_bit)
    if is_nan_in(bits, source):
        return target.quiet_nan
    if is_infinite_in(bits, source):
        return (target.sign_bit if negative else 0) | target.infinity
    value = decode(bits, source)
    return encode(value, target, negative_zero=negative and value == 0)


def double(value: Fraction | int, negative_zero: bool = False) -> int:
    return encode(Fraction(value), BINARY64, negative_zero)


def is_integral(bits: int) -> bool:
    return R.is_finite(bits) and R.to_fraction(bits).denominator == 1


# --------------------------------------------------------------------------------------------------
# The integer conversions (ECMA-262 7.1.5 to 7.1.22).


def _truncated(bits: int) -> int | None:
    """truncate(ℝ(number)) for a finite number, None for NaN and the infinities."""
    if not R.is_finite(bits):
        return None
    value = R.to_fraction(bits)
    return int(value) if value >= 0 else -int(-value)


def to_integer_or_infinity(bits: int) -> int:
    """ToIntegerOrInfinity, as the Number it is: NaN and -0 give +0, the infinities themselves."""
    if R.is_nan(bits):
        return 0
    if R.is_infinite(bits):
        return bits
    return double(_truncated(bits))


def modular(bits: int, width: int, signed: bool) -> int:
    """ToInt32, ToUint32, ToInt16, ToUint16, ToInt8 and ToUint8: the integer, not yet a Number."""
    integer = _truncated(bits)
    if integer is None:
        return 0
    integer %= 1 << width
    if signed and integer >= 1 << (width - 1):
        integer -= 1 << width
    return integer


def to_uint8_clamp(bits: int) -> int:
    if R.is_nan(bits):
        return 0
    if R.is_infinite(bits):
        return 0 if R.is_negative(bits) else 255
    value = R.to_fraction(bits)
    if value <= 0:
        return 0
    if value >= 255:
        return 255
    floor = int(value)
    if value - floor > Fraction(1, 2):
        return floor + 1
    if value - floor < Fraction(1, 2):
        return floor
    return floor if floor % 2 == 0 else floor + 1


MAX_SAFE_INTEGER = (1 << 53) - 1


def to_length(bits: int) -> int:
    if R.is_nan(bits):
        return 0
    if R.is_infinite(bits):
        return 0 if R.is_negative(bits) else MAX_SAFE_INTEGER
    integer = _truncated(bits)
    return min(max(integer, 0), MAX_SAFE_INTEGER)


def to_index(bits: int) -> int | None:
    """ToIndex: the index, or None where it throws a RangeError."""
    if R.is_nan(bits):
        return 0
    if R.is_infinite(bits):
        return None
    integer = _truncated(bits)
    if integer < 0 or integer > MAX_SAFE_INTEGER:
        return None
    return integer


def to_big_int64(value: int) -> int:
    value %= 1 << 64
    return value - (1 << 64) if value >= 1 << 63 else value


def to_big_uint64(value: int) -> int:
    return value % (1 << 64)


# --------------------------------------------------------------------------------------------------
# Float16 (NumericToRawBytes and RawBytesToNumeric for Float16, Math.f16round) and Math.fround.


def float16_encode(bits: int) -> int | None:
    """The binary16 bits a Number is written as; None for NaN, whose encoding is the implementation's."""
    if R.is_nan(bits):
        return None
    return convert(bits, BINARY64, BINARY16)


def float16_decode(half: int) -> int:
    """The Number binary16 bits denote, NaN for every NaN payload."""
    return convert(half, BINARY16, BINARY64)


def f16round(bits: int) -> int:
    if R.is_nan(bits):
        return R.CANONICAL_NAN
    return convert(convert(bits, BINARY64, BINARY16), BINARY16, BINARY64)


def fround(bits: int) -> int:
    if R.is_nan(bits):
        return R.CANONICAL_NAN
    return convert(convert(bits, BINARY64, BINARY32), BINARY32, BINARY64)


def float16_nan_preserving(bits: int) -> int:
    """The binary16 NaN a NaN of these double bits is written as when its payload is kept: quiet, with
    the sign and the ten most significant bits of the double's significand. This is the policy
    Broiler.JS's plan names for its engine, which does not normalise (SharedLibraries.md, SJL-4.2)."""
    assert R.is_nan(bits)
    sign = 0x8000 if R.is_negative(bits) else 0
    return sign | 0x7E00 | ((bits & R.FRACTION_MASK) >> 42)


FLOAT16_CANONICAL_NAN = 0x7E00


# --------------------------------------------------------------------------------------------------
# Math functions the specification determines exactly.


def math_round(bits: int) -> int:
    if not R.is_finite(bits) or is_integral(bits):
        return bits
    value = R.to_fraction(bits)
    if 0 < value < Fraction(1, 2):
        return 0
    if Fraction(-1, 2) <= value < 0:
        return R.NEGATIVE_ZERO
    floor = value.numerator // value.denominator
    rounded = floor + 1 if value - floor >= Fraction(1, 2) else floor
    return double(rounded, negative_zero=value < 0)


def clz32(bits: int) -> int:
    return 32 - modular(bits, 32, False).bit_length()


def imul(a: int, b: int) -> int:
    product = (modular(a, 32, False) * modular(b, 32, False)) % (1 << 32)
    return product - (1 << 32) if product >= 1 << 31 else product


def sum_precise(values: list[int]) -> int:
    """Math.sumPrecise over a list of Numbers (its steps 3 to 11)."""
    state = "minus-zero"
    total = Fraction(0)
    for n in values:
        if state == "not-a-number":
            continue
        if R.is_nan(n):
            state = "not-a-number"
        elif n == R.POSITIVE_INFINITY:
            state = "not-a-number" if state == "minus-infinity" else "plus-infinity"
        elif n == R.NEGATIVE_INFINITY:
            state = "not-a-number" if state == "plus-infinity" else "minus-infinity"
        elif n != R.NEGATIVE_ZERO and state in ("minus-zero", "finite"):
            state = "finite"
            total += R.to_fraction(n)
    if state == "not-a-number":
        return R.CANONICAL_NAN
    if state == "plus-infinity":
        return R.POSITIVE_INFINITY
    if state == "minus-infinity":
        return R.NEGATIVE_INFINITY
    if state == "minus-zero":
        return R.NEGATIVE_ZERO
    return double(total)


# --------------------------------------------------------------------------------------------------
# Correct rounding of a real number the module can enclose.


@dataclass(frozen=True)
class Approximated:
    """An implementation-approximated result: the correctly rounded double, and the faithful pair,
    the doubles on either side of the exact value (one double where the exact value is one, or, for a
    power, where it lies beyond 2^1100 or below 2^-1100)."""

    correct: int
    faithful: tuple[int, ...]


def _next_toward_infinity(bits: int) -> int:
    """The next double away from zero, for a finite double of either sign."""
    return bits + 1


# The least magnitude that rounds to an infinity: the largest finite double plus half its unit.
OVERFLOW_THRESHOLD = Fraction(2) ** 1024 - Fraction(2) ** 970


def _neighbours(value: Fraction) -> tuple[int, ...]:
    """The doubles bracketing a nonzero exact value: itself if it is one, the largest finite double and
    the infinity beyond it."""
    if abs(value) > R.to_fraction(R.MAX_FINITE):
        sign = R.SIGN_BIT if value < 0 else 0
        return (sign | R.MAX_FINITE, sign | R.POSITIVE_INFINITY)
    rounded = double(value)
    if R.to_fraction(rounded) == value:
        return (rounded,)
    below = rounded if abs(R.to_fraction(rounded)) < abs(value) else rounded - 1
    return tuple(sorted((below, below + 1)))


def _round_enclosed(lower: Fraction, upper: Fraction) -> Approximated | None:
    """The rounding of a value known to lie in [lower, upper], or None if the interval does not decide it."""
    a, b = double(lower), double(upper)
    if a != b:
        return None
    # Both ends round to one double; the faithful pair is decided if both ends share their neighbours.
    if lower == upper:
        return Approximated(a, _neighbours(lower))
    na, nb = _neighbours(lower), _neighbours(upper)
    if na != nb or len(na) != 2:
        return None
    return Approximated(a, na)


def _root(value: Fraction, degree: int) -> Approximated:
    """The correctly rounded `degree`-th root of a positive rational, by exact comparison."""
    assert value > 0
    # An integer approximation of root * 2**k with about 70 significant bits.
    k = 70 - _floor_log2(value) // degree
    scaled = value * Fraction(2) ** (degree * k)
    approximation = _integer_root(scaled.numerator // scaled.denominator, degree)
    guess = double(Fraction(approximation, 1) / Fraction(2) ** k)

    def compare(t: Fraction) -> int:
        power = t ** degree
        return (value > power) - (value < power)

    if compare(OVERFLOW_THRESHOLD) >= 0:
        return Approximated(R.POSITIVE_INFINITY, (R.MAX_FINITE, R.POSITIVE_INFINITY))
    guess = min(guess, R.MAX_FINITE)
    while True:
        current = R.to_fraction(guess)
        above = R.to_fraction(guess + 1) if guess + 1 < R.POSITIVE_INFINITY else None
        below = R.to_fraction(guess - 1) if guess > 0 else Fraction(0)
        high_mid = (current + above) / 2 if above is not None else None
        low_mid = (current + below) / 2
        if high_mid is not None and compare(high_mid) > 0:
            guess += 1
            continue
        if compare(low_mid) < 0:
            guess -= 1
            continue
        if high_mid is not None and compare(high_mid) == 0 and guess % 2 == 1:
            guess += 1
        elif compare(low_mid) == 0 and guess % 2 == 1:
            guess -= 1
        break
    exact = compare(R.to_fraction(guess))
    if exact == 0:
        faithful = (guess,)
    elif exact > 0:
        faithful = (guess, guess + 1)
    else:
        faithful = (guess - 1, guess)
    return Approximated(guess, faithful)


def _integer_root(n: int, degree: int) -> int:
    if degree == 2:
        return isqrt(n)
    if n < 2:
        return n
    x = 1 << ((n.bit_length() + degree - 1) // degree)
    while True:
        y = ((degree - 1) * x + n // x ** (degree - 1)) // degree
        if y >= x:
            return x
        x = y


def hypot(values: list[int]) -> int | Approximated:
    """Math.hypot: the exact result of its special cases, or the approximated one."""
    if any(R.is_infinite(v) for v in values):
        return R.POSITIVE_INFINITY
    if any(R.is_nan(v) for v in values):
        return R.CANONICAL_NAN
    if all(R.is_zero(v) for v in values):
        return 0
    total = sum((R.to_fraction(v) ** 2 for v in values), Fraction(0))
    return _root(total, 2)


def cbrt(bits: int) -> int | Approximated:
    if not R.is_finite(bits) or R.is_zero(bits):
        return bits
    value = R.to_fraction(bits)
    root = _root(abs(value), 3)
    if value > 0:
        return root
    return Approximated(root.correct | R.SIGN_BIT, tuple(sorted(b | R.SIGN_BIT for b in root.faithful)))


def _negate(result: Approximated) -> Approximated:
    return Approximated(result.correct ^ R.SIGN_BIT, tuple(sorted(b ^ R.SIGN_BIT for b in result.faithful)))


def _exact_decimal(value: Fraction) -> decimal.Decimal:
    """A dyadic rational as the Decimal with exactly its value."""
    numerator, denominator = value.numerator, value.denominator
    k = denominator.bit_length() - 1
    assert denominator == 1 << k
    return decimal.Decimal(f"{numerator * 5 ** k}E-{k}")


def _decimal_fraction(value: decimal.Decimal) -> Fraction:
    return Fraction(value)


def _enclose(compute, precision: int = 60) -> Approximated:
    """Narrows an enclosure until it decides the rounding. `compute(p)` returns (lower, upper)."""
    for _ in range(12):
        lower, upper = compute(precision)
        result = _round_enclosed(lower, upper)
        if result is not None:
            return result
        precision *= 2
    raise AssertionError("the enclosure did not decide the rounding")


def _series_bound_exp(x: Fraction, terms: int) -> tuple[Fraction, Fraction]:
    """expm1(x) for |x| <= 2**-10, as an interval: the series' first terms and a bound on the rest."""
    total, term = Fraction(0), Fraction(1)
    for k in range(1, terms + 1):
        term = term * x / k
        total += term
    # The remainder is at most |x|**(n+1) / (n+1)! * e**|x| < 2 * |next term|.
    rest = abs(term * x / (terms + 1)) * 2
    return total - rest, total + rest


def _series_bound_log(x: Fraction, terms: int) -> tuple[Fraction, Fraction]:
    """log1p(x) for |x| <= 2**-10: an alternating or geometric remainder bound on the series."""
    total, power = Fraction(0), Fraction(1)
    for k in range(1, terms + 1):
        power *= x
        total += power / k if k % 2 == 1 else -power / k
    # |remainder| <= sum_{k>n} |x|**k / k <= |x|**(n+1) / (1 - |x|).
    rest = abs(power * x) / (1 - abs(x))
    return total - rest, total + rest


SMALL = Fraction(1, 1 << 10)


def expm1(bits: int) -> int | Approximated:
    if R.is_nan(bits) or R.is_zero(bits) or bits == R.POSITIVE_INFINITY:
        return R.CANONICAL_NAN if R.is_nan(bits) else bits
    if bits == R.NEGATIVE_INFINITY:
        return double(-1)
    x = R.to_fraction(bits)
    if abs(x) <= SMALL:
        return _enclose(lambda p: _series_bound_exp(x, max(4, p // 6)), 60)
    if x > 710:
        return Approximated(R.POSITIVE_INFINITY, (R.MAX_FINITE, R.POSITIVE_INFINITY))

    def compute(p: int) -> tuple[Fraction, Fraction]:
        with decimal.localcontext() as context:
            context.prec = p
            context.Emin, context.Emax = -999999, 999999
            e = _exact_decimal(x).exp()
            ulp = Fraction(10) ** (e.adjusted() - p + 1)
            value = _decimal_fraction(e) - 1
            return value - ulp, value + ulp

    return _enclose(compute)


def log1p(bits: int) -> int | Approximated:
    if R.is_nan(bits) or R.is_zero(bits) or bits == R.POSITIVE_INFINITY:
        return R.CANONICAL_NAN if R.is_nan(bits) else bits
    x = R.to_fraction(bits) if R.is_finite(bits) else None
    if x is None or x < -1:
        return R.CANONICAL_NAN
    if x == -1:
        return R.NEGATIVE_INFINITY
    if abs(x) <= SMALL:
        return _enclose(lambda p: _series_bound_log(x, max(4, p // 6)), 60)

    def compute(p: int) -> tuple[Fraction, Fraction]:
        with decimal.localcontext() as context:
            context.prec = p
            context.Emin, context.Emax = -999999, 999999
            # 1 + x is exact in a context wide enough for its digits.
            context.prec = max(p, 1200)
            argument = _exact_decimal(1 + x)
            context.prec = p
            value = argument.ln()
            ulp = Fraction(10) ** (value.adjusted() - p + 1)
            middle = _decimal_fraction(value)
            return middle - ulp, middle + ulp

    return _enclose(compute)


def _is_odd_integral(value: Fraction) -> bool:
    return value.denominator == 1 and value.numerator % 2 != 0


def exponentiate(base: int, exponent: int) -> int | Approximated:
    """Number::exponentiate (ECMA-262 6.1.6.1.3): a special case's exact Number, or the approximated
    result of its last step."""
    if R.is_nan(exponent):
        return R.CANONICAL_NAN
    if R.is_zero(exponent):
        return double(1)
    if R.is_nan(base):
        return R.CANONICAL_NAN
    y = R.to_fraction(exponent) if R.is_finite(exponent) else None
    positive_exponent = (y is not None and y > 0) or exponent == R.POSITIVE_INFINITY
    odd = y is not None and _is_odd_integral(y)
    if base == R.POSITIVE_INFINITY:
        return R.POSITIVE_INFINITY if positive_exponent else 0
    if base == R.NEGATIVE_INFINITY:
        if positive_exponent:
            return R.NEGATIVE_INFINITY if odd else R.POSITIVE_INFINITY
        return R.NEGATIVE_ZERO if odd else 0
    if base == 0:
        return 0 if positive_exponent else R.POSITIVE_INFINITY
    if base == R.NEGATIVE_ZERO:
        if positive_exponent:
            return R.NEGATIVE_ZERO if odd else 0
        return R.NEGATIVE_INFINITY if odd else R.POSITIVE_INFINITY
    b = R.to_fraction(base)
    if exponent == R.POSITIVE_INFINITY:
        return R.POSITIVE_INFINITY if abs(b) > 1 else R.CANONICAL_NAN if abs(b) == 1 else 0
    if exponent == R.NEGATIVE_INFINITY:
        return 0 if abs(b) > 1 else R.CANONICAL_NAN if abs(b) == 1 else R.POSITIVE_INFINITY
    if b < 0 and y.denominator != 1:
        return R.CANONICAL_NAN
    negative = b < 0 and odd
    magnitude = abs(b)
    if magnitude == 1:
        # 1 and -1 to a finite power are exactly 1 or -1 (an odd integral power of -1).
        return Approximated(double(-1 if negative else 1), (double(-1 if negative else 1),))
    # Far outside the range, the result is an infinity or rounds to zero whatever the digits, and the
    # faithful set is that one result: no implementation returns a finite or nonzero value there.
    huge, tiny = Fraction(2) ** 1100, Fraction(1, 1 << 1100)
    if y.denominator == 1 and abs(y.numerator) <= 4096:
        exact = magnitude ** y.numerator
        if exact >= huge or exact <= tiny:
            result = Approximated(double(exact), (double(exact),))
        else:
            result = Approximated(double(exact), _neighbours(exact))
        return _negate(result) if negative else result

    def compute(p: int) -> tuple[Fraction, Fraction]:
        with decimal.localcontext() as context:
            context.Emin, context.Emax = -999999, 999999
            context.prec = p
            logarithm = _exact_decimal(magnitude).ln()
            product = logarithm * _exact_decimal(y)
            # |error of product| <= ulp(log) * |y| + ulp(product); widen it by a factor of four.
            slack = (Fraction(10) ** (logarithm.adjusted() - p + 1) * abs(y)
                     + Fraction(10) ** (product.adjusted() - p + 1)) * 4
            middle = _decimal_fraction(product)
            if middle - slack > 720:
                return huge, huge
            if middle + slack < -760:
                return tiny, tiny
            low = _exact_fraction_exp(middle - slack, p, -1)
            high = _exact_fraction_exp(middle + slack, p, +1)
            return low, high

    result = _enclose(compute)
    if result.correct in (R.POSITIVE_INFINITY, 0) and compute(60)[0] in (huge, tiny):
        result = Approximated(result.correct, (result.correct,))
    return _negate(result) if negative else result


def _exact_fraction_exp(t: Fraction, p: int, direction: int) -> Fraction:
    """A bound on exp(t) in the given direction, from decimal's correctly rounded exp."""
    with decimal.localcontext() as context:
        context.Emin, context.Emax = -999999, 999999
        context.prec = p
        # t as a decimal, rounded outwards by one unit of p digits.
        rounded = decimal.Decimal(t.numerator) / decimal.Decimal(t.denominator)
        step = Fraction(10) ** (rounded.adjusted() - p + 1)
        bound = Fraction(rounded) + direction * step
        context.prec = max(p, 1200)
        bound_decimal = decimal.Decimal(bound.numerator) / decimal.Decimal(bound.denominator)
        context.prec = p
        e = bound_decimal.exp()
        ulp = Fraction(10) ** (e.adjusted() - p + 1)
        return Fraction(e) + direction * 2 * ulp


# --------------------------------------------------------------------------------------------------
# Exact rounding: the rounding modes of Temporal and Intl (ECMA-262's ApplyUnsignedRoundingMode family,
# as the Temporal proposal and ECMA-402 define them).

MODES = ("ceil", "floor", "expand", "trunc", "halfCeil", "halfFloor", "halfExpand", "halfTrunc", "halfEven")

_UNSIGNED = {
    "ceil": ("infinity", "zero"),
    "floor": ("zero", "infinity"),
    "expand": ("infinity", "infinity"),
    "trunc": ("zero", "zero"),
    "halfCeil": ("half-infinity", "half-zero"),
    "halfFloor": ("half-zero", "half-infinity"),
    "halfExpand": ("half-infinity", "half-infinity"),
    "halfTrunc": ("half-zero", "half-zero"),
    "halfEven": ("half-even", "half-even"),
}


def unsigned_rounding_mode(mode: str, negative: bool) -> str:
    return _UNSIGNED[mode][1 if negative else 0]


def negate_rounding_mode(mode: str) -> str:
    return {"ceil": "floor", "floor": "ceil", "halfCeil": "halfFloor", "halfFloor": "halfCeil"}.get(mode, mode)


def apply_unsigned_rounding_mode(x: Fraction, r1: int, r2: int, mode: str) -> int:
    if x == r1:
        return r1
    if mode == "zero":
        return r1
    if mode == "infinity":
        return r2
    d1, d2 = x - r1, r2 - x
    if d1 < d2:
        return r1
    if d2 < d1:
        return r2
    if mode == "half-zero":
        return r1
    if mode == "half-infinity":
        return r2
    assert mode == "half-even"
    return r1 if (r1 // (r2 - r1)) % 2 == 0 else r2


def _floor(value: Fraction) -> int:
    return value.numerator // value.denominator


def round_number_to_increment(x: int | Fraction, increment: int, mode: str) -> Fraction:
    quotient = Fraction(x) / increment
    negative = quotient < 0
    if negative:
        quotient = -quotient
    r1 = _floor(quotient)
    rounded = apply_unsigned_rounding_mode(quotient, r1, r1 + 1, unsigned_rounding_mode(mode, negative))
    return Fraction(-rounded if negative else rounded) * increment


def round_number_to_increment_as_if_positive(x: int | Fraction, increment: int, mode: str) -> Fraction:
    quotient = Fraction(x) / increment
    r1 = _floor(quotient)
    rounded = apply_unsigned_rounding_mode(quotient, r1, r1 + 1, unsigned_rounding_mode(mode, False))
    return Fraction(rounded) * increment


# --------------------------------------------------------------------------------------------------
# BigInt (ECMA-262 6.1.6.2, 7.1.13 to 7.1.15, 21.2).

STR_WHITE_SPACE = R.STR_WHITE_SPACE


def bigint_divide(x: int, y: int) -> int | None:
    if y == 0:
        return None
    quotient = abs(x) // abs(y)
    return quotient if (x < 0) == (y < 0) else -quotient


def bigint_remainder(x: int, y: int) -> int | None:
    if y == 0:
        return None
    remainder = abs(x) % abs(y)
    return -remainder if x < 0 else remainder


def bigint_left_shift(x: int, y: int) -> int:
    return x << y if y >= 0 else x >> -y


def as_int_n(bits: int, value: int) -> int:
    if bits == 0:
        return 0
    if bit_width(value) < bits:
        return value  # -2^(bits-1) <= value < 2^(bits-1): the value is its own residue
    modulus = value % (1 << bits)
    return modulus - (1 << bits) if modulus >= 1 << (bits - 1) else modulus


def as_uint_n(bits: int, value: int) -> int:
    if 0 <= value and value.bit_length() <= bits:
        return value
    if bits > 1 << 24:
        raise ValueError("a residue of more than 2^24 bits is not computed")
    return value % (1 << bits)


def bigint_to_string(value: int, radix: int) -> str:
    if value == 0:
        return "0"
    digits = []
    magnitude = abs(value)
    while magnitude:
        magnitude, digit = divmod(magnitude, radix)
        digits.append(R.DIGITS[digit])
    return ("-" if value < 0 else "") + "".join(reversed(digits))


_PREFIXES = {"0x": 16, "0X": 16, "0o": 8, "0O": 8, "0b": 2, "0B": 2}


def string_to_big_int(s: str) -> int | None:
    """StringToBigInt (7.1.14): the value, or None where the text is not a StringIntegerLiteral."""
    start, end = 0, len(s)
    while start < end and ord(s[start]) in STR_WHITE_SPACE:
        start += 1
    while end > start and ord(s[end - 1]) in STR_WHITE_SPACE:
        end -= 1
    t = s[start:end]
    if t == "":
        return 0
    if t[:2] in _PREFIXES:
        radix = _PREFIXES[t[:2]]
        body = t[2:]
        alphabet = R.DIGITS[:radix]
        if body and all(c.lower() in alphabet for c in body):
            return int(body, radix)
        return None
    sign = 1
    if t[0] in "+-":
        sign = -1 if t[0] == "-" else 1
        t = t[1:]
    if t and all("0" <= c <= "9" for c in t):
        return sign * int(t)
    return None


def bigint_to_number(value: int) -> int:
    return double(value)


def number_to_big_int(bits: int) -> int | None:
    """NumberToBigInt: the BigInt, or None where it throws a RangeError."""
    if not is_integral(bits):
        return None
    return int(R.to_fraction(bits))


def compare_bigint_number(x: int, bits: int) -> int | None:
    """The order of a BigInt and a Number: -1, 0 or 1, or None where the Number is NaN."""
    if R.is_nan(bits):
        return None
    if R.is_infinite(bits):
        return 1 if R.is_negative(bits) else -1
    y = R.to_fraction(bits)
    return (x > y) - (x < y)


def bit_width(value: int) -> int:
    """The width Broiler.VM's BigInt policy bounds (System.Numerics.BigInteger.GetBitLength): the
    shortest two's-complement form without its sign bit."""
    return value.bit_length() if value >= 0 else (-value - 1).bit_length()
