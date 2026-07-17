#!/usr/bin/env python3
"""Pack the runtime graph, restore it into a clean consumer, and execute JavaScript."""

from __future__ import annotations

import argparse
import html
import os
from pathlib import Path
import subprocess
import tempfile
import xml.etree.ElementTree as ET
import zipfile


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_PROJECT = REPOSITORY_ROOT / "Broiler.JS" / "Broiler.JavaScript.All" / "Broiler.JavaScript.All.csproj"


def run(arguments: list[str], *, cwd: Path, env: dict[str, str] | None = None) -> None:
    print("+", " ".join(arguments), flush=True)
    subprocess.run(arguments, cwd=cwd, env=env, check=True)


def runtime_project_closure(root: Path) -> list[Path]:
    """Return dependencies before dependants, excluding analyzer-only references."""
    ordered: list[Path] = []
    visited: set[Path] = set()

    def visit(project: Path) -> None:
        project = project.resolve()
        if project in visited:
            return
        visited.add(project)

        document = ET.parse(project)
        for reference in document.findall(".//ProjectReference"):
            if reference.get("ReferenceOutputAssembly", "true").lower() == "false":
                continue
            if reference.get("OutputItemType", "").lower() == "analyzer":
                continue
            include = reference.get("Include")
            if not include or "$" in include:
                continue
            dependency = (project.parent / include.replace("\\", os.sep)).resolve()
            if dependency.exists():
                visit(dependency)

        ordered.append(project)

    visit(root)
    return ordered


def package_identity(package: Path) -> tuple[str, str]:
    with zipfile.ZipFile(package) as archive:
        nuspec_name = next(name for name in archive.namelist() if name.endswith(".nuspec"))
        nuspec = ET.fromstring(archive.read(nuspec_name))
    values = {
        element.tag.rsplit("}", 1)[-1]: element.text
        for element in nuspec.iter()
        if element.tag.rsplit("}", 1)[-1] in {"id", "version"}
    }
    package_id = values.get("id")
    version = values.get("version")
    if package_id is None or version is None:
        raise RuntimeError(f"Could not read package identity from {package}")
    return package_id, version


def package_dependencies(package: Path) -> set[str]:
    with zipfile.ZipFile(package) as archive:
        nuspec_name = next(name for name in archive.namelist() if name.endswith(".nuspec"))
        nuspec = ET.fromstring(archive.read(nuspec_name))
    return {
        element.get("id")
        for element in nuspec.iter()
        if element.tag.rsplit("}", 1)[-1] == "dependency" and element.get("id")
    }


def is_multi_targeted(project: Path) -> bool:
    document = ET.parse(project)
    return any(
        ";" in (element.text or "")
        for element in document.iter()
        if element.tag.rsplit("}", 1)[-1] == "TargetFrameworks"
    )


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, default=DEFAULT_PROJECT)
    parser.add_argument("--configuration", default="Release")
    parser.add_argument("--keep", action="store_true", help="Retain the temporary consumer directory")
    arguments = parser.parse_args()

    project = arguments.project.resolve()
    if not project.exists():
        parser.error(f"project does not exist: {project}")

    temporary = Path(tempfile.mkdtemp(prefix="broiler-pristine-consumer-"))
    feed = temporary / "feed"
    consumer = temporary / "consumer"
    feed.mkdir()
    consumer.mkdir()

    try:
        run(["dotnet", "restore", str(project)], cwd=REPOSITORY_ROOT)
        run(
            ["dotnet", "build", str(project), "-c", arguments.configuration, "--no-restore"],
            cwd=REPOSITORY_ROOT,
        )

        for dependency in runtime_project_closure(project):
            pack_arguments = [
                "dotnet",
                "pack",
                str(dependency),
                "-c",
                arguments.configuration,
                "--no-restore",
                "-p:IncludeSymbols=false",
                "--output",
                str(feed),
            ]
            # The aggregate build creates the compatible net10 output. Multi-targeted
            # leaf packages still need their other lib/ref assets built before packing.
            if not is_multi_targeted(dependency):
                pack_arguments.insert(6, "--no-build")
            run(pack_arguments, cwd=REPOSITORY_ROOT)

        packages = [path for path in feed.glob("*.nupkg") if not path.name.endswith(".symbols.nupkg")]
        identities = {package_identity(path): path for path in packages}
        built_ins_package = next(
            path for (package_id, _), path in identities.items() if package_id == "Broiler.JavaScript.BuiltIns"
        )
        missing_runtime_dependencies = {
            "Broiler.Regex",
            "Broiler.DateTime",
            "Broiler.Unicode.Properties",
        } - package_dependencies(built_ins_package)
        if missing_runtime_dependencies:
            raise RuntimeError(
                f"BuiltIns package is missing runtime dependencies: {sorted(missing_runtime_dependencies)}"
            )
        root_id = "Broiler.JavaScript.All"
        root_versions = sorted(version for package_id, version in identities if package_id == root_id)
        if len(root_versions) != 1:
            raise RuntimeError(f"Expected one {root_id} package, found versions: {root_versions}")
        root_version = root_versions[0]

        (consumer / "Consumer.csproj").write_text(
            f"""<Project Sdk=\"Microsoft.NET.Sdk\">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=\"{root_id}\" Version=\"{root_version}\" />
  </ItemGroup>
</Project>
""",
            encoding="utf-8",
        )
        (consumer / "Program.cs").write_text(
            """using System;
using Broiler.JavaScript.Engine;

using var context = new JSContext();
var result = context.Eval("Array.from([20, 22]).reduce((a, b) => a + b, 0)");
if (result.ToString() != "42")
    throw new InvalidOperationException($"Expected 42, received {result}");
Console.WriteLine("pristine-consumer: 42");
""",
            encoding="utf-8",
        )
        (temporary / "NuGet.Config").write_text(
            f"""<?xml version=\"1.0\" encoding=\"utf-8\"?>
<configuration>
  <packageSources>
    <clear />
    <add key=\"phase1-local\" value=\"{html.escape(str(feed))}\" />
    <add key=\"nuget.org\" value=\"https://api.nuget.org/v3/index.json\" />
  </packageSources>
</configuration>
""",
            encoding="utf-8",
        )

        clean_environment = os.environ.copy()
        clean_environment["NUGET_PACKAGES"] = str(temporary / "packages")
        run(
            ["dotnet", "restore", "--configfile", str(temporary / "NuGet.Config")],
            cwd=consumer,
            env=clean_environment,
        )
        run(["dotnet", "run", "-c", "Release", "--no-restore"], cwd=consumer, env=clean_environment)
        print(f"Validated {root_id} {root_version} from {len(packages)} locally produced packages")
        return 0
    finally:
        if arguments.keep:
            print(f"Retained: {temporary}")
        else:
            import shutil

            shutil.rmtree(temporary, ignore_errors=True)


if __name__ == "__main__":
    raise SystemExit(main())
