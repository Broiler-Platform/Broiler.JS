#!/usr/bin/env python3
"""Part K's listing aid (bundle sjl-14-001, decision rule part K; the owner's answer 15 of 2026-11-24): every hit of
hits.tsv that is not a facade member, listed with the declaration, at the after commit 83a9bd82, that shows what it is.
Written 2026-11-24, after part K was read NOT MET; a reading aid with no run (it reads git objects alone).

    list_k.py <hits.tsv> <repository> <out file>

<repository> is any clone holding the after commit; every file is read as `git show 83a9bd82:<path>`, never from a
working tree. hits.tsv's rows are read in their order; a row sorted "facade member" is left out (the facade members
are read against dispositions.md, not here), and every other row is listed. Each hit line is read again from the tree
and must be the line hits.tsv gives. What each is:

  changelog line   a line of a CHANGELOG.md; the declaration is the heading of the entry it belongs to, the nearest
                   line above it that begins with '#'
  record line      a line of another Markdown file, a JSON manifest or a PublicAPI.txt list; when the line names a
                   member of another type by its full name, the declaration is that member's (RECORDS below),
                   otherwise the nearest heading above it
  comment line     a code line that is a comment (it begins with //, /// or *); the declaration is the line itself
  code line        every other line: each occurrence of the hit's name in it (as a whole word) is read as the code
                   reads it, and its declaration found:
                     inside a string literal      the words of a message string; the declaration is the member whose
                                                  message table holds it (STRING_HOLDERS)
                     Q.Name                       a member of Q's type: Q a type (QUALIFIER_TYPES) or a variable whose
                                                  declared type is VARIABLE_TYPES' entry for the file
                     Type { Name: ... }           a member of the property pattern's type
                     Name( after new              a constructor of the type Name
                     Name( otherwise              a method of the type the line is in (for BridgeTreeBuilder's partial
                                                  files, its four files)
                     a declaration line of Name   the line itself (an enum member, a field, a constant, a property)
                     a member of the type it is   MEMBERS_IN_FILE (a constant, a field or a parameter used bare)
                     anything else                the type Name (a pattern, a cast, a parameter's or a list's type)
                   and each declaration must be one line of the tree that declares that name, checked by its pattern.
Every row is "another type's member of the same name" (the compiler's FastCompiler members among them, which
dispositions.md keeps until SJL-15.5), "the facade's own" (a facade file's State and its members, which dispositions.md
keeps; hits.tsv sorted them OTHER), or text: a changelog, record or comment line, or the words of a message string.
Writes one row per listed hit and the counts. Run with python3 -I."""
import collections, re, subprocess, sys

COMMIT = "83a9bd82a5d258d3001468095ebf43f9efc3e9a7"
hits_path, repo, out = sys.argv[1:4]
_files = {}


def lines_of(path):
    if path not in _files:
        text = subprocess.run(["git", "-C", repo, "show", f"{COMMIT}:{path}"], capture_output=True, check=True).stdout
        _files[path] = text.decode("utf-8").lstrip("\ufeff").split("\n")
    return _files[path]


P = "Broiler.JS/Broiler.JavaScript.Parser/"
B = P + "Bridge/"
F = "Broiler.ECMAScript/Broiler.ECMAScript.Frontend/"
TREE = [F + "SyntaxTree.Expressions.cs", F + "SyntaxTree.Statements.cs", F + "SyntaxTree.Functions.cs", F + "SyntaxTree.Modules.cs"]
BUILDER = [B + "BridgeTreeBuilder.cs", B + "BridgeTreeBuilder.Expressions.cs", B + "BridgeTreeBuilder.Functions.cs", B + "BridgeTreeBuilder.Modules.cs"]

