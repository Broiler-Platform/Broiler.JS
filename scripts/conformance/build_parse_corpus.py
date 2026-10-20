#!/usr/bin/env python3
"""Build the parse corpus of milestone SJL-12.4: conformance/parse/corpus.manifest and its stored inputs.

    python3 -I scripts/conformance/build_parse_corpus.py import-vm --vm <Broiler.VM checkout> --commit <sha> \
        --generated <dir> --write-output <dir>
    python3 -I scripts/conformance/build_parse_corpus.py import-js --capture <dir> [--capture <dir> ...] --commit <sha>
    python3 -I scripts/conformance/build_parse_corpus.py manifest --test262 <archive or checkout> [--test262-also <other>]
    python3 -I scripts/conformance/build_parse_corpus.py check --test262 <archive or checkout>

The corpus is the population of bundle sjl-12-001's corpus parts in both repositories: Broiler.JS's rule, part C,
and Broiler.VM's rule, part B. Its format is described in conformance/parse/README.md, section "The manifest";
nothing here may change a byte of it without that section changing too.

**Three origins.**
- `test262`, at the pin, referenced by path and SHA-256 and never copied: every test under `test/` (front matter
  read with a YAML loader, variants as INTERPRETING.md gives them), every `_FIXTURE.js` file as a module input and
  every `_FIXTURE.json` file as a JSON input. The suite is read from Broiler.VM's archive of the pin
  (`test262-<revision>.tar.gz`, checked against its SHA-256) or from a checkout at the pin (checked by `git
  rev-parse`, read only). `--test262-also` reads the other one and refuses unless every file under `test/` is
  byte-identical in both.
- `broiler-vm`, stored under `inputs/broiler-vm/`: the retained source corpus `src/tests/corpus/js-1/source` at
  the Broiler.VM commit given, byte for byte, each checked against `source.manifest` and against what the slice
  compiler's own `--write` writes at that commit (a committed file `--write` no longer writes is kept and marked
  `js-1-unwritten`), and the texts of `SliceSourcePrograms`' generated members, which `import-vm` takes from a
  directory written by a scratch program at that commit (see the README) and checks against `source.manifest`'s
  SHA-256.
- `broiler-js`, stored under `inputs/broiler-js/`: the texts a scratch capture hook recorded while
  Broiler.JS's Parser.Tests and Compiler.Tests ran at the commit given (see the README). They are LF-normalised
  and deduplicated on their normalised bytes and goal.

`import-vm` and `import-js` write the stored inputs and `provenance/<origin>.tsv`, and are run once, when the
corpus is made. `manifest` writes `corpus.manifest` from the suite, the stored inputs and the provenance files
alone, and `check` rebuilds it in memory and compares it with the committed file byte for byte, so the committed
corpus reproduces from the suite and from what it holds. Every mode is deterministic: no clock, no locale, no
hash order and no file-system order reaches the output.

Exit codes: 0 done (or `check` identical), 1 `check` found a difference, 2 an input was refused (nothing is
written).
"""

from __future__ import annotations

import argparse
import collections
import hashlib
import io
import json
import os
import re
import shutil
import subprocess
import sys
import tarfile
from dataclasses import dataclass
from pathlib import Path, PurePosixPath

FORMAT = ("broiler-parse-corpus", "1")
CORPUS_NAME = "parse-1"
VIRTUAL_ROOT = "/test262/"
STORED_ROOT = "/parse-1/"
STRICT_PROLOGUE = '"use strict";\n'

TEST262_REVISION = "ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e"
TEST262_ARCHIVE_PATH = f"src/tests/conformance/pins/test262-{TEST262_REVISION}.tar.gz"
TEST262_ARCHIVE_SHA256 = "f58ce79141529c9fa33592e22ff3ff0d83b83830ac8e372ecd32e1623db1c040"

