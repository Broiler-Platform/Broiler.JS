#!/usr/bin/env python3
"""SJL-9.5's name search for the deletion list (bundle sjl-9-001, cutover/deletion-search.txt).

Usage: python3 deletion-search.py <repo> <base> <head>   (writes the report to standard output)

<base> is the cut-over's base, the default branch it merged last (its first merge's first parent is older; every merge
of main into the branch is a parent of <head>, so `git diff <base> <head>` is the cut-over's own change). The names are
every type, method, property, field and constant declared on a C# line the cut-over removes under Broiler.JS/ (product
and test code) that no C# line of the repository declares at <head>, and the names a commit of the branch itself declared and a later
one removed, which neither <base> nor <head> declares. Each is searched word-bounded in every tracked .cs
file of the repository at <head>, comments included, and each hit is marked "comment" (inside a // or /// comment or a
block comment's line) or "code". The deleted files' names are searched in every tracked file. The script decides
nothing: the report's classification of each name with a code hit is written by hand beneath it.
"""
import re
import subprocess
import sys

repo, base, head = sys.argv[1:4]


def git(*args):
    return subprocess.run(["git", "-C", repo, *args], capture_output=True, text=True, check=True).stdout


MODIFIERS = r"(?:public|private|protected|internal|static|readonly|sealed|abstract|override|virtual|const|partial|unsafe|new|extern|async|required|file)"
DECL = re.compile(
    r"^\s*(?:\[[^\]]*\]\s*)*(?:" + MODIFIERS + r"\s+)+"
    r"(?:(?:class|struct|record(?:\s+struct)?|interface|enum)\s+(?P<t>[A-Za-z_]\w*)"
    r"|[\w<>\[\],.?() ]+?\s+(?P<m>[A-Za-z_]\w*)\s*(?:\(|=|;|\{|=>|<))")


def declared_name(line):
    m = DECL.match(line)
    return (m.group("t") or m.group("m")) if m else None


removed = set()
for line in git("diff", "-U0", base, head, "--", "Broiler.JS/*.cs").splitlines():
    if line.startswith("-") and not line.startswith("---"):
        name = declared_name(line[1:])
        if name:
            removed.add(name)

declared = set()
for line in git("grep", "-h", "-I", "-e", "", head, "--", "*.cs").splitlines():
    name = declared_name(line)
    if name in removed:
        declared.add(name)
    for t in re.findall(r"\b(?:class|struct|record|interface|enum)\s+([A-Za-z_]\w*)", line):
        if t in removed:
            declared.add(t)

# Names a commit of the branch itself (no merge) declared and a later one removed: the interim members the base never had.
added = set()
for line in git("log", "-p", "--no-merges", "--format=", f"{base}..{head}", "--", "Broiler.JS/*.cs").splitlines():
    if line.startswith("+") and not line.startswith("+++"):
        name = declared_name(line[1:])
        if name:
            added.add(name)
everywhere = set()
for rev in (base, head):
    for line in git("grep", "-h", "-I", "-e", "", rev, "--", "*.cs").splitlines():
        name = declared_name(line)
        if name:
            everywhere.add(name)
        everywhere.update(re.findall(r"\b(?:class|struct|record|interface|enum)\s+([A-Za-z_]\w*)", line))
interim = added - everywhere

names = sorted((removed - declared) | interim)
deleted_files = [p for p in git("diff", "--name-only", "--diff-filter=D", base, head).splitlines()]

