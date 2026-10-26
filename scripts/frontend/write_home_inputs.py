#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Write the inputs of bundle sjl-13-001's home rule: part F's fuzz seeds and part G's deep-nesting corpus.

    python3 -I scripts/frontend/write_home_inputs.py write
    python3 -I scripts/frontend/write_home_inputs.py check

`write` writes docs/evidence/sjl-13-001/inputs/fuzz-seeds.txt and docs/evidence/sjl-13-001/inputs/deep/ (its
manifest.tsv and one source per row); `check` builds the same files in memory and compares them with the committed
ones byte for byte, and reports any file in deep/ that it would not write. Both run from the repository root.
Exit codes: 0 done (or `check` identical), 1 `check` found a difference, 2 an input was refused (nothing written).

THE FUZZ SEEDS (rule part F). Every seed is an input of the parse corpus (conformance/parse/corpus.manifest, checked
against its SHA-256 below), named by its id, chosen by a rule that reads the manifest alone:
  - every input whose origin is broiler-vm (Broiler.VM's own source corpus);
  - every input whose origin is broiler-js and whose goal is module;
  - for each directory of test262's test/language/ and test/annexB/language/, taken to the depth of two names below
    language/ (or one, for a test file directly under the first), the first input in the manifest's order that is
    no negative test.
Each seed is listed once in configuration V and once in configuration J (the rule's "The two configurations"), with
its goal (a json input under the Script goal, as part A reads a json variant), MUTANTS mutants, and the time bound
per parse the file's header states.

THE DEEP-NESTING CORPUS (rule part G). One source per shape and depth, and one manifest row per source and nesting
parameter, with the expected outcome under that parameter. The measure is stated in inputs/README.md: nesting as
Broiler.VM's wide parser counts it at 4cf9ca4, and tree depth as it charges it, with a left operand's spine charged
to the chain it begins (decision D14). Every source is a Script, one line, LF-terminated, ASCII.

Deterministic: no clock, no locale, no hash order and no file-system order reaches the output.
"""

from __future__ import annotations

import hashlib
import os
import sys

MANIFEST = "conformance/parse/corpus.manifest"
MANIFEST_SHA256 = "1c5342104766f14d0c17d950537e4ed078a0c438c3652f8c65bff8c6c347e9cc"
OUT = "docs/evidence/sjl-13-001/inputs"
SEEDS = OUT + "/fuzz-seeds.txt"
DEEP = OUT + "/deep"

MUTANTS = 64
TIME_BOUND_MS = 2000
CONFIGURATIONS = ("V", "J")

# The nesting parameters the corpus names: Broiler.VM's default and the largest a Broiler.VM request may carry
# (SliceParseOptions.DefaultMaximumNestingDepth and MaximumSupportedNestingDepth at 4cf9ca4).
NESTING_PARAMETERS = (64, 512)
# The tree-depth bound: the library's constant, equal to Broiler.VM's SliceParseOptions.MaximumTreeDepth
# (decision D14); the tree shapes name the default nesting parameter, which none of them approaches.
TREE_BOUND = 10_000
TREE_PARAMETER = 64
FAR = 100_000
FAR_HALF = 50_000


def fail(message: str) -> None:
    print("refused: " + message, file=sys.stderr)
    sys.exit(2)


def read_manifest() -> list[dict[str, str]]:
    data = open(MANIFEST, "rb").read()
    digest = hashlib.sha256(data).hexdigest()
    if digest != MANIFEST_SHA256:
        fail(f"{MANIFEST} has SHA-256 {digest}, not {MANIFEST_SHA256}")
    lines = [line for line in data.decode("utf-8").split("\n") if line and not line.startswith("#")]
    header = lines[0].split("\t")
    rows = [dict(zip(header, line.split("\t"))) for line in lines[1:]]
    for row in rows:
        if len(row) != len(header):
            fail("a manifest row has the wrong number of cells: " + row.get("id", "?"))
    return rows


def seed_rows(rows: list[dict[str, str]]) -> list[tuple[str, str, str]]:
    """(id, goal, selection) for every seed, in the manifest's order."""
    chosen: list[tuple[str, str, str]] = []
    seen_directories: set[str] = set()
    for row in rows:
        goal = "script" if row["goal"] == "json" else row["goal"]
        if goal not in ("script", "module"):
            fail("unknown goal " + row["goal"] + " for " + row["id"])
        origin = row["origin"]
        if origin == "broiler-vm":
            chosen.append((row["id"], goal, "broiler-vm"))
        elif origin == "broiler-js":
            if row["goal"] == "module":
                chosen.append((row["id"], goal, "broiler-js-module"))
        elif origin == "test262":
            parts = row["path"].split("/")
            if row["negative"] != "-":
                continue
            if parts[:2] == ["test", "language"]:
                prefix = 2
            elif parts[:3] == ["test", "annexB", "language"]:
                prefix = 3
            else:
                continue
            below = parts[prefix:-1][:2]
            directory = "/".join(parts[:prefix] + below)
            if directory in seen_directories:
                continue
            seen_directories.add(directory)
            chosen.append((row["id"], goal, "test262:" + directory + "/"))
        else:
            fail("unknown origin " + origin + " for " + row["id"])
    return chosen


def seeds_file(rows: list[dict[str, str]]) -> bytes:
    lines = [
        "# Bundle sjl-13-001 (Broiler.JS), rule part F: the seeds of the shared syntax library's fuzz target.",
        "# Written by scripts/frontend/write_home_inputs.py from the parse corpus's manifest; never edited by hand.",
        "# inputs/README.md says how a seed is read. Each line is one seed in one configuration of the rule.",
        "#% format\tsjl-13-001-fuzz-seeds\t1",
        "#% corpus\t" + MANIFEST + "\t" + MANIFEST_SHA256,
        "#% time-bound-per-parse-ms\t" + str(TIME_BOUND_MS),
        "#% mutants-per-line\t" + str(MUTANTS),
        "id\tgoal\tconfiguration\tmutants\tselection",
    ]
    for seed_id, goal, selection in seed_rows(rows):
        for configuration in CONFIGURATIONS:
            lines.append("\t".join((seed_id, goal, configuration, str(MUTANTS), selection)))
    return ("\n".join(lines) + "\n").encode("ascii")


# ---- the deep-nesting corpus ------------------------------------------------------------------------------------

def nesting_sources() -> list[tuple[str, str, int, int, str]]:
    """(shape, file stem, levels, charge, text) for every nesting source.

    The charge is the source's nesting under the measure inputs/README.md states: `var x = ` and its initialiser
    take two levels (a statement and an assignment expression), each bracket, parenthesis or template one more
    (its element, its contents or its substitution is an assignment expression), each block or function
    declaration one (a statement)."""
    def var(body: str) -> str:
        return "var x = " + body + ";\n"

    shapes = {
        "brackets": (lambda n: var("[" * n + "1" + "]" * n), 2),
        "parentheses": (lambda n: var("(" * n + "1" + ")" * n), 2),
        "templates": (lambda n: var("`${" * n + "1" + "}`" * n), 2),
        "blocks": (lambda n: "{" * n + "}" * n + "\n", 0),
        "functions": (lambda n: "function f(){" * n + "}" * n + "\n", 0),
    }
    sources = []
    for shape, (make, overhead) in shapes.items():
        levels = set()
        for parameter in NESTING_PARAMETERS:
            levels.add(parameter - overhead)
            levels.add(parameter - overhead + 1)
        levels.add(FAR)
        for n in sorted(levels):
            sources.append((shape, f"{shape}-{n}", n, n + overhead, make(n)))
    # parentheses around a template and inside its substitution: `var x = `, the parentheses around, the template's
    # substitution and the parentheses inside
    pairs = set()
    for parameter in NESTING_PARAMETERS:
        room = parameter - 3
        around = (room + 1) // 2
        pairs.add((around, room - around))
        pairs.add((around, room - around + 1))
    pairs.add((FAR_HALF, FAR_HALF))
    for around, inside in sorted(pairs):
        text = ("var x = " + "(" * around + "`${" + "(" * inside + "1" + ")" * inside + "}`" + ")" * around
                + ";\n")
        sources.append(("substitutions", f"substitutions-{around}-{inside}", around + inside, 3 + around + inside,
                        text))
    return sources


def tree_sources() -> list[tuple[str, str, int, str]]:
    """(shape, file stem, depth, text) for every tree-depth source; depth under the measure inputs/README.md
    states (each node a binary chain or a member or call chain adds; a left operand's spine counted toward the
    chain it begins)."""
    def chain(n: int) -> str:
        return "1" + "+1" * n

    def members(n: int) -> str:
        return "o" + ".a" * n

    single = {
        "binary-chain": lambda n: "var x = " + chain(n) + ";\n",
        "member-chain": lambda n: "var o = {}; var x = " + members(n) + ";\n",
    }
    pairs = {
        "left-parenthesised-chain": lambda a, b: "var x = (" + chain(a) + ")" + "+1" * b + ";\n",
        "left-template-chain": lambda a, b: "var x = `${" + chain(a) + "}`" + "+1" * b + ";\n",
        "left-member-chain": lambda a, b: "var o = {}; var x = " + members(a) + "+1" * b + ";\n",
        "object-parenthesised-chain": lambda a, b: "var x = (" + chain(a) + ")" + ".a" * b + ";\n",
    }
    sources = []
    for shape, make in single.items():
        for n in (TREE_BOUND, TREE_BOUND + 1, FAR):
            sources.append((shape, f"{shape}-{n}", n, make(n)))
    half = TREE_BOUND // 2
    for shape, make in pairs.items():
        for a, b in ((half, TREE_BOUND - half), (half, TREE_BOUND - half + 1), (FAR_HALF, FAR_HALF)):
            sources.append((shape, f"{shape}-{a}-{b}", a + b, make(a, b)))
    return sources


def deep_files() -> dict[str, bytes]:
    files: dict[str, bytes] = {}
    rows = [
        "# Bundle sjl-13-001 (Broiler.JS), rule part G: the deep-nesting corpus. Written by",
        "# scripts/frontend/write_home_inputs.py; never edited by hand. inputs/README.md states the measure.",
        "# One row per source and nesting parameter; every source is a Script, parsed alone, in configuration V's",
        "# request with the row's nesting parameter.",
        "#% format\tsjl-13-001-deep\t1",
        "#% tree-depth-bound\t" + str(TREE_BOUND),
        "id\tfile\tshape\tkind\tgoal\tnesting-parameter\tlevels\tcharge\texpect\tsha256",
    ]
    counter = 0

    def add(file_name: str, text: str, shape: str, kind: str, parameter: int, levels: int, charge: int,
            bound: int) -> None:
        nonlocal counter
        data = text.encode("ascii")
        if file_name in files and files[file_name] != data:
            fail("two texts for " + file_name)
        files[file_name] = data
        counter += 1
        expect = "accepted" if charge <= bound else "refused"
        rows.append("\t".join((f"G{counter:03d}", file_name, shape, kind, "script", str(parameter), str(levels),
                               str(charge), expect, hashlib.sha256(data).hexdigest())))

    nesting = nesting_sources()
    for parameter in NESTING_PARAMETERS:
        for shape, stem, levels, charge, text in nesting:
            # each parameter's rows: the sources at its bound, one past it, and far past it
            if charge in (parameter, parameter + 1) or levels >= FAR:
                add(stem + ".js", text, shape, "nesting", parameter, levels, charge, parameter)
    for shape, stem, depth, text in tree_sources():
        add(stem + ".js", text, shape, "tree-depth", TREE_PARAMETER, depth, depth, TREE_BOUND)
    files["manifest.tsv"] = ("\n".join(rows) + "\n").encode("ascii")
    return files


def expected_files() -> dict[str, bytes]:
    rows = read_manifest()
    files = {SEEDS: seeds_file(rows)}
    for name, data in deep_files().items():
        files[DEEP + "/" + name] = data
    return files


def main(argv: list[str]) -> int:
    if len(argv) != 2 or argv[1] not in ("write", "check"):
        print(__doc__.split("\n\n")[0], file=sys.stderr)
        return 2
    files = expected_files()
    if argv[1] == "write":
        os.makedirs(DEEP, exist_ok=True)
        for name in sorted(files):
            with open(name, "wb") as handle:
                handle.write(files[name])
        return 0
    differences = []
    for name in sorted(files):
        try:
            current = open(name, "rb").read()
        except FileNotFoundError:
            differences.append("missing: " + name)
            continue
        if current != files[name]:
            differences.append("differs: " + name)
    for name in sorted(os.listdir(DEEP)) if os.path.isdir(DEEP) else []:
        if DEEP + "/" + name not in files:
            differences.append("not written by this script: " + DEEP + "/" + name)
    for line in differences:
        print(line)
    return 1 if differences else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
