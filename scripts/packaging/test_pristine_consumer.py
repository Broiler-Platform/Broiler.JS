#!/usr/bin/env python3
"""Pack a package set, restore it into a clean consumer, and run it.

The engine set (the default) packs the runtime graph of Broiler.JavaScript.All and executes
JavaScript. The shared set (--set shared) packs every packable project of Broiler.ECMAScript.slnf,
the shared libraries' own version stream, and runs a consumer of their contract types; the restore
maps the Broiler.ECMAScript prefix to the local feed alone, so a package that only nuget.org could
supply fails it."""

from __future__ import annotations

import argparse
import html
import json
import os
from pathlib import Path
import subprocess
import tempfile
import xml.etree.ElementTree as ET
import zipfile


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_PROJECT = REPOSITORY_ROOT / "Broiler.JS" / "Broiler.JavaScript.All" / "Broiler.JavaScript.All.csproj"
SHARED_FILTER = REPOSITORY_ROOT / "Broiler.ECMAScript.slnf"
SHARED_PREFIX = "Broiler.ECMAScript."

# The packages Broiler.JavaScript.BuiltIns declares, as its project file references them. This list
# named Broiler.DateTime and Broiler.Unicode.Properties until 2026-10-12, which BuiltIns no longer
# references; it now names what the project does (repaired at milestone SJL-2.13). Milestone SJL-7.5
# added Broiler.Unicode.Segmentation, and the Runtime's Broiler.Unicode.Algorithms (Ast's since
# milestone SJL-12.5) below.
BUILT_INS_DEPENDENCIES = {"Broiler.Regex", "Broiler.Unicode.Cldr.LocaleData", "Broiler.Unicode.Segmentation"}

# The Unicode area's code package, whose tables Broiler.JavaScript.Ast embeds, which Ast's package declares.
# No package that carries tables is among them. Until 2026-10-21 the Runtime package was checked for it
# (milestone SJL-7.5), when the Runtime embedded the tables; milestone SJL-12.5 moved the tables and the
# package reference to Ast (Broiler.VM's JSD-0080, decision D20), so the Ast package is checked for it,
# and the Runtime package, which reaches the tables through Ast, is checked for Ast.
AST_DEPENDENCIES = {"Broiler.Unicode.Algorithms"}
RUNTIME_DEPENDENCIES = {"Broiler.JavaScript.Ast"}


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


def evaluate(project: Path, configuration: str, version: str | None, *names: str) -> dict[str, str]:
    arguments = ["dotnet", "msbuild", str(project), "-nologo", f"-p:Configuration={configuration}"]
    if version:
        arguments += [f"-p:Version={version}", f"-p:PackageVersion={version}"]
    arguments += [f"-getProperty:{name}" for name in names]
    output = subprocess.run(arguments, cwd=REPOSITORY_ROOT, check=True, capture_output=True, text=True).stdout
    return json.loads(output)["Properties"]


