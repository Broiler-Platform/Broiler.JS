#!/usr/bin/env python3
"""Rebuild persisted source-generator output twice and compare content hashes."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import subprocess


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_PROJECT = REPOSITORY_ROOT / "Broiler.JS" / "Broiler.JavaScript.BuiltIns" / "Broiler.JavaScript.BuiltIns.csproj"


def snapshot(directory: Path) -> dict[str, str]:
    return {
        str(path.relative_to(directory)).replace("\\", "/"): hashlib.sha256(path.read_bytes()).hexdigest()
        for path in sorted(directory.rglob("*.cs"))
    }


def rebuild(project: Path, configuration: str, framework: str) -> dict[str, str]:
    subprocess.run(
        [
            "dotnet",
            "build",
            str(project),
            "-c",
            configuration,
            "--no-restore",
            "-t:Rebuild",
            "-p:BroilerPersistGeneratedFiles=true",
            "-p:WarningLevel=0",
        ],
        cwd=REPOSITORY_ROOT,
        check=True,
    )
    generated = project.parent / "obj" / "generated" / configuration / framework
    result = snapshot(generated)
    if not result:
        raise RuntimeError(f"No persisted generated C# files found below {generated}")
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, default=DEFAULT_PROJECT)
    parser.add_argument("--configuration", default="Release")
    parser.add_argument("--framework", default="net10.0")
    parser.add_argument("--output", type=Path)
    arguments = parser.parse_args()

    project = arguments.project.resolve()
    first = rebuild(project, arguments.configuration, arguments.framework)
    second = rebuild(project, arguments.configuration, arguments.framework)
    if first != second:
        removed = sorted(first.keys() - second.keys())
        added = sorted(second.keys() - first.keys())
        changed = sorted(name for name in first.keys() & second.keys() if first[name] != second[name])
        raise RuntimeError(
            f"Generated output is not deterministic; removed={removed}, added={added}, changed={changed}"
        )

    report = {
        "project": str(project.relative_to(REPOSITORY_ROOT)).replace("\\", "/"),
        "configuration": arguments.configuration,
        "framework": arguments.framework,
        "file_count": len(first),
        "files": first,
    }
    if arguments.output:
        output = arguments.output.resolve()
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(json.dumps(report, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(f"generator-determinism: {len(first)} files match")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
