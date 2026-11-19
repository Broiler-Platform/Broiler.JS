#!/usr/bin/env python3
"""Part D's base reports read again against corpus revision 32's class lines, through a scratch pin. Runs nothing.

    python3 -I docs/evidence/sjl-11-001/base/part-d/read-revision-32.py --repo <a clone of Broiler.JS> \
        --scratch <an empty scratch directory> --out docs/evidence/sjl-11-001/base/part-d

    and, for the after runs, the same with --runs after --tree-commit <the after commit> --out <their folder>, which
    holds their three reports under the same names.

The owner's answer 5 of 2026-11-19, "Read revision 32" ("As the rule says: the base reports are read again against
revision 32's class lines through a scratch pin, with no new runs needed, and the after runs are read the same way."),
has part D read at the revision its rule names (decision-rule.md:218 and :242). The base reports beside this script
(run-invariant.tsv.gz, run-tr-TR.tsv.gz and run-lt-LT.tsv.gz) were taken at 5429c406, whose corpus is revision 43.
This script:

  1. writes the scratch pin: git archive of conformance/vectors/regexp at PIN_COMMIT (the merge of
     Broiler-Platform/Broiler.JS#1089, the first default-branch commit that holds revision 32, whose regexp/ tree is
     revision 32's last change, de8d7b54's), written twice, byte for byte the same, the second extracted and each file hashed, as
     ../../engine/commands.txt describes its scratch pins; the pin is written to revision-32.pin;
  2. reads the regexp/ vectors of both revisions (the pin's, and from git those of the commit the reports were taken
     at, --tree-commit, the base commit unless given) and checks that every vector has the same fields in both but
     class and divergence.*: same ids, op, in, out, per-engine, prov, script, note and since, and that the expected
     outcome the comparison uses (per-engine on an impl-defined vector, otherwise out) is the same in both;
  3. reads each report's regexp/ lines, checks each line's class and expected columns against the vector of the
     corpus the run read (at the base commit, revision 43), and computes each line's verdict again under revision 32's
     vector by VectorRun.Compare (Broiler.ECMAScript/Broiler.ECMAScript.Conformance/VectorRun.cs) and SCHEMA.md
     section 8: the rendered outcome is the report's own, and since the expected outcome is the same at both
     revisions (step 2), whether the rendered outcome equals an alternative of it is the report's own match or not;
     a non-match is then a classified divergence when revision 32's vector is divergence or impl-defined and carries
     divergence.broiler-js, and otherwise a difference (a fault always a difference); a match on a vector carrying
     divergence.broiler-js is "divergence no longer holds";
  4. writes reading-revision-32.txt: the pin, the equivalence, each report's verdicts as written and read again at
     revision 32, every line whose verdict reads differently, the three cultures' re-read lines against each other,
     and the part's clauses on those lines.

It reads only git objects and the retained reports, and changes no retained file. The reading decides nothing.
"""

from __future__ import annotations

import argparse
import gzip
import hashlib
import io
import os
import subprocess
import sys
import tarfile
from collections import Counter

PIN_COMMIT = "b2c2e2243a086285d806866a64b83297791a8ec5"
BASE_COMMIT = "5429c4065e8b273e5c7965598b8a2f5490a8ad98"
REGEXP = "conformance/vectors/regexp"
CULTURES = ("invariant", "tr-TR", "lt-LT")
ENGINE = "broiler-js"


def git(repo: str, *args: str) -> bytes:
    return subprocess.run(["git", "-C", repo, *args], check=True, capture_output=True).stdout