# Broiler.VM's EvalRequestFlags (JsFormat.cs): None 0, Strict 1, InFunction 2, InMethod 4,
# InDerivedConstructor 8, InClassFieldInitializer 16, InClassBody 32. The sets are the VM rule's, part B, B3.
EVAL_FLAG_BITS = {
    "None": 0,
    "Strict": 1,
    "InFunction": 2,
    "InMethod": 4,
    "InDerivedConstructor": 8,
    "InClassFieldInitializer": 16,
    "InClassBody": 32,
}
EVAL_FLAG_SETS = (
    ("E0", ("None",)),
    ("E1", ("Strict",)),
    ("E2", ("InFunction",)),
    ("E3", ("InFunction", "Strict")),
    ("E4", ("InFunction", "InMethod")),
    ("E5", ("InFunction", "InMethod", "InDerivedConstructor")),
    ("E6", ("InFunction", "InMethod", "InClassFieldInitializer")),
    ("E7", ("InFunction", "InMethod", "InClassBody")),
)
EVAL_FAMILY_MARKERS = ("eval", "arguments", "super", "new.target", "#")

ORIGINS = ("test262", "broiler-vm", "broiler-js")
ID_PREFIX = {"test262": "T", "broiler-vm": "V", "broiler-js": "J"}
COLUMNS = ("id", "origin", "path", "sha256", "goal", "variants", "negative", "eval-family", "provenance")

GOAL_BY_EXTENSION = {".js": "script", ".mjs": "module", ".json": "json"}
EXTENSION_BY_GOAL = {"script": ".js", "module": ".mjs"}

# test262's front matter: the first /*--- ... ---*/ block (test262's INTERPRETING.md, "Metadata").
FRONT_MATTER = re.compile(r"/\*---(.*?)---\*/", re.S)

# The two Broiler.JS test projects the capture runs (milestone SJL-12.4: "Broiler.JS's parser and compiler test
# inputs").
CAPTURED_PROJECTS = ("Broiler.JavaScript.Parser.Tests", "Broiler.JavaScript.Compiler.Tests")

VM_SOURCE = "src/tests/corpus/js-1/source"
VM_KINDS = ("accepted", "refused")


class Refused(Exception):
    """An input the builder will not use; nothing is written."""


@dataclass(frozen=True)
class Row:
    origin: str
    path: str
    sha256: str
    goal: str
    variants: str
    negative: str
    eval_family: str
    provenance: str


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def decode(data: bytes, where: str) -> str:
    """The text of an input: strict UTF-8, no byte-order mark."""
    if data.startswith(b"\xef\xbb\xbf"):
        raise Refused(f"{where}: begins with a byte-order mark")
    try:
        return data.decode("utf-8")
    except UnicodeDecodeError as error:
        raise Refused(f"{where}: not UTF-8 ({error})") from None


def eval_family(goal: str, text: str) -> str:
    return "yes" if goal == "script" and any(marker in text for marker in EVAL_FAMILY_MARKERS) else "no"


def lf_normalise(data: bytes) -> bytes:
    return data.replace(b"\r\n", b"\n").replace(b"\r", b"\n")


def check_field(value: str, where: str) -> str:
    if not value or "\t" in value or "\n" in value or "\r" in value:
        raise Refused(f"{where}: a manifest field may not be empty or hold a tab or line break: {value!r}")
    return value


# ---- test262 --------------------------------------------------------------------------------------------------


class Suite:
    """test262's files under test/, from Broiler.VM's archive of the pin or from a checkout at the pin."""

    def __init__(self, location: Path):
        self.location = location
        self.files: dict[str, bytes] = {}
        if location.is_file():
            self._read_archive(location)
        elif location.is_dir():
            self._read_checkout(location)
        else:
            raise Refused(f"{location}: no test262 archive or checkout there")
        if not self.files:
            raise Refused(f"{location}: no file under test/")

    def _read_archive(self, archive: Path) -> None:
        digest = sha256(archive.read_bytes())
        if digest != TEST262_ARCHIVE_SHA256:
            raise Refused(f"{archive}: SHA-256 {digest}, not the pin's {TEST262_ARCHIVE_SHA256}")
        prefix = f"test262-{TEST262_REVISION}/"
        with tarfile.open(archive, "r:gz") as tar:
            for member in tar:
                if member.name == prefix.rstrip("/") and member.isdir():
                    continue
                if not member.name.startswith(prefix):
                    raise Refused(f"{archive}: member {member.name} outside {prefix}")
                relative = member.name[len(prefix):]
                if not relative.startswith("test/") or not member.isfile():
                    continue
                extracted = tar.extractfile(member)
                assert extracted is not None
                self.files[relative] = extracted.read()

    def _read_checkout(self, root: Path) -> None:
        head = subprocess.run(
            ["git", "-C", str(root), "rev-parse", "HEAD"], capture_output=True, text=True, check=False
        ).stdout.strip()
        if head != TEST262_REVISION:
            raise Refused(f"{root}: checkout at {head or 'no commit'}, not the pin {TEST262_REVISION}")
        test = root / "test"
        for directory, _, names in os.walk(test):
            for name in names:
                full = Path(directory) / name
                if full.is_symlink() or not full.is_file():
                    continue
                self.files[full.relative_to(root).as_posix()] = full.read_bytes()


