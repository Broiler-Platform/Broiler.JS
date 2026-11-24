#!/usr/bin/env python3
"""Part H's comparison (bundle sjl-14-001, decision rule part H): "per member, present at the base, at the after, or at
both with a changed signature or attribute", from the API lister's listings at the base commit (base/local/part-h/api/)
and at the after commit (after/local/api/api/), every assembly of both pack sets; and its reading aid: each member
removed, changed or newly obsolete, with the row of docs/public-api.md at the after commit (section "Public API changes,
2026-11-19 (window W2 ...)") whose disposition covers it, and dispositions.tsv's disposition for it. Written 2026-11-24.

    read_h.py <repo root> <base api dir> <after api dir> <obsolete-marks.tsv> <out dir>

A listing (PublicApiGenerator's text) is read into members: a type is "(type)", every other declaration its signature
with white space folded, keyed by assembly, namespace and the enclosing types' path; an attribute (its lines while a string
concatenation or a bracket stays open), or a run of them, belongs to the declaration after it; a declaration that runs over several lines (a parenthesis left open, a comma at a
line's end outside an enum, or a "where" line after it) is one member. A member is obsolete when it or any type
that encloses it carries System.Obsolete; its mark is that attribute's text. A member present at the base in one
assembly and at the after commit in another, with the same namespace, path and signature, is "moved" (and the
after commit's metadata reader, obsolete-marks.tsv, says whether the first assembly forwards the type). Writes
members.tsv (every member that differs: kind removed, added, moved, attributes changed or newly obsolete, with both
attribute texts and the row) and reading.txt (counts by assembly, kind and row; every member no row covers). Run with
python3 -I."""
import collections, os, re, sys

root, base_dir, after_dir, marks_path, out = sys.argv[1:6]
TYPE = re.compile(r"^public (?:(?:static|abstract|sealed|readonly|ref|partial|unsafe|new|record)\s+)*(class|struct|enum|interface|delegate|record)\s+(?:[\w.<>,\[\] ]+\s+)?([A-Za-z_]\w*)")


def logical(path):
    lines = open(path, encoding="utf-8").read().split("\n")
    out, i, kinds = [], 0, []
    while i < len(lines):
        raw = lines[i]; s = raw.strip(); ind = len(raw) - len(raw.lstrip()); i += 1
        if not s:
            continue
        if s.startswith("["):
            buf = s
            while (buf.endswith("+") or buf.count("[") > buf.count("]")) and i < len(lines):
                buf += " " + lines[i].strip(); i += 1
            out.append(("attr", ind, buf)); continue
        if s in ("{", "}"):
            out.append((s, ind, s)); continue
        buf = s
        in_enum = bool(kinds) and kinds[-1][0] == "enum" and ind > kinds[-1][1]
        while i < len(lines):
            nxt = lines[i].strip()
            if (buf.count("(") > buf.count(")") or (buf.endswith(",") and not in_enum) or nxt.startswith("where ")) and nxt not in ("{", "}"):
                buf += " " + nxt; i += 1
            else:
                break
        m = TYPE.match(buf)
        if m and not buf.endswith("{ }"):
            while kinds and kinds[-1][1] >= ind: kinds.pop()
            kinds.append((m.group(1), ind))
        out.append(("decl", ind, re.sub(r"\s+", " ", buf)))
    return out


def members(path, asm):
    res, ns, stack, attrs = {}, None, [], []
    for kind, ind, text in logical(path):
        if kind == "decl" and text.startswith("namespace "):
            ns = text.split()[1]; stack = []; continue
        if kind == "attr":
            attrs.append(text); continue
        if kind in ("{", "}"):
            if kind == "}" and stack and stack[-1][1] == ind:
                stack.pop()
            continue
        while stack and stack[-1][1] >= ind:
            stack.pop()
        m = TYPE.match(text)
        own = " ".join(attrs); attrs = []
        enclosing = " ".join(a for _, _, a in stack)
        if m:
            name = m.group(2)
            pathname = ".".join([n for n, _, _ in stack] + [name])
            res[(asm, ns, pathname, "(type)")] = (own, enclosing, text)
            if not text.endswith("{ }"):
                stack.append((name, ind, own))
            continue
        sig = re.sub(r"\s*\{ \}$|\s*\{ get; (?:set; |init; )?\}$|\s*\{ (?:set|init); \}$|;$", "", text)
        res[(asm, ns, ".".join(n for n, _, _ in stack), sig)] = (own, enclosing, text)
    return res


