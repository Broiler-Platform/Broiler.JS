#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Collect Broiler.JS's evidence for milestone SJL-7 (work package SJL-7.6) into bundle sjl-7-001.

    python3 docs/evidence/sjl-7-001/collect.py --tree <a clean checkout of the after commit> \
        [--owner-tree <a checkout of Broiler.UniCode holding its publishing commit>] \
        [--artifact-zip <part A's after run's test262-evidence artifact> --run-url <its workflow run> \
         --artifact-id <its id> --run-times <when it was dispatched and completed>] \
        [--bench-tree <a clean checkout of the rule's commit> --lane smoke|controlled \
         [--lane-record <the lane's record>] [--dry-run]] \
        --out docs/evidence/sjl-7-001 --only STEP [STEP ...]

THE RULE IS decision-rule.md BESIDE THIS FILE, committed at SJL-7.1 in 3916bd2b before any code of SJL-7.
Part A takes the whole pinned suite, negative-metadata tests included, through
.github/workflows/test262-evidence.yml. Its base was taken at the commit that carries the rule and imported
by hand into test262/base/ with its imported.txt; this collector imports the after run the same way, into
test262/after/, and compares the two through scripts/compliance/compare_test262_paths.py as the rule fixes
it, with test262/comparison/classifications.json beside the report. Part B is the measurement over
UnicodeBenchmarks, which the measure step takes into measurement/, a sub-bundle of its own.

THE AFTER ARM IS A CHECKOUT, NOT THIS TREE. Every local build and run is taken at `--tree`, a clean checkout of
a commit whose product sources are those of the commit that lands SJL-7.5's cut-over; the identity step lists
every file there that differs. Part B's arm B is `--bench-tree`, a clean checkout of the rule's commit.

IT REUSES BUNDLE SJL-6-001'S COLLECTOR, and through it sjl-5-001's, sjl-4-001's, sjl-3-001's and sjl-0-001's,
for its helpers, the import of a workflow's run, the comparison and its checks, the pristine consumer and the
manifest; its measure step follows sjl-3-001's, with this rule's workloads, answers and lane record. What it
adds is SJL-7's: a home that is another repository's published packages over the tables this engine embeds
from its own pin, a pristine consumer of the Unicode area, the area's deletion list and the cut-over's
searches repeated, and the search for a surviving duplicate of the owner's API.

THE COLLECTOR JUDGES NOTHING. Each step writes what it ran and what it printed, failures included; the README
reads them under the rule.

Steps, each with `--only`:

  identity        the after commit and its cleanliness, the cut-over's commits and the files under the product
                  paths that differ between its last commit and the after commit, the rule commit, its ancestry
                  and the files it fixes unchanged since, the SDK, Node and the machine, into identity.txt;
  home            the owner's packages as this engine restores them (their references, the package closure, each
                  package's SHA-512 against the copy nuget.org serves now, its repository signature and its
                  dependencies), this engine's pin against the owner's at its publishing commit, and the area's
                  tests built in Release and run under TZ=UTC, the regeneration of the embedded tables byte for
                  byte among them, into home/;
  test262         part A's after run imported into test262/after/, unchanged, with imported.txt;
  compare         the two runs' completeness and host-zone records, the comparison with the base, with
                  test262/comparison/classifications.json when it exists, and the script's checks, into
                  test262/comparison/; it may be run again, the import may not;
  consumer        the engine set packed and restored by a pristine consumer, which then runs the Unicode area's
                  corrections from the same isolated cache, into engine/;
  simplification  the cut-over's two searches repeated at the after commit and set against the cut-over's, the
                  area's Broiler.JS deletion list entry by entry, and the search for a surviving duplicate of the
                  owner's API in the engine's product sources, into simplification/;
  measure         part B: arms B, A and A-again rotated by repetition, seven runs each, into measurement/, a
                  sub-bundle of its own, whose manifest is written and made read-only before any run; with
                  --dry-run it checks the arms and prints the protocol, and builds, runs and writes nothing;
  manifest        manifest.json: the SHA-256 of every file the bundle retains.