def parse_vectors(text: str, name: str) -> dict[str, dict[str, str]]:
    """SCHEMA.md section 1: comments, then "schema: 1", then records of key: value lines separated by blank lines."""
    records: dict[str, dict[str, str]] = {}
    seen_schema = False
    current: dict[str, str] = {}

    def flush() -> None:
        nonlocal current
        if current:
            vid = current["id"]
            if vid in records:
                raise SystemExit(f"{name}: duplicate id {vid}")
            records[vid] = current
            current = {}

    for line in text.split("\n"):
        if line.startswith("#"):
            continue
        if not seen_schema:
            if line == "":
                continue
            if line != "schema: 1":
                raise SystemExit(f"{name}: first non-comment line is not 'schema: 1'")
            seen_schema = True
            continue
        if line == "":
            flush()
            continue
        key, sep, value = line.partition(": ")
        if not sep:
            raise SystemExit(f"{name}: a line that is not key: value: {line!r}")
        if key in current:
            raise SystemExit(f"{name}: key {key} twice in a record")
        current[key] = value
    flush()
    return records


def is_status(vector: dict[str, str]) -> bool:
    # CorpusRun.IsStatusVector: an alternative of out whose kind is a status. Alternatives are separated by " | "
    # (SCHEMA.md section 4); a status alternative begins "status: ".
    return any(alt.startswith("status: ") for alt in vector["out"].split(" | "))


def expected_for(vector: dict[str, str]) -> str:
    # VectorRun.ExpectedFor
    if vector["class"] == "impl-defined" and f"per-engine.{ENGINE}" in vector:
        return vector[f"per-engine.{ENGINE}"]
    return vector["out"]


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def write_pin(repo: str, scratch: str) -> tuple[list[str], dict[str, str]]:
    archives = []
    for n in (1, 2):
        data = git(repo, "archive", "--format=tar.gz", PIN_COMMIT, "--", REGEXP)
        path = os.path.join(scratch, f"revision-32-regexp.{n}.tar.gz")
        with open(path, "wb") as f:
            f.write(data)
        archives.append(data)
    if archives[0] != archives[1]:
        raise SystemExit("the two archives differ")
    files: dict[str, str] = {}
    extracted = os.path.join(scratch, "extracted")
    os.makedirs(extracted, exist_ok=False)
    with tarfile.open(fileobj=io.BytesIO(archives[1]), mode="r:gz") as tar:
        tar.extractall(extracted, filter="data")
    texts: dict[str, str] = {}
    for root, _, names in os.walk(extracted):
        for name in names:
            full = os.path.join(root, name)
            rel = os.path.relpath(full, extracted).replace(os.sep, "/")
            data = open(full, "rb").read()
            files[rel] = sha256(data)
            texts[rel] = data.decode("utf-8")
    tree = git(repo, "rev-parse", f"{PIN_COMMIT}:{REGEXP}").decode().strip()
    content = sha256("".join(f"{files[p]}  {p}\n" for p in sorted(files)).encode())
    lines = [
        "# A scratch pin of corpus revision 32's regular-expression vectors, for part D's reading (answer 5 of 2026-11-19).",
        "# Not a pin any runner reads, and nothing is run from it: read-revision-32.py reads the extracted files' class and",
        "# divergence lines against the retained base reports. Written by that script, from git, as engine/commands.txt",
        "# describes its scratch pins: the archive written twice, byte for byte the same, the second extracted and hashed.",
        "",
        "corpus revision 32 (conformance/vectors/REVISIONS.md row 32)",
        f"commit {PIN_COMMIT} (Broiler-Platform/Broiler.JS#1089's merge, the first default-branch commit holding revision 32)",
        f"tree {REGEXP} {tree} (the same tree as de8d7b54, revision 32's last change)",
        f"archive git archive --format=tar.gz {PIN_COMMIT} -- {REGEXP}",
        f"archive-sha256 {sha256(archives[1])}",
        f"content-sha256 {content} (the SHA-256 of the lines below, each \"<sha256>  <path>\\n\", in byte order of path)",
        f"files {len(files)}",
    ]
    lines += [f"{files[p]}  {p}" for p in sorted(files)]
    return lines, texts


