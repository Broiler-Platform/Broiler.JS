#!/usr/bin/env python3
"""Part K's listing aid, extended (bundle sjl-14-001, decision rule part K; the owner's answer 16 of 2026-11-24): the
hits listing.tsv lists as words of a message string, each with the message table that holds its literal, and the hits
it lists as a member of a type with no declaration in the repository, each with that type and its assembly. Written
2026-11-24, after part K's second reading; a reading aid with no run. listing.tsv is not changed.

    list_k2.py <listing.tsv> <repository> <reference pack folder> <out file>

<repository> is any clone holding the after commit; every file of the tree is read as `git show 83a9bd82:<path>` or
`git grep` at 83a9bd82, never from a working tree. <reference pack folder> is the folder of .NET's reference pack
Microsoft.NETCore.App.Ref that holds the target framework's reference assemblies and their documentation files (for
example <dotnet root>/packs/Microsoft.NETCore.App.Ref/<version>/ref/net10.0); it is read only for the library type's
assembly. Every check below must hold, or the script stops and writes nothing.

The message strings (listing.tsv's "text: words of a message string ..." rows):
  M1  each such row is a line of the bridge's BridgeMessages.cs and names the message table BridgeMessages.Text, its
      declaration cell the line at the after commit that declares Text;
  M2  Text is declared once, as an expression-bodied switch on its parameter messageId, and each such hit line lies
      between that declaration and the switch's closing "};";
  M3  each such hit line is one arm of that switch, "<identifier>" => "<text>",: the identifier a message identifier
      (lower-case words joined by dots), and the hit's name, as a whole word, only inside the text's literal, never in
      the identifier the switch looks up;
  M4  inside BridgeMessages.cs, Text is private, and is named, outside comments, only at its declaration and at one
      call, whose value is assigned to the local `template`; `template` is named only there and in
      `template.Replace(...)`, whose value is the rendered message;
  M5  BridgeMessages.cs names no reflection or lookup API (REFLECTION below);
  M6  over the search's scope at the after commit (Broiler.JS/ and Broiler.ECMAScript/ outside docs/), BridgeMessages
      is named outside its own file only in documentation comments and in calls of BridgeMessages.Render(;
  M7  over the same scope, each hit's literal text (up to its first "{0}") is found on its own arm, and elsewhere only
      on a line that asserts a rendered message (a line holding "Assert."); listed in the row.
So each such literal is a message text the bridge renders, and none is a member name read by reflection or a key looked
up by text.

The members of a type outside the repository (listing.tsv's rows whose declaration cell is "- :: -"):
  N1  each such row is a qualified call "<Type>.<name>(" on its line, the type's simple name named in the row's note;
  N2  no file of the after commit's tree declares a type of that simple name, or an alias of it (`using <Type> =`);
  N3  the hit's file imports the namespace System (`using System;`), so the name is System's type of that name;
  N4  the hit's project targets the framework the reference pack folder is for (its csproj's TargetFramework, the
      folder's last name);
  N5  that folder's System.Runtime.xml, the documentation file of the reference assembly System.Runtime, documents the
      type (T:System.<Type>) and a member of the hit's name taking as many arguments as the call passes.
The row then names the type and its assembly in place of a file and line. Run with python3 -I."""
import os, re, subprocess, sys

COMMIT = "83a9bd82a5d258d3001468095ebf43f9efc3e9a7"
listing_path, repo, refpack, out = sys.argv[1:5]
SCOPE = ["Broiler.JS", "Broiler.ECMAScript", ":!Broiler.JS/docs", ":!Broiler.ECMAScript/docs"]
MESSAGES = "Broiler.JS/Broiler.JavaScript.Parser/Bridge/BridgeMessages.cs"
STRING_KIND = "text: words of a message string (not a comment, changelog or record line)"
REFLECTION = ["typeof(", "nameof(", "GetType(", "GetMethod", "GetProperty", "GetField", "GetMember", "InvokeMember",
              "Activator", "Type.GetType", "Enum.Parse", "Enum.TryParse", "System.Reflection", "BindingFlags"]
checks = []


def fail(msg):
    raise SystemExit("check failed: " + msg)


def lines_of(path):
    text = subprocess.run(["git", "-C", repo, "show", f"{COMMIT}:{path}"], capture_output=True, check=True).stdout
    return text.decode("utf-8").lstrip("\ufeff").split("\n")


def grep(*args, scope=SCOPE):
    r = subprocess.run(["git", "-C", repo, "grep", "-n", *args, COMMIT, "--", *scope], capture_output=True)
    if r.returncode not in (0, 1):
        fail("git grep " + " ".join(args))
    found = []
    for raw in r.stdout.decode("utf-8").splitlines():
        _, path, num, text = raw.split(":", 3)
        found.append((path, int(num), text))
    return found


