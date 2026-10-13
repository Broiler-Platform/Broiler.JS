#!/usr/bin/env python3
"""Classify the differences of bundle sjl-3-001's reference-arm comparison against the reference.

    python3 scripts/conformance/classify_reference_arms.py --run <comparison output directory> \
        [--corpus conformance/vectors/numbers] [--jobs N] [--examples 20]

The comparison (Broiler.ECMAScript.Algorithms.Tests, run as `compare`) classifies nothing: it lists
every input on which an arm's variant differs from S. This script is the owner's classification,
written before the judged run and applied mechanically, so that every class can be checked against
the reference value line by line. The arbiter is numref.py, the corpus's exact reference computation;
the agreement of the arms decides nothing.

The rules, per difference of one variant from S:

- the reference's admissible results are computed for the input (every result the clause admits:
  several where the specification leaves the last digit, an implementation approximation, or the
  twenty-digit truncation open);
- class (a), a correction: S's result is admissible and the variant's is not. A variant that throws,
  or arm J's `unfinished:` result, is not admissible; an `unfinished:` result is class (a) only when
  the estimate it had reached is more than 2^20 units in the last place from every admissible value,
  and is otherwise unclassified, because then the arm's step cap may have changed a result;
- class (b), implementation-defined: both results are admissible. It is "in a vector" when the
  corpus holds a vector of the same operation, with the same parameter where the operation takes
  one, whose class is impl-defined or whose expected outcome admits more than one result; the
  vector's id and the revision it was added in are written beside it. A class (b) difference no
  vector covers is unclassified;
- otherwise unclassified: S's result is not admissible (class (c), a defect of S), or the input is
  one the reference cannot judge.

Outputs, in the run directory: classification.tsv.gz (every difference of every variant, with the
input, S's result, the variant's result, the reference's admissible results, the class and the
reason), report.md (per operation, variant, class and reason: the count, and the first --examples
differences in full) and classification.json (the counts, and the SHA-256 of both listings'
uncompressed text).
"""

from __future__ import annotations

import argparse
import gzip
import hashlib
import json
import multiprocessing
import re
import sys
from collections import Counter, defaultdict
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import numref as R  # noqa: E402

# The population's longest digit strings run to 100,000 digits; Python refuses to convert more than
# 4,300 by default.
sys.set_int_max_str_digits(0)

STEP_CAP_DISTANCE = 1 << 20
LONG_INPUT = 256


# --------------------------------------------------------------------------------------------------
# Decoding the comparison's text.

ESCAPE = re.compile(r"\\u([0-9A-F]{4})")


def unescape(text: str) -> str:
    return ESCAPE.sub(lambda m: chr(int(m.group(1), 16)), text)


def render_double(bits: int) -> str:
    return "NaN" if R.is_nan(bits) else f"f64:{bits:016X}"


def parse_double(rendered: str) -> int | None:
    if rendered == "NaN":
        return R.CANONICAL_NAN
    if rendered.startswith("f64:"):
        return int(rendered[4:], 16)
    return None


def order_key(bits: int) -> int:
    """Doubles as integers in numeric order, for a distance in units in the last place."""
    return -(bits & ~R.SIGN_BIT) if R.is_negative(bits) else bits


# --------------------------------------------------------------------------------------------------
# The reference: the admissible results of an input, as the comparison renders them.

class Reference:
    def __init__(self, admissible: frozenset[str], shown: str, approximated: bool = False) -> None:
        self.admissible = admissible
        self.shown = shown
        self.approximated = approximated

    def admits(self, result: str) -> bool:
        if result in self.admissible:
            return True
        if self.approximated:
            # An implementation-approximated integer (parseInt at a radix other than 2, 4, 8, 10, 16
            # or 32): any Number of the exact value's sign represents it.
            bits = parse_double(result)
            exact = parse_double(next(iter(self.admissible)))
            return bits is not None and not R.is_nan(bits) and exact is not None and \
                R.is_negative(bits) == R.is_negative(exact)
        return False


def doubles(values) -> Reference:
    rendered = frozenset(render_double(b) for b in values)
    return Reference(rendered, " | ".join(sorted(rendered)))


def strings(values, recommended: str | None = None) -> Reference:
    admissible = frozenset(values)
    shown = recommended if len(admissible) == 1 else "one of " + " | ".join(sorted(admissible))
    return Reference(admissible, shown)