# Where each type the hits name is declared (the files searched for its declaration), and whose it is.
TYPES = {
    "FrameKind": ([B + "BridgeScopes.cs"], "the bridge's"), "Frame": ([B + "BridgeScopes.cs"], "the bridge's"),
    "BridgeTreeBuilder": (BUILDER, "the bridge's"), "BridgeMessages": ([B + "BridgeMessages.cs"], "the bridge's"),
    "FastVariableKind": (["Broiler.JS/Broiler.JavaScript.Ast/AstVariableDeclaration.cs"], "the old tree's"),
    "AstExpressionStatement": (["Broiler.JS/Broiler.JavaScript.Ast/Statements/AstExpressionStatement.cs"], "the old tree's"),
    "ObjectProperty": (["Broiler.JS/Broiler.JavaScript.Ast/ObjectProperty.cs"], "the old tree's"),
    "FastToken": (["Broiler.JS/Broiler.JavaScript.Ast/Misc/FastToken.cs"], "the old tree's"),
    "TokenTypes": (["Broiler.JS/Broiler.JavaScript.Expressions/Misc/TokenTypes.cs"], "the engine's"),
    "FastScanner": ([P + "FastScanner.cs"], "the facade's"), "State": ([P + "FastScanner.cs"], "the facade's"),
    "FastCompiler": (["Broiler.JS/Broiler.JavaScript.Compiler/FastCompiler.EarlyErrors.cs"], "the compiler's"),
    "LegacyOctalLiteralDetector": (["Broiler.JS/Broiler.JavaScript.Compiler/FastCompiler.EarlyErrors.cs"], "the compiler's"),
    "ScopeKind": ([F + "DeclarationTables.cs"], "the library's"), "BindingKind": ([F + "DeclarationTables.cs"], "the library's"),
    "ProposalFlags": ([F + "ProposalFlags.cs"], "the library's"), "TokenKind": ([F + "TokenKind.cs"], "the library's"),
    "Tokenizer": ([F + "Tokenizer.cs"], "the library's"), "Catalogue": ([F + "Tokenizer.cs"], "the library's"),
    "UriCodec": (["Broiler.ECMAScript/Broiler.ECMAScript.Algorithms/Text/UriCodec.cs"], "the library's"),
    "JsonReader": (["Broiler.ECMAScript/Broiler.ECMAScript.Algorithms/Text/JsonReader.cs"], "the library's"),
    "NumberPattern": (["Broiler.ECMAScript/Broiler.ECMAScript.Intl/NumberFormatter.cs"], "the library's"),
    "JsNumberPattern": (["Broiler.ECMAScript/Broiler.ECMAScript.Intl.Tests/Differential/Vm/JsNumberData.cs"], "the library's tests'"),
    "JSUInt8ArrayText": (["Broiler.ECMAScript/Broiler.ECMAScript.Algorithms.Tests/ReferenceArms/Text/J/JSUInt8ArrayText.cs"], "the library's tests'"),
}
for t in ["Expression", "Statement", "StringLiteral", "TemplateLiteral", "ObjectLiteral", "ParenthesizedExpression",
          "FunctionExpression", "ArrowFunctionExpression", "ClassExpression", "ChainExpression", "YieldExpression", "Decorator",
          "ComputedPropertyName", "FunctionNode", "ClassNode", "MethodDefinition", "FieldDefinition", "ImportDeclaration",
          "ExportNamedDeclaration", "ExportDefaultDeclaration", "BlockStatement", "VariableDeclaration", "ExpressionStatement",
          "IfStatement", "DoWhileStatement", "WhileStatement", "ForStatement", "WithStatement", "SwitchStatement", "TryStatement",
          "FunctionDeclaration", "ClassDeclaration"]:
    TYPES[t] = (TREE, "the library's syntax tree's")

# A qualifier written as a type (Q.Name), by its last name.
QUALIFIER_TYPES = {"FrameKind", "FastVariableKind", "ScopeKind", "BindingKind", "ProposalFlags", "TokenTypes", "TokenKind",
                   "Catalogue", "UriCodec"}
EXTERNAL = {"HashCode": "System.HashCode, of the .NET base class library: no declaration in the repository"}