def code(s):
    return not s.strip().startswith(("//", "///", "*", "/*"))


rows = []
for raw in open(listing_path, encoding="utf-8"):
    if raw.startswith("#") or raw.startswith("section\t"):
        continue
    rows.append(raw.rstrip("\n").split("\t"))
strings = [r for r in rows if r[3] == STRING_KIND]
library = [r for r in rows if r[4] == "- :: -"]
if not strings or not library:
    fail("listing.tsv lists no message-string hit or no hit without a declaration")

# The message strings.
ml = lines_of(MESSAGES)
decl = [i + 1 for i, s in enumerate(ml) if re.search(r"\bText\s*\(\s*string\s+messageId\s*\)\s*=>\s*messageId\s+switch\b", s) and code(s)]
if len(decl) != 1 or not re.match(r"\s*private\s+static\s+string\?\s+Text\(", ml[decl[0] - 1]):
    fail("M2/M4: Text is not declared once, private, as a switch on messageId")
d = decl[0]
end = next(i + 1 for i in range(d, len(ml)) if ml[i].strip() == "};")
decl_cell = "%s:%d :: %s" % (MESSAGES, d, ml[d - 1].strip())
out_rows = []
for r in strings:
    name, key, cell, whose = r[1], r[2], r[4], r[5]
    path, num = key.rsplit(":", 1)
    num = int(num)
    if path != MESSAGES or whose != "a message text of the bridge's BridgeMessages.Text" or cell != decl_cell:
        fail(f"M1: {key}")
    if not d < num < end:
        fail(f"M2: {key} is not inside Text's switch")
    arm = re.fullmatch(r'\s*"([^"\\]*)"\s*=>\s*"((?:[^"\\]|\\.)*)",\s*', ml[num - 1])
    if not arm or not re.fullmatch(r"[a-z0-9-]+(\.[a-z0-9-]+)+", arm.group(1)):
        fail(f"M3: {key} is not one arm \"<identifier>\" => \"<text>\",")
    word = r"(?<![A-Za-z0-9_])%s(?![A-Za-z0-9_])" % re.escape(name)
    if re.search(word, arm.group(1)) or not re.search(word, arm.group(2)):
        fail(f"M3: {key}: {name} is not in the text's literal alone")
    prefix = arm.group(2).split("{0}")[0]
    elsewhere = [h for h in grep("-F", prefix) if (h[0], h[1]) != (MESSAGES, num)]
    if any("Assert." not in h[2] for h in elsewhere):
        fail(f"M7: {key}: its text is found on a line that is not an assertion: {elsewhere}")
    where = "; ".join("%s:%d, an assertion of a rendered message" % (h[0], h[1]) for h in elsewhere) or "none"
    out_rows.append(("text: words of a message string, in a message table", name, key,
                     "the message table BridgeMessages.Text, its arm for the message identifier " + arm.group(1),
                     decl_cell, '"%s"; the hit is in the text, not the identifier; the text elsewhere in the scope: %s' % (arm.group(2), where)))
checks.append("M1 every message-string row of listing.tsv is a line of %s and names BridgeMessages.Text, declared at %s:%d" % (MESSAGES, MESSAGES, d))
checks.append("M2 Text is declared once (%s:%d), a switch on messageId closed at line %d; every such hit line lies inside it" % (MESSAGES, d, end))
checks.append("M3 every such hit line is one arm \"<message identifier>\" => \"<text>\", and the hit's name is a whole word of the text alone")
uses = [i + 1 for i, s in enumerate(ml) if re.search(r"(?<![A-Za-z0-9_.])Text(?![A-Za-z0-9_])", s) and code(s)]
call = [i for i in uses if i != d]
if len(call) != 1 or not re.search(r"\bvar\s+template\s*=\s*Text\(", ml[call[0] - 1]):
    fail(f"M4: Text is named at {uses}")
tl = [i + 1 for i, s in enumerate(ml) if code(s) and re.search(r"\btemplate\b", re.sub(r'"(?:[^"\\]|\\.)*"', '""', s))]
rep = [i for i in tl if i != call[0]]
if len(rep) != 1 or not re.search(r"\bvar\s+message\s*=\s*template\.Replace\(", ml[rep[0] - 1]):
    fail(f"M4: template is named at {tl}")
if not any(re.search(r"\breturn\b.*\bmessage\b", ml[i]) for i in range(rep[0], min(rep[0] + 3, len(ml)))):
    fail("M4: the rendered message is not returned")
checks.append("M4 Text is private and named, outside comments, at its declaration and once at %s:%d (var template = Text(...)); "
              "template is named again only at %s:%d (var message = template.Replace(...)), and message is returned" % (MESSAGES, call[0], MESSAGES, rep[0]))
named = [api for api in REFLECTION if any(api in s for s in ml if code(s))]
if named:
    fail(f"M5: {named}")
