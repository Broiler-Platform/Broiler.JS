#!/usr/bin/env python3
"""Resolve Broiler.JS test262 assembly groupings.

The mapping from a Broiler.JS assembly name (e.g. ``parser``, ``compiler``,
``runtime``, ``builtins``, ``intl``, ``annexb``) to a list of test262 path
prefixes lives in ``scripts/compliance/test262-assemblies.json``. This helper
exposes that mapping in two forms used by ``.github/workflows/test262-by-assembly.yml``
and by contributors who want to run ``run_test262.py`` for a single assembly
locally:

* ``--selection <name>`` prints a compact JSON array of assembly names suitable
  for use as a GitHub Actions matrix (``all`` expands to every assembly).
* ``--paths-for <name>`` prints the test262 paths for a single assembly, one
  path per line, suitable for passing to ``run_test262.py --path-file``.

Both modes support ``--output <path>`` to write the result to a file instead of
stdout.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def _load_manifest(manifest_path: Path) -> dict[str, dict[str, object]]:
    data = json.loads(manifest_path.read_text(encoding="utf-8"))
    assemblies = data.get("assemblies")
    if not isinstance(assemblies, dict) or not assemblies:
        raise SystemExit(
            f"Manifest {manifest_path} is missing a non-empty 'assemblies' object"
        )
    return assemblies


def _resolve_selection(selection: str, assemblies: dict[str, dict[str, object]]) -> list[str]:
    if selection == "all":
        return sorted(assemblies)
    if selection not in assemblies:
        raise SystemExit(
            f"Unknown assembly '{selection}'. Known: {', '.join(sorted(assemblies))}"
        )
    return [selection]


def _resolve_paths(name: str, assemblies: dict[str, dict[str, object]]) -> list[str]:
    if name not in assemblies:
        raise SystemExit(
            f"Unknown assembly '{name}'. Known: {', '.join(sorted(assemblies))}"
        )
    entry = assemblies[name]
    paths = entry.get("paths") if isinstance(entry, dict) else None
    if not isinstance(paths, list) or not paths:
        raise SystemExit(f"Assembly '{name}' has no test262 paths configured")
    return [str(p) for p in paths]


def _emit(text: str, output: str | None) -> None:
    if output:
        Path(output).parent.mkdir(parents=True, exist_ok=True)
        Path(output).write_text(text, encoding="utf-8")
    else:
        sys.stdout.write(text)
        if not text.endswith("\n"):
            sys.stdout.write("\n")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--manifest",
        required=True,
        help="Path to test262-assemblies.json",
    )
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument(
        "--selection",
        help="Assembly name to plan ('all' expands to every assembly).",
    )
    group.add_argument(
        "--paths-for",
        dest="paths_for",
        help="Assembly name whose test262 paths should be emitted.",
    )
    parser.add_argument(
        "--output",
        help="Optional file to write the result to; defaults to stdout.",
    )
    args = parser.parse_args()

    manifest_path = Path(args.manifest).resolve()
    assemblies = _load_manifest(manifest_path)

    if args.selection is not None:
        resolved = _resolve_selection(args.selection, assemblies)
        _emit(json.dumps(resolved), args.output)
    else:
        resolved = _resolve_paths(args.paths_for, assemblies)
        _emit("\n".join(resolved) + "\n", args.output)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