# A variable's declared type, by the file it is used in (each read from its declaration above the hit: a case pattern, a
# parameter or a var; the checks below find the member in that type).
VARIABLE_TYPES = {
    "BridgeTreeBuilder.Expressions.cs": {"parenthesized": "ParenthesizedExpression", "computed": "ComputedPropertyName",
                                         "chain": "ChainExpression", "function": "FunctionExpression",
                                         "arrow": "ArrowFunctionExpression", "@class": "ClassExpression",
                                         ")": "FunctionExpression"},
    "BridgeTreeBuilder.Functions.cs": {"function": "FunctionNode", "computed": "ComputedPropertyName", "@class": "ClassNode",
                                       "method": "MethodDefinition", "field": "FieldDefinition"},
    "BridgeTreeBuilder.Modules.cs": {"function": "FunctionDeclaration", "@class": "ClassDeclaration"},
    "BridgeTreeBuilder.cs": {"expression": "ExpressionStatement", "function": "FunctionDeclaration",
                             "declaration": "ClassDeclaration", "@try": "TryStatement"},
    "BridgeScopes.cs": {"top": "Frame", "frame": "Frame", "function": "Frame"},
    "FastScanner.cs": {"current": "FastToken"},
    "JsNumberData.cs": {"result": "JsNumberPattern"},
    "NumberFormatter.cs": {"result": "NumberPattern"},
}
# A name used bare as a member of the type the line is in (not a call): (file, name) -> type.
MEMBERS_IN_FILE = {(F + "Tokenizer.cs", "NoBigIntWidth"): "Tokenizer", (P + "FastScanner.cs", "position"): "State"}
# A record line that names another type's member by its full name: (file:line) -> (type, member).
RECORDS = {
    "Broiler.ECMAScript/CODE-ASSURANCE.md:199": ("Tokenizer", "ReadToken"),
    "Broiler.ECMAScript/HUMAN_REVIEW.md:530": ("Tokenizer", "ReadToken"),
    "Broiler.ECMAScript/hairs.manifest.json:3253": ("Tokenizer", "ReadToken"),
    "Broiler.ECMAScript/CODE-ASSURANCE.md:154": ("JsonReader", "ReadNumber"),
    "Broiler.ECMAScript/HUMAN_REVIEW.md:440": ("JsonReader", "ReadNumber"),
    "Broiler.ECMAScript/hairs.manifest.json:1152": ("JsonReader", "ReadNumber"),
    "Broiler.ECMAScript/hairs.manifest.json:4649": ("NumberPattern", "ReadNumber"),
    "Broiler.ECMAScript/Broiler.ECMAScript.Algorithms/PublicAPI.txt:819": ("UriCodec", "HexDigitValue"),
    "Broiler.ECMAScript/Broiler.ECMAScript.Algorithms/docs/adapter-guide-text.md:189": ("UriCodec", "HexDigitValue"),
    "Broiler.ECMAScript/hairs.manifest.json:1229": ("UriCodec", "HexDigitValue"),
    "Broiler.ECMAScript/hairs.manifest.json:3224": ("Catalogue", "UnexpectedCharacter"),
    "Broiler.ECMAScript/hairs.manifest.json:3275": ("Tokenizer", "UnexpectedCharacter"),
    "Broiler.ECMAScript/hairs.manifest.json:3243": ("Tokenizer", "NoBigIntWidth"),
}
# A string literal's holder: the member whose literal text the line is, by file.
STRING_HOLDERS = {B + "BridgeMessages.cs": ("BridgeMessages", "Text")}

FACADE_OWN = "the facade's own: FastScanner's State and its members, which dispositions.md keeps"
TYPE_DECL = r"\b(class|struct|record|enum|interface)\s+%s\b"


def braces(s):
    """The change of brace depth a code line makes, its comment, string and character literals left out."""
    s = re.sub(r"'(\\.|[^'\\])'", "", s)
    s = re.sub(r'@?"(\\.|[^"\\])*"', "", s)
    s = s.split("//")[0]
    return s.count("{") - s.count("}"), "{" in s


def find_all(files, pattern, after=None):
    """Every line of <files> matching <pattern> that is not a comment (inside the braces of the type <after> names)."""
    found = []
    for path in files:
        ls = lines_of(path)
        start, end = 0, len(ls)
        if after is not None:
            heads = [i for i, s in enumerate(ls) if re.search(after, s) and not s.strip().startswith(("//", "*"))]
            if len(heads) != 1:
                continue
            start = heads[0]
            depth, opened, end = 0, False, len(ls)
            for i in range(start, len(ls)):
                change, has = braces(ls[i])
                depth += change
                opened = opened or has
                if opened and depth <= 0:
                    end = i + 1
                    break
        for i in range(start, end):
            s = ls[i]
            if re.search(pattern, s) and not s.strip().startswith(("//", "*", "///")):
                found.append((path, i + 1, s.strip()))
    return found


def find_one(files, pattern, after=None, what=""):
    found = find_all(files, pattern, after)
    if len(found) != 1:
        raise SystemExit(f"declaration of {what} not found once: {pattern} after {after}: {found}")
    return found[0]