Paths in retained logs are written as <tree>, <bench>, <owner>, <repo>, <run>, <scratch> and <tmp>.
"""

from __future__ import annotations

import argparse
import base64
import gzip
import hashlib
import importlib.util
import json
import os
import re
import shutil
import statistics
import subprocess
import sys
import urllib.request
import zipfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
DATE_BUNDLE = HERE.parent / "sjl-6-001"

_spec = importlib.util.spec_from_file_location("sjl_6_001_collect", DATE_BUNDLE / "collect.py")
date = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(date)
prior = date.prior  # sjl-3-001's collector: its measure step, LANES and utc()
base = date.base  # sjl-0-001's: the suite revision, the benchmark project and the repetitions

RULE = "docs/evidence/sjl-7-001/decision-rule.md"
BENCH = base.BENCH_PROJECT
BENCH_FILES = (BENCH + "/UnicodeBenchmarks.cs", BENCH + "/Program.cs", BENCH + "/BenchmarkConfiguration.cs")
# What the rule fixes with itself: it, the comparison script, the workflow part A dispatches, and part B's
# workloads with the program and the configuration that run them.
RULE_FILES = (RULE, "scripts/compliance/compare_test262_paths.py", ".github/workflows/test262-evidence.yml",
              *BENCH_FILES)
# The rule's commit, which carries part A's base and is part B's arm B.
BASE_COMMIT = "3916bd2b13f672239ae7a0e1498f98aff0c2f10d"
# The commit that lands SJL-7.5's cut-over (corpus revision 20), part A's after run and part B's arm A.
AFTER_COMMIT = "a34b1f78cc4bda6bcdfe68ed943af6cc069c1b71"
# SJL-7.5's first and last commits that change product sources; the after commit's product sources are the last's.
CUT_OVER_FIRST = "3374c933"
CUT_OVER = "c7a19d35abf664b774ae55c55b1b524bc5d75486"
# The paths whose files are product sources or decide what the product restores and embeds.
PRODUCT_PATHS = ("Broiler.JS", "Broiler.ECMAScript", "data/unicode", "Directory.Packages.props",
                 "Directory.Build.props", "Directory.Build.targets", "NuGet.config", "global.json")
# The owner: Broiler.Unicode's packages at the version nuget.org serves, published from this commit.
OWNER_VERSION = "0.1.0-preview.4"
OWNER_COMMIT = "6e14f93ad753094c99f0013d22269b47550ae552"
# The owner's code packages; the generator, which reaches the tests alone; and the two that arrive through
# Broiler.Regex until SJL-11, which no code of this engine calls.
UNICODE_PACKAGES = (("Broiler.Unicode.Algorithms", OWNER_VERSION), ("Broiler.Unicode.Segmentation", OWNER_VERSION),
                    ("Broiler.Unicode.Generation", OWNER_VERSION), ("Broiler.UniCode.Properties", "0.1.0-preview.3"),
                    ("Broiler.UniCode.Emoji.StringProperties", "0.1.0-preview.3"))
PIN = "data/unicode/pins"
ENGINE_ROOT = "Broiler.JS/Broiler.JavaScript.All/Broiler.JavaScript.All.csproj"
# The area's tests, by project, as the changelog's SJL-7.5 section names them.
AREA_TESTS = {
    "Broiler.JS/Broiler.JavaScript.Runtime.Tests": ("JSUnicodeTests", "UnicodeTableRegenerationTests"),
    "Broiler.JS/Broiler.JavaScript.BuiltIns.Tests": ("SharedUnicodeTests", "UnicodeDependencyTests"),
    "Broiler.JS/Broiler.JavaScript.Parser.Tests": ("UnicodeLexicalClassTests",),
    "Broiler.JS/Broiler.JavaScript.Ast.Tests": ("StringSpanTrimTests",),
}
STEPS = ("identity", "home", "test262", "compare", "consumer", "simplification", "measure", "manifest")

# Part B's protocol (decision-rule.md, part B).
ANSWERS = "--unicode-answers"
FILTER = "*UnicodeBenchmarks*"
WORKLOADS = ("Normalize", "NormalizeControl", "Casing", "CasingControl", "Identifiers", "IdentifiersControl")
MEASURED = ("Normalize", "Casing", "Identifiers")
REPETITIONS = base.REPETITIONS
LANES = prior.LANES
# What keeps a build server from outliving the builds and running beside the lanes.
NO_BUILD_SERVERS = {"MSBUILDDISABLENODEREUSE": "1", "DOTNET_CLI_USE_MSBUILD_SERVER": "0", "UseSharedCompilation": "false"}

# The area's Broiler.JS deletion list (Broiler.VM docs/javascript-shared-libraries/milestones/sjl-7-unicode.md,
# SJL-7.5, with what JSD-0074 adds to it), entry by entry: what was deleted or replaced, the file or folder
# that held it, and what is searched there. A call to the owner, an adapter or a comment shows its line.
JS = "Broiler.JS/Broiler.JavaScript.BuiltIns/"
PARSER = "Broiler.JS/Broiler.JavaScript.Parser/"
DELETED = (
    ("the host string.Normalize in JSStringPrototype.Transform and in the collator's NFD step",
     [JS + "String/JSStringPrototype.Transform.cs", JS + "Intl/JSIntl.cs"],
     r"\.Normalize\(|NormalizationForm|\bIsNormalized\b|\bNormalizeText\("),
    ("JSStringSpecialCasing usage, and the CultureInfo resolution for locale casing", [JS + "String"],
     r"\bJSStringSpecialCasing\b|CultureInfo|TextInfo|To(Upper|Lower)Invariant|\.To(Upper|Lower)\(|"
     r"Broiler\.Unicode\.Properties|JSUnicode\.CaseMapping"),
    ("CharExtensions' identifier classification and its Unicode-17 patch tables", [PARSER + "CharExtensions.cs"],
     r"Identifier|UnicodeV17|InRanges|char\.Is(Letter|LetterOrDigit|WhiteSpace|Digit)|UnicodeCategory|"
     r"CharUnicodeInfo|Rune\."),
    ("the whitespace lists: the scanner's, StringSpan.Trim's and JSRegExp.Escape's",
     [PARSER + "FastScanner.cs", "Broiler.JS/Broiler.JavaScript.Ast/Misc/StringSpan.cs", JS + "RegExp/JSRegExp.cs"],
     r"char\.IsWhiteSpace|IsNullOrWhiteSpace|\\x85|\\u0085|0x0?085\b|\\u00[aA]0|\\x[aA]0|\\u1680|\\u2000|"
     r"\\u200[aA]|\\u202[fF]|\\u205[fF]|\\u3000|\\u[fF][eE][fF][fF]|JSUnicode\.IsWhiteSpace|TrimStrWhiteSpace"),
    ("JSRegExp's identifier classes for group names, and its loose property lookup", [JS + "RegExp"],
     r"IsRegExpIdentifier|NormalizeKey|EmojiStringPropertyNames|char\.IsLetter|UnicodeCategory|"
     r"Broiler\.Unicode\.Properties|UnicodeEmoji|JSUnicode\.(IsIdentifier|Properties|StringProperties)"),
    ("JSIntl's ComputeGraphemes, ComputeWords, ComputeSentences and IsWordLike", [JS + "Intl"],
     r"ComputeGraphemes|ComputeWords|ComputeSentences|IsWordLike\(|StringInfo|TextElementEnumerator|"
     r"UnicodeSegmenter|GetWordStatus"),
    ("JSD-0074: IsValidNativeFunctionToStringName's char.IsLetter", [JS + "BuiltInsAssemblyInitializer.cs"],
     r"IsValidNativeFunctionToStringName|char\.IsLetter|JSUnicode\.IsIdentifier"),
    ("JSD-0074: the collator's IsNormalized", [JS + "Intl"], r"\bIsNormalized\b|JSUnicode\.Normalization"),
    ("JSD-0074: the translator's loose property lookup with its NormalizeKey, which becomes a call to the "
     "owner's exact lookup", [JS + "RegExp"], r"\bNormalizeKey\b|\bTryResolve\(|JSUnicodePropertyNames"),
    ("the code's direct calls of Broiler.Unicode.Properties and the emoji package (JSD-0074, decision SJL-D-6 "
     "(c) as this engine applies it)", ["Broiler.JS"],
     r"using Broiler\.Unicode\.Properties|using UnicodeEmoji|Broiler\.Unicode\.Properties\.|UnicodeEmoji\."),
)
# The search for a surviving duplicate of the owner's API in the engine's product sources. Each is reviewed in
# the README; the cut-over's host-API search, repeated, covers the platform APIs it names, so the first part
# names the others.
DUPLICATE_SEARCHES = (
    ("the platform's Unicode APIs the cut-over's host-API search does not name: a character's case and "
     "category, Rune's casing, UnicodeCategory, a string's casing in the current culture, culture comparison, "
     "IDN mapping, and case-insensitive comparison and matching",
     r"char\.(IsUpper|IsLower|IsNumber|IsSeparator|IsControl|ToUpper|ToLower|GetNumericValue)\b|\bRune\.(To|Get)|"
     r"\bUnicodeCategory\b|\.To(Upper|Lower)\(|CompareInfo|IdnMapping|IgnoreCase"),
    ("code points of the area's sets and algorithms spelled in code: white space and line terminators, the "
     "joiners, the casing specials (capital and final sigma, dotted capital and dotless small i, the combining "
     "dot above, sharp s), Hangul's composition constants, the regional indicators and the emoji variation "
     "selector",
     r"\\u(0085|00[aA]0|1680|180[eE]|200[0-9a-dA-D]|202[89fF]|205[fF]|3000|[fF][eE][fF][fF]|03[aA]3|03[cC]2|0130|"
     r"0131|0307|00[dD][fF])|0x0*(85|[aA]0|1680|180[eE]|200[0-9a-dA-D]|202[89fF]|205[fF]|3000|[fF][eE][fF][fF]|"
     r"3[aA]3|3[cC]2|130|131|307|[dD][fF]|[aA][cC]00|1100|1161|11[aA]7|1[fF]1[eE]6|1[fF]1[fF][fF]|[fF][eE]0[fF])\b|"
     r"\b11172\b"),
    ("definitions of the area's predicates and algorithms: identifier, white-space and line-terminator tests, "
     "casing conditions, word-likeness, segmentation, decomposition, case folding and property resolution "
     "(JSUnicode's own four, which call the owner, are found too, as the search's control)",
     r"(static|private|internal|public|protected)[^=;(]*\b(Is(Identifier|Id|RegExpIdentifier)(Start|Part|Continue)\w*|"
     r"Is\w*WhiteSpace\w*|Is\w*LineTerminator\w*|IsCased|IsCaseIgnorable|Is\w*SoftDotted|Is\w*FinalSigma|"
     r"IsWordLike\w*|\w*Grapheme\w*|\w*WordBreak\w*|\w*SentenceBreak\w*|\w*Decompos\w*|\w*Fold\w*|CanonicalizeKey|"
     r"Resolve\w*Property\w*|\w*UnicodeProperty\w*|\w*PropertyEscape\w*|\w*Casing\w*|\w*SpecialCas\w*)\s*[(<]"),
    ("names and aliases of Unicode properties and values spelled as strings, which the owner's exact lookup "
     "holds: General_Category's two-letter values and long names, the binary properties and the Script property "
     "(the one-letter values and the short property names are left out: they match unrelated strings)",
     r'"(Lu|Ll|Lt|Lm|Lo|LC|Mn|Mc|Me|Nd|Nl|No|Pc|Pd|Ps|Pe|Pi|Pf|Po|Sm|Sc|Sk|So|Zs|Zl|Zp|Cc|Cf|Cs|Co|Cn|'
     r'Letter|\w+_(Letter|Mark|Number|Punctuation|Symbol|Separator)|General_Category|Script|Script_Extensions|'
     r'ID_Start|ID_Continue|Alphabetic|White_Space|Any|ASCII|Assigned|Emoji|RGI_Emoji)"'),
)
# The product sources the searches read: Broiler.JS/, test projects, benchmarks and samples excluded, and the
# generated tables, which are the owner's generator's output (held byte for byte by the home's test).
PRODUCT = [*prior.PRODUCT, ":!**/JSUnicodeTableData.g.cs"]

# The pristine consumer's checks of the Unicode area: each a correction or a pinned-data answer the cut-over
# made, run from the packages restored into the consumer's isolated cache. Each line is a JavaScript
# expression that must be true.
UNICODE_CHECKS = (
    ("N1", r"'\uD800'.normalize() === '\uD800'"),
    ("N2", r"'\u{105C9}'.normalize('NFD') === '\u{105D2}\u0307'"),
    ("C1", r"'\u00AA\u03A3'.toLowerCase() === '\u00AA\u03C2'"),
    ("C7", r"'i'.toLocaleUpperCase(['tr', 'en']) === '\u0130'"),
    ("P1", r"(() => { try { new RegExp('\\p{lu}', 'u'); return false; } catch (e) { return e instanceof SyntaxError; } })()"),
    ("P2", r"new RegExp('\\p{sc=Hrkt}', 'u').test('\u3042') === false"),
    ("S1", r"new RegExp('^\\p{RGI_Emoji}$', 'v').test('\u{1F44D}\u{1F3FD}')"),
    ("I1", r"(() => { try { (0, eval)('var \u2E2F = 1'); return false; } catch (e) { return e instanceof SyntaxError; } })()"),
    ("W1", r"(() => { try { (0, eval)('1\u0085+1'); return false; } catch (e) { return e instanceof SyntaxError; } })()"),
    ("G1", r"[...new Intl.Segmenter('en', { granularity: 'sentence' }).segment('One. Two? Three! Mr. Smith')].length === 5"),
    ("G2", r"[...new Intl.Segmenter('en', { granularity: 'word' }).segment('can\'t')].length === 1"),
    ("G3", r"[...new Intl.Segmenter('en').segment('e\u0301\u{1F44D}\u{1F3FD}\u{1F1E9}\u{1F1EA}')].length === 3"),
)


class Collector(date.Collector):
    def __init__(self, args: argparse.Namespace) -> None:
        super().__init__(args)
        self.base_commit = BASE_COMMIT
        self.owner = Path(args.owner_tree).resolve() if args.owner_tree else None
        self.lane = args.lane
        self.lane_record = Path(args.lane_record).resolve() if args.lane_record else None
        self.dry_run = args.dry_run

    def hide(self, text: str) -> str:
        if self.owner is not None:
            text = text.replace(str(self.owner), "<owner>")
        text = super().hide(text)
        # The temporary folders the packaging script makes, under the machine's TMPDIR.
        temporary = os.environ.get("TMPDIR")
        return text.replace(str(Path(temporary).resolve()), "<tmp>") if temporary else text

    def grep(self, *args: str, paths: list[str] | None = None) -> list[str]:
        return super().grep(*args, paths=paths or PRODUCT)

    def after(self) -> str:
        return self.git(self.tree, "rev-parse", "HEAD")

    def rule_commit(self) -> str:
        return self.git(self.tree, "log", "--diff-filter=A", "--format=%H", "--", RULE).splitlines()[-1]

    def is_ancestor(self, older: str, newer: str) -> bool:
        return subprocess.run(["git", "-C", str(self.tree), "merge-base", "--is-ancestor", older, newer]).returncode == 0

    def machine(self) -> list[str]:
        lines = [f"machine: {prior.platform.machine()}; {prior.platform.platform()}; logical processors {os.cpu_count()}"]
        cpu = [l for l in Path("/proc/cpuinfo").read_text().splitlines() if l.startswith("model name")]
        memory = [l for l in Path("/proc/meminfo").read_text().splitlines() if l.startswith("MemTotal")]
        lines.append(f"cpu: {cpu[0].split(':', 1)[1].strip() if cpu else 'unknown'}")
        lines.append(f"memory: {memory[0].split(':', 1)[1].strip() if memory else 'unknown'}")
        return lines

    # -- identity --------------------------------------------------------------------------------

    def identity(self) -> None:
        after = self.after()
        rule = self.rule_commit()
        lines = [
            "bundle: sjl-7-001 (Broiler.JS), its after arm",
            f"after commit: {after}",
            f"after commit is the one part A's after run and part B's arm A name ({AFTER_COMMIT}): {after == AFTER_COMMIT}",
            f"after tree clean: {self.git(self.tree, 'status', '--porcelain') == ''}",
            f"cut-over: SJL-7.5, {CUT_OVER_FIRST} to {CUT_OVER}; its commits that change product sources:",
        ]
        first = self.git(self.tree, "rev-parse", CUT_OVER_FIRST)
        for line in self.git(self.tree, "log", "--format=%H %s", f"{first}^..{CUT_OVER}", "--",
                             *PRODUCT_PATHS).splitlines():
            lines.append(f"  {line}")
        lines.append(f"commits between the cut-over's last ({CUT_OVER}) and the after commit, with the files each changes:")
        for line in self.git(self.tree, "log", "--reverse", "--format=%H %s", f"{CUT_OVER}..{after}").splitlines():
            commit = line.split(" ", 1)[0]
            lines.append(f"  {line}")
            files = self.git(self.tree, "diff-tree", "--no-commit-id", "--name-only", "-r", commit).splitlines()
            lines += [f"    {f}" for f in files]
        lines.append(f"files under {', '.join(PRODUCT_PATHS)} that differ between the cut-over's last commit and "
                     "the after commit:")
        changed = self.git(self.tree, "diff", "--name-only", CUT_OVER, after, "--", *PRODUCT_PATHS).splitlines()
        lines += [f"  {p}" for p in changed] or ["  none"]
        lines += [
            f"base: test262/base/, taken at {BASE_COMMIT} (test262/base/imported.txt); part B's arm B is that commit",
            f"rule commit: {rule} (the commit that added {RULE})",
            f"rule commit is the base commit: {rule == BASE_COMMIT}",
            f"rule commit an ancestor of the after commit: {self.is_ancestor(rule, after)}",
            "files the rule fixes, changed between the rule commit and the after commit:",
        ]
        fixed = self.git(self.tree, "diff", "--name-only", rule, after, "--", *RULE_FILES).splitlines()
        lines += [f"  {p}" for p in fixed] or ["  none"]
        lines.append("part B's files, their blobs at the rule commit and at the after commit:")
        for path in BENCH_FILES:
            lines.append(f"  {path}: {self.git(self.tree, 'rev-parse', f'{rule}:{path}')} "
                         f"{self.git(self.tree, 'rev-parse', f'{after}:{path}')}")
        bench_moved = self.git(self.tree, "diff", "--name-only", rule, after, "--", BENCH).splitlines()
        lines.append(f"files of the benchmark project changed between the rule commit and the after commit: "
                     f"{', '.join(bench_moved) if bench_moved else 'none'}")
        lines.append(f"collector: docs/evidence/sjl-7-001/collect.py, sha256 {self.sha256(Path(__file__))}, run from "
                     f"<repo> at {self.git(HERE, 'rev-parse', 'HEAD')}, committed with this bundle")
        log: list[str] = []
        for command in (["dotnet", "--version"], ["dotnet", "--list-runtimes"], ["node", "--version"]):
            lines.append(f"{' '.join(command)}: {self.run(command, self.tree, log).stdout.strip()}")
        lines += self.machine()
        lines.append(f"suite revision: {base.SUITE_REVISION}")
        lines.append(f"collected: {prior.utc()}")
        self.write("identity.txt", "\n".join(lines) + "\n")

    # -- home ------------------------------------------------------------------------------------

    def global_packages(self) -> Path:
        result = self.run(["dotnet", "nuget", "locals", "global-packages", "--list"], self.tree, [])
        return Path(result.stdout.strip().split(":", 1)[1].strip())

    @staticmethod
    def content_hash(output: str) -> str:
        found = re.search(r"^Content hash: (\S+)", output, re.M)
        return found.group(1) if found else "(none printed)"

    def package_record(self, package_id: str, version: str, folder: Path, log: list[str]) -> None:
        """A restored package against the copy nuget.org serves now: its content, nuget.org's repository
        signature over it, its dependencies and its assemblies."""
        lowered = package_id.lower()
        restored = folder / lowered / version / f"{lowered}.{version}.nupkg"
        log.append(f"## {package_id} {version}\n")
        if not restored.exists():
            log.append(f"# not in the global packages folder: {restored}\n\n")
            return
        recorded = (restored.parent / f"{restored.name}.sha512").read_text(encoding="utf-8").strip()
        metadata = restored.parent / ".nupkg.metadata"
        source = json.loads(metadata.read_text(encoding="utf-8")).get("source", "?") if metadata.exists() else "?"
        digest = base64.b64encode(hashlib.sha512(restored.read_bytes()).digest()).decode()
        log.append(f"restored: {restored.stat().st_size:,} bytes; sha512 {digest}; the restore's record "
                   f"(.nupkg.sha512) equal: {recorded == digest}; restored into this global folder from: {source}\n")
        verified = self.run(["dotnet", "nuget", "verify", "--all", str(restored)], self.tree, log)
        served = self.scratch / "nuget-org" / restored.name
        served.parent.mkdir(parents=True, exist_ok=True)
        url = f"https://api.nuget.org/v3-flatcontainer/{lowered}/{version}/{restored.name}"
        try:
            with urllib.request.urlopen(url, timeout=60) as response:
                served.write_bytes(response.read())
        except OSError as error:
            log.append(f"nuget.org: not fetched ({error})\n\n")
            return
        log.append(f"nuget.org serves {url}: {served.stat().st_size:,} bytes\n")
        served_verified = self.run(["dotnet", "nuget", "verify", "--all", str(served)], self.tree, log)
        with zipfile.ZipFile(served) as theirs, zipfile.ZipFile(restored) as ours:
            names_theirs, names_ours = set(theirs.namelist()), set(ours.namelist())
            differing = sorted(n for n in names_theirs & names_ours if theirs.read(n) != ours.read(n))
            log.append(f"entries only in nuget.org's copy: {sorted(names_theirs - names_ours) or 'none'}; only in the "
                       f"restored copy: {sorted(names_ours - names_theirs) or 'none'}; entries in both that differ: "
                       f"{differing or 'none'}\n")
        log.append(f"content hash, nuget.org's copy: {self.content_hash(served_verified.stdout)}; the restored copy: "
                   f"{self.content_hash(verified.stdout)}; equal: "
                   f"{self.content_hash(served_verified.stdout) == self.content_hash(verified.stdout)}\n")
        with zipfile.ZipFile(restored) as archive:
            nuspec = next(n for n in archive.namelist() if n.endswith(".nuspec"))
            text = archive.read(nuspec).decode("utf-8")
            log.append("nuspec dependencies: " + (", ".join(
                f"{m.group(1)} {m.group(2)}" for m in re.finditer(r'<dependency id="([^"]+)" version="([^"]+)"', text))
                or "none") + "\n")
            repository = re.search(r"<repository [^>]*>", text)
            log.append(f"nuspec repository: {repository.group(0) if repository else 'none'}\n")
            for name in sorted(archive.namelist()):
                if name.startswith("lib/") and name.endswith(".dll"):
                    log.append(f"  {hashlib.sha256(archive.read(name)).hexdigest()}  {name}\n")
        log.append("\n")

    def home(self) -> None:
        after = self.after()
        # The references: the versions the repository pins, and every product project that references a
        # package of the owner's or Broiler.Regex.
        log: list[str] = [f"# SJL-7.6, the home at {after}: Broiler.Unicode's packages as this engine restores them\n\n"]
        log.append("## Directory.Packages.props: the owner's packages and Broiler.Regex\n")
        log += [l + "\n" for l in self.grep("-E", r"Broiler\.(Unicode|Regex)", paths=["--", "Directory.Packages.props"])]
        log.append("\n## The product projects' references to them\n")
        log += [l + "\n" for l in self.grep("-E", r'PackageReference Include="Broiler\.(Unicode|Regex)',
                                            paths=["--", "Broiler.JS/*.csproj", "Broiler.JS/**/*.csproj"])]
        log.append("\n## The engine's package closure\n")
        self.run(["dotnet", "restore", ENGINE_ROOT], self.tree, log)
        self.run(["dotnet", "list", ENGINE_ROOT, "package", "--include-transitive"], self.tree, log)
        folder = self.global_packages()
        log.append(f"\n## Each package as restored, against nuget.org (global packages folder {folder})\n\n")
        for package_id, version in UNICODE_PACKAGES:
            self.package_record(package_id, version, folder, log)
        self.log_to("home/packages.log", log)

        # The pin: this engine's, against the owner's at its publishing commit, file by file by git blob.
        lines = [f"# SJL-7.6, this engine's Unicode pin ({PIN}/) at {after}, against the owner's {PIN}/ at "
                 f"{OWNER_COMMIT}, the commit Broiler.Unicode's {OWNER_VERSION} packages were published from.",
                 "# Two files of the owner's are not copied, as the pin's README says: its README and broiler-vm/.",
                 "# A file is byte-identical when its git blob is.", ""]

        def blobs(tree: Path, commit: str) -> dict[str, str]:
            listing = self.git(tree, "ls-tree", "-r", commit, "--", PIN + "/")
            return {line.split("\t", 1)[1][len(PIN) + 1:]: line.split()[2] for line in listing.splitlines()}

        here = blobs(self.tree, after)
        lines.append(f"files here: {len(here)}")
        pin = self.tree / PIN / "unicode.pin"
        lines.append(f"unicode.pin: sha256 {self.sha256(pin)}")
        if self.owner is None:
            lines.append("owner's pin: not read (no --owner-tree)")
        else:
            there = blobs(self.owner, OWNER_COMMIT)
            same = sorted(p for p in here if there.get(p) == here[p])
            lines.append(f"owner's files: {len(there)}")
            lines.append(f"byte-identical: {len(same)}")
            lines.append("differ: " + (", ".join(sorted(p for p in here if p in there and there[p] != here[p])) or "none"))
            lines.append("only here: " + (", ".join(sorted(p for p in here if p not in there)) or "none"))
            lines.append("only the owner's: " + (", ".join(sorted(p for p in there if p not in here)) or "none"))
        lines.append("")
        lines += [f"{blob}  {path}" for path, blob in sorted(here.items())]
        self.write("home/pin.log", "\n".join(lines) + "\n")

        # The area's tests, built in Release and run under TZ=UTC, as this repository's tests run.
        log = []
        for project in AREA_TESTS:
            self.run(["dotnet", "build", project, "-c", "Release", "--nologo"], self.tree, log)
        self.log_to("home/build.log", log)
        log = []
        env, self.env = self.env, dict(self.env, TZ="UTC")
        try:
            for project, classes in AREA_TESTS.items():
                selection = "|".join(f"FullyQualifiedName~.{name}." for name in classes)
                self.run(["dotnet", "test", project, "-c", "Release", "--no-build", "--nologo", "--filter", selection,
                          "--logger", "console;verbosity=normal"], self.tree, log)
        finally:
            self.env = env
        self.log_to("home/tests.log", log)

    # -- test262 ---------------------------------------------------------------------------------

    def test262(self) -> None:
        if self.artifact_zip is None or not self.run_url or not self.artifact_id or not self.run_times:
            raise SystemExit("test262 imports the after run: pass --artifact-zip, --run-url, --artifact-id "
                             "and --run-times")
        after = self.out / "test262" / "after"
        if after.exists():
            raise SystemExit(f"{after} exists; the import writes it once")
        after.mkdir(parents=True)
        with zipfile.ZipFile(self.artifact_zip) as archive:
            names = sorted(archive.namelist())
            archive.extractall(after)
        digest = hashlib.sha256(self.artifact_zip.read_bytes()).hexdigest()
        run = (after / "run.txt").read_text(encoding="utf-8") if (after / "run.txt").exists() else ""
        fields = dict(line.split(": ", 1) for line in run.splitlines() if ": " in line)
        tested = fields.get("tested commit", "(run.txt names none)")
        (after / "imported.txt").write_text("\n".join([
            "# imported: the after run was taken by .github/workflows/test262-evidence.yml, not locally",
            f"workflow run: {self.run_url}, {self.run_times}",
            f"tested commit: {tested} (the commit that lands SJL-7.5's cut-over, corpus revision 20), whose product "
            f"sources are {CUT_OVER}'s but for the files identity.txt lists",
            "configuration: part A's, with negative-metadata tests (include_negative true; includeNegative "
            f"{fields.get('includeNegative', '(not in run.txt)')} in run.txt), in the runner's own zone (time_zone "
            "empty: TZ unset, UTC)",
            f"artifact: test262-evidence, id {self.artifact_id}, {self.artifact_zip.stat().st_size:,} bytes, "
            f"sha256 {digest} (the zip as downloaded)",
            f"retained: every file of the artifact, unchanged, in this directory ({len(names)} entries); this "
            "file is the only one added",
        ]) + "\n", encoding="utf-8", newline="\n")

    # -- compare ---------------------------------------------------------------------------------

    def completeness(self, folder: Path, merged: dict) -> list[str]:
        """The rule's completeness conditions for one run, as its own files state them."""
        run = (folder / "run.txt").read_text(encoding="utf-8") if (folder / "run.txt").exists() else ""
        fields = dict(line.split(": ", 1) for line in run.splitlines() if ": " in line)
        configuration = merged.get("runConfiguration") or {}
        rerun = folder / "rerun-timeouts.json"
        rerun_negative = json.loads(rerun.read_text(encoding="utf-8")).get("includeNegative") if rerun.exists() else None
        shells = (folder / "shell-hashes.log").read_text(encoding="utf-8").splitlines() if (folder / "shell-hashes.log").exists() else []
        zone = (folder / "host-zone.log").read_text(encoding="utf-8").splitlines() if (folder / "host-zone.log").exists() else []
        statuses = sorted((folder / "shards").glob("*-status.json"))
        return [
            f"tested commit (run.txt): {fields.get('tested commit', '?')}",
            f"workflow run (run.txt): {fields.get('workflow run', '?')}",
            f"runner (run.txt): {fields.get('runner', '?')}; dotnetVersion {fields.get('dotnetVersion', '?')}",
            f"expected shards: {merged.get('expectedShards')}; reported: {merged.get('reportedShards')}; "
            f"incomplete: {merged.get('incompleteShards')}; retried: {merged.get('retriedShards')}; "
            f"status files: {len(statuses)}",
            f"configuration failures: {merged.get('configurationFailures')}",
            f"suite revisions (suiteRefs): {merged.get('suiteRefs')}",
            f"includeNegative: run.txt {fields.get('includeNegative', '?')}; merged results "
            f"{configuration.get('includeNegative')}; re-run {rerun_negative}",
            f"selection: {configuration.get('selectionMode')}; workers {configuration.get('maxWorkers')}; "
            f"timeout {configuration.get('testTimeoutSeconds')} s; memory limit {configuration.get('memoryLimitMb')}; "
            f"minifier {configuration.get('minifier')} {configuration.get('minifierVersion')}",
            "shell: " + "; ".join(l for l in shells if l.startswith("shards:") or l.startswith("the re-run")),
            "zone: " + "; ".join(l for l in zone if l.startswith("expected:") or l.startswith("shards:")),
            f"summary: {json.dumps(merged.get('summary'), sort_keys=True)}",
        ]

    def compare(self) -> None:
        root = self.out / "test262"
        compared = "test262/comparison"
        merged = {}
        loaded = {}
        for side in ("base", "after"):
            merged[side] = self.scratch / f"{side}-test262-results.json"
            with gzip.open(root / side / "test262-results.json.gz", "rt", encoding="utf-8") as handle:
                text = handle.read()
            merged[side].write_text(text, encoding="utf-8")
            loaded[side] = json.loads(text)

        # Each run's completeness, as the rule states it, from its own files; and the two zone records alike.
        lines = ["# Part A's completeness conditions for each run, read from the files the workflow wrote:",
                 "# every expected shard reported, no configuration failure, one suite revision, one shell, every",
                 "# shard in the zone, includeNegative true.", ""]
        for side in ("base", "after"):
            lines.append(f"## {side}: test262/{side}/")
            lines += self.completeness(root / side, loaded[side])
            lines.append("")
        self.write(f"{compared}/completeness.log", "\n".join(lines))
        zones = []
        records = {}
        for side in ("base", "after"):
            path = root / side / "host-zone.log"
            records[side] = path.read_text(encoding="utf-8").splitlines() if path.exists() else []
            zones.append(f"## {side}: test262/{side}/host-zone.log")
            zones += [l for l in records[side] if l.startswith("expected: ") or l.startswith("shards: ")]
        alike = ([l for l in records["base"] if l.startswith("expected: ")]
                 == [l for l in records["after"] if l.startswith("expected: ")])
        zones.append(f"# the two runs' expected zone lines read alike, line for line: {alike}")
        self.write(f"{compared}/zone-records.log", "\n".join(zones) + "\n")

        script = self.tree / "scripts" / "compliance" / "compare_test262_paths.py"
        output = self.scratch / "comparison.json"
        output.unlink(missing_ok=True)
        command = [sys.executable, str(script), "--base", str(merged["base"]), "--after", str(merged["after"]),
                   "--output", str(output)]
        for side in ("base", "after"):
            rerun = root / side / "rerun-timeouts.json"
            if rerun.exists():
                command += [f"--{side}-rerun", str(rerun)]
        classifications = root / "comparison" / "classifications.json"
        if classifications.exists():
            command += ["--classifications", str(classifications)]
        log: list[str] = []
        self.run(command, self.tree, log)
        self.log_to(f"{compared}/comparison.log", log)
        if output.exists():
            self.write(f"{compared}/comparison.json", output.read_text(encoding="utf-8"))

        # The script on the after run: against itself (no difference), and against a copy with one passing
        # row turned to failed (one regression, exit 1).
        checks: list[str] = []
        self.run([sys.executable, str(script), "--base", str(merged["after"]), "--after", str(merged["after"])],
                 self.tree, checks)
        mutated = loaded["after"]
        victim = next(r for r in sorted(mutated["results"], key=lambda r: (str(r["path"]), str(r.get("variant", ""))))
                      if r["status"] == "passed")
        victim["status"] = "failed"
        copy = self.scratch / "test262-negative-control.json"
        copy.write_text(json.dumps(mutated), encoding="utf-8")
        checks.append(f"# negative control: {victim['path']} [{victim.get('variant', 'original')}] turned from "
                      "passed to failed in a copy\n")
        self.run([sys.executable, str(script), "--base", str(merged["after"]), "--after", str(copy)], self.tree, checks)
        copy.unlink()
        self.write(f"{compared}/compare-checks.log", "".join(checks))

    # -- consumer --------------------------------------------------------------------------------

    def consumer(self) -> None:
        # The engine set, packed from the after commit, restored by a clean consumer from a local feed and
        # nuget.org into an isolated cache, and run (it prints 42); the consumer is kept for the next part.
        log: list[str] = []
        result = self.run([sys.executable, "scripts/packaging/test_pristine_consumer.py", "--set", "engine", "--keep"],
                          self.tree, log)
        self.log_to("engine/pristine-consumer.log", log)
        kept = re.search(r"^Retained: (.+)$", result.stdout, re.M)
        if result.returncode != 0 or not kept:
            return
        temporary = Path(kept.group(1).strip())
        consumer = temporary / "consumer"
        cache = temporary / "packages"
        try:
            # The same consumer, the same isolated cache, offline: the Unicode area's corrections.
            checks = ",\n".join(f"  [{json.dumps(row)}, {expression}]" for row, expression in UNICODE_CHECKS)
            script = "(() => {\n const checks = [\n" + checks + "\n ];\n" \
                     " const failed = checks.filter(c => c[1] !== true).map(c => c[0]);\n" \
                     " return failed.length === 0 ? 'ok ' + checks.length : 'failed ' + failed.join(' ');\n})()"
            (consumer / "unicode-checks.js").write_text(script, encoding="utf-8")
            (consumer / "Program.cs").write_text(
                """using System;
using System.IO;
using Broiler.JavaScript.Engine;

using var context = new JSContext();
var result = context.Eval(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "unicode-checks.js"))).ToString();
Console.WriteLine($"pristine-consumer: the Unicode area's checks: {result}");
if (!result.StartsWith("ok ", StringComparison.Ordinal))
    throw new InvalidOperationException(result);
""", encoding="utf-8")
            project = (consumer / "Consumer.csproj").read_text(encoding="utf-8")
            project = project.replace("</Project>", """  <ItemGroup>
    <None Include="unicode-checks.js" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>""")
            (consumer / "Consumer.csproj").write_text(project, encoding="utf-8")
            log = ["# the pristine consumer above, kept, with its Program.cs replaced by a run of these checks, each a "
                   "JavaScript expression that must be true:\n"]
            log += [f"#   {row}: {expression}\n" for row, expression in UNICODE_CHECKS]
            env, self.env = self.env, dict(self.env, NUGET_PACKAGES=str(cache))
            try:
                self.run(["dotnet", "run", "-c", "Release", "--no-restore"], consumer, log)
            finally:
                self.env = env
            log.append("\n# the Unicode packages in the consumer's isolated cache: where each was restored from, whether "
                       "it carries a signature, and its content hash against the repository's restore of the same "
                       "version (the hash of the package without its signature):\n")
            folder = self.global_packages()
            for package_id, _ in UNICODE_PACKAGES:
                lowered = package_id.lower()
                found = sorted((cache / lowered).glob("*/.nupkg.metadata")) if (cache / lowered).exists() else []
                if not found:
                    log.append(f"{package_id}: not in the consumer's closure\n")
                    continue
                for metadata in found:
                    version = metadata.parent.name
                    record = json.loads(metadata.read_text(encoding="utf-8"))
                    with zipfile.ZipFile(metadata.parent / f"{lowered}.{version}.nupkg") as archive:
                        signed = ".signature.p7s" in archive.namelist()
                    theirs = folder / lowered / version / ".nupkg.metadata"
                    ours = json.loads(theirs.read_text(encoding="utf-8")).get("contentHash") if theirs.exists() else None
                    log.append(f"{package_id} {version}: from {record.get('source')}; signed: {signed}; content hash "
                               f"{record.get('contentHash')}; the repository's restore holds the same content: "
                               f"{ours == record.get('contentHash')}\n")
            self.log_to("engine/pristine-consumer-unicode.log", log)
        finally:
            shutil.rmtree(temporary, ignore_errors=True)

    # -- simplification --------------------------------------------------------------------------

    def simplification(self) -> None:
        after = self.after()
        # The cut-over's two searches, each repeated by its own command line at the after commit. ripgrep's
        # order is its threads', so both sides are compared as sorted sets of lines.
        lines = [f"# SJL-7.6: the cut-over's two searches (cutover/deletion-search.txt and cutover/host-api-search.txt,",
                 f"# taken at {CUT_OVER}), each repeated by the command its second line records, at {after}, from the",
                 "# after tree's root. The output is sorted; ripgrep writes in its threads' order. Each search is then",
                 "# set against the cut-over's, line by line.", ""]
        for name in ("deletion-search.txt", "host-api-search.txt"):
            recorded = (HERE / "cutover" / name).read_text(encoding="utf-8").splitlines()
            command = next(l[2:] for l in recorded if l.startswith("$ "))
            before = sorted(l for l in recorded if not l.startswith("#") and not l.startswith("$ "))
            result = subprocess.run(command, shell=True, cwd=self.tree, capture_output=True, text=True)
            now = sorted(result.stdout.splitlines())
            lines += [f"## cutover/{name}", f"$ {command}", f"# exit {result.returncode}; lines {len(now)}; at the "
                      f"cut-over {len(before)}; the same {len(set(now) & set(before))}"]
            lines += [f"# only at the cut-over: {l}" for l in before if l not in set(now)]
            lines += [f"# only at the after commit: {l}" for l in now if l not in set(before)]
            lines += now + [""]
        self.write("simplification/cutover-searches.log", "\n".join(lines) + "\n")

        lines = [f"# SJL-7.6 deletion check, Broiler.JS at {after}: every entry of the Unicode area's Broiler.JS",
                 "# deletion list (Broiler.VM docs/javascript-shared-libraries/milestones/sjl-7-unicode.md, SJL-7.5, with",
                 "# what JSD-0074 adds to it), searched in the file or folder that held it with git grep -E, test",
                 "# projects included where the folder holds them. An entry with no line is absent; a line that remains",
                 "# is a call to the owner, an adapter, a comment or another member of the same name, which the bundle's",
                 "# README reads.", ""]
        for entry, paths, pattern in DELETED:
            hits = self.grep("-E", pattern, paths=["--", *paths])
            lines += [f"## {entry}", f"# in {', '.join(paths)}: {pattern}", f"# hits: {len(hits)}"]
            lines += [h[:220] for h in hits] + [""]
        self.write("simplification/deletion-check.log", "\n".join(lines) + "\n")

        search = [f"# SJL-7.6 search for a surviving intra-engine duplicate of the Unicode area's owner, Broiler.JS at {after}:",
                  "# the engine's product sources (Broiler.JS/; test projects, benchmarks, samples and the generated tables",
                  "# JSUnicodeTableData.g.cs excluded), searched with git grep -E in four parts. Each hit is reviewed in",
                  "# the bundle's README.", ""]
        for title, pattern in DUPLICATE_SEARCHES:
            hits = self.grep("-E", pattern)
            search += [f"## {title}", f"# pattern: {pattern}", f"# hits: {len(hits)}"]
            search += [h[:220] for h in hits] + [""]
        self.write("simplification/duplicate-search.log", "\n".join(search) + "\n")

    # -- measure ---------------------------------------------------------------------------------

    def lane_lines(self) -> list[str]:
        """What Measurement.md asks a lane to record: CPU, microcode, topology, memory, OS, governor, thermal."""
        lines = self.machine()
        cpuinfo = Path("/proc/cpuinfo").read_text().splitlines()
        microcode = sorted({l.split(":", 1)[1].strip() for l in cpuinfo if l.startswith("microcode")})
        lines.append(f"microcode: {', '.join(microcode) or 'not exposed'}")
        topology = subprocess.run(["lscpu"], capture_output=True, text=True)
        lines += [f"lscpu: {l}" for l in topology.stdout.splitlines()
                  if l.split(":", 1)[0].strip() in ("Thread(s) per core", "Core(s) per socket", "Socket(s)",
                                                    "NUMA node(s)", "CPU(s)", "On-line CPU(s) list", "Hypervisor vendor",
                                                    "Virtualization type", "CPU max MHz", "CPU min MHz")]
        release = Path("/etc/os-release")
        pretty = [l.split("=", 1)[1].strip('"') for l in release.read_text().splitlines()
                  if l.startswith("PRETTY_NAME=")] if release.exists() else []
        lines.append(f"os: {pretty[0] if pretty else 'unknown'}; kernel {prior.platform.release()}")
        governors = sorted({p.read_text().strip() for p in Path("/sys/devices/system/cpu").glob("cpu*/cpufreq/scaling_governor")})
        lines.append(f"governor: {', '.join(governors) or 'not exposed'}")
        zones = sorted(Path("/sys/class/thermal").glob("thermal_zone*")) if Path("/sys/class/thermal").exists() else []
        lines.append("thermal: " + (", ".join(f"{z.name} {(z / 'type').read_text().strip()} "
                                              f"{(z / 'policy').read_text().strip() if (z / 'policy').exists() else ''}".strip()
                                              for z in zones) or "not exposed"))
        icu = subprocess.run(["sh", "-c", "ldconfig -p | grep -o 'libicuuc\\.so\\.[0-9.]*' | sort -u"],
                             capture_output=True, text=True).stdout.split()
        lines.append(f"host ICU: {', '.join(icu) or 'not found by ldconfig'}")
        for name in sorted(k for k in os.environ if k.startswith(("DOTNET_", "COMPlus_", "LC_")) or k in ("LANG", "TZ")):
            lines.append(f"environment: {name}={os.environ[name]}")
        return lines

    def measure(self) -> None:
        if self.bench is None:
            raise SystemExit("measure needs --bench-tree, a clean checkout of the rule's commit (arm B)")
        if self.lane not in ("smoke", "controlled"):
            raise SystemExit("measure needs --lane smoke or --lane controlled: the README reads part B as void "
                             "unless the lane is a controlled lane as Measurement.md requires")
        if self.lane == "controlled" and (self.lane_record is None or not self.lane_record.exists()):
            raise SystemExit("--lane controlled needs --lane-record, the lane's record as Measurement.md asks for it")
        if os.environ.get("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "").strip().lower() not in ("", "0", "false"):
            raise SystemExit("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT is set: part B runs with the host's ICU loaded")
        out = "measurement"
        rule = self.git(self.bench, "rev-parse", "HEAD")
        after = self.after()
        identity = [f"rule commit: {BASE_COMMIT}", f"arm B: {rule}", f"arm B is the rule's commit: {rule == BASE_COMMIT}",
                    f"arm A and A-again: {after}", f"arm A is the after commit: {after == AFTER_COMMIT}",
                    f"bench tree clean: {self.git(self.bench, 'status', '--porcelain') == ''}",
                    f"after tree clean: {self.git(self.tree, 'status', '--porcelain') == ''}"]
        moved = self.git(self.tree, "diff", "--name-only", rule, after, "--", *BENCH_FILES).splitlines()
        identity.append("UnicodeBenchmarks.cs, Program.cs and BenchmarkConfiguration.cs at A as the rule's commit has "
                        f"them (git): {not moved}")
        on_disk = [self.sha256(self.bench / p) == self.sha256(self.tree / p) for p in BENCH_FILES]
        identity.append(f"the same three files byte-identical in the two checkouts: {all(on_disk)}")
        for path in BENCH_FILES:
            identity.append(f"  {self.sha256(self.tree / path)}  {path}")
        project_moved = self.git(self.tree, "diff", "--name-only", rule, after, "--", BENCH).splitlines()
        identity.append("files of the benchmark project changed between the arms: "
                        + (", ".join(project_moved) if project_moved else "none"))
        refused = (rule != BASE_COMMIT or after != AFTER_COMMIT or moved or not all(on_disk)
                   or self.git(self.bench, "status", "--porcelain") or self.git(self.tree, "status", "--porcelain"))
        schedule = [f"r{r}: {', '.join(LANES[r % 3:] + LANES[:r % 3])}" for r in range(REPETITIONS)]
        if self.dry_run:
            print("\n".join(["# dry run: nothing is built, run or written", *identity,
                             f"arms refused: {bool(refused)}", f"lane: {self.lane}", "schedule:", *schedule,
                             f"each run: dotnet <arm>/{base.BENCH_DLL} --filter {FILTER} --buildTimeout 1800 with "
                             f"BROILER_BENCHMARK_PROFILE=baseline, between two runs of {ANSWERS}",
                             f"output: {self.out / out}"]))
            return
        if refused:
            raise SystemExit("an arm is not the one the rule names:\n" + "\n".join(identity))
        if (self.out / out).exists():
            # An interrupted collection is run again from the start; the stopped one is kept beside it.
            stopped = self.out / f"{out}-stopped-{prior.utc().replace(':', '')}"
            (self.out / out).rename(stopped)
            print(f"the earlier {out}/ is kept as {stopped.name}/", flush=True)

        log: list[str] = []
        self.env = dict(self.env, **NO_BUILD_SERVERS)
        trees = {"B": self.bench, "A": self.tree, "A-again": self.tree}
        for tree in (self.bench, self.tree):
            self.run(["dotnet", "build", "-c", "Release", "-nologo", BENCH], tree, log, check=True)
        self.run(["dotnet", "build-server", "shutdown"], self.tree, log)
        for lane, tree in (("B", self.bench), ("A and A-again (one build)", self.tree)):
            identity.append(f"arm {lane} binaries:")
            identity += [f"  {self.sha256(p)}  {p.name}" for p in sorted(Path(tree, base.BENCH_DLL).parent.glob("*.dll"))]
        identity.append(f"profile: baseline; repetitions per lane: {REPETITIONS}; lanes: B, A, A-again, rotated by "
                        "repetition: " + "; ".join(schedule))
        identity.append(f"each run: {FILTER}, all six workloads ({', '.join(WORKLOADS)}), each in BenchmarkDotNet's own "
                        f"child process; {ANSWERS} before and after it")
        identity.append("lane record:")
        identity += [f"  {l}" for l in self.lane_lines()]
        if self.lane == "controlled":
            identity.append(f"lane: controlled, as the record below states ({self.lane_record.name})")
            identity += [f"  {l}" for l in self.lane_record.read_text(encoding="utf-8").splitlines()]
        else:
            identity.append("lane: smoke - this machine is not recorded as a controlled lane as Measurement.md's "
                            "section \"Controlled lanes and effective configuration\" requires; part B is void on it")
        identity.append(f"written: {prior.utc()}, before the first run")
        manifest = self.write(f"{out}/manifest.txt", "\n".join(identity) + "\n")
        manifest.chmod(0o444)

        def answers(tree: Path) -> dict:
            result = self.run(["dotnet", str(tree / base.BENCH_DLL), ANSWERS], tree, log, check=True)
            return json.loads(result.stdout.strip().splitlines()[-1])

        reference = answers(self.bench)
        conditions = ["repetition\tlane\twhen\tanswers equal B's first\tconfiguration equal B's first",
                      "-\tB\tfirst check\tTrue\tTrue"]
        runs = ["repetition\tlane\tworkload\tmedian_ns\tmean_ns\tallocated_bytes\tgen0\tgen1\tgen2"]
        issues = ["repetition\tlane\texit\tstarted\tworkloads reported\tBenchmarkDotNet's lines on issues"]
        environments = []
        for repetition in range(REPETITIONS):
            for lane in LANES[repetition % 3:] + LANES[:repetition % 3]:
                tree = trees[lane]
                artifacts = self.scratch / "bdn" / f"r{repetition}-{lane}"
                if artifacts.exists():
                    shutil.rmtree(artifacts)
                before = answers(tree)
                conditions.append(f"{repetition}\t{lane}\tbefore\t{before['answers'] == reference['answers']}\t"
                                  f"{before['configuration'] == reference['configuration']}")
                env = dict(self.env, BROILER_BENCHMARK_PROFILE="baseline", BROILER_BENCHMARK_ARTIFACTS=str(artifacts))
                started = prior.utc()
                result = subprocess.run(["dotnet", str(tree / base.BENCH_DLL), "--filter", FILTER, "--buildTimeout", "1800"],
                                        cwd=Path(tree, base.BENCH_DLL).parent, env=env, capture_output=True, text=True)
                log.append(f"# run r{repetition} {lane}: started {started}, exit {result.returncode}\n")
                after_answers = answers(tree)
                conditions.append(f"{repetition}\t{lane}\tafter\t{after_answers['answers'] == reference['answers']}\t"
                                  f"{after_answers['configuration'] == reference['configuration']}")
                self.write_gz(f"{out}/runs/r{repetition}-{lane}.stdout.txt.gz", result.stdout + result.stderr)
                # BenchmarkDotNet's own list of the benchmarks with issues, from its header to the next blank line.
                flagged, listing = [], False
                for line in result.stdout.splitlines():
                    if "Benchmarks with issues" in line:
                        listing = True
                    elif listing and not line.strip():
                        listing = False
                    if listing:
                        flagged.append(line.strip())
                reports = sorted((artifacts / "results").glob("*-report-full.json"))
                if not reports:
                    runs.append(f"{repetition}\t{lane}\t(no report)\t\t\t\t\t\t")
                    issues.append(f"{repetition}\t{lane}\t{result.returncode}\t{started}\t0\t{' | '.join(flagged)}")
                    continue
                report = json.loads(reports[0].read_text(encoding="utf-8"))
                self.write_gz(f"{out}/runs/r{repetition}-{lane}.report-full.json.gz", json.dumps(report, indent=1, sort_keys=True))
                environments.append(json.dumps({
                    "host": report.get("HostEnvironmentInfo", {}),
                    "hardwareIntrinsics": sorted({str(b.get("HardwareIntrinsics")) for b in report["Benchmarks"]}),
                }, sort_keys=True))
                for benchmark in report["Benchmarks"]:
                    workload = benchmark["Parameters"].split("=", 1)[1]
                    stats = benchmark.get("Statistics") or {}
                    memory = benchmark.get("Memory") or {}
                    runs.append("\t".join(str(v) for v in (repetition, lane, workload, stats.get("Median", ""), stats.get("Mean", ""),
                                                          memory.get("BytesAllocatedPerOperation", ""), memory.get("Gen0Collections", ""),
                                                          memory.get("Gen1Collections", ""), memory.get("Gen2Collections", ""))))
                issues.append(f"{repetition}\t{lane}\t{result.returncode}\t{started}\t{len(report['Benchmarks'])}\t"
                              f"{' | '.join(flagged)}")
        self.write(f"{out}/conditions.tsv", "\n".join(conditions) + "\n")
        self.write(f"{out}/runs.tsv", "\n".join(runs) + "\n")
        self.write(f"{out}/issues.tsv", "\n".join(issues) + "\n")
        self.write(f"{out}/answers.json", json.dumps(reference, indent=1, sort_keys=True) + "\n")
        self.write(f"{out}/environments.txt", f"distinct BenchmarkDotNet host environment records: {len(set(environments))}\n"
                   + "\n".join(sorted(set(environments))) + "\n")
        self.write_gz(f"{out}/collect.log.gz", "".join(log))

        # The rule's quantities. They are computed whatever the lane; on a smoke lane they decide nothing.
        medians: dict[tuple[str, str], list[float]] = {}
        for line in runs[1:]:
            fields = line.split("\t")
            if fields[3]:
                medians.setdefault((fields[1], fields[2]), []).append(float(fields[3]))
        q = {key: statistics.median(values) for key, values in medians.items()}
        summary = ["workload\truns B\truns A\truns A-again\tQ(B) ns\tQ(A) ns\tQ(A-again) ns\tN ns\tD ns\tE(B) ns\tE(A) ns"]
        for workload in MEASURED:
            control = workload + "Control"
            try:
                qb, qa, qaa = q[("B", workload)], q[("A", workload)], q[("A-again", workload)]
                eb, ea = qb - q[("B", control)], qa - q[("A", control)]
            except KeyError:
                summary.append(f"{workload}\t\t\t\t\t\t\t\t\t\tincomplete")
                continue
            summary.append(f"{workload}\t{len(medians[('B', workload)])}\t{len(medians[('A', workload)])}\t"
                           f"{len(medians[('A-again', workload)])}\t{qb:.1f}\t{qa:.1f}\t{qaa:.1f}\t{abs(qa - qaa):.1f}\t"
                           f"{qa - qb:.1f}\t{eb:.1f}\t{ea:.1f}")
        self.write(f"{out}/summary.tsv", "\n".join(summary) + "\n")
        hashes = [f"{self.sha256(p)}  {p.relative_to(self.out / out)}" for p in sorted((self.out / out).rglob("*"))
                  if p.is_file() and p.name != "hashes.txt"]
        self.write(f"{out}/hashes.txt", "\n".join(hashes) + "\n")

    # -- manifest --------------------------------------------------------------------------------

    def manifest(self) -> None:
        files = {}
        for path in sorted(self.out.rglob("*")):
            if path.is_file() and path.name != "manifest.json" and "__pycache__" not in path.parts:
                files[str(path.relative_to(self.out)).replace(os.sep, "/")] = self.sha256(path)
        manifest = {
            "bundle": "sjl-7-001",
            "repository": "Broiler.JS",
            "baseCommit": BASE_COMMIT,
            "afterCommit": self.after(),
            "cutOverCommit": CUT_OVER,
            "ruleCommit": self.rule_commit(),
            "ownerCommit": OWNER_COMMIT,
            "ownerVersion": OWNER_VERSION,
            "suiteRevision": base.SUITE_REVISION,
            "files": files,
        }
        (self.out / "manifest.json").write_text(json.dumps(manifest, indent=1, sort_keys=True) + "\n",
                                                encoding="utf-8", newline="\n")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--tree", required=True, help="a clean checkout of the after commit (part B's arm A)")
    parser.add_argument("--owner-tree", help="home: a checkout of Broiler.Unicode holding its publishing commit")
    parser.add_argument("--artifact-zip", help="test262: part A's after run's test262-evidence artifact, as downloaded")
    parser.add_argument("--run-url", help="test262: the after run's workflow run, with its attempt")
    parser.add_argument("--artifact-id", help="test262: the artifact's id")
    parser.add_argument("--run-times", help="test262: when the after run was dispatched and completed")
    parser.add_argument("--bench-tree", help="measure: a clean checkout of the rule's commit (arm B)")
    parser.add_argument("--lane", choices=("smoke", "controlled"), help="measure: what the lane is, as recorded")
    parser.add_argument("--lane-record", help="measure: with --lane controlled, the lane's record")
    parser.add_argument("--dry-run", action="store_true", help="measure: check the arms and print the protocol only")
    parser.add_argument("--out", required=True)
    parser.add_argument("--scratch", default=os.path.join(os.environ.get("TMPDIR", "/tmp"), "sjl-7-001-js"))
    parser.add_argument("--only", nargs="+", choices=STEPS, required=True)
    args = parser.parse_args(argv)
    args.label = "after"
    args.part = "A"
    args.suite_archive = None
    args.terser_module = None
    args.test262_from = None
    args.max_workers = 4
    collector = Collector(args)
    for step in args.only:
        print(f"== {step} {prior.utc()}", flush=True)
        getattr(collector, step)()
    shutil.rmtree(collector.scratch, ignore_errors=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