def reference(operation: str, parameter: str, raw_input: str) -> Reference:
    if operation in ("Number::toString", "Number::toString(radix)", "toFixed", "toExponential", "toPrecision"):
        bits = int(raw_input, 16)
        if operation == "Number::toString":
            result = R.number_to_string(bits, 10)
            return strings(result.values, result.recommended)
        p = int(parameter)
        if operation == "Number::toString(radix)":
            result = R.number_to_string(bits, p)
            return strings(result.values, result.recommended)
        if operation == "toFixed":
            text = R.to_fixed(bits, p)
            return strings([text], text)
        if operation == "toExponential":
            result = R.to_exponential(bits, None if p < 0 else p)
            return strings(result.values, result.recommended)
        text = R.to_precision(bits, p)
        return strings([text], text)

    text = unescape(raw_input)
    if operation == "StringToNumber":
        return doubles(R.string_to_number(text))
    if operation == "parseFloat":
        return doubles(R.parse_float(text))
    if operation == "parseInt":
        radix = int(parameter)
        result = R.parse_int(text, None if radix == 0 else radix)
        ref = doubles(result.values)
        if result.approximated and not R.is_nan(result.values[0]):
            ref.approximated = True
            ref.shown += " (or any implementation approximation)"
        return ref
    if operation == "NumericLiteral":
        strictness, bits_text = parameter.split("/")
        literal = R.numeric_literal(text, strictness == "strict")
        if literal.kind == "error":
            return strings(["error:SyntaxError"], "error:SyntaxError")
        if literal.kind == "bigint":
            if literal.bigint.bit_length() > int(bits_text):
                return strings(["error:limit"], "error:limit")
            value = f"bigint:{literal.bigint}"
            return strings([value], value)
        return doubles(literal.numbers)
    if operation == "ArrayIndex":
        numbers = R.string_to_number(text)
        bits = numbers[0]
        if not R.is_nan(bits) and R.is_finite(bits) and not R.is_negative(bits):
            value = R.to_fraction(bits)
            if value.denominator == 1 and value < 2 ** 32 - 1 and R.number_to_string(bits, 10).recommended == text:
                return strings([f"index:{value.numerator}"], f"index:{value.numerator}")
        return strings(["none"], "none")
    if operation == "StrWhiteSpaceChar":
        answer = "true" if ord(text) in R.STR_WHITE_SPACE else "false"
        return strings([answer], answer)
    raise ValueError(f"no reference for {operation}")


# --------------------------------------------------------------------------------------------------
# One difference.

def reason(operation: str, result: str, ref: Reference, shared: str) -> str:
    """A short, groupable account of why a variant's result is or is not admissible."""
    if result.startswith("exception:"):
        return f"throws {result[len('exception:'):]}"
    if result.startswith("unfinished:"):
        return "does not finish (step cap)"
    if "CreateClass=" in result:
        return "its two BigInt readers disagree"
    if operation in ("Number::toString", "Number::toString(radix)", "toExponential"):
        mine, theirs = significant(result), significant(shared)
        if mine > theirs:
            return "more digits than the shortest"
        if mine < theirs:
            return "fewer digits, which do not read back"
        return "as many digits, not an admitted choice"
    if operation in ("toFixed", "toPrecision"):
        return "different digits or placement"
    if operation in ("StringToNumber", "parseFloat", "parseInt", "NumericLiteral"):
        mine, expected = parse_double(result), parse_double(next(iter(sorted(ref.admissible))))
        if mine is not None and expected is not None:
            if R.is_nan(mine) != R.is_nan(expected):
                return "NaN where the reference has a value" if R.is_nan(mine) else "a value where the reference has NaN"
            if R.is_finite(mine) and R.is_finite(expected):
                return "not correctly rounded"
            return "an infinity where the reference has a finite value" if not R.is_finite(mine) else "a finite value where the reference has an infinity"
        if result.startswith("error:") and not shared.startswith("error:"):
            return "refuses a literal the grammar admits" if result == "error:SyntaxError" else "refused at the width limit"
        if shared.startswith("error:") and not result.startswith("error:"):
            return "admits a literal the grammar refuses"
        return "a different value"
    if operation == "ArrayIndex":
        return "an index where the reference has none" if result.startswith("index:") else "no index where the reference has one"
    if operation == "StrWhiteSpaceChar":
        return "counts a code unit the set does not hold" if result == "true" else "misses a code unit the set holds"
    return "differs"


