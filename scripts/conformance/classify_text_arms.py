#!/usr/bin/env python3
"""Classify the differences of bundle sjl-5-001's reference-arm comparison against the reference.

    python3 scripts/conformance/classify_text_arms.py --run <comparison output directory> \
        --population <population directory> [--examples 20]

The comparison (Broiler.ECMAScript.Algorithms.Tests, run as `compare-text`) classifies nothing: it lists
every input on which a variant of an arm differs from S called as that arm's engine calls it. This script is
the owner's classification. It was written before the judged run, with the rule's revision of 2026-10-14
(docs/evidence/sjl-5-001/reference-arms-revised-2026-10-14.md), and is applied mechanically, so that every
class can be checked against the reference value line by line. The arbiter is textref.py, the corpus's
reference computation, written from the specification's text; the agreement of the arms decides nothing.

For each difference of one variant from S:

- the reference computes the specification's result for the input, as the variant projects it (a decode's
  read count, bytes and error; arm J's fromHex as its bytes or its error; a parse as its structure), under
  the policy S ran at (Broiler.VM's string ceiling and JSON depth against arm V; no limit the population
  reaches against arm J);
- a variant whose result and S's carry a charge (arm V's) is judged on its value first; when the values
  agree and the charges do not, the difference is unclassified, because the rule makes a charge of S that
  differs from arm V's class (c);
- class (a), a correction: S's result is the reference's and the arm's is not. It is listed with the engine
  it corrects and, where the area verdict's correspondence names it, its row;
- class (b), implementation-defined: both results are admitted, a refusal past the arm's own maximum length
  or depth where S under that policy refuses at the same point. The comparison runs S under each arm's
  policy, so this class is expected to be empty; a class (b) difference no vector covers is unclassified;
- otherwise unclassified: S's result is not the reference's (class (c), a defect of S, which is fixed and the
  whole comparison run again), the reference cannot judge the input, or the variant is S under another
  maximum length (a defect of S whatever its value).

Outputs, in the run directory: classification.tsv.gz holds every difference with its input, S's result, the
arm's result, the reference, the class and the reason; a field longer than 256 characters is shown by its
length. report.md gives per operation, variant, class and reason the count and the first --examples
differences in full. classification.json gives the counts and the SHA-256 of both listings' uncompressed
text.
"""

from __future__ import annotations

import argparse
import gzip
import hashlib
import json
import re
import sys
from collections import Counter, defaultdict
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import textref as T  # noqa: E402

sys.setrecursionlimit(20000)

SHOWN = 256
VM_CEILING = 1 << 24
J_CEILING = (1 << 31) - 1
DOTNET_STRING = 0x3FFFFFDF
VM_JSON_DEPTH = 512
VM_MODULE_DEPTH = 1024
MODES = ["loose", "strict", "stop-before-partial"]
COUNTS = [0, 1, 2, 9, 10, 11, 99]
URI_RESERVED_AND_HASH = ";/?:@&=+$,#"


# --------------------------------------------------------------------------------------------------
# The comparison's rendering, mirrored.

def escape(text: str) -> str:
    return "".join(c if "!" <= c <= "~" and c != "\\" else f"\\u{ord(c):04X}" for c in text)


def unescape(text: str) -> str:
    return re.sub(r"\\u([0-9A-F]{4})", lambda m: chr(int(m.group(1), 16)), text)


def string(text: str) -> str:
    return "str:" + escape(text)


def number(value: int | float) -> str:
    if value == float("inf"):
        return "num:+Infinity"
    if value == float("-inf"):
        return "num:-Infinity"
    return f"num:{int(value)}"


def boolean(value: bool) -> str:
    return "bool:true" if value else "bool:false"


def byte_list(data) -> str:
    return "bytes:" + (bytes(data).hex() if len(data) else "-")


def decoded(read: int, data, error: str | None) -> str:
    return f"read:{read}|{byte_list(data)}|" + ("ok" if error is None else "error:" + error)


def f64(bits: int) -> str:
    return f"f64:{bits:016X}"


def is_array_index(key: str) -> bool:
    return key.isascii() and key.isdigit() and str(int(key)) == key and int(key) < 0xFFFFFFFF