def obsolete(attr_text):
    m = re.search(r"System\.Obsolete\((.*?)\)\]", attr_text)
    return m.group(0) if m else ""


B, A = {}, {}
for name in sorted(os.listdir(base_dir)):
    B.update(members(os.path.join(base_dir, name), name[:-4]))
for name in sorted(os.listdir(after_dir)):
    A.update(members(os.path.join(after_dir, name), name[:-4]))
forwarded = {}
for line in open(marks_path, encoding="utf-8"):
    f = line.rstrip("\n").split("\t")
    if len(f) >= 3 and f[2] == "forwarded":
        forwarded[(f[0], re.sub(r"`\d+$", "", f[1]))] = f[5] if len(f) > 5 else ""
disp = {}
for line in open(os.path.join(root, "docs/evidence/sjl-14-001/dispositions.tsv"), encoding="utf-8"):
    if line.startswith("#") or line.startswith("assembly\t"): continue
    f = line.rstrip("\n").split("\t")
    disp[(f[0], f[1], f[2].replace(".", "."), re.sub(r"\s+", " ", f[3]))] = f[4]
MOVED = {"StringSpan", "StringSpanReader", "ArraySpan", "IPropertyAccessor", "IPropertyValue", "UnaryOperator", "TokenTypes",
         "CancellableDisposableAction", "RegExpUnicodeValidator"}
ROWS = {
    "parser": "row 1: every public type of Broiler.JavaScript.Parser and every member of each, (b) obsolete, removal 1.0.0 (facade-obsolete)",
    "undomark": "row 2: FastTokenStream.UndoMark(), kept obsolete without a removal version (kept-obsolete-no-version)",
    "ast": "row 3: every public type of Broiler.JavaScript.Ast but the non-tree types and FastParseException, (b) obsolete, removal 1.0.0 (obsolete)",
    "moved": "row 4: the non-tree types moved from Ast to Expressions, Ast forwarding each, (a) forwarder (moved-forwarded)",
    "execute": "row 6: DirectEvalSupport.Execute(...), the new overload with bool inDerivedConstructor, added",
}


def mark_ok(mark, asm):
    return 'DiagnosticId="BJS0002"' in mark and "removed in version 1.0.0" in mark.replace('" + "', "")


def eff(entry):
    own, enc, _ = entry
    return obsolete(own) or obsolete(enc)