# The hand classification of each name with a code hit (the review of 2026-11-10, SJL-9.5's records). Every such hit
# is a different declaration that shares the name, or the name as text; none is a copy of the area's code left in the
# engine, and none reaches a deleted member (the cut-over's build at <head> has no error).
CLASSIFIED = {
    "IsField": "the deleted engine's Token.IsField. The one code hit is ILPack's own attribute encoder, "
               "Broiler.JS/Broiler.JavaScript/ILPack/AssemblyGenerator.Attributes.cs, a member of System.Reflection's "
               "custom attribute model of the same name",
    "Preferred": "the deleted private Preferred(string) of JSIntl's keyword tables. The code hits are the library's "
                 "RegionHourCycles.Preferred, a record property, its readers, and the differential check's copy of "
                 "Broiler.VM's time data",
    "States": "NumberingSystemDecimalData's member, declared at stage 3 and deleted at stage 4. The code hit is the "
              "English word in a library test's expected string, \"United States\"",
    "own": "NumberingSystemDecimalData's field, declared at stage 3 and deleted at stage 4. The hits are the English "
           "word, in comments and strings, and other members' locals of that name",
    "availableLocales": "the deleted private static field of JSIntl. The code hits are the library's LocaleNegotiation "
                        "parameters of that name and the generator tests' reads of CLDR's availableLocales.json",
    "digitOptions": "the deleted option snapshot's private field. The code hits are locals and parameters of that name "
                    "in the engine's adapter and the library",
    "fractionalDigits": "the deleted option snapshot's private field. The code hits are DurationFormat's option name as "
                        "a string, its key, and locals and parameters of that name",
    "localeTag": "a parameter of the deleted ResolveNumberingSystem and ResolveCalendar overloads. The code hits are "
                 "parameters of that name of the members the Collator keeps until SJL-17.5",
    "unitDisplay": "the deleted DurationFormat field and JSIntlNumberFormatResolved's constructor parameter. The code hits "
                   "are the option's name as a string and locals and parameters of that name",
    "unitStyle": "the deleted DurationFormat field. The code hits are locals of that name",
    "dateStyle": "the deleted option snapshot's private field. The code hits are the option's name as a string, its key "
                 "and the library's parameters and properties of that name",
    "timeStyle": "the deleted option snapshot's private field. The code hits are the option's name as a string, its key "
                 "and the library's parameters and properties of that name",
    "timeZoneName": "a parameter of the deleted engine's Fields constructors. The code hits are the option's name as a "
                    "string, its key, and other parameters and locals of that name",
    "notation": "the deleted snapshots' private field and JSIntlNumberFormatResolved's constructor parameter. The code "
                "hits are the option's name as a string and the library's parameters and locals of that name",
    "numberingSystem": "the deleted services' private fields. The code hits are the option's name as a string and "
                       "parameters, locals and properties of that name",
    "style": "the deleted services' private fields. The code hits are the option's name as a string and parameters, "
             "locals and properties of that name",
}
LIST_LIMIT = 40

out = []
out.append(f"## The deleted files, searched by file name in every tracked file at {head[:8]}")
for path in deleted_files:
    leaf = path.rsplit("/", 1)[-1]
    try:
        raw = git("grep", "-n", "-F", leaf, head, "--", ".")
    except subprocess.CalledProcessError:
        raw = ""
    hits = [h[len(head) + 1:] for h in raw.splitlines()]
    out.append(f"{path}: {len(hits)}")
    for h in hits:
        out.append(f"    {h[:200]}")
out.append("")
out.append(f"## The names, searched in every tracked .cs file at {head[:8]}")
out.append(f"(A name with more than {LIST_LIMIT} hits is listed by file, with each file's code hits; every other hit is")
out.append("listed by line.)")
nohit, withhit = [], []
for name in names:
    try:
        raw = git("grep", "-n", "-w", "-e", name, head, "--", "*.cs")
    except subprocess.CalledProcessError:
        raw = ""
    hits = []
    for h in raw.splitlines():
        h = h[len(head) + 1:]
        path, lineno, text = h.split(":", 2)
        pre = text[: re.search(r"\b" + re.escape(name) + r"\b", text).start()]
        kind = "comment" if ("//" in pre or pre.strip().startswith(("*", "/*"))) else "code"
        hits.append((kind, path, f"{path}:{lineno}: {text.strip()[:160]}"))
    if not hits:
        nohit.append(name)
        continue
    code = sum(1 for k, _, _ in hits if k == "code")
    withhit.append(name)
    out.append(f"{name}: {len(hits)} ({code} code)")
    if code:
        out.append(f"  classified: {CLASSIFIED.get(name, 'UNCLASSIFIED')}")
    else:
        out.append("  classified: comments only")
    if len(hits) <= LIST_LIMIT:
        for kind, _, h in hits:
            out.append(f"    [{kind}] {h}")
    else:
        by_file = {}
        for kind, path, _ in hits:
            total, codes = by_file.get(path, (0, 0))
            by_file[path] = (total + 1, codes + (kind == "code"))
        for path, (total, codes) in sorted(by_file.items()):
            out.append(f"    {path}: {total} ({codes} code)")
out.append("")
out.append(f"## Names with no hit ({len(nohit)})")
out.append(" ".join(nohit))
print(f"# names declared on removed lines: {len(removed)}; still declared at {head[:8]}: {len(declared)}; declared and removed within the branch: {len(interim)} ({' '.join(sorted(interim))}); searched: {len(names)}; with a hit: {len(withhit)}; with no hit: {len(nohit)}")
print("\n".join(line.rstrip() for line in out))