def test262_kind(path: str) -> str | None:
    """`test`, `fixture`, `json-fixture`, or None for a file that is not an input."""
    name = PurePosixPath(path).name
    if "_FIXTURE" in name:
        if name.endswith("_FIXTURE.js"):
            return "fixture"
        if name.endswith("_FIXTURE.json"):
            return "json-fixture"
        return None
    if name.endswith(".js"):
        return "test"
    return None


def front_matter(text: str, where: str) -> dict:
    import yaml  # PyYAML; imported here so that the stored-input modes run without it

    match = FRONT_MATTER.search(text)
    if match is None:
        raise Refused(f"{where}: no /*--- ... ---*/ front matter")
    try:
        data = yaml.safe_load(match.group(1))
    except yaml.YAMLError as error:
        raise Refused(f"{where}: front matter is not YAML ({error})") from None
    if data is None:
        return {}
    if not isinstance(data, dict):
        raise Refused(f"{where}: front matter is not a mapping")
    return data


def variants_of(flags: set[str]) -> tuple[str, str]:
    """(goal, variants) by INTERPRETING.md, with Test262Run's precedence: module, raw, onlyStrict, noStrict."""
    if "module" in flags:
        return "module", "module"
    if "raw" in flags:
        return "script", "raw"
    if "onlyStrict" in flags:
        return "script", "strict"
    if "noStrict" in flags:
        return "script", "sloppy"
    return "script", "strict,sloppy"


def test262_rows(suite: Suite, only: set[str] | None = None) -> tuple[list[Row], list[str], list[str]]:
    """The test262 rows, the files under test/ that are not inputs, and the flag combinations noted."""
    rows: list[Row] = []
    excluded: list[str] = []
    notes: list[str] = []
    for path in sorted(suite.files):
        kind = test262_kind(path)
        if kind is None:
            excluded.append(path)
            continue
        if only is not None and path not in only:
            continue
        data = suite.files[path]
        text = decode(data, path)
        check_field(path, path)
        if kind == "test":
            meta = front_matter(text, path)
            flags = meta.get("flags") or []
            if not isinstance(flags, list) or not all(isinstance(flag, str) for flag in flags):
                raise Refused(f"{path}: flags is not a list of names")
            flag_set = set(flags)
            governing = flag_set & {"module", "raw", "onlyStrict", "noStrict"}
            if len(governing) > 1:
                notes.append(f"{path}\t{','.join(sorted(governing))}")
            goal, variants = variants_of(flag_set)
            negative = meta.get("negative")
            if negative is None:
                negative_field = "-"
            else:
                if not isinstance(negative, dict) or "phase" not in negative or "type" not in negative:
                    raise Refused(f"{path}: negative has no phase and type")
                negative_field = f"{negative['phase']}:{negative['type']}"
        elif kind == "fixture":
            goal, variants, negative_field = "module", "module", "-"
        else:
            goal, variants, negative_field = "json", "json", "-"
        rows.append(
            Row(
                "test262",
                path,
                sha256(data),
                goal,
                variants,
                check_field(negative_field, path),
                eval_family(goal, text),
                kind,
            )
        )
    if only is not None:
        missing = sorted(only - {row.path for row in rows})
        if missing:
            raise Refused(f"selected test262 paths that are not inputs: {missing}")
    return rows, excluded, notes