def shared_consumer(arguments: argparse.Namespace) -> int:
    """Pack the shared set, restore it from a local feed alone, and run a consumer of its contract types."""
    filter_document = json.loads(SHARED_FILTER.read_text(encoding="utf-8"))
    projects = [REPOSITORY_ROOT / path for path in filter_document["solution"]["projects"]]
    temporary = Path(tempfile.mkdtemp(prefix="broiler-pristine-shared-"))
    feed = temporary / "feed"
    consumer = temporary / "consumer"
    feed.mkdir()
    consumer.mkdir()
    try:
        packed: list[tuple[str, str]] = []
        for project in projects:
            properties = evaluate(project, arguments.configuration, arguments.version, "IsPackable", "PackageId", "PackageVersion")
            if properties["IsPackable"].lower() != "true":
                continue
            pack = ["dotnet", "pack", str(project), "-c", arguments.configuration, "-p:IncludeSymbols=false", "--output", str(feed)]
            if arguments.version:
                pack += [f"-p:Version={arguments.version}", f"-p:PackageVersion={arguments.version}"]
            run(pack, cwd=REPOSITORY_ROOT)
            packed.append((properties["PackageId"], properties["PackageVersion"]))
        if not packed:
            raise RuntimeError("Broiler.ECMAScript.slnf has no packable project")
        identities = {package_identity(path) for path in feed.glob("*.nupkg")}
        if identities != set(packed):
            raise RuntimeError(f"packed {sorted(identities)}, expected {sorted(packed)}")

        references = "\n".join(
            f'    <PackageReference Include="{html.escape(package_id)}" Version="[{html.escape(version)}]" />'
            for package_id, version in sorted(packed))
        (consumer / "Consumer.csproj").write_text(
            f"""<Project Sdk=\"Microsoft.NET.Sdk\">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
{references}
  </ItemGroup>
</Project>
""",
            encoding="utf-8",
        )
        (consumer / "Program.cs").write_text(
            """using System;
using Broiler.ECMAScript;

ulong charged = 0;
WorkCharge meter = units => charged += units;
meter(2);
var failure = new Failure(ResultStatus.Malformed, 3, "number.malformed-exponent");
var limit = new LengthLimit(8);
if (charged != 2 || failure.Position != 3 || !limit.Admits(8) || limit.Admits(9) || new DepthLimit(1).Admits(2))
    throw new InvalidOperationException("the contract types do not behave as documented");
Console.WriteLine("pristine-consumer: Broiler.ECMAScript contract types");
""",
            encoding="utf-8",
        )
        (temporary / "NuGet.Config").write_text(
            f"""<?xml version=\"1.0\" encoding=\"utf-8\"?>
<configuration>
  <packageSources>
    <clear />
    <add key=\"shared-local\" value=\"{html.escape(str(feed))}\" />
    <add key=\"nuget.org\" value=\"https://api.nuget.org/v3/index.json\" />
  </packageSources>
  <packageSourceMapping>
    <clear />
    <packageSource key=\"shared-local\"><package pattern=\"{SHARED_PREFIX}*\" /></packageSource>
    <packageSource key=\"nuget.org\"><package pattern=\"*\" /></packageSource>
  </packageSourceMapping>
</configuration>
""",
            encoding="utf-8",
        )
        clean_environment = os.environ.copy()
        clean_environment["NUGET_PACKAGES"] = str(temporary / "packages")
        run(["dotnet", "restore", "--configfile", str(temporary / "NuGet.Config"), "--no-http-cache"], cwd=consumer, env=clean_environment)
        run(["dotnet", "run", "-c", "Release", "--no-restore"], cwd=consumer, env=clean_environment)
        print(f"Validated {', '.join(f'{i} {v}' for i, v in sorted(packed))} from a local feed in an isolated cache")
        return 0
    finally:
        if arguments.keep:
            print(f"Retained: {temporary}")
        else:
            import shutil

            shutil.rmtree(temporary, ignore_errors=True)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--set", choices=["engine", "shared"], default="engine", help="The package set to pack and consume")
    parser.add_argument("--project", type=Path, default=DEFAULT_PROJECT)
    parser.add_argument("--configuration", default="Release")
    parser.add_argument("--version", help="The shared set's version to pack, for example 0.0.1-probe.1")
    parser.add_argument("--keep", action="store_true", help="Retain the temporary consumer directory")
    arguments = parser.parse_args()

    if arguments.set == "shared":
        return shared_consumer(arguments)

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
        missing_runtime_dependencies = BUILT_INS_DEPENDENCIES - package_dependencies(built_ins_package)
        if missing_runtime_dependencies:
            raise RuntimeError(
                f"BuiltIns package is missing runtime dependencies: {sorted(missing_runtime_dependencies)}"
            )
        ast_package = next(
            path for (package_id, _), path in identities.items() if package_id == "Broiler.JavaScript.Ast"
        )
        missing_ast_dependencies = AST_DEPENDENCIES - package_dependencies(ast_package)
        if missing_ast_dependencies:
            raise RuntimeError(f"Ast package is missing dependencies: {sorted(missing_ast_dependencies)}")
        runtime_package = next(
            path for (package_id, _), path in identities.items() if package_id == "Broiler.JavaScript.Runtime"
        )
        missing_runtime_dependencies = RUNTIME_DEPENDENCIES - package_dependencies(runtime_package)
        if missing_runtime_dependencies:
            raise RuntimeError(
                f"Runtime package is missing dependencies: {sorted(missing_runtime_dependencies)}"
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
