#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Bundle sjl-13-001's part G runner: the deep-nesting corpus, each source parsed alone in its own process.

    python3 -I docs/evidence/sjl-13-001/tools/deep_nesting.py run   --probe "<command>" [--out report.tsv]
    python3 -I docs/evidence/sjl-13-001/tools/deep_nesting.py stack --probe "<command>" [--out stack.tsv] [--rows G001,G019]

Run from the repository root. <command> starts the Native AOT probe, Broiler.ECMAScript.Probe, under the JIT
(`dotnet Broiler.ECMAScript/Broiler.ECMAScript.Probe/bin/Release/net10.0/Broiler.ECMAScript.Probe.dll`) or as its
Native AOT image (`Broiler.ECMAScript/Broiler.ECMAScript.Probe/bin/Release/net10.0/linux-x64/publish/Broiler.ECMAScript.Probe`),
built at the commit judged (stage SJL-13.2e added the probe's `--parse-one` mode, which this script calls).

`run` (the rule's part G population and reading). For every row of docs/evidence/sjl-13-001/inputs/deep/manifest.tsv:
check the source's SHA-256 against the row's; start one process, `<command> --parse-one <source> --nesting <n>`, which
parses the source under configuration V's request with the row's nesting parameter on a thread of the stack the library
documents (ParseLimits.StackBytes); and read its exit code and its one line. A row's expected exit code is 0 for
`accepted`, 10 for `refused` of kind `nesting` (EX0011's shared code, JSE9201) and 11 for `refused` of kind
`tree-depth` (EX0012's, JSE9202). A process that ends with any other code, by a signal, or past the time limit is lost.
Every row is reported, one line each, with what it expected and what it got; the summary says how many rows held.
Exit codes: 0 every row held, 1 a row did not, 2 the corpus or a source was refused.

`stack` (the derivation's measurement, Broiler.ECMAScript/docs/frontend/limits-derivation.md). For each row (or the
rows named), the least thread stack, in KiB, at which the process still ends with the row's expected code: a bisection
over `--stack-kib` between 64 KiB and the documented stack, to a resolution of 16 KiB. A process that lost its stack
ends by a signal (SIGABRT, the runtime's stack-overflow exit). Not a judgement: the derivation reads the figures.

Deterministic in what it reads and writes: no clock, locale or hash order reaches the report; the processes' timings
are not recorded.
"""

from __future__ import annotations

import hashlib
import os
import shlex
import subprocess
import sys

CORPUS = "docs/evidence/sjl-13-001/inputs/deep"
EXPECTED = {("accepted", "nesting"): 0, ("accepted", "tree-depth"): 0, ("refused", "nesting"): 10, ("refused", "tree-depth"): 11}
DOCUMENTED_STACK_KIB = 16 * 1024
TIME_LIMIT_SECONDS = 120


def read_manifest() -> tuple[dict[str, str], list[dict[str, str]]]:
    header: dict[str, str] = {}
    rows: list[dict[str, str]] = []
    columns: list[str] | None = None
    with open(os.path.join(CORPUS, "manifest.tsv"), encoding="utf-8", newline="") as handle:
        for line in handle.read().split("\n"):
            if not line:
                continue
            if line.startswith("#%"):
                key, value = line[3:].split("\t", 1)
                header[key] = value
                continue
            if line.startswith("#"):
                continue
            fields = line.split("\t")
            if columns is None:
                columns = fields
                continue
            rows.append(dict(zip(columns, fields)))
    return header, rows


def check_source(row: dict[str, str]) -> str | None:
    path = os.path.join(CORPUS, row["file"])
    try:
        with open(path, "rb") as handle:
            data = handle.read()
    except OSError as error:
        return f"{row['id']}: {row['file']} cannot be read ({error.strerror})"
    if hashlib.sha256(data).hexdigest() != row["sha256"]:
        return f"{row['id']}: {row['file']} differs from the manifest's SHA-256"
    return None


def parse_one(probe: list[str], row: dict[str, str], stack_kib: int | None) -> tuple[str, str]:
    command = probe + ["--parse-one", os.path.join(CORPUS, row["file"]), "--nesting", row["nesting-parameter"], "--goal", row["goal"]]
    if stack_kib is not None:
        command += ["--stack-kib", str(stack_kib)]
    try:
        completed = subprocess.run(command, capture_output=True, timeout=TIME_LIMIT_SECONDS, check=False)
    except subprocess.TimeoutExpired:
        return "timeout", "-"
    code = completed.returncode
    status = f"signal {-code}" if code < 0 else f"exit {code}"
    if code == 134:
        status = "exit 134 (abort)"
    line = completed.stdout.decode("utf-8", "replace").strip().replace("\t", " ")
    return status, line or "-"


def expected_code(row: dict[str, str]) -> int:
    return EXPECTED[(row["expect"], row["kind"])]


def run(probe: list[str], out: str | None) -> int:
    header, rows = read_manifest()
    problems = [p for p in (check_source(row) for row in rows) if p]
    if problems:
        for problem in problems:
            print(problem, file=sys.stderr)
        return 2
    report = ["# bundle sjl-13-001, part G: the deep-nesting corpus, each source parsed alone in its own process",
              f"# tree-depth-bound\t{header.get('tree-depth-bound', '-')}",
              "id\tfile\tkind\tnesting-parameter\tcharge\texpect\texpected-exit\tgot\tline\tholds"]
    held = 0
    for row in rows:
        status, line = parse_one(probe, row, None)
        want = expected_code(row)
        holds = status == f"exit {want}"
        held += holds
        report.append("\t".join([row["id"], row["file"], row["kind"], row["nesting-parameter"], row["charge"], row["expect"],
                                 str(want), status, line, "yes" if holds else "NO"]))
    report.append(f"# {held} of {len(rows)} rows held")
    text = "\n".join(report) + "\n"
    if out:
        with open(out, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(text)
    sys.stdout.write(text)
    return 0 if held == len(rows) else 1


def least_stack(probe: list[str], row: dict[str, str]) -> tuple[int | None, str]:
    want = f"exit {expected_code(row)}"
    status, line = parse_one(probe, row, DOCUMENTED_STACK_KIB)
    if status != want:
        return None, status
    low, high = 64, DOCUMENTED_STACK_KIB
    status, _ = parse_one(probe, row, low)
    if status == want:
        return low, line
    while high - low > 16:
        middle = (low + high) // 2 // 16 * 16
        status, _ = parse_one(probe, row, middle)
        if status == want:
            high = middle
        else:
            low = middle
    return high, line


def stack(probe: list[str], out: str | None, only: set[str] | None) -> int:
    _, rows = read_manifest()
    report = ["# bundle sjl-13-001, part G's corpus: the least thread stack (KiB, to 16 KiB) at which each source still ends as expected",
              "id\tfile\tkind\tnesting-parameter\tcharge\texpect\tleast-stack-kib\tline"]
    lost = 0
    for row in rows:
        if only is not None and row["id"] not in only:
            continue
        least, line = least_stack(probe, row)
        lost += least is None
        report.append("\t".join([row["id"], row["file"], row["kind"], row["nesting-parameter"], row["charge"], row["expect"],
                                 "LOST" if least is None else str(least), line]))
        sys.stdout.write(report[-1] + "\n")
        sys.stdout.flush()
    text = "\n".join(report) + "\n"
    if out:
        with open(out, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(text)
    return 0 if lost == 0 else 1


def main(argv: list[str]) -> int:
    if len(argv) < 2 or argv[1] not in ("run", "stack"):
        print(__doc__, file=sys.stderr)
        return 2
    probe: list[str] | None = None
    out: str | None = None
    only: set[str] | None = None
    arguments = argv[2:]
    while arguments:
        flag = arguments.pop(0)
        if flag == "--probe" and arguments:
            probe = shlex.split(arguments.pop(0))
        elif flag == "--out" and arguments:
            out = arguments.pop(0)
        elif flag == "--rows" and arguments:
            only = set(arguments.pop(0).split(","))
        else:
            print(__doc__, file=sys.stderr)
            return 2
    if probe is None:
        print(__doc__, file=sys.stderr)
        return 2
    return run(probe, out) if argv[1] == "run" else stack(probe, out, only)


if __name__ == "__main__":
    sys.exit(main(sys.argv))