# ---- stored inputs --------------------------------------------------------------------------------------------


def read_provenance(path: Path) -> tuple[dict[str, str], list[list[str]], list[str]]:
    """A provenance file: its `#%` fields, its rows (after the column line), and its column line."""
    fields: dict[str, str] = {}
    rows: list[list[str]] = []
    columns: list[str] = []
    for line in path.read_text(encoding="utf-8").split("\n"):
        if not line:
            continue
        if line.startswith("#%"):
            key, _, value = line[2:].strip().partition("\t")
            fields[key] = value
        elif line.startswith("#"):
            continue
        elif not columns:
            columns = line.split("\t")
        else:
            rows.append(line.split("\t"))
    return fields, rows, columns


def stored_files(corpus: Path, origin: str) -> dict[str, bytes]:
    root = corpus / "inputs" / origin
    files: dict[str, bytes] = {}
    if not root.is_dir():
        return files
    for directory, _, names in os.walk(root):
        for name in names:
            full = Path(directory) / name
            files[full.relative_to(corpus).as_posix()] = full.read_bytes()
    return files


def stored_rows(corpus: Path, origin: str) -> tuple[list[Row], str]:
    """The rows of one stored origin, from inputs/<origin>/ and provenance/<origin>.tsv; and its commit."""
    provenance_path = corpus / "provenance" / f"{origin}.tsv"
    files = stored_files(corpus, origin)
    if not provenance_path.is_file():
        if files:
            raise Refused(f"{provenance_path}: missing, and inputs/{origin}/ holds files")
        return [], ""
    fields, records, columns = read_provenance(provenance_path)
    commit = fields.get("commit", "")
    if not re.fullmatch(r"[0-9a-f]{40}", commit):
        raise Refused(f"{provenance_path}: no forty-digit commit")

    by_path: dict[str, tuple[str, list[list[str]]]] = {}
    if origin == "broiler-vm":
        if columns != ["path", "sha256", "source", "provenance"]:
            raise Refused(f"{provenance_path}: unexpected columns {columns}")
        for path, digest, _source, provenance in records:
            if path in by_path:
                raise Refused(f"{provenance_path}: {path} twice")
            by_path[path] = (digest, [[provenance]])
    else:
        if columns != ["path", "sha256", "goal", "site", "captures", "normalised"]:
            raise Refused(f"{provenance_path}: unexpected columns {columns}")
        for path, digest, goal, site, captures, normalised in records:
            entry = by_path.setdefault(path, (digest, []))
            if entry[0] != digest:
                raise Refused(f"{provenance_path}: {path} with two digests")
            entry[1].append([goal, site, captures, normalised])

    if sorted(by_path) != sorted(files):
        extra = sorted(set(files) - set(by_path))
        absent = sorted(set(by_path) - set(files))
        raise Refused(f"{origin}: stored files and provenance differ: unlisted {extra[:5]}, absent {absent[:5]}")

    rows: list[Row] = []
    for path in sorted(files):
        data = files[path]
        digest, records_for_path = by_path[path]
        if sha256(data) != digest:
            raise Refused(f"{path}: SHA-256 {sha256(data)}, provenance says {digest}")
        if b"\r" in data:
            raise Refused(f"{path}: holds a CR; stored inputs are LF-normalised")
        text = decode(data, path)
        check_field(path, path)
        extension = PurePosixPath(path).suffix
        goal = GOAL_BY_EXTENSION.get(extension)
        if goal is None:
            raise Refused(f"{path}: no goal for the extension {extension!r}")
        if origin == "broiler-vm":
            provenance = records_for_path[0][0]
        else:
            goals = {record[0] for record in records_for_path}
            if goals != {goal}:
                raise Refused(f"{path}: provenance goal {sorted(goals)} against the extension's {goal}")
            sites = sorted(record[1] for record in records_for_path)
            provenance = sites[0] + (f" (+{len(sites) - 1})" if len(sites) > 1 else "")
        variants = {"script": "strict,sloppy", "module": "module", "json": "json"}[goal]
        rows.append(
            Row(origin, path, digest, goal, variants, "-", eval_family(goal, text), check_field(provenance, path))
        )
    return rows, commit