def significant(text: str) -> int:
    body = text.lstrip("-")
    if "e" in body and not any(c.isalpha() and c != "e" for c in body):
        body = body.split("e")[0]
    return len(body.replace(".", "").strip("0"))


def classify_row(line: str) -> list[tuple[str, ...]]:
    fields = line.rstrip("\n").split("\t")
    operation, population_class, population_line, parameter, raw_input, raw_shared, raw_variants = fields
    shared = unescape(raw_shared)
    try:
        ref = reference(operation, parameter, raw_input)
    except Exception as error:  # noqa: BLE001 - an input the reference cannot judge is unclassified
        ref = None
        failure = f"the reference cannot judge the input: {type(error).__name__}"
    out = []
    for item in raw_variants.split(" "):
        name, _, raw_result = item.partition("=")
        result = unescape(raw_result)
        if result == "(not called)" or result == shared:
            continue
        if ref is None:
            out.append((operation, population_class, population_line, parameter, raw_input, raw_shared, name, raw_result,
                        "", "unclassified", failure, ""))
            continue
        shared_ok = ref.admits(shared)
        variant_ok = ref.admits(result)
        why = reason(operation, result, ref, shared)
        if result.startswith("unfinished:") and shared_ok:
            estimate = parse_double(result[len("unfinished:"):])
            distance = min(abs(order_key(estimate) - order_key(parse_double(a))) for a in ref.admissible
                           if parse_double(a) is not None and not R.is_nan(parse_double(a)))
            label, why = ("a", f"does not finish: its estimate is {distance} units in the last place away") \
                if distance > STEP_CAP_DISTANCE else ("unclassified", f"the step cap stopped it {distance} units away")
        elif shared_ok and not variant_ok:
            label = "a"
        elif shared_ok and variant_ok:
            label, why = "b", "both admitted" + (": an implementation approximation" if ref.approximated and result not in ref.admissible else "")
        else:
            label, why = "unclassified", "S's result is not admitted (class (c))"
        out.append((operation, population_class, population_line, parameter, raw_input, raw_shared, name, raw_result,
                    ref.shown, label, why, ""))
    return out


def classify_chunk(lines: list[str]) -> list[tuple[str, ...]]:
    rows: list[tuple[str, ...]] = []
    for line in lines:
        rows += classify_row(line)
    return rows


# --------------------------------------------------------------------------------------------------
# Vectors covering class (b).

def covering_vectors(corpus: Path) -> dict[tuple[str, str], tuple[str, str]]:
    """(operation, parameter) -> (vector id, revision) for every vector with more than one admitted
    result or of class impl-defined; the first such vector in file order."""
    names = {"Number::toString": "Number::toString", "Number.prototype.toString": "Number::toString(radix)",
             "Number.prototype.toFixed": "toFixed", "Number.prototype.toExponential": "toExponential",
             "Number.prototype.toPrecision": "toPrecision", "StringToNumber": "StringToNumber",
             "parseFloat": "parseFloat", "parseInt": "parseInt", "NumericLiteral": "NumericLiteral"}
    found: dict[tuple[str, str], tuple[str, str]] = {}
    for path in sorted(corpus.glob("*.vectors")):
        for block in path.read_text(encoding="utf-8").split("\n\n"):
            record = dict(line.split(": ", 1) for line in block.splitlines() if ": " in line and not line.startswith("#"))
            if "id" not in record:
                continue
            operation = names.get(record.get("op", ""))
            if operation is None:
                continue
            several = record.get("out", "").count(" | ") > 0 or record.get("out", "").startswith("one-of")
            if record.get("class") != "impl-defined" and not several:
                continue
            match = re.search(r"int:(-?\d+)\s*$", record.get("in", ""))
            parameter = match.group(1) if match else "-"
            if operation == "toExponential" and record.get("in", "").endswith("undefined"):
                parameter = "-1"
            if operation == "parseInt" and record.get("in", "").endswith("undefined"):
                parameter = "0"
            found.setdefault((operation, parameter), (record["id"], record.get("since", "?")))
    return found


# --------------------------------------------------------------------------------------------------

