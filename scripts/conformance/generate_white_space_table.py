"""Write the Space_Separator table of Broiler.ECMAScript.Algorithms' string white-space set.

    python scripts/conformance/generate_white_space_table.py [--proplist <UCD 17.0.0 PropList.txt>] [--out <file>]

StrWhiteSpaceChar (ECMA-262 7.1.4.1.1) is WhiteSpace or LineTerminator, and WhiteSpace takes every
code point of general category Zs (Space_Separator) from the Unicode data. The library never reads the
runtime's Unicode data for it; it reads a table this script writes, from the same set the number
vectors' reference computation uses (numref.SPACE_SEPARATORS), which is re-derived from the pinned
PropList.txt and refused when the two differ, exactly as generate_number_vectors.py does.

The output is the table's body: one line per range of consecutive code points, each as four bytes,
the first and last code unit big-endian. It is the text between the BEGIN and END markers of
Broiler.ECMAScript/Broiler.ECMAScript.Algorithms/Numbers/StrWhiteSpace.cs, which rule SL7 compares
with this script's output byte for byte. Without --out it is printed.
"""

from __future__ import annotations

import argparse
import hashlib
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import numref as R  # noqa: E402

PROPLIST_SHA256 = "130dcddcaadaf071008bdfce1e7743e04fdfbc910886f017d9f9ac931d8c64dd"

# The names the table's comments carry, from Unicode 17.0.0's UnicodeData.txt; a name never changes
# (the Name stability policy), so they are fixed here rather than read from the runtime's data.
NAMES = {
    0x0020: "SPACE", 0x00A0: "NO-BREAK SPACE", 0x1680: "OGHAM SPACE MARK", 0x2000: "EN QUAD",
    0x200A: "HAIR SPACE", 0x202F: "NARROW NO-BREAK SPACE", 0x205F: "MEDIUM MATHEMATICAL SPACE",
    0x3000: "IDEOGRAPHIC SPACE",
}


def ranges(code_points: frozenset[int]) -> list[tuple[int, int]]:
    out: list[tuple[int, int]] = []
    for cp in sorted(code_points):
        if out and out[-1][1] == cp - 1:
            out[-1] = (out[-1][0], cp)
        else:
            out.append((cp, cp))
    return out


def body() -> str:
    lines = []
    for first, last in ranges(R.SPACE_SEPARATORS):
        if first > 0xFFFF:
            raise SystemExit("a Space_Separator outside the BMP needs a surrogate-aware table")
        name = NAMES[first] if first == last else NAMES[first] + " to " + NAMES[last]
        span = f"U+{first:04X}" if first == last else f"U+{first:04X}..U+{last:04X}"
        lines.append(
            f"        0x{first >> 8:02X}, 0x{first & 0xFF:02X}, 0x{last >> 8:02X}, 0x{last & 0xFF:02X}, // {span} {name}\n")
    return "".join(lines)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--proplist", help="the pinned UCD 17.0.0 PropList.txt, to re-derive the Zs set from")
    parser.add_argument("--out", type=Path)
    args = parser.parse_args(argv)
    if args.proplist:
        digest = hashlib.sha256(Path(args.proplist).read_bytes()).hexdigest()
        if digest != PROPLIST_SHA256:
            print(f"PropList.txt does not match its pin: {digest}", file=sys.stderr)
            return 2
        if R.zs_from_proplist(args.proplist) != R.SPACE_SEPARATORS:
            print("the Zs set derived from PropList.txt differs from numref.SPACE_SEPARATORS", file=sys.stderr)
            return 2
    text = body()
    if args.out:
        args.out.write_text(text, encoding="utf-8", newline="\n")
    else:
        sys.stdout.write(text)
    return 0


if __name__ == "__main__":
    sys.exit(main())