# ---- the manifest ---------------------------------------------------------------------------------------------


def render(rows: list[Row], vm_commit: str, js_commit: str) -> bytes:
    order = {origin: index for index, origin in enumerate(ORIGINS)}
    rows = sorted(rows, key=lambda row: (order[row.origin], row.path))
    keys = [(row.origin, row.path) for row in rows]
    if len(set(keys)) != len(keys):
        raise Refused("two rows share an origin and a path")

    out = io.StringIO()
    out.write("# The parse corpus of milestone SJL-12.4 (bundle sjl-12-001). Written by the builder named below;\n")
    out.write("# never edited by hand. conformance/parse/README.md, section \"The manifest\", defines every field.\n")
    out.write(f"#% format\t{FORMAT[0]}\t{FORMAT[1]}\n")
    out.write(f"#% corpus\t{CORPUS_NAME}\n")
    out.write(f"#% rows\t{len(rows)}\n")
    out.write(f"#% virtual-root\t{VIRTUAL_ROOT}\n")
    out.write(f"#% stored-root\t{STORED_ROOT}\n")
    out.write(f"#% strict-prologue\t{json.dumps(STRICT_PROLOGUE)}\n")
    for name, flags in EVAL_FLAG_SETS:
        value = sum(EVAL_FLAG_BITS[flag] for flag in flags)
        out.write(f"#% eval-flags\t{name}\t{value}\t{','.join(flags)}\n")
    out.write("#% eval-family-markers\t" + "\t".join(EVAL_FAMILY_MARKERS) + "\n")
    out.write(f"#% pin-test262\t{TEST262_REVISION}\n")
    out.write(f"#% pin-test262-archive\t{TEST262_ARCHIVE_PATH}\t{TEST262_ARCHIVE_SHA256}\n")
    out.write(f"#% pin-broiler-vm\t{vm_commit or '-'}\n")
    out.write(f"#% pin-broiler-js\t{js_commit or '-'}\n")
    out.write("#% builder\tscripts/conformance/build_parse_corpus.py\n")
    out.write("\t".join(COLUMNS) + "\n")

    counters = collections.Counter()
    for row in rows:
        counters[row.origin] += 1
        identifier = f"{ID_PREFIX[row.origin]}{counters[row.origin]:06d}"
        out.write(
            "\t".join(
                (
                    identifier,
                    row.origin,
                    row.path,
                    row.sha256,
                    row.goal,
                    row.variants,
                    row.negative,
                    row.eval_family,
                    row.provenance,
                )
            )
            + "\n"
        )
    return out.getvalue().encode("utf-8")


def load_suite(arguments: argparse.Namespace) -> Suite:
    suite = Suite(Path(arguments.test262))
    if arguments.test262_also:
        other = Suite(Path(arguments.test262_also))
        if other.files != suite.files:
            differing = sorted(
                path
                for path in set(suite.files) | set(other.files)
                if suite.files.get(path) != other.files.get(path)
            )
            raise Refused(f"the two test262 sources differ under test/: {differing[:10]}")
    return suite


def build(arguments: argparse.Namespace) -> tuple[bytes, list[str], list[str], list[Row]]:
    corpus = Path(arguments.corpus)
    only = None
    if arguments.only_test262:
        only = {line.strip() for line in Path(arguments.only_test262).read_text().splitlines() if line.strip()}
    suite = load_suite(arguments)
    rows, excluded, notes = test262_rows(suite, only)
    vm_rows, vm_commit = stored_rows(corpus, "broiler-vm")
    js_rows, js_commit = stored_rows(corpus, "broiler-js")
    all_rows = rows + vm_rows + js_rows
    return render(all_rows, vm_commit, js_commit), excluded, notes, all_rows