rows = []
for key in sorted(set(B) | set(A)):
    asm, ns, path, sig = key
    b, a = B.get(key), A.get(key)
    outer = path.split(".")[0] if path else sig
    if b and a:
        if b[0] == a[0] and eff(b) == eff(a):
            continue
        kind = "newly obsolete" if not eff(b) and eff(a) else "attributes changed"
    elif b:
        alt = [k for k in A if k[1:] == key[1:] and k[0] != asm]
        kind = "moved to " + alt[0][0] if alt else "removed"
    else:
        alt = [k for k in B if k[1:] == key[1:] and k[0] != asm]
        kind = "moved from " + alt[0][0] if alt else "added"
    row, why = "", ""
    mark = eff(a) if a else ""
    top = path.split(".")[0] if path else sig.split()[-1]
    if asm == "Broiler.JavaScript.Parser" and kind in ("newly obsolete", "attributes changed"):
        if "UndoMark()" in sig:
            row, why = ROWS["undomark"], "own mark %s at the base and after" % ("unchanged" if obsolete(b[0]) == obsolete(a[0]) else "CHANGED")
        else:
            row, why = ROWS["parser"], "mark names BJS0002 and 1.0.0: %s" % mark_ok(mark, asm)
    elif asm == "Broiler.JavaScript.Ast" and kind in ("newly obsolete", "attributes changed") and top not in MOVED and top != "FastParseException":
        row, why = ROWS["ast"], "mark names BJS0002 and 1.0.0: %s" % mark_ok(mark, asm)
    elif kind.startswith("moved to Broiler.JavaScript.Expressions") and asm == "Broiler.JavaScript.Ast" and top in MOVED:
        tname = ns + "." + top
        row, why = ROWS["moved"], "the after commit's metadata reader: %s" % ("Ast forwards " + tname + " to " + forwarded[(asm, tname)] if (asm, tname) in forwarded else "NO forwarder for " + tname)
    elif kind.startswith("moved from Broiler.JavaScript.Ast") and asm == "Broiler.JavaScript.Expressions" and top in MOVED:
        row, why = ROWS["moved"], "the same member's arrival in Expressions, attributes %s" % ("unchanged" if A[key][0] == B[(alt[0][0],) + key[1:]][0] else "CHANGED")
    elif asm == "Broiler.JavaScript.Compiler" and kind == "added" and "Execute(" in sig and "bool inDerivedConstructor" in sig:
        row, why = ROWS["execute"], "added; not a removal, change or new mark"
    d = disp.get(key, "")
    if not d:
        # dispositions.tsv's own reader keeps a member whose parameter attribute opens a brace up to that brace
        # ("...TupleElementNames(new string[] {"); such a line is read as a prefix of the member here
        d = next((v for k, v in disp.items() if k[:3] == key[:3] and k[3].endswith("{") and sig.startswith(k[3])), "")
    if not d and asm == "Broiler.JavaScript.Expressions":
        d = disp.get(("Broiler.JavaScript.Ast",) + key[1:], "")
    rows.append((asm, ns, path, sig, kind, row or "NO ROW", why, d or "(not in dispositions.tsv)", (b[0] if b else "-")[:300], (a[0] if a else "-")[:300]))
os.makedirs(out, exist_ok=True)
with open(os.path.join(out, "members.tsv"), "w", encoding="utf-8", newline="\n") as w:
    w.write("assembly\tnamespace\ttype\tmember\tkind\trow\tcheck\tdispositions.tsv\tattributes-base\tattributes-after\n")
    for r in rows:
        w.write("\t".join(x.replace("\t", " ") for x in r) + "\n")
c = collections.Counter((r[0], r[4], r[5]) for r in rows)
agree = collections.Counter()
for r in rows:
    want = {ROWS["parser"]: "facade-obsolete", ROWS["undomark"]: "kept-obsolete-no-version", ROWS["ast"]: "obsolete", ROWS["moved"]: "moved-forwarded"}.get(r[5])
    if want: agree[(r[5][:6], "dispositions.tsv " + ("agrees" if r[7] == want else "does not list it" if r[7].startswith("(not") else "DIFFERS: " + r[7]))] += 1
with open(os.path.join(out, "reading.txt"), "w", encoding="utf-8", newline="\n") as w:
    w.write("# read_h.py: part H's members that differ between the base and after listings, by assembly, kind and the row of\n")
    w.write("# docs/public-api.md (W2's section) that covers each; members.tsv lists each with both attribute texts.\n")
    w.write("assemblies at the base: %d; at the after commit: %d; members read: %d at the base, %d after\n" % (len(os.listdir(base_dir)), len(os.listdir(after_dir)), len(B), len(A)))
    for k, v in sorted(c.items()):
        w.write("%d\t%s\t%s\t%s\n" % (v, *k))
    w.write("## dispositions.tsv against the row, per member the row covers\n")
    for k, v in sorted(agree.items()):
        w.write("%d\t%s\t%s\n" % (v, *k))
    bad = [r for r in rows if r[5] == "NO ROW" or "False" in r[6] or "NO forwarder" in r[6] or "CHANGED" in r[6]]
    w.write("members no row covers, or whose check fails: %d\n" % len(bad))
    for r in bad:
        w.write("\t".join(r[:7]) + "\n")