checks.append("M5 %s names none of: %s" % (MESSAGES, ", ".join(REFLECTION)))
refs = [h for h in grep("-w", "BridgeMessages") if h[0] != MESSAGES]
bad = [h for h in refs if code(h[2]) and "BridgeMessages.Render(" not in h[2]]
if bad:
    fail(f"M6: {bad}")
checks.append("M6 outside its own file, BridgeMessages is named in the scope only in documentation comments and calls of BridgeMessages.Render(: %s" % (
    "; ".join("%s:%d" % (h[0], h[1]) for h in refs)))
checks.append("M7 every such hit's literal text, up to its first {0}, is found in the scope on its own arm, and elsewhere only on an assertion line (each row says where)")

# The members of a type outside the repository.
ref = os.path.normpath(refpack)
tfm = os.path.basename(ref)
pack_version = os.path.basename(os.path.dirname(os.path.dirname(ref)))
pack_name = os.path.basename(os.path.dirname(os.path.dirname(os.path.dirname(ref))))
xml = open(os.path.join(ref, "System.Runtime.xml"), encoding="utf-8").read()
for r in library:
    name, key, whose = r[1], r[2], r[5]
    path, num = key.rsplit(":", 1)
    num = int(num)
    line = lines_of(path)[num - 1]
    m = re.match(r"System\.([A-Za-z_][A-Za-z0-9_]*), of the \.NET base class library: no declaration in the repository$", whose)
    if not m:
        fail(f"N1: {key}: {whose}")
    t = m.group(1)
    callm = re.search(r"(?<![A-Za-z0-9_.])%s\.%s\(" % (re.escape(t), re.escape(name)), line)
    if not callm:
        fail(f"N1: {key}: no call {t}.{name}(")
    depth, i, args = 0, callm.end() - 1, 1
    for ch in line[callm.end() - 1:]:
        depth += {"(": 1, ")": -1}.get(ch, 0)
        if ch == "," and depth == 1:
            args += 1
        if depth == 0:
            break
    whole = grep("-E", r"(\b(class|struct|record|enum|interface)\s+%s\b)|(using\s+%s\s*=)" % (t, t), scope=["."])
    if whole:
        fail(f"N2: {whole}")
    fl = lines_of(path)
    if not any(s.strip() == "using System;" for s in fl):
        fail(f"N3: {path} does not import System")
    proj_dir = path
    while True:
        proj_dir = os.path.dirname(proj_dir)
        projs = [p for p in subprocess.run(["git", "-C", repo, "ls-tree", "--name-only", COMMIT, proj_dir + "/"], capture_output=True,
                                            check=True).stdout.decode().split() if p.endswith(".csproj")]
        if projs or not proj_dir:
            break
    if len(projs) != 1:
        fail(f"N4: {path}: no project")
    tf = re.findall(r"<TargetFramework>([^<]+)</TargetFramework>", "\n".join(lines_of(projs[0])))
    if tf != [tfm]:
        fail(f"N4: {projs[0]} targets {tf}, the reference pack folder is {tfm}")
    member = "M:System.%s.%s``%d(%s)" % (t, name, args, ",".join("``%d" % k for k in range(args)))
    if ('<member name="T:System.%s">' % t) not in xml or ('<member name="%s">' % member) not in xml:
        fail(f"N5: System.Runtime.xml does not document T:System.{t} and {member}")
    checks.append("N1 %s is the call %s.%s( with %d arguments" % (key, t, name, args))
    checks.append("N2 no file of the after commit's tree declares a type %s or an alias of it" % t)
    checks.append("N3 %s has `using System;`" % path)
    checks.append("N4 %s targets %s" % (projs[0], tfm))
    checks.append("N5 %s %s, ref/%s/System.Runtime.xml documents T:System.%s and %s" % (pack_name, pack_version, tfm, t, member))
    out_rows.append(("a member of a .NET library type", name, key, "System.%s.%s" % (t, name),
                     "System.%s, in the reference assembly System.Runtime (%s, ref/%s), in place of a file and line" % (t, pack_name, tfm),
                     "the line: %s" % line.strip()))

L = ["# list_k2.py: part K's listing aid, extended (answer 16): the message-string hits with their message table, and the "
     "hits of a type outside the repository with that type and its assembly, at 83a9bd82; listing.tsv is not changed",
     "# every check held:"]
L += ["# " + c for c in checks]
L.append("what it is\tname\tfile:line\tshown by\tdeclaration (file:line :: its line), or the type and its assembly\tthe literal, or the line")
L += ["\t".join(r) for r in out_rows]
L.append("# listed: message-string hits %d; hits of a .NET library type %d" % (len(strings), len(library)))
open(out, "w", encoding="utf-8", newline="\n").write("\n".join(L) + "\n")
print(L[-1])