def summary(rows: list[Row]) -> str:
    counts = collections.Counter()
    for row in rows:
        counts[(row.origin, "inputs")] += 1
        counts[(row.origin, "variants")] += len(row.variants.split(","))
        counts[(row.origin, f"goal {row.goal}")] += 1
        if row.eval_family == "yes":
            counts[(row.origin, "eval-family")] += 1
        if row.origin == "test262" and row.provenance == "test":
            counts[(row.origin, "parse-negative" if row.negative.startswith("parse:") else "not parse-negative")] += 1
    return "\n".join(f"{origin}\t{what}\t{counts[(origin, what)]}" for origin, what in sorted(counts))


def command_manifest(arguments: argparse.Namespace) -> int:
    data, excluded, notes, rows = build(arguments)
    target = Path(arguments.out) if arguments.out else Path(arguments.corpus) / "corpus.manifest"
    target.write_bytes(data)
    print(f"wrote {target} ({len(data)} bytes, SHA-256 {sha256(data)})")
    print(summary(rows))
    for path in excluded:
        print(f"not an input\t{path}")
    for note in notes:
        print(f"flags combined\t{note}")
    return 0


def command_check(arguments: argparse.Namespace) -> int:
    data, _, _, _ = build(arguments)
    target = Path(arguments.corpus) / "corpus.manifest"
    committed = target.read_bytes() if target.is_file() else b""
    if committed == data:
        print(f"identical: {target} (SHA-256 {sha256(data)})")
        return 0
    old = committed.decode("utf-8", "replace").split("\n")
    new = data.decode("utf-8", "replace").split("\n")
    for index, (left, right) in enumerate(zip(old, new)):
        if left != right:
            print(f"line {index + 1} differs:\n  committed {left}\n  rebuilt   {right}")
            break
    else:
        print(f"lengths differ: committed {len(old)} lines, rebuilt {len(new)}")
    return 1


# ---- import-vm ------------------------------------------------------------------------------------------------


def read_source_manifest(path: Path) -> list[tuple[str, str, str, str, str]]:
    rows = []
    for line in path.read_text(encoding="utf-8").split("\n"):
        if not line or line.startswith("#"):
            continue
        fields = line.split("|")
        if len(fields) != 5:
            raise Refused(f"{path}: malformed row {line!r}")
        rows.append(tuple(fields))
    return rows


