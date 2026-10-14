"""The text area's reference computation: the specification's algorithms, written from its text.

Milestone SJL-5 of the shared JavaScript libraries programme moves the text algorithms into
`Broiler.ECMAScript.Algorithms`. This module is the corpus's independent computation of their expected
results (`ref:textref@1`): every function below follows the clause it names, step by step, and no result
is taken from either engine. The base64 and hex codecs follow the clauses the Uint8Array base64
proposal added to ECMA-262, and `JSON.rawJSON` the JSON.parse source text access proposal's.

**A JavaScript string is a Python `str` of UTF-16 code units**: every character is below U+10000, and a
code point above U+FFFF is two characters, its surrogate pair. `units` and `from_units` convert at the
edges; nothing else in this module holds a character above U+FFFF.

Outcomes that the specification makes an abrupt completion are returned as `Error(name)`; a function's
caller decides what the vector says.
"""

from __future__ import annotations

import json
from dataclasses import dataclass, field
from fractions import Fraction

import numref as R

STR_WHITE_SPACE = frozenset(chr(c) for c in R.STR_WHITE_SPACE)
ASCII_WHITE_SPACE = frozenset("\t\n\f\r ")  # the proposal's SkipAsciiWhitespace set
BASE64_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"
HEX_DIGITS = frozenset("0123456789abcdefABCDEF")


@dataclass(frozen=True)
class Error:
    """An abrupt completion: the name of the error's constructor."""

    name: str


def units(text: str) -> str:
    """A Python string of code points as the JavaScript string of its UTF-16 code units."""
    out = []
    for ch in text:
        code = ord(ch)
        if code > 0xFFFF:
            code -= 0x10000
            out.append(chr(0xD800 + (code >> 10)))
            out.append(chr(0xDC00 + (code & 0x3FF)))
        else:
            out.append(ch)
    return "".join(out)


def is_leading(unit: str) -> bool:
    return 0xD800 <= ord(unit) <= 0xDBFF


def is_trailing(unit: str) -> bool:
    return 0xDC00 <= ord(unit) <= 0xDFFF


# --------------------------------------------------------------------------------------------------
# Whitespace and UTF-16: 11.1.4 CodePointAt, 6.1.4 IsStringWellFormedUnicode, 22.1.3.31 toWellFormed,
# 22.1.3.32.1 TrimString, 22.2.7.3 AdvanceStringIndex.


@dataclass(frozen=True)
class CodePoint:
    code_point: int
    code_unit_count: int
    is_unpaired_surrogate: bool


def code_point_at(string: str, position: int) -> CodePoint:
    """11.1.4 CodePointAt (string, position)."""
    size = len(string)
    assert 0 <= position < size
    first = ord(string[position])
    cp = first
    if not (0xD800 <= first <= 0xDFFF):
        return CodePoint(cp, 1, False)
    if 0xDC00 <= first <= 0xDFFF or position + 1 == size:
        return CodePoint(cp, 1, True)
    second = ord(string[position + 1])
    if not (0xDC00 <= second <= 0xDFFF):
        return CodePoint(cp, 1, True)
    cp = (first - 0xD800) * 0x400 + (second - 0xDC00) + 0x10000
    return CodePoint(cp, 2, False)


def string_to_code_points(string: str) -> list[CodePoint]:
    """6.1.4.1 StringToCodePoints, keeping each code point's record."""
    out = []
    position = 0
    while position < len(string):
        record = code_point_at(string, position)
        out.append(record)
        position += record.code_unit_count
    return out


def is_string_well_formed_unicode(string: str) -> bool:
    """6.1.4 IsStringWellFormedUnicode."""
    return not any(record.is_unpaired_surrogate for record in string_to_code_points(string))


def to_well_formed(string: str) -> str:
    """22.1.3.31 String.prototype.toWellFormed, steps 4 to 7."""
    out = []
    position = 0
    while position < len(string):
        record = code_point_at(string, position)
        if record.is_unpaired_surrogate:
            out.append("�")
        else:
            out.append(string[position:position + record.code_unit_count])
        position += record.code_unit_count
    return "".join(out)


def trim_string(string: str, where: str) -> str:
    """22.1.3.32.1 TrimString (string, where), where is "start", "end" or "start+end"."""
    start, end = 0, len(string)
    if where in ("start", "start+end"):
        while start < end and string[start] in STR_WHITE_SPACE:
            start += 1
    if where in ("end", "start+end"):
        while end > start and string[end - 1] in STR_WHITE_SPACE:
            end -= 1
    return string[start:end]