def read_report(path: str) -> tuple[list[str], list[list[str]]]:
    with gzip.open(path, "rt", encoding="utf-8", newline="\n") as f:
        text = f.read()
    header = [l for l in text.split("\n") if l.startswith("# ")]
    rows = []
    for line in text.split("\n"):
        if line.startswith("regexp/"):
            rows.append(line.split("\t"))
    return header, rows


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--repo", required=True)
    ap.add_argument("--scratch", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--tree-commit", default=BASE_COMMIT, help="the commit the reports were taken at")
    ap.add_argument("--runs", choices=("base", "after"), default="base")
    a = ap.parse_args()
    if os.listdir(a.scratch):
        raise SystemExit("the scratch directory is not empty")

    pin_lines, texts32 = write_pin(a.repo, a.scratch)
    v32: dict[str, dict[str, str]] = {}
    v43: dict[str, dict[str, str]] = {}
    names43 = sorted(git(a.repo, "ls-tree", "--name-only", f"{a.tree_commit}:{REGEXP}").decode().split())
    names32 = sorted(p.rsplit("/", 1)[1] for p in texts32)
    if names32 != names43:
        raise SystemExit(f"the files differ: {names32} {names43}")
    for name in names43:
        v32.update(parse_vectors(texts32[f"{REGEXP}/{name}"], name))
        v43.update(parse_vectors(git(a.repo, "show", f"{a.tree_commit}:{REGEXP}/{name}").decode("utf-8"), name))

    out: list[str] = []
    w = out.append
    w(f"# part-d/reading-revision-32.txt: the three {a.runs} reports' regexp/ lines read again against corpus revision 32's")
    w("# class lines (the owner's answer 5 of 2026-11-19, \"Read revision 32\"), by read-revision-32.py beside it, through")
    w("# the scratch pin revision-32.pin. No run was taken for it: the rendered outcomes are the reports' own, and no")
    if a.runs == "base":
        w("# retained file changes. Retained and read, not judged, as part D's configuration says of the base runs.")
    else:
        w(f"# retained file changes. The after reports, taken at {a.tree_commit}, which part D judges.")
    w("")

    # The equivalence of revision 32 and revision 43 over regexp/.
    if a.tree_commit == BASE_COMMIT:
        w("== Revision 32 against revision 43 (the base commit's tree), conformance/vectors/regexp/")
    else:
        w(f"== Revision 32 against the tree of {a.tree_commit}, conformance/vectors/regexp/")
    if sorted(v32) != sorted(v43):
        raise SystemExit("the ids differ")
    w(f"vectors: {len(v32)} at each, the same ids; status vectors (no engine-level line): "
      f"{sum(is_status(v) for v in v32.values())} at each")
    class_moves: Counter[str] = Counter()
    other_diffs = []
    div_removed = div_added = div_changed = 0
    for vid in sorted(v32):
        a32, a43 = v32[vid], v43[vid]
        keys = set(a32) | set(a43)
        for k in sorted(keys):
            if k == "class":
                if a32[k] != a43[k]:
                    class_moves[f"{a32[k]} -> {a43[k]}"] += 1
            elif k.startswith("divergence."):
                if k in a32 and k not in a43:
                    div_removed += 1
                elif k in a43 and k not in a32:
                    div_added += 1
                elif a32[k] != a43[k]:
                    div_changed += 1
            elif a32.get(k) != a43.get(k):
                other_diffs.append((vid, k))
    w("every other field (id, area, op, in, out, per-engine.*, prov, script, note, since) is the same in both: "
      + ("yes" if not other_diffs else f"NO: {other_diffs}"))
    if other_diffs:
        raise SystemExit(f"fields other than class and divergence differ: {other_diffs[:5]}")
    w("class lines that differ: " + (", ".join(f"{n} {m}" for m, n in sorted(class_moves.items())) or "none"))
    w(f"divergence.* fields: {div_removed} at revision 32 and not at 43; {div_added} at 43 and not at 32; "
      f"{div_changed} at both with other text")
    exp_moves = [vid for vid in v32 if expected_for(v32[vid]) != expected_for(v43[vid])]
    w("the expected outcome the comparison uses for broiler-js (per-engine on impl-defined, otherwise out) is the same "
      "at both revisions for every vector: " + ("yes" if not exp_moves else f"NO: {exp_moves}"))
    if exp_moves:
        raise SystemExit("an expected outcome differs; the re-read needs the outcome parser")
    w("")

    population = sorted(vid for vid, v in v32.items() if not is_status(v))
    reread: dict[str, list[str]] = {}
    changed_lines: dict[str, list[tuple[str, str, str]]] = {}
    w(f"== Each {a.runs} report")
    for culture in CULTURES:
        path = os.path.join(a.out, f"run-{culture}.tsv.gz")
        header, rows = read_report(path)
        ids = [r[0] for r in rows]
        if sorted(ids) != population:
            raise SystemExit(f"{culture}: the regexp/ lines are not revision 32's non-status vectors")
        if len(set(ids)) != len(ids):
            raise SystemExit(f"{culture}: an id twice")
        v43_counts: Counter[str] = Counter()
        v32_counts: Counter[str] = Counter()
        nlh43 = nlh32 = 0
        lines32 = []
        changed = []
        for r in rows:
            if len(r) != 5:
                raise SystemExit(f"{culture}: a line of {len(r)} columns: {r[0]}")
            vid, klass, expected, rendered, verdict_text = r
            vec43, vec32 = v43[vid], v32[vid]
            if klass != vec43["class"]:
                raise SystemExit(f"{culture}: {vid}'s class column is not revision 43's")
            if expected != expected_for(vec43):
                raise SystemExit(f"{culture}: {vid}'s expected column is not revision 43's")
            suffix = " (divergence no longer holds)"
            nlh = verdict_text.endswith(suffix)
            verdict43 = verdict_text[: -len(suffix)] if nlh else verdict_text
            if verdict43 not in ("match", "classified-divergence", "difference", "not-implemented"):
                raise SystemExit(f"{culture}: {vid}: verdict {verdict_text!r}")
            if nlh != (verdict43 == "match" and f"divergence.{ENGINE}" in vec43):
                raise SystemExit(f"{culture}: {vid}: the report's no-longer-holds mark does not follow revision 43")
            v43_counts[verdict43] += 1
            nlh43 += nlh
            # VectorRun.Compare under revision 32's vector, the rendered outcome the report's.
            matched = verdict43 == "match"
            if matched:
                verdict32 = "match"
            elif rendered.startswith("fault: "):
                verdict32 = "difference"
            elif vec32["class"] in ("divergence", "impl-defined") and f"divergence.{ENGINE}" in vec32:
                verdict32 = "classified-divergence"
            else:
                verdict32 = "difference"
            if not matched and verdict43 == "classified-divergence" and not (
                    vec43["class"] in ("divergence", "impl-defined") and f"divergence.{ENGINE}" in vec43):
                raise SystemExit(f"{culture}: {vid}: the report's verdict does not follow revision 43")
            nlh32_here = matched and f"divergence.{ENGINE}" in vec32
            v32_counts[verdict32] += 1
            nlh32 += nlh32_here
            line32 = "\t".join([vid, vec32["class"], expected_for(vec32), rendered,
                                verdict32 + (suffix if nlh32_here else "")])
            lines32.append(line32)
            if line32 != "\t".join(r):
                changed.append((vid, f"{klass}, {verdict_text}", f"{vec32['class']}, {verdict32 + (suffix if nlh32_here else '')}"))
        reread[culture] = lines32
        changed_lines[culture] = changed
        corpus = next((h for h in header if h.startswith("# corpus revision: ")), "# corpus revision: ?")
        w(f"{culture}: run-{culture}.tsv.gz, {corpus[2:]}; regexp/ lines: {len(rows)}, one for each of revision 32's "
          f"non-status vectors; class and expected columns are revision 43's for every line")
        written = corpus[2:].replace("corpus revision: ", "revision ")
        w(f"  as written ({written}): " + ", ".join(f"{k} {v43_counts[k]}" for k in sorted(v43_counts))
          + f"; divergence no longer holds: {nlh43}")
        w("  read again (revision 32): " + ", ".join(f"{k} {v32_counts[k]}" for k in sorted(v32_counts))
          + f"; divergence no longer holds: {nlh32}")
        w(f"  lines that read differently: {len(changed)}")
        w(f"  sha256 of the re-read regexp/ lines (each line and LF): "
          f"{sha256(''.join(l + chr(10) for l in lines32).encode())}")
    w("")

    w("== The three cultures' re-read regexp/ lines against each other")
    for x, y in (("invariant", "tr-TR"), ("invariant", "lt-LT"), ("tr-TR", "lt-LT")):
        same = reread[x] == reread[y]
        w(f"{x} against {y}: " + ("byte-identical" if same else
                                   f"{sum(1 for p, q in zip(reread[x], reread[y]) if p != q)} lines differ"))
    w("")

    w("== The lines that read differently, as written -> revision 32 read again (class, verdict)")
    if all(changed_lines[c] == changed_lines["invariant"] for c in CULTURES):
        w("(the same lines, with the same readings, in all three reports)")
        for vid, before, after in changed_lines["invariant"]:
            w(f"{vid}\t{before}\t->\t{after}")
    else:
        for culture in CULTURES:
            w(f"-- {culture}")
            for vid, before, after in changed_lines[culture]:
                w(f"{vid}\t{before}\t->\t{after}")
    w("")

    # The part's clauses on the re-read lines: at the base, its configuration's "retained and read, not judged".
    by_class: dict[str, dict[str, Counter[str]]] = {}
    for culture in CULTURES:
        for r in (l.split("\t") for l in reread[culture]):
            key = "match (divergence no longer holds)" if r[4].endswith("(divergence no longer holds)") else r[4]
            by_class.setdefault(culture, {}).setdefault(r[1], Counter())[key] += 1
    identical = reread["invariant"] == reread["tr-TR"] == reread["lt-LT"]
    if a.runs == "base":
        w("== Part D's clauses at the base, at revision 32 (decision-rule.md:216-228, with its note of 2026-11-01)")
    else:
        w("== Part D's clauses on the after runs, at revision 32 (decision-rule.md:216-228, with its note of 2026-11-01)")
    w(f"population: revision 32's non-status vectors of regexp/, {len(population)}; each of the three reports gives "
      f"each one line, and the {len(v32) - len(population)} status vectors none")
    if a.runs == "base":
        w("comparison, the reports byte for byte: read at the base, the three reports' regexp/ lines read again are "
          + ("byte-identical" if identical else "NOT byte-identical"))
        w("comparison, each vector's result against its class's expectation for broiler-js, read at the base "
          "(revision 32's class; the rule judges it on the after runs alone):")
    else:
        w("comparison, the three after reports byte for byte (their regexp/ lines read again): "
          + ("byte-identical" if identical else "NOT byte-identical"))
        w("comparison, each vector's result after against its class's expectation for broiler-js (revision 32's "
          "class):")
    for culture in CULTURES:
        for klass in sorted(by_class[culture]):
            w(f"  {culture}, {klass}: " + ", ".join(f"{k} {n}" for k, n in sorted(by_class[culture][klass].items())))
    if a.runs == "base":
        w("MET clause: not read here; it is the after runs' (\"the three after reports are byte-identical and every "
          "vector's result after is the outcome its class expects\").")
    else:
        w("MET clause: the bundle's README reads it from the lines above; this file judges nothing.")

    text = "\n".join(out) + "\n"
    with open(os.path.join(a.out, "reading-revision-32.txt"), "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
    with open(os.path.join(a.out, "revision-32.pin"), "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(pin_lines) + "\n")
    sys.stdout.write(text)
    return 0


if __name__ == "__main__":
    sys.exit(main())