def command_import_vm(arguments: argparse.Namespace) -> int:
    vm = Path(arguments.vm)
    corpus = Path(arguments.corpus)
    source = vm / VM_SOURCE
    generated = Path(arguments.generated)
    commit = arguments.commit
    if not re.fullmatch(r"[0-9a-f]{40}", commit or ""):
        raise Refused("--commit must be the Broiler.VM commit's forty digits")
    head = subprocess.run(
        ["git", "-C", str(vm), "rev-parse", "HEAD"], capture_output=True, text=True, check=False
    ).stdout.strip()
    if head != commit:
        raise Refused(f"{vm}: checkout at {head}, not {commit}")

    manifest_rows = read_source_manifest(source / "source.manifest")
    by_name: dict[tuple[str, str], tuple[str, str, str]] = {}
    for kind, name, digest, answer, retained in manifest_rows:
        if (kind, name) in by_name:
            raise Refused(f"source.manifest: {kind} {name} twice")
        by_name[(kind, name)] = (digest, answer, retained)

    generated_index: dict[str, str] = {}
    index_path = generated / "generated.tsv"
    for line in index_path.read_text(encoding="utf-8").split("\n"):
        if line and not line.startswith("#"):
            name, member = line.split("\t")
            generated_index[name] = member

    # What the slice compiler's own --write mode writes at the commit: every committed file it writes must be
    # byte-identical to it, and a committed file it no longer writes is kept and marked `js-1-unwritten`.
    written = Path(arguments.write_output)
    written_files = {
        file.relative_to(written).as_posix(): file.read_bytes()
        for kind in VM_KINDS
        for file in sorted((written / kind).iterdir())
        if file.is_file()
    }

    entries: list[tuple[str, bytes, str, str]] = []
    covered: set[tuple[str, str]] = set()
    committed: set[str] = set()
    for kind in VM_KINDS:
        for file in sorted((source / kind).iterdir()):
            if not file.is_file():
                continue
            name = file.stem
            relative = f"{kind}/{file.name}"
            committed.add(relative)
            if (kind, name) not in by_name:
                raise Refused(f"{file}: no source.manifest row for {kind} {name}")
            digest, answer, retained = by_name[(kind, name)]
            data = file.read_bytes()
            if sha256(data) != digest:
                raise Refused(f"{file}: SHA-256 {sha256(data)}, source.manifest says {digest}")
            if retained != "file":
                raise Refused(f"{file}: source.manifest says {retained}, not file")
            if relative in written_files:
                if written_files[relative] != data:
                    raise Refused(f"{file}: differs from what --write writes at {commit}")
                label = "js-1"
            else:
                label = "js-1-unwritten"
            covered.add((kind, name))
            entries.append(
                (
                    f"inputs/broiler-vm/js-1/{relative}",
                    data,
                    f"{VM_SOURCE}/{relative}",
                    f"{label}:{kind}:{name}:{answer}",
                )
            )
    missing = sorted(set(written_files) - committed)
    if missing:
        raise Refused(f"--write writes files the committed directory does not hold: {missing}")
    for (kind, name), (digest, answer, retained) in sorted(by_name.items()):
        if retained == "file":
            if (kind, name) not in covered:
                raise Refused(f"source.manifest: {kind} {name} is retained as a file and none is there")
            continue
        if retained != "generated":
            raise Refused(f"source.manifest: {kind} {name} retained as {retained!r}")
        if name not in generated_index:
            raise Refused(f"{index_path}: no generated text for {name}")
        data = (generated / f"{name}.js").read_bytes()
        if sha256(data) != digest:
            raise Refused(f"{generated}/{name}.js: SHA-256 {sha256(data)}, source.manifest says {digest}")
        member = generated_index[name]
        entries.append(
            (
                f"inputs/broiler-vm/generated/{name}.js",
                data,
                f"SliceSourcePrograms.{member}",
                f"generated:SliceSourcePrograms.{member}:{name}:{answer}",
            )
        )

    for path, data, _, _ in entries:
        if b"\r" in data:
            raise Refused(f"{path}: holds a CR")
        decode(data, path)

    root = corpus / "inputs" / "broiler-vm"
    if root.exists():
        shutil.rmtree(root)
    lines = [
        "# The broiler-vm inputs of the parse corpus: each stored file, its SHA-256, where it was taken from, and the",
        "# manifest's provenance field. Written by scripts/conformance/build_parse_corpus.py import-vm.",
        f"#% commit\t{commit}",
        "path\tsha256\tsource\tprovenance",
    ]
    for path, data, origin_path, provenance in sorted(entries):
        target = corpus / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(data)
        lines.append(f"{path}\t{sha256(data)}\t{origin_path}\t{provenance}")
    (corpus / "provenance").mkdir(parents=True, exist_ok=True)
    (corpus / "provenance" / "broiler-vm.tsv").write_bytes(("\n".join(lines) + "\n").encode("utf-8"))
    print(f"broiler-vm: {len(entries)} inputs from {source} and {generated} at {commit}")
    return 0


# ---- import-js ------------------------------------------------------------------------------------------------