def json_structure(value: T.JsonValue) -> str:
    if value.kind in ("null", "true", "false"):
        return value.kind
    if value.kind == "number":
        return f64(value.value)
    if value.kind == "string":
        return string(value.value)
    if value.kind == "array":
        return "[" + ",".join(json_structure(v) for v in value.value) + "]"
    items = list(value.value.items())
    ordered = sorted((kv for kv in items if is_array_index(kv[0])), key=lambda kv: int(kv[0])) + [kv for kv in items if not is_array_index(kv[0])]
    return "{" + ",".join(string(k) + ":" + json_structure(v) for k, v in ordered) + "}"


def field(listed: str) -> str | None:
    """A listed field's text, or None when the comparison shortened it to its length and digest."""
    return None if listed.startswith("len:") and ":sha256:" in listed else unescape(listed)


def charged(listed: str) -> tuple[str, str | None]:
    source = listed if field(listed) is None else field(listed)
    if "|charge:" in source:
        value, _, charge = source.rpartition("|charge:")
        return value, charge
    return source, None


def shown(text: str | None) -> str:
    if text is None:
        return "-"
    return text if len(text) <= SHOWN else f"({len(text)} characters)"


# --------------------------------------------------------------------------------------------------
# The inputs, rebuilt from a listed row as the comparison expanded them.

class Inputs:
    def __init__(self, population: Path) -> None:
        def lines(name: str) -> list[str]:
            with gzip.open(population / f"{name}.txt.gz", "rt", encoding="utf-8", newline="\n") as stream:
                return stream.read().split("\n")[:-1]
        self.fillers = [unescape(line) for line in lines("P1F")]

    def text(self, cls: str, raw: str) -> str:
        if cls == "U1C":
            point = int(raw, 16)
            return chr(point) if point <= 0xFFFF else T.units(chr(point))
        return unescape(raw)


def escaped_form(text: str) -> str:
    return "".join(f"%{ord(u):02X}" if ord(u) < 256 else f"%u{ord(u):04X}" for u in text)


# --------------------------------------------------------------------------------------------------
# The reference, per operation and variant.

def ref_pad(text: str, target: int, filler: str, placement: str, ceiling: int) -> str:
    if target <= len(text) or filler == "":
        return string(text)
    if target > ceiling or target > DOTNET_STRING:
        return "error:RangeError"
    return string(T.string_pad(text, target, filler, placement))


def substitution_inputs(detail: str):
    count, state, groups, position = detail.split("/")
    count = int(count)
    states = {"-": 0, "defined": 0, "undefined": 1, "empty": 2}
    s = states[state]
    captures = [("c" + str(n)) if s == 0 else (None if s == 1 else "") for n in range(1, count + 1)]
    named = None if groups == "no-groups" else {"a": "ga"}
    subject = {"start": "abzw", "middle": "xyabzw", "end": "xyab"}[position]
    at = 0 if position == "start" else 2
    return captures, named, subject, at