def type_decl(t):
    files, owner = TYPES[t]
    return find_one(files, TYPE_DECL % re.escape(t), what=t) + (owner,)


def member_decl(t, m):
    files, owner = TYPES[t]
    pat = (r"(^\s*%s\s*(,|=[^=>]|$))|(\b%s\s*(\{|=>|;|=[^=>]|\(|\)|,))" % (re.escape(m), re.escape(m)))
    after = TYPE_DECL % re.escape(t)
    hits = find_all(files, pat, after)
    e = re.escape(m)
    decl = (r"^((public|private|internal|protected|static|readonly|const|override|sealed|virtual|abstract|new|partial)\s+)+"
            r"[A-Za-z_][A-Za-z0-9_<>?,\[\]. ]*\s%s\s*(\{|=>|;|=|\()" % e)
    enum_member = r"^%s\s*(,|=\s*[0-9-]|$)" % e
    parameter = r"\(([^()]*,)?\s*[A-Za-z_][A-Za-z0-9_<>?\[\]]*\s+%s\)" % e
    decl_only = [h for h in hits if re.search(decl, h[2]) or re.search(enum_member, h[2]) or re.search(parameter, h[2])]
    if len(decl_only) != 1:
        raise SystemExit(f"member {t}.{m}: {hits}")
    return decl_only[0] + (owner,)


def member_has(t, m):
    try:
        member_decl(t, m)
        return True
    except SystemExit:
        return False


def containing_type(path, num):
    """The innermost type declared above line <num> of <path> whose braces hold it."""
    types = containing_types(path, num)
    return types[0] if types else None


def containing_types(path, num):
    """The types declared above line <num> of <path> whose braces hold it, the innermost first."""
    ls = lines_of(path)
    held = []
    for i in range(num - 1):
        m = re.search(r"\b(class|struct|record|enum|interface)\s+([A-Za-z_][A-Za-z0-9_]*)", ls[i])
        if not m or ls[i].strip().startswith(("//", "*")):
            continue
        depth, opened, inside = 0, False, True
        for j in range(i, num - 1):
            change, has = braces(ls[j])
            depth += change
            opened = opened or has
            if opened and depth <= 0:
                inside = False
                break
        if inside:
            held.insert(0, m.group(2))
    return held


def in_string(before):
    return len(re.findall(r'(?<!\\)"', before)) % 2 == 1


def read_code(name, path, num, line):
    base = path.split("/")[-1]
    out = []
    for m in re.finditer(r"(?<![A-Za-z0-9_])%s(?![A-Za-z0-9_])" % re.escape(name), line):
        before, after = line[: m.start()], line[m.end():]
        if in_string(before):
            t, mem = STRING_HOLDERS[path]
            d = member_decl(t, mem)
            out.append(("text: words of a message string (not a comment, changelog or record line)", d,
                        f"a message text of the bridge's {t}.{mem}"))
            continue
        q = re.search(r"(@?[A-Za-z_][A-Za-z0-9_]*|\))\s*(\?)?\.$", before)
        if q:
            qual = q.group(1)
            if qual in EXTERNAL:
                out.append(("another type's member of the same name", None, EXTERNAL[qual]))
                continue
            t = qual if qual in QUALIFIER_TYPES else VARIABLE_TYPES[base][qual]
            d = member_decl(t, name)
            out.append(("another type's member of the same name", d, f"{d[3]} {t}.{name}"))
            continue
        pat = re.search(r"([A-Za-z_][A-Za-z0-9_]*)\s*\{[^{}]*$", before)
        if re.match(r"\s*:", after) and pat and re.search(r"[{,]\s*$", before):
            t = pat.group(1)
            d = member_decl(t, name)
            out.append(("another type's member of the same name", d, f"{d[3]} {t}.{name}, in a property pattern"))
            continue
        if re.match(r"\s*\(", after) and not re.search(r"\bnew\s+$", before):
            # A method of the type the line is in, or of a type that holds it (a nested type calls its holder's).
            for t in (["BridgeTreeBuilder"] if path in BUILDER else containing_types(path, num)):
                if find_all(TYPES[t][0], r"\b%s\s*\(" % re.escape(name), TYPE_DECL % re.escape(t)) and member_has(t, name):
                    break
            d = member_decl(t, name)
            kept = ", which dispositions.md keeps until SJL-15.5" if d[3] == "the compiler's" else ""
            out.append(("another type's member of the same name", d, f"{d[3]} {t}.{name}, a method{kept}"))
            continue
        if re.match(r"\s*\(", after):
            d = type_decl(name)
            out.append(("another type's member of the same name", d, f"{d[3]} type {name}, a constructor call"))
            continue
        if (path, name) in MEMBERS_IN_FILE:
            t = MEMBERS_IN_FILE[(path, name)]
            d = member_decl(t, name)
            sort = FACADE_OWN if d[3] == "the facade's" else "another type's member of the same name"
            out.append((sort, d, f"{d[3]} {t}.{name}"))
            continue
        own = containing_type(path, num)
        if re.match(r"^\s*%s\s*(,|=|$)" % re.escape(name), line) or re.search(r"\b%s\s*(;|=>|=\s)" % re.escape(name), line) and re.match(r"\s*(public|private|internal|protected)\s", line):
            d = member_decl(own, name)
            if d[1] != num or d[0] != path:
                raise SystemExit(f"{path}:{num}: a declaration line of {name} that is not its declaration {d}")
            sort = FACADE_OWN if d[3] == "the facade's" else "another type's member of the same name"
            out.append((sort, d, f"{d[3]} {own}.{name}, declared on this line"))
            continue
        d = type_decl(name)
        out.append(("another type's member of the same name", d, f"{d[3]} type {name}"))
    if not out:
        raise SystemExit(f"{path}:{num}: no occurrence of {name}")
    return out