def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--run", type=Path, required=True)
    parser.add_argument("--corpus", type=Path, default=Path(__file__).resolve().parents[2] / "conformance/vectors/numbers")
    parser.add_argument("--jobs", type=int, default=multiprocessing.cpu_count())
    parser.add_argument("--examples", type=int, default=20)
    args = parser.parse_args(argv)

    vectors = covering_vectors(args.corpus)
    differences = args.run / "differences.tsv.gz"
    source_lines = 0

    def chunks():
        """The listing in chunks of 2,000 lines, read as the pool asks, never whole."""
        nonlocal source_lines
        with gzip.open(differences, "rt", encoding="utf-8", newline="\n") as stream:
            header = stream.readline()
            assert header.startswith("operation\t"), header
            chunk: list[str] = []
            for line in stream:
                chunk.append(line)
                source_lines += 1
                if len(chunk) == 2000:
                    yield chunk
                    chunk = []
            if chunk:
                yield chunk

    counts: Counter[tuple[str, str, str, str]] = Counter()
    examples: dict[tuple[str, str, str, str], list[tuple[str, ...]]] = defaultdict(list)
    listing = hashlib.sha256()
    total = 0
    out_path = args.run / "classification.tsv.gz"
    with open(out_path, "wb") as raw, gzip.GzipFile(filename="", mode="wb", fileobj=raw, mtime=0, compresslevel=6) as gz:
        head = "operation\tclass\tline\tparameter\tinput\tS\tvariant\tresult\treference\tlabel\treason\tvector\n"
        gz.write(head.encode("utf-8"))
        listing.update(head.encode("utf-8"))
        with multiprocessing.Pool(args.jobs, initializer=sys.set_int_max_str_digits, initargs=(0,)) as pool:
            for rows in pool.imap(classify_chunk, chunks(), chunksize=1):
                for row in rows:
                    row = list(row)
                    if row[9] == "b":
                        parameter = row[3] if row[0] not in ("Number::toString", "StringToNumber", "parseFloat",
                                                            "NumericLiteral") else "-"
                        vector = vectors.get((row[0], parameter))
                        if vector is None:
                            row[9], row[10] = "unclassified", row[10] + "; class (b) with no vector covering it"
                        else:
                            row[11] = f"{vector[0]} (revision {vector[1]})"
                    if len(row[4]) > LONG_INPUT:
                        row[4] = f"@{row[1]}:{row[2]} ({len(row[4])} characters)"
                    text = "\t".join(row) + "\n"
                    gz.write(text.encode("utf-8"))
                    listing.update(text.encode("utf-8"))
                    key = (row[0], row[6], row[9], row[10] if row[9] != "b" else "both admitted")
                    counts[key] += 1
                    total += 1
                    if len(examples[key]) < args.examples:
                        examples[key].append(tuple(row))

    with gzip.open(differences, "rb") as stream:
        source = hashlib.sha256()
        for block in iter(lambda: stream.read(1 << 20), b""):
            source.update(block)

    summary = {
        "differences": {"file": "differences.tsv.gz", "lines": source_lines, "sha256": source.hexdigest()},
        "classification": {"file": "classification.tsv.gz", "lines": total, "sha256": listing.hexdigest()},
        "unclassified": sum(c for k, c in counts.items() if k[2] == "unclassified"),
        "groups": [{"operation": k[0], "variant": k[1], "class": k[2], "reason": k[3], "count": c}
                   for k, c in sorted(counts.items())],
    }
    (args.run / "classification.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")

    report = ["# Reference-arm comparison: classification", "",
              f"Differences listed: {source_lines} inputs; variant differences classified: {total}; "
              f"unclassified: {summary['unclassified']}.", ""]
    for key, count in sorted(counts.items()):
        report.append(f"## {key[0]} / {key[1]} / class {key[2]} / {key[3]}: {count}")
        report.append("")
        report.append("| population | parameter | input | S | variant's result | reference | vector |")
        report.append("|---|---|---|---|---|---|---|")
        for row in examples[key]:
            cells = [f"{row[1]}:{row[2]}", row[3], row[4][:120], row[5][:120], row[7][:120], row[8][:120], row[11]]
            report.append("| " + " | ".join(c.replace("|", "\\|") for c in cells) + " |")
        report.append("")
    (args.run / "report.md").write_text("\n".join(report) + "\n", encoding="utf-8")
    print(f"classified {total} variant differences; unclassified {summary['unclassified']}", file=sys.stderr)
    return 0


if __name__ == "__main__":
    sys.exit(main())