def reference(operation: str, variant: str, cls: str, detail: str, text: str, inputs: Inputs) -> str | None:
    arm = variant.split(":", 1)[0]
    if operation == "IsStringWellFormedUnicode":
        return boolean(T.is_string_well_formed_unicode(text))
    if operation == "toWellFormed":
        return string(T.to_well_formed(text))
    if operation == "CodePointAt":
        at = int(detail)
        return "undefined" if at < 0 or at >= len(text) else number(T.code_point_at(text, at).code_point)
    if operation == "AdvanceStringIndex":
        at, _, unicode = detail.partition("/")
        return number(T.advance_string_index(text, int(at), unicode == "unicode"))
    if operation == "QuoteJSONString":
        return string(T.quote_json_string(text))
    if operation == "StringPad":
        target, filler, placement = detail.split("/")
        filler_text = inputs.fillers[int(filler[len("filler"):])]
        return ref_pad(text, int(target), filler_text, placement, VM_CEILING if arm == "V" else J_CEILING)
    if operation == "RelativeIndex":
        length, relative = text.split(" ")
        value = float(relative) if "Infinity" in relative else int(relative)
        return number(T.relative_index(value, int(length)))
    if operation == "GetSubstitution":
        captures, named, subject, at = substitution_inputs(detail)
        replaced = T.get_substitution("ab", subject, at, captures, named, text)
        if variant == "V:RegExpReplaceText":
            return string(subject[:at] + replaced + subject[at + 2:])
        return string(replaced)
    if operation.startswith("Encode "):
        result = T.encode(text, URI_RESERVED_AND_HASH if operation.endswith("encodeURI") else "")
        return "error:URIError" if isinstance(result, T.Error) else string(result)
    if operation.startswith("Decode "):
        result = T.decode(text, URI_RESERVED_AND_HASH if operation.endswith("decodeURI") else "")
        return "error:URIError" if isinstance(result, T.Error) else string(result)
    if operation == "escape":
        return string(T.escape(text))
    if operation == "unescape":
        return string(T.unescape(escaped_form(text) if detail == "escaped" else text))
    if operation == "FromBase64":
        alphabet, mode, destination = detail.split("/")
        limit = 2 ** 53 - 1 if destination == "unbounded" else int(destination)
        result = T.from_base64(text, alphabet, mode, limit)
        return decoded(result.read, result.bytes, result.error)
    if operation == "FromHex":
        limit = 2 ** 53 - 1 if detail == "unbounded" else int(detail)
        result = T.from_hex(text, limit)
        if arm == "V":
            return decoded(result.read, result.bytes, result.error)
        if detail == "unbounded":
            return "error:SyntaxError" if result.error else byte_list(result.bytes)
        destination = list(result.bytes) + [0] * (limit - len(result.bytes))
        if result.error:
            return "error:SyntaxError|" + byte_list(destination)
        return f"read:{result.read}|written:{len(result.bytes)}|" + byte_list(destination)
    if operation in ("ToBase64", "ToHex"):
        data = bytes.fromhex(text) if text != "-" else b""
        if operation == "ToHex":
            return string(T.to_hex(data))
        alphabet, _, omit = detail.partition("/")
        return string(T.to_base64(data, alphabet, omit == "omitPadding"))
    if operation == "JSON parse":
        if variant == "V:JsJsonModule.TryTranslate":
            try:
                T.json_parse(text, VM_MODULE_DEPTH)
                return "accepted"
            except T.JsonDepthError:
                return "refused:too-deep"
            except T.JsonSyntaxError:
                return "refused"
        try:
            return "json:" + json_structure(T.json_parse(text, VM_JSON_DEPTH if arm == "V" else None))
        except T.JsonDepthError:
            return "error:RangeError"
        except T.JsonSyntaxError:
            return "error:SyntaxError"
    if operation == "JSON gap":
        kind, _, rest = text.partition(" ")
        if kind == "n":
            space = float(rest)
            return string("" if space == float("-inf") else T.gap_from_number(space))
        return string(T.gap_from_string(unescape(rest)))
    return None


# --------------------------------------------------------------------------------------------------
# The correspondence's rows, where a correction is one it names.

def row_of(operation: str, variant: str, text: str, detail: str, arm_value: str, ref: str) -> str:
    if operation == "StringPad" and variant.startswith("V:") and arm_value == "error:RangeError":
        filler = detail.split("/")[1]
        return "P2" if filler == "filler0" else "P1"
    if operation == "GetSubstitution" and variant == "J:JSRegExp.Replace(string, string)" and "/groups/" in detail:
        return "S3"
    if operation == "FromHex" and variant.startswith("J:") and detail != "unbounded":
        return "B1"
    if operation == "JSON parse" and variant.startswith("J:"):
        if arm_value.startswith("exception:"):
            return "J3"
        if arm_value == "error:SyntaxError" and ref.startswith("json:"):
            if T.json_depth(text) > 64:
                return "J1"
            if "Infinity" in ref or re.search(r"f64:(7FF0|FFF0)000000000000", ref):
                return "J4"
            if re.search(r"\\u[dD][89abAB]|\\u[dD][c-fC-F]", text):
                return "J2"
        return "J1-J4"
    return "-"