def heading(path, num, changelog):
    ls = lines_of(path)
    for i in range(num - 1, -1, -1):
        s = ls[i]
        if s.startswith("#"):
            return (path, i + 1, s.strip())
    raise SystemExit(f"{path}:{num}: no heading above")


rows = []
for raw in open(hits_path, encoding="utf-8"):
    if raw.startswith("#") or raw.startswith("section\t"):
        continue
    section, name, key, sort, same, text = raw.rstrip("\n").split("\t")
    if sort == "facade member":
        continue
    name = name.split(" (")[0]
    path, num = key.rsplit(":", 1)
    num = int(num)
    line = lines_of(path)[num - 1]
    if line[:200].rstrip() != text[:200].rstrip():
        raise SystemExit(f"{key}: the tree's line is not hits.tsv's")
    found = []
    if path.endswith("CHANGELOG.md"):
        found.append(("text: a changelog line", heading(path, num, True), "the heading of the changelog entry it belongs to"))
    elif path.endswith((".md", ".json")) or path.endswith("PublicAPI.txt"):
        if key in RECORDS:
            t, mem = RECORDS[key]
            if not re.search(r"\b%s\.%s\b" % (re.escape(t), re.escape(mem)), line) and mem not in line:
                raise SystemExit(f"{key}: does not name {t}.{mem}")
            d = member_decl(t, mem)
            found.append(("text: a record line, naming another type's member of the same name", d, f"{d[3]} {t}.{mem}"))
        else:
            found.append(("text: a record line", heading(path, num, False), "the record's heading above it"))
    elif line.strip().startswith(("//", "*", "/*")):
        found.append(("text: a comment line", (path, num, line.strip()), "the line itself, a comment"))
    else:
        found = read_code(name, path, num, line)
    sorts = sorted({f[0] for f in found})
    decls = []
    for f in found:
        d = f[1]
        cell = "%s:%d\t%s" % (d[0], d[1], d[2][:160]) if d else "-\t-"
        if (cell, f[2]) not in decls:
            decls.append((cell, f[2]))
    rows.append((section.split(" (scope")[0], name, key, " + ".join(sorts), " | ".join(c for c, _ in decls).replace("\t", " :: "),
                 " | ".join(w for _, w in decls)))

c = collections.Counter(r[3] for r in rows)
L = ["# list_k.py: every hit of hits.tsv but the facade members, with the declaration at 83a9bd82 that shows what it is",
     "section\tname\tfile:line\twhat it is\tdeclaration (file:line :: its line)\twhose"]
L += ["\t".join(r) for r in rows]
L.append("# listed: %d; by what it is: %s" % (len(rows), "; ".join("%s %d" % kv for kv in sorted(c.items()))))
open(out, "w", encoding="utf-8", newline="\n").write("\n".join(L) + "\n")
print(L[-1])