def command_import_js(arguments: argparse.Namespace) -> int:
    corpus = Path(arguments.corpus)
    commit = arguments.commit
    if not re.fullmatch(r"[0-9a-f]{40}", commit or ""):
        raise Refused("--commit must be the Broiler.JS commit's forty digits")

    # (normalised bytes' SHA-256, goal) -> data, sites (site -> captures), whether normalising changed any text
    inputs: dict[tuple[str, str], bytes] = {}
    sites: dict[tuple[str, str], collections.Counter] = collections.defaultdict(collections.Counter)
    normalised: dict[tuple[str, str], bool] = collections.defaultdict(bool)
    records = 0
    for capture in arguments.capture:
        capture_dir = Path(capture)
        index = capture_dir / "index.tsv"
        for line in index.read_text(encoding="utf-8").split("\n"):
            if not line:
                continue
            fields = line.split("\t")
            if len(fields) != 4:
                raise Refused(f"{index}: malformed record {line!r}")
            raw_digest, length, goal, site = fields
            if goal not in EXTENSION_BY_GOAL:
                raise Refused(f"{index}: goal {goal!r}")
            raw = (capture_dir / f"{raw_digest}.js").read_bytes()
            if sha256(raw) != raw_digest or len(raw) != int(length):
                raise Refused(f"{capture_dir}/{raw_digest}.js: does not match its record")
            data = lf_normalise(raw)
            decode(data, f"{capture_dir}/{raw_digest}.js")
            key = (sha256(data), goal)
            inputs[key] = data
            sites[key][check_field(site, str(index))] += 1
            if data != raw:
                normalised[key] = True
            records += 1

    root = corpus / "inputs" / "broiler-js"
    if root.exists():
        shutil.rmtree(root)
    root.mkdir(parents=True)
    lines = [
        "# The broiler-js inputs of the parse corpus: for each stored file, every test method whose run constructed a",
        "# FastTokenStream over its text, with the number of times it did, and whether the stored text is the captured",
        "# one LF-normalised. Written by scripts/conformance/build_parse_corpus.py import-js.",
        f"#% commit\t{commit}",
        "#% projects\t" + "\t".join(CAPTURED_PROJECTS),
        "path\tsha256\tgoal\tsite\tcaptures\tnormalised",
    ]
    for (digest, goal), data in sorted(inputs.items(), key=lambda item: (item[0][0], item[0][1])):
        path = f"inputs/broiler-js/{digest}{EXTENSION_BY_GOAL[goal]}"
        (corpus / path).write_bytes(data)
        for site, captures in sorted(sites[(digest, goal)].items()):
            flag = "yes" if normalised[(digest, goal)] else "no"
            lines.append(f"{path}\t{digest}\t{goal}\t{site}\t{captures}\t{flag}")
    lines_sorted = lines[:6] + sorted(lines[6:])
    (corpus / "provenance").mkdir(parents=True, exist_ok=True)
    (corpus / "provenance" / "broiler-js.tsv").write_bytes(("\n".join(lines_sorted) + "\n").encode("utf-8"))
    changed = sum(1 for key in inputs if normalised[key])
    print(f"broiler-js: {records} captures, {len(inputs)} inputs ({changed} changed by LF normalisation) at {commit}")
    return 0


# ---- entry ----------------------------------------------------------------------------------------------------


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    sub = parser.add_subparsers(dest="command", required=True)
    default_corpus = str(Path(__file__).resolve().parents[2] / "conformance" / "parse")

    for name in ("manifest", "check"):
        mode = sub.add_parser(name)
        mode.add_argument("--test262", required=True, help="the pin's archive (.tar.gz) or a checkout at the pin")
        mode.add_argument("--test262-also", help="the other of the two, which must agree byte for byte")
        mode.add_argument("--corpus", default=default_corpus)
        mode.add_argument("--only-test262", help="a file of test262 paths: build a sample over these alone")
        if name == "manifest":
            mode.add_argument("--out", help="where to write the manifest (default: <corpus>/corpus.manifest)")

    vm = sub.add_parser("import-vm")
    vm.add_argument("--vm", required=True, help="a Broiler.VM checkout at --commit")
    vm.add_argument("--commit", required=True)
    vm.add_argument("--generated", required=True, help="the generated members' texts and generated.tsv")
    vm.add_argument("--write-output", required=True, help="the source/ directory the slice compiler's --write wrote")
    vm.add_argument("--corpus", default=default_corpus)

    js = sub.add_parser("import-js")
    js.add_argument("--capture", required=True, action="append", help="a capture directory (index.tsv)")
    js.add_argument("--commit", required=True)
    js.add_argument("--corpus", default=default_corpus)

    arguments = parser.parse_args(argv)
    try:
        return {
            "manifest": command_manifest,
            "check": command_check,
            "import-vm": command_import_vm,
            "import-js": command_import_js,
        }[arguments.command](arguments)
    except Refused as refusal:
        print(f"build_parse_corpus: refused: {refusal}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    sys.exit(main())