def classify(row: list[str], inputs: Inputs) -> tuple[str, str, str]:
    operation, cls, _line, detail_listed, raw_listed, variant, s_listed, arm_listed = row
    detail = field(detail_listed) or detail_listed
    raw = field(raw_listed)
    if variant.startswith("S:"):
        return "(c)", "S under another maximum length differs from S under arm J's policy", "-"
    s_value, s_charge = charged(s_listed)
    arm_value, arm_charge = charged(arm_listed)
    if raw is None:
        return "unclassified", "the input was shortened in the listing", "-"
    if s_value == arm_value and s_charge != arm_charge:
        return "unclassified", f"charges differ: S {s_charge}, arm {arm_charge}", "-"
    text = raw if operation in ("RelativeIndex", "JSON gap", "ToBase64", "ToHex") else inputs.text(cls, raw)
    try:
        ref = reference(operation, variant, cls, detail, text, inputs)
    except Exception as exception:  # noqa: BLE001 - the reference failing to judge is a reason
        return "unclassified", f"the reference cannot judge the input: {type(exception).__name__}", "-"
    if ref is None:
        return "unclassified", "no reference for the operation", "-"
    if s_value == ref and arm_value != ref:
        arm = variant.split(":", 1)[0]
        engine = "Broiler.VM" if arm == "V" else "Broiler.JS"
        return "(a)", f"correction of {engine}: S is the reference's; " + row_of(operation, variant, text, detail, arm_value, ref), ref
    if s_value != ref:
        return "(c)", "S is not the reference's", ref
    return "unclassified", "S and the arm are both the reference's, yet differ", ref


# --------------------------------------------------------------------------------------------------

def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--run", required=True, type=Path)
    parser.add_argument("--population", required=True, type=Path)
    parser.add_argument("--examples", type=int, default=20)
    args = parser.parse_args(argv)
    inputs = Inputs(args.population)

    with gzip.open(args.run / "differences.tsv.gz", "rt", encoding="utf-8", newline="\n") as stream:
        lines = stream.read().split("\n")
    header, rows = lines[0], [line.split("\t") for line in lines[1:] if line]
    assert header == "operation\tclass\tline\tdetail\tinput\tvariant\tS\tresult", header

    out_lines = ["operation\tclass\tline\tdetail\tinput\tvariant\tS\tresult\treference\tverdict\treason"]
    counts: Counter = Counter()
    examples: dict = defaultdict(list)
    for row in rows:
        verdict, reason, ref = classify(row, inputs)
        out_lines.append("\t".join(row[:6] + [shown(row[6]), shown(row[7]), shown(escape(ref) if ref != "-" else "-"), verdict, reason]))
        key = (row[0], row[5], verdict, reason)
        counts[key] += 1
        if len(examples[key]) < args.examples:
            examples[key].append(row)

    text = "".join(line + "\n" for line in out_lines)
    with gzip.GzipFile(args.run / "classification.tsv.gz", "wb", compresslevel=9, mtime=0) as target:
        target.write(text.encode("utf-8"))
    differences_text = "".join(line + "\n" for line in lines if line)
    by_verdict = Counter()
    for (_, _, verdict, _), n in counts.items():
        by_verdict[verdict] += n
    summary = {
        "differences": len(rows),
        "verdicts": dict(sorted(by_verdict.items())),
        "classes": [{"operation": o, "variant": v, "verdict": c, "reason": r, "count": n} for (o, v, c, r), n in sorted(counts.items())],
        "differences.tsv": {"lines": len([line for line in lines if line]), "sha256": hashlib.sha256(differences_text.encode("utf-8")).hexdigest()},
        "classification.tsv": {"lines": len(out_lines), "sha256": hashlib.sha256(text.encode("utf-8")).hexdigest()},
    }
    (args.run / "classification.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")

    report = ["# Bundle sjl-5-001's reference-arm comparison, classified", "",
              f"{len(rows)} differences: " + ", ".join(f"{n} {v}" for v, n in sorted(by_verdict.items())) + ".", ""]
    for (operation, variant, verdict, reason), n in sorted(counts.items()):
        report.append(f"## {operation}, {variant}: {verdict}, {n}")
        report.append("")
        report.append(f"{reason}.")
        report.append("")
        for row in examples[(operation, variant, verdict, reason)]:
            report.append(f"- `{row[1]}:{row[2]}` {shown(row[3])} input `{shown(row[4])}`: S `{shown(row[6])}`, arm `{shown(row[7])}`")
        report.append("")
    (args.run / "report.md").write_text("\n".join(report), encoding="utf-8")
    print(json.dumps(summary["verdicts"]))
    return 0 if set(by_verdict) <= {"(a)", "(b)"} else 1


if __name__ == "__main__":
    sys.exit(main())