def advance_string_index(string: str, index: int, unicode: bool) -> int:
    """22.2.7.3 AdvanceStringIndex (S, index, unicode)."""
    assert 0 <= index <= 2 ** 53 - 1
    if not unicode:
        return index + 1
    if index + 1 >= len(string):
        return index + 1
    return index + code_point_at(string, index).code_unit_count


# --------------------------------------------------------------------------------------------------
# Padding and indices: 22.1.3.17.2 StringPad, and the relative-index pattern of 22.1.3.22 step 4.


def string_pad(string: str, max_length: int, fill_string: str, placement: str) -> str:
    """22.1.3.17.2 StringPad (S, maxLength, fillString, placement); maxLength is ToLength's result."""
    string_length = len(string)
    if max_length <= string_length:
        return string
    if fill_string == "":
        return string
    fill_len = max_length - string_length
    repeats = -(-fill_len // len(fill_string))
    truncated = (fill_string * repeats)[:fill_len]
    return truncated + string if placement == "start" else string + truncated


def relative_index(relative: float | int, length: int) -> int:
    """The relative-index pattern: relative is already ToIntegerOrInfinity's result, possibly infinite."""
    if relative == float("-inf"):
        return 0
    if relative < 0:
        return max(length + int(relative), 0)
    if relative == float("inf"):
        return length
    return min(int(relative), length)


# --------------------------------------------------------------------------------------------------
# Substitution templates: 22.1.3.19.1 GetSubstitution.


def get_substitution(matched: str, string: str, position: int, captures: list[str | None],
                     named_captures: dict[str, str | None] | None, template: str) -> str:
    """22.1.3.19.1 GetSubstitution. A capture is None for undefined. named_captures is None for
    undefined, else a plain mapping read with Get (an absent name reads undefined)."""
    string_length = len(string)
    assert position <= string_length
    result = []
    remainder = template
    while remainder:
        if remainder.startswith("$$"):
            ref, replacement = "$$", "$"
        elif remainder.startswith("$`"):
            ref, replacement = "$`", string[:position]
        elif remainder.startswith("$&"):
            ref, replacement = "$&", matched
        elif remainder.startswith("$'"):
            tail = position + len(matched)
            ref, replacement = "$'", string[min(tail, string_length):]
        elif len(remainder) >= 2 and remainder[0] == "$" and remainder[1] in "0123456789":
            digit_count = 2 if len(remainder) >= 3 and remainder[2] in "0123456789" else 1
            index = int(remainder[1:1 + digit_count])
            capture_len = len(captures)
            if index > capture_len and digit_count == 2:
                digit_count = 1
                index = int(remainder[1:2])
            ref = remainder[:1 + digit_count]
            if 1 <= index <= capture_len:
                capture = captures[index - 1]
                replacement = "" if capture is None else capture
            else:
                replacement = ref
        elif remainder.startswith("$<"):
            gt = remainder.find(">")
            if gt == -1 or named_captures is None:
                ref, replacement = "$<", "$<"
            else:
                ref = remainder[:gt + 1]
                group_name = remainder[2:gt]
                capture = named_captures.get(group_name)
                replacement = "" if capture is None else capture
        else:
            ref, replacement = remainder[0], remainder[0]
        remainder = remainder[len(ref):]
        result.append(replacement)
    return "".join(result)


# --------------------------------------------------------------------------------------------------
# URI: 19.2.6.5 Encode, 19.2.6.6 Decode, B.2.1.1 escape, B.2.1.2 unescape.

URI_ALPHA = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"
URI_DIGITS = "0123456789"
URI_MARK = "-_.!~*'()"
URI_RESERVED = ";/?:@&=+$,"
UNESCAPED_URI_COMPONENT = URI_ALPHA + URI_DIGITS + URI_MARK
UNESCAPED_URI = UNESCAPED_URI_COMPONENT + URI_RESERVED + "#"
PRESERVE_URI = URI_RESERVED + "#"


def encode(string: str, extra_unescaped: str) -> str | Error:
    """19.2.6.5 Encode (string, extraUnescaped)."""
    unescaped = URI_ALPHA + URI_DIGITS + URI_MARK + extra_unescaped
    out = []
    k = 0
    while k < len(string):
        c = string[k]
        if c in unescaped:
            out.append(c)
            k += 1
            continue
        record = code_point_at(string, k)
        if record.is_unpaired_surrogate:
            return Error("URIError")
        k += record.code_unit_count
        for octet in chr(record.code_point).encode("utf-8"):
            out.append("%%%02X" % octet)
    return "".join(out)


def _parse_hex_octet(string: str, position: int) -> int | None:
    pair = string[position:position + 2]
    if len(pair) != 2 or any(ch not in HEX_DIGITS for ch in pair):
        return None
    return int(pair, 16)


def decode(string: str, preserve_escape_set: str) -> str | Error:
    """19.2.6.6 Decode (string, preserveEscapeSet)."""
    length = len(string)
    out = []
    k = 0
    while k < length:
        c = string[k]
        if c != "%":
            out.append(c)
            k += 1
            continue
        if k + 3 > length:
            return Error("URIError")
        escape = string[k:k + 3]
        b = _parse_hex_octet(string, k + 1)
        if b is None:
            return Error("URIError")
        k += 2
        n = 0
        while n < 8 and b & (0x80 >> n):
            n += 1
        if n == 0:
            ascii_char = chr(b)
            out.append(escape if ascii_char in preserve_escape_set else ascii_char)
        else:
            if n == 1 or n > 4:
                return Error("URIError")
            octets = [b]
            j = 1
            while j < n:
                k += 1
                if k + 3 > length:
                    return Error("URIError")
                if string[k] != "%":
                    return Error("URIError")
                continuation = _parse_hex_octet(string, k + 1)
                if continuation is None:
                    return Error("URIError")
                octets.append(continuation)
                k += 2
                j += 1
            try:
                decoded = bytes(octets).decode("utf-8", "strict")
            except UnicodeDecodeError:
                return Error("URIError")
            # A valid UTF-8 encoding of one code point: strict decoding refuses an overlong form, an
            # encoded surrogate and a value above U+10FFFF, and n bytes decode to exactly one.
            assert len(decoded) == 1
            out.append(units(decoded))
        k += 1
    return "".join(out)


ESCAPE_UNESCAPED = frozenset(URI_ALPHA + URI_DIGITS + "@*_+-./")


def escape(string: str) -> str:
    """B.2.1.1 escape (string), steps 3 to 6."""
    out = []
    for c in string:
        n = ord(c)
        if c in ESCAPE_UNESCAPED:
            out.append(c)
        elif n < 256:
            out.append("%%%02X" % n)
        else:
            out.append("%%u%04X" % n)
    return "".join(out)


def unescape(string: str) -> str:
    """B.2.1.2 unescape (string), steps 3 to 6."""
    length = len(string)
    out = []
    k = 0
    while k < length:
        c = string[k]
        if c == "%":
            hex_digits = ""
            optional_advance = 0
            if k + 5 < length and string[k + 1] == "u":
                hex_digits = string[k + 2:k + 6]
                optional_advance = 5
            elif k + 3 <= length:
                hex_digits = string[k + 1:k + 3]
                optional_advance = 2
            if hex_digits and all(ch in HEX_DIGITS for ch in hex_digits):
                c = chr(int(hex_digits, 16))
                k += optional_advance
        out.append(c)
        k += 1
    return "".join(out)


# --------------------------------------------------------------------------------------------------
# Base64 and hex: the Uint8Array base64 clauses (FromBase64, DecodeBase64Chunk, SkipAsciiWhitespace,
# FromHex, and the encoders of toBase64 and toHex).


@dataclass
class DecodeResult:
    read: int
    bytes: list[int] = field(default_factory=list)
    error: str | None = None


def skip_ascii_whitespace(string: str, index: int) -> int:
    while index < len(string) and string[index] in ASCII_WHITE_SPACE:
        index += 1
    return index


def decode_base64_chunk(chunk: str, throw_on_extra_bits: bool = False) -> list[int] | Error:
    chunk_length = len(chunk)
    if chunk_length == 2:
        chunk += "AA"
    elif chunk_length == 3:
        chunk += "A"
    value = 0
    for ch in chunk:
        value = (value << 6) | BASE64_ALPHABET.index(ch)
    sequence = [(value >> 16) & 0xFF, (value >> 8) & 0xFF, value & 0xFF]
    if chunk_length == 2:
        if throw_on_extra_bits and sequence[1] != 0:
            return Error("SyntaxError")
        return sequence[:1]
    if chunk_length == 3:
        if throw_on_extra_bits and sequence[2] != 0:
            return Error("SyntaxError")
        return sequence[:2]
    return sequence


def from_base64(string: str, alphabet: str, last_chunk_handling: str,
                max_length: int = 2 ** 53 - 1) -> DecodeResult:
    """FromBase64 (string, alphabet, lastChunkHandling [, maxLength])."""
    if max_length == 0:
        return DecodeResult(0)
    read = 0
    out: list[int] = []
    chunk = ""
    index = 0
    length = len(string)
    while True:
        index = skip_ascii_whitespace(string, index)
        if index == length:
            if chunk:
                if last_chunk_handling == "stop-before-partial":
                    return DecodeResult(read, out)
                if last_chunk_handling == "loose":
                    if len(chunk) == 1:
                        return DecodeResult(read, out, "SyntaxError")
                    out.extend(decode_base64_chunk(chunk, False))
                else:
                    return DecodeResult(read, out, "SyntaxError")
            return DecodeResult(length, out)
        char = string[index]
        index += 1
        if char == "=":
            if len(chunk) < 2:
                return DecodeResult(read, out, "SyntaxError")
            index = skip_ascii_whitespace(string, index)
            if len(chunk) == 2:
                if index == length:
                    if last_chunk_handling == "stop-before-partial":
                        return DecodeResult(read, out)
                    return DecodeResult(read, out, "SyntaxError")
                char = string[index]
                if char == "=":
                    index = skip_ascii_whitespace(string, index + 1)
            if index < length:
                return DecodeResult(read, out, "SyntaxError")
            decoded = decode_base64_chunk(chunk, last_chunk_handling == "strict")
            if isinstance(decoded, Error):
                return DecodeResult(read, out, decoded.name)
            out.extend(decoded)
            return DecodeResult(length, out)
        if alphabet == "base64url":
            if char in "+/":
                return DecodeResult(read, out, "SyntaxError")
            if char == "-":
                char = "+"
            elif char == "_":
                char = "/"
        if char not in BASE64_ALPHABET:
            return DecodeResult(read, out, "SyntaxError")
        remaining = max_length - len(out)
        if (remaining == 1 and len(chunk) == 2) or (remaining == 2 and len(chunk) == 3):
            return DecodeResult(read, out)
        chunk += char
        if len(chunk) == 4:
            out.extend(decode_base64_chunk(chunk))
            chunk = ""
            read = index
            if len(out) == max_length:
                return DecodeResult(read, out)


def from_hex(string: str, max_length: int = 2 ** 53 - 1) -> DecodeResult:
    """FromHex (string [, maxLength])."""
    length = len(string)
    out: list[int] = []
    read = 0
    if length % 2 != 0:
        return DecodeResult(0, [], "SyntaxError")
    while read < length and len(out) < max_length:
        hexits = string[read:read + 2]
        if any(ch not in HEX_DIGITS for ch in hexits):
            return DecodeResult(read, out, "SyntaxError")
        read += 2
        out.append(int(hexits, 16))
    return DecodeResult(read, out)


def to_base64(data: bytes, alphabet: str, omit_padding: bool) -> str:
    """Uint8Array.prototype.toBase64's encoding, step by step over 3-byte groups."""
    table = BASE64_ALPHABET if alphabet == "base64" else BASE64_ALPHABET[:62] + "-_"
    out = []
    for start in range(0, len(data), 3):
        group = data[start:start + 3]
        value = int.from_bytes(group + bytes(3 - len(group)), "big")
        characters = [table[(value >> shift) & 0x3F] for shift in (18, 12, 6, 0)]
        used = len(group) + 1
        out.append("".join(characters[:used]))
        if not omit_padding:
            out.append("=" * (4 - used))
    return "".join(out)


def to_hex(data: bytes) -> str:
    return "".join("%02x" % b for b in data)


# --------------------------------------------------------------------------------------------------
# JSON: 25.5.1 JSON.parse's grammar (ECMA-404 through the clause), 25.5.2.3 QuoteJSONString, the gap
# of 25.5.2 JSON.stringify steps 6 to 8, and SerializeJSONObject and SerializeJSONArray over plain values.

JSON_WHITE_SPACE = frozenset("\t\n\r ")


class JsonSyntaxError(Exception):
    pass


class JsonDepthError(Exception):
    """A value deeper than the caller's limit, found before anything after it is read (the reference
    for an engine's depth policy; the specification itself has no limit)."""


@dataclass
class JsonValue:
    """A parsed value: kind is "object", "array", "string", "number", "true", "false" or "null"."""

    kind: str
    value: object = None


def json_depth(text: str) -> int:
    """The deepest nesting of a text the grammar admits: 0 for a primitive, 1 for [] or {}."""
    depth = deepest = 0
    in_string = escaped = False
    for ch in text:
        if in_string:
            if escaped:
                escaped = False
            elif ch == "\\":
                escaped = True
            elif ch == '"':
                in_string = False
        elif ch == '"':
            in_string = True
        elif ch in "[{":
            depth += 1
            deepest = max(deepest, depth)
        elif ch in "]}":
            depth -= 1
    return deepest


def json_parse(text: str, max_depth: int | None = None) -> JsonValue:
    """Parses a JavaScript string as JSON text, or raises JsonSyntaxError. Strings keep every code unit,
    lone surrogates escaped or raw included; numbers are converted to the nearest double, -0 and
    overflow to infinity included; a repeated key keeps its first position and its last value.

    With max_depth, a value deeper than it (the document is depth 0, a member or element one deeper
    than its container) raises JsonDepthError when the reader reaches it, after the white space before
    it: an engine's depth policy, which the bundle sjl-5-001 classifier judges refusals by."""
    position = 0
    length = len(text)

    def skip() -> None:
        nonlocal position
        while position < length and text[position] in JSON_WHITE_SPACE:
            position += 1

    def expect(literal: str) -> None:
        nonlocal position
        if text[position:position + len(literal)] != literal:
            raise JsonSyntaxError(position)
        position += len(literal)

    def parse_string() -> str:
        nonlocal position
        expect('"')
        out = []
        while True:
            if position >= length:
                raise JsonSyntaxError(position)
            ch = text[position]
            if ch == '"':
                position += 1
                return "".join(out)
            if ord(ch) < 0x20:
                raise JsonSyntaxError(position)
            if ch == "\\":
                if position + 1 >= length:
                    raise JsonSyntaxError(position)
                escape_char = text[position + 1]
                simple = {'"': '"', "\\": "\\", "/": "/", "b": "\b", "f": "\f", "n": "\n", "r": "\r", "t": "\t"}
                if escape_char in simple:
                    out.append(simple[escape_char])
                    position += 2
                elif escape_char == "u":
                    digits = text[position + 2:position + 6]
                    if len(digits) != 4 or any(d not in HEX_DIGITS for d in digits):
                        raise JsonSyntaxError(position)
                    out.append(chr(int(digits, 16)))
                    position += 6
                else:
                    raise JsonSyntaxError(position)
            else:
                out.append(ch)
                position += 1

    def parse_number() -> int:
        nonlocal position
        start = position
        if position < length and text[position] == "-":
            position += 1
        if position < length and text[position] == "0":
            position += 1
        elif position < length and "1" <= text[position] <= "9":
            while position < length and text[position].isdigit() and text[position] < "\u0080":
                position += 1
        else:
            raise JsonSyntaxError(position)
        if position < length and text[position] == ".":
            position += 1
            digits_start = position
            while position < length and "0" <= text[position] <= "9":
                position += 1
            if position == digits_start:
                raise JsonSyntaxError(position)
        if position < length and text[position] in "eE":
            position += 1
            if position < length and text[position] in "+-":
                position += 1
            digits_start = position
            while position < length and "0" <= text[position] <= "9":
                position += 1
            if position == digits_start:
                raise JsonSyntaxError(position)
        literal = text[start:position]
        negative = literal.startswith("-")
        magnitude = Fraction(literal.lstrip("-").replace("E", "e").split("e")[0]) * (
            Fraction(10) ** int(literal.replace("E", "e").split("e")[1]) if "e" in literal.replace("E", "e") else 1)
        bits = R.from_fraction(magnitude)
        if negative:
            bits |= R.SIGN_BIT
        return bits

    def parse_value(depth: int = 0) -> JsonValue:
        nonlocal position
        skip()
        if max_depth is not None and depth > max_depth:
            raise JsonDepthError(position)
        if position >= length:
            raise JsonSyntaxError(position)
        ch = text[position]
        if ch == "{":
            position += 1
            entries: dict[str, JsonValue] = {}
            skip()
            if position < length and text[position] == "}":
                position += 1
                return JsonValue("object", entries)
            while True:
                skip()
                key = parse_string()
                skip()
                expect(":")
                value = parse_value(depth + 1)
                entries[key] = value  # a repeated key keeps its first position and its last value
                skip()
                if position < length and text[position] == ",":
                    position += 1
                    continue
                expect("}")
                return JsonValue("object", entries)
        if ch == "[":
            position += 1
            items: list[JsonValue] = []
            skip()
            if position < length and text[position] == "]":
                position += 1
                return JsonValue("array", items)
            while True:
                items.append(parse_value(depth + 1))
                skip()
                if position < length and text[position] == ",":
                    position += 1
                    continue
                expect("]")
                return JsonValue("array", items)
        if ch == '"':
            return JsonValue("string", parse_string())
        if ch == "-" or "0" <= ch <= "9":
            return JsonValue("number", parse_number())
        for literal in ("true", "false", "null"):
            if text.startswith(literal, position):
                position += len(literal)
                return JsonValue(literal)
        raise JsonSyntaxError(position)

    value = parse_value()
    skip()
    if position != length:
        raise JsonSyntaxError(position)
    return value


def quote_json_string(value: str) -> str:
    """25.5.2.3 QuoteJSONString (value)."""
    table = {0x08: "\\b", 0x09: "\\t", 0x0A: "\\n", 0x0C: "\\f", 0x0D: "\\r", 0x22: '\\"', 0x5C: "\\\\"}
    out = ['"']
    for record in string_to_code_points(value):
        c = record.code_point
        if c in table:
            out.append(table[c])
        elif c < 0x20 or record.is_unpaired_surrogate:
            out.append("\\u%04x" % c)
        elif c > 0xFFFF:
            out.append(units(chr(c)))
        else:
            out.append(chr(c))
    out.append('"')
    return "".join(out)


def gap_from_number(space: float) -> str:
    """JSON.stringify step 8 for a Number space (ToIntegerOrInfinity already applied to a finite one)."""
    space_mv = 10 if space == float("inf") else min(10, int(space)) if space == space else 0
    return " " * space_mv if space_mv >= 1 else ""


def gap_from_string(space: str) -> str:
    return space[:10]


def number_texts(bits: int) -> tuple[str, ...]:
    """Number::toString(x, 10), which JSON.stringify writes for a finite number: every text the clause
    admits, the least significant digit being "not necessarily uniquely determined"."""
    return R.number_to_string(bits, 10).values


def serialize(value: JsonValue, gap: str, indent: str = "") -> list[str]:
    """SerializeJSONProperty over a parsed value, with SerializeJSONObject and SerializeJSONArray: every
    text the clauses admit, in a stable order (one per choice of each number's admitted digits)."""
    if value.kind in ("true", "false", "null"):
        return [value.kind]
    if value.kind == "string":
        return [quote_json_string(value.value)]
    if value.kind == "number":
        bits = value.value
        if not R.is_finite(bits):
            return ["null"]
        return list(number_texts(bits & ~R.SIGN_BIT if R.is_zero(bits) else bits))
    stepback = indent
    inner = indent + gap
    if value.kind == "array":
        choices = [serialize(item, gap, inner) for item in value.value]
        open_, close = "[", "]"
    else:
        choices = [[quote_json_string(key) + ":" + (" " if gap else "") + text for text in serialize(item, gap, inner)]
                   for key, item in value.value.items()]
        open_, close = "{", "}"
    if not choices:
        return [open_ + close]
    out = []
    for parts in _product(choices):
        if gap == "":
            out.append(open_ + ",".join(parts) + close)
        else:
            out.append(open_ + "\n" + inner + (",\n" + inner).join(parts) + "\n" + stepback + close)
    return out


def _product(choices: list[list[str]]) -> list[list[str]]:
    result: list[list[str]] = [[]]
    for options in choices:
        result = [prefix + [option] for prefix in result for option in options]
        assert len(result) <= 16, "a document whose numbers admit too many texts"
    return result
