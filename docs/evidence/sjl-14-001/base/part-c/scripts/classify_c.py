#!/usr/bin/env python3
"""Part C's classes over the side-by-side tool's run at the base commit (bundle sjl-14-001, decision rule part C).

    classify_c.py <repo root> <report.tsv.gz> <spans.tsv> <dumps-p-b.txt.gz> <readings.tsv> <out dir> [<bridge-codes.tsv> [revised-2026-11-23]]

<repo root> is a checkout whose records are the base commit's: corrections/ (K1 to K10, classes E and T, partly.tsv,
test262-paths.tsv, k-paths.tsv, k-rows.tsv), Broiler.ECMAScript/docs/frontend/stages.tsv and early-errors.tsv, bundle
sjl-13-001's classification/classification.tsv.gz, and the bridge's message table
(Broiler.JS/Broiler.JavaScript.Parser/Bridge/BridgeMessages.cs). <report> is the side-by-side tool's report (P, B, K,
K-on with --dump); <spans.tsv> spancheck.py's listing over the --text dumps of P and B, and <dumps-p-b.txt.gz> those
dumps; <readings.tsv> the hand-written readings this script names below (written by a reader, each with its basis).

Every difference the rule's comparison names ("per input and variant, P against B and K-off against K-on: outcome,
exception type, message and dump hash") is written to <out>/differences.tsv.gz with the class it is read as, or
"unclassified", and its basis. The classes, as the rule and its header define them, read by the corrections as the
base commit holds them (no row is added here):
  C1  an outcome move to the outcome bundle sjl-13-001's classification expects in configuration J, explained by a row
      of corrections/k-rows.tsv (K1 to K10): for a test262 input as corrections/test262-paths.tsv explains it; for any
      other input by the same reading test262-paths.tsv makes of the classification (a K when the input's basis names
      a row of k-rows.tsv; else, for an input expected refused that P and K both accepted, K9, K8 or K7 by the
      candidate rows' families), P and K taken at the base; or an input corrections/k-paths.tsv cites for a row; for a
      tree difference outside spans, an input k-paths.tsv cites for K10's W1a, W1b or W1c
  C3  a refusal for nesting past the library's bound ("nested too deeply" or "syntax tree is too deep" "to parse
      (maximum depth N)"), with the depth the message reports
  E   P accepted, B refused, K refusing on both sides, the move explained as class E's (test262-paths.tsv's or the
      classification's reading: E, T at P/B, E-if-coded, E-partly, or the correspondence's D31 bucket of "same in
      outcome, Broiler.JS refusing only in its compiler"), and the row of the library's code qualifying: a syntax row
      of class-e.tsv, or a syntax row whose js_compiler_only is partly or no that the classification lists among the
      rows whose codes may come first; never not-checked. The library's code is not in the run (the reading aid that
      would give it failed at the base), so the row is read from B's text: the rows whose message identifier renders
      it (early-errors.tsv's message_id, the bridge's message table). When every such row qualifies the move is E;
      when B's text renders rows the classification lists first, only those are read (the library's code read as one
      the classification expects first); when some do and some do not, the move is read by <readings.tsv> (the row the input's construct matches, by the
      production each row names), or is unclassified. Every E is therefore provisional, a reading of B's text in place of
      the code, and its basis says so; where B's text also renders rows that would not qualify, which only the narrowing
      to the classification's first rows sets aside, the basis names them (changed 2026-11-23, after review: the basis
      began "row(s) ..." and named no row the narrowing set aside)
  T   a move test262-paths.tsv or the classification explains by T (every candidate row a class T row), at K/K-on, or
      at P/B where K accepted at the base
  M   a refusal at both entries with the same exception type and another text (compare.py --messages' tables)
  S   a dump difference whose every differing line is a span difference where P's span disagrees with the node's
      source text and B's agrees: spancheck.py's S, or a line spancheck.py lists both-agree that the readings below
      decide, each by its kind and both texts:
        S-head      a declaration in a for head: P's text takes the head's ";" (or the "in"/"of" side), which is no part
                    of the declaration; B's ends at its last declarator
        S-start     P's span begins inside the node's first operand (after "new X", "[" or "{"), B's at its start
        S-punct     P's span takes punctuation or a comment that follows the node and is no part of it (an expression's
                    ";", ")", "]", "}" or ":"; a statement's trailing comment or the enclosing block's "}")
        S-semi      a break or continue statement: P's span leaves out the statement's own ";", B's takes it
      and a both-agree line no reading decides is "undecided". Three readings are named but are not S, as P's span agrees
      with the node's text:
        B-short-semicolon  a declaration in statement position whose B span leaves out the statement's ";" while P's
                    takes it: B's does not agree
        P-eof-same-text    P's span ends at the end-of-input token ("eof") where B's gives the same offset as a number:
                    both spans give exactly the node's text, so neither disagrees; the dumps differ in how the end is
                    written, not in the span
        B-short-bracket    a node whose end the AST type takes from its child (a member, an optional chain, an import
                    call), where the child's line in the same dump is S-punct: P's span is the construct's whole text,
                    and B's leaves out its closing bracket, which spancheck.py admits for a child-end kind
      A dump difference with any line read as one of these is unclassified. (Changed 2026-11-23, after review: the last
      two were read S, as "S-eof" ("P's span ends at the end-of-input token, which is no part of the node, and B's at
      the node's last token; both texts are the same characters only because nothing follows the node") and "S-derived"
      ("P's end is its child's wrong end"); checked against the text, P's span agrees in both, so neither is S by the
      rule's words.) spancheck.py's own S lines are taken as it reads them: for an edge the AST type fixes, it admits
      a B text that leaves a bracket open or unopened, so B's text in such a line may not be the node's balanced text
  By code (added 2026-11-23, records part 4): given <bridge-codes.tsv>, the corrected reading aid's output (each input
  variant P or B refused, with the library's diagnostics in its order, code:messageId, the first of which is the one the
  bridge refuses with), class E is read by the library's code and not from B's text: the row is the stage table's row
  whose shared_code is the first diagnostic's code, and the move is E when that row qualifies as above, else
  unclassified; <readings.tsv> is not read for it, and no E is provisional. A variant B refused that the aid finds the
  library accepting, or with no diagnostic, is unclassified (the refusal is the bridge's own). Without the file, class E
  is read from B's text as above.
  Under the rule's revision of 2026-11-23 (added 2026-11-23, records part 5): given the word revised-2026-11-23 after
  <bridge-codes.tsv>, the classes decision-rule-revised-2026-11-23.md adds to part C are read too, each only for the
  inputs it names, and nothing else changes:
    rev-code     an outcome move at P/B of one of the eleven inputs that revision names, in its strict or sloppy
                 variant, P accepting, B refusing and K refusing on both sides, whose first diagnostic by the library's
                 code is the one it names for that input: EX0009 (JSE9109) for T035220, T039055, T047023, J000058 and
                 J001174; EX0008 (JSE9108) for T034966, T034969, T046767 and T046770; EE0033 (JSE1329) for J000040
                 and J001147; read only where class E is not
    rev-eof      a dump difference, in any variant, whose every differing line is S or the P-eof-same-text reading
                 above, with at least one of the latter (the end written eof read as the same offset written as the
                 source's length: one span); on these runs, exactly the input variants the earlier unclassified.tsv
                 (SHA-256 1622df7102aa57fa94730542b05df91843f5c2f8099feeefc8dc26166820194f) lists with that reading
    rev-bracket  a dump difference of T036087, T039546 or T051374, in its strict or sloppy variant, whose every
                 differing line is S or the B-short-bracket reading above, with at least one of the latter
  Without the word, nothing of the revision is read, and the script writes the tables it wrote before it.
Anything else is unclassified, with the reason. Writes differences.tsv.gz, span-readings.tsv.gz (every both-agree
line with its reading), unclassified.tsv (every unclassified difference, with both sides) and classes.txt. The gzip
files are written with no name and mtime 0, so the same inputs give the same bytes. Run with python3 -I.
"""
import collections, gzip, json, os, re, sys

root, report, spans_path, dumps_path, readings_path, out = sys.argv[1:7]
codes_path = sys.argv[7] if len(sys.argv) > 7 else None
revised = sys.argv[8] if len(sys.argv) > 8 else None
if revised not in (None, "revised-2026-11-23") or (revised and not codes_path):
    sys.exit("classify_c.py: the eighth argument is revised-2026-11-23, after <bridge-codes.tsv>")
# The rule's revision of 2026-11-23: the inputs it names, each with its row and the library's code (rev-code), and the
# inputs whose B-short-bracket lines it admits (rev-bracket); both variants of each.
REV_VARIANTS = ("strict", "sloppy")
REV_CODE = {i: ("EX0009", "JSE9109") for i in ("T035220", "T039055", "T047023", "J000058", "J001174")}
REV_CODE.update({i: ("EX0008", "JSE9108") for i in ("T034966", "T034969", "T046767", "T046770")})
REV_CODE.update({i: ("EE0033", "JSE1329") for i in ("J000040", "J001147")})
REV_BRACKET = {"T036087", "T039546", "T051374"}
B = os.path.join(root, "docs", "evidence", "sjl-14-001", "corrections") + "/"
op = lambda p: gzip.open(p, "rt", encoding="utf-8", newline="\n") if p.endswith(".gz") else open(p, encoding="utf-8", newline="\n")


def gz_write(path, text):
    with open(path, "wb") as f:
        with gzip.GzipFile(filename="", mode="wb", fileobj=f, compresslevel=9, mtime=0) as g:
            g.write(text.encode("utf-8"))


def table(path):
    cols, rows = None, []
    with op(path) as h:
        for line in h:
            if line.startswith("#"):
                continue
            f = line.rstrip("\n").split("\t")
            if cols is None:
                cols = f
                continue
            rows.append(dict(zip(cols, f)))
    return rows


# The report.
rep = collections.defaultdict(dict)
order = []
with op(report) as h:
    for line in h:
        if line.startswith("#%") or line.startswith("id\t"):
            continue
        f = line.rstrip("\n").split("\t")
        k = (f[0], f[1])
        if k not in rep:
            order.append(k)
        rep[k][f[2]] = f

# The records.
manifest = {r["id"]: r for r in table(os.path.join(root, "conformance", "parse", "corpus.manifest"))}
paths = {(r["id"], r["variant"]): r for r in table(B + "test262-paths.tsv")}
kp = collections.defaultdict(set)
for r in table(B + "k-paths.tsv"):
    if r["id"] != "-":
        kp[r["id"]].add(r["k"] + ":" + r["row"])
krows = {r["row"]: r["k"] for r in table(B + "k-rows.tsv")}
class_e = {r["id"] for r in table(B + "class-e.tsv")}
class_t = {r["id"] for r in table(B + "class-t.tsv")}
partly = {r["id"] for r in table(B + "partly.tsv")}
stages = {r["id"]: r for r in table(os.path.join(root, "Broiler.ECMAScript", "docs", "frontend", "stages.tsv"))}
catalogue = {r["id"]: r for r in table(os.path.join(root, "Broiler.ECMAScript", "docs", "frontend", "early-errors.tsv"))}
rows_of_msg = collections.defaultdict(set)
for r in catalogue.values():
    rows_of_msg[r["message_id"]].add(r["id"])
classification = {}
for r in table(os.path.join(root, "docs", "evidence", "sjl-13-001", "classification", "classification.tsv.gz")):
    if r["config"] == "J":
        classification[(r["id"], r["variant"])] = r
CORR_ROW = re.compile(r"^(?:[TPNDRB]\d|IC\d)")
REGEXP_ROWS = {"EE0186", "EE0187", "EE0188", "EE0189", "EE0190", "EE0208"}
readings = {}
for r in table(readings_path):
    readings[(r["id"], r["variant"])] = r
row_of_code = {r["shared_code"]: r["id"] for r in stages.values()}
codes = None
if codes_path:
    codes = {}
    with op(codes_path) as h:
        for line in h:
            f = line.rstrip("\n").split("\t")
            diags = [d.split(":", 1) for d in f[3].split(";")] if len(f) > 3 and f[3] else []
            codes[(f[0], f[1])] = (f[2], diags)

# The bridge's texts: message identifier -> template, from its message table.
src = open(os.path.join(root, "Broiler.JS", "Broiler.JavaScript.Parser", "Bridge", "BridgeMessages.cs"), encoding="utf-8").read()
templates = dict(re.findall(r'^\s*"([a-z0-9.\-]+)" => "((?:[^"\\]|\\.)*)",\s*$', src, re.M))
unnamed = sorted(m for m in rows_of_msg if m not in templates)
pats = []
for mid, t in sorted(templates.items()):
    rx = re.escape(t.replace('\\"', '"')).replace(re.escape("{0}"), ".+?")
    pats.append((mid, re.compile("^" + rx + "( at \\d+, \\d+)?$")))


def msgids(text):
    ids = {mid for mid, rx in pats if rx.match(text)}
    if re.match(r"^Unexpected token .+?( at \d+, \d+)?$", text):
        ids |= set(unnamed)  # an identifier the table does not name renders as "Unexpected token {0}"
    return ids


def rows_of_text(text):
    s = set()
    for m in msgids(text):
        s |= rows_of_msg.get(m, set())
    return s


def explain(key, p_out, k_out):
    """test262-paths.tsv's row for a test262 input; for any other input the same reading of the classification."""
    if key in paths:
        r = paths[key]
        return r["expect"], [x for x in r["first"].split(";") if x != "-"], r["explained_by"], "test262-paths.tsv", r["rows"]
    c = classification.get(key)
    if not c or c["expect"] == "either":
        return None
    exp = c["expect"]
    first = [x for x in c["first"].split(";") if x != "-"]
    basis = [b for b in c["basis"].split(";") if CORR_ROW.match(b)]
    ks = sorted({krows[b] for b in basis if b in krows}, key=lambda x: int(x[1:]))
    other = sorted({b for b in basis if b not in krows})
    if exp == "refused" and p_out == "accepted" and k_out != "accepted":
        kind = "stage"
    elif exp == "refused" and p_out == "accepted":
        kind = "missed"
    elif exp == "accepted" and (p_out != "accepted" or k_out != "accepted"):
        kind = "refused-legal"
    else:
        kind = "none"
    if ks:
        eb = ";".join(ks)
    elif kind == "stage":
        if first and all(f in class_e for f in first):
            eb = "T" if all(f in class_t for f in first) else "E"
        elif any(f in class_e for f in first):
            eb = "E-if-coded"
        elif any(f in partly for f in first):
            eb = "E-partly"
        else:
            eb = ";".join("row:" + o for o in other) or "unexplained"
    elif kind == "missed":
        fam = {catalogue[f]["family"] for f in first if f in catalogue}
        if first and set(first) <= REGEXP_ROWS:
            eb = "K9"
        elif first and fam <= {"EX"}:
            eb = "K8"
        elif first and fam and "EX" not in fam:
            eb = "K7"
        elif first:
            eb = "K7;K8"
        else:
            eb = "unexplained"
    else:
        eb = ";".join("row:" + o for o in other) or "unexplained"
    return exp, first, eb, "the classification (configuration J), read as test262-paths.tsv reads it, P and K at the base", ";".join(basis) or "-"


def qualifies(row, first):
    s = stages.get(row, {})
    if s.get("stage") != "syntax" or s.get("js_compiler_only") == "not-checked":
        return False
    if row in class_e:
        return True
    return s.get("js_compiler_only") in ("partly", "no") and row in first


nest = re.compile(r"(nested too deeply|syntax tree is too deep) to parse \(maximum depth (\d+)\)")


def move(pair, key, x, y, e):
    frm, to = x[3], y[3]
    k_both_refused = e["K"][3] == "refused" and e["K-on"][3] == "refused"
    if to == "refused":
        m = nest.search(y[5])
        if m:
            return "C3", "nesting past the library's bound, depth " + m.group(2)
    ex = explain(key, e["P"][3], e["K"][3])
    if ex:
        exp, first, eb, src_, rows = ex
        if exp != to:
            return "unclassified", "moves to %s; %s expects %s (%s)" % (to, src_, exp, eb)
        if re.fullmatch(r"K\d+(;K\d+)*", eb):
            return "C1", "%s, rows %s (%s)" % (eb, rows, src_)
        if eb == "T" and (pair == "K/K-on" or e["K"][3] == "accepted"):
            return "T", "class T row(s) %s (%s)" % (";".join(first), src_)
        if pair == "P/B" and eb in ("E", "T", "E-if-coded", "E-partly", "row:D31") and frm == "accepted":
            if not k_both_refused:
                return "unclassified", "%s's %s, but K does not refuse on both sides" % (src_, eb)
            if codes is not None:
                lib, diags = codes.get(key, ("absent", []))
                if not diags:
                    return "unclassified", "the reading aid finds the library %s with no diagnostic: the refusal is the bridge's own (%s's %s)" % (lib, src_, eb)
                code, mid = diags[0]
                row = row_of_code.get(code)
                if row is None:
                    return "unclassified", "the library's code %s (%s) is no row of the stage table (%s's %s)" % (code, mid, src_, eb)
                s_ = stages[row]
                if qualifies(row, first):
                    why = "class-e.tsv" if row in class_e else "js_compiler_only %s, among the classification's first rows" % s_["js_compiler_only"]
                    return "E", "row %s by the library's code %s (%s; %s), K refusing on both sides (%s's %s)" % (row, code, mid, why, src_, eb)
                if revised and key[0] in REV_CODE and key[1] in REV_VARIANTS and REV_CODE[key[0]] == (row, code):
                    return "rev-code", "row %s by the library's code %s (%s), K refusing on both sides, which decision-rule-revised-2026-11-23.md names for %s; not class E: stage %s, js_compiler_only %s, %s the classification's first rows %s (%s's %s)" % (
                        row, code, mid, key[0], s_.get("stage"), s_.get("js_compiler_only"), "among" if row in first else "not among", ";".join(first) or "-", src_, eb)
                return "unclassified", "row %s by the library's code %s (%s) does not qualify as class E: stage %s, js_compiler_only %s, %s the classification's first rows %s (%s's %s)" % (
                    row, code, mid, s_.get("stage"), s_.get("js_compiler_only"), "among" if row in first else "not among", ";".join(first) or "-", src_, eb)
            rendered = rows_of_text(y[5])
            cand = (rendered & set(first)) or rendered
            how = "B's text and the classification's first rows" if rendered & set(first) else "B's text"
            ok = {r for r in cand if qualifies(r, first)}
            if cand and ok == cand:
                aside = sorted(r for r in rendered - cand if not qualifies(r, first))
                note = ("; B's text also renders %s, which would not qualify, set aside only by reading the library's code as one of the classification's first rows" % ";".join(aside)) if aside else ""
                return "E", "provisional (the library's code is not in the run): row(s) %s by %s, every one qualifying, K refusing on both sides (%s's %s)%s" % (";".join(sorted(ok)), how, src_, eb, note)
            rd = readings.get(key)
            if rd and rd["row"] in rendered:
                if qualifies(rd["row"], first):
                    return "E", "provisional (the library's code is not in the run): row %s by the reading of the input's construct (readings.tsv: %s), K refusing on both sides (%s's %s)" % (rd["row"], rd["basis"], src_, eb)
                return "unclassified", "row %s by the reading of the input's construct (readings.tsv: %s) does not qualify as class E (%s's %s; first %s)" % (
                    rd["row"], rd["basis"], src_, eb, ";".join(first) or "-")
            return "unclassified", "%s's %s: by %s the rows %s, of which %s qualify as class E (first %s)" % (
                src_, eb, how, ";".join(sorted(cand)) or "none", ";".join(sorted(ok)) or "none", ";".join(first) or "-")
        return "unclassified", "%s explains it by %s, which no class admits at %s" % (src_, eb, pair)
    if key[0] in kp and to != frm:
        return "C1", "k-paths.tsv cites the input for " + ",".join(sorted(kp[key[0]]))
    return "unclassified", "neither test262-paths.tsv, the classification nor k-paths.tsv explains it"


# Span readings: spancheck.py's listing, and the P dump's field label for each listed line.
spans = collections.defaultdict(list)
with op(spans_path) as h:
    cols = None
    for line in h:
        f = line.rstrip("\n").split("\t")
        if cols is None:
            cols = f
            continue
        spans[(f[0], f[1])].append(dict(zip(cols, f)))
want = {k: {int(r["line"]) for r in v if r["class"] == "both-agree"} for k, v in spans.items()}
want = {k: v for k, v in want.items() if v}
labels = {}
with op(dumps_path) as h:
    cur, idx = None, 0
    for line in h:
        if line.startswith("=== "):
            p = line[4:].split()
            cur = (p[0], p[1]) if p[2] == "P" and (p[0], p[1]) in want else None
            idx, stack = 1, []  # spancheck.py numbers a dump's lines from 1
            continue
        if cur is not None:
            m = re.match(r"^(\d+) ([^:]+): (\S+)(?: (\w+))?", line)
            depth = int(m.group(1)) if m else -1
            stack = stack[:max(depth, 0)] + [(m.group(2), m.group(4) if m.group(3).startswith("Broiler.") and m.group(4) else "") if m else ("?", "")]
            if idx in want[cur]:
                parent = next((s[1] for s in reversed(stack[:-1]) if s[1]), "-")
                labels[(cur[0], cur[1], idx)] = (stack[-1][0], parent)
            idx += 1

EXPR_KINDS = {"SequenceExpression", "YieldExpression", "BinaryExpression", "AssignmentExpression", "ConditionalExpression"}
STMT_KINDS = {"ExpressionStatement", "ReturnStatement", "ThrowStatement", "VariableDeclaration"}


def unq(s):
    try:
        return json.loads(s)
    except Exception:
        return s


def read_span(r, by_line):
    kind, tp, tb = r["kind"], unq(r["text-P"]), unq(r["text-B"])
    sp, sb = r["span-P"].split(".."), r["span-B"].split("..")
    label, parent = labels.get((r["id"], r["variant"], int(r["line"])), ("?", "?"))
    if sp[1] == "eof" and sb[1] != "eof" and sp[0] == sb[0] and tp == tb:
        return "P-eof-same-text"
    if kind == "VariableDeclaration" and sp[0] == sb[0] and tp.startswith(tb):
        tail = tp[len(tb):]
        if re.fullmatch(r"(?:\s|/\*.*?\*/)*;", tail, re.S):
            if label == "Init" and parent == "ForStatement":
                return "S-head"
            return "B-short-semicolon"
    if sp[1] == sb[1] and tb.endswith(tp) and len(tb) > len(tp):
        head = tb[:len(tb) - len(tp)]
        if re.fullmatch(r"new [\w$.]+ ?|\[|\{ ?", head):
            return "S-start"
    if sp[0] == sb[0] and tp.startswith(tb) and len(tp) > len(tb):
        tail = tp[len(tb):]
        if kind in ("BreakStatement", "ContinueStatement"):
            return "undecided"
        if kind in EXPR_KINDS and re.fullmatch(r"\s*[;)\]}:]", tail):
            return "S-punct"
        if kind in STMT_KINDS - {"VariableDeclaration"} and re.fullmatch(r"\s*(?:/\*.*?\*/\s*)+|\s*}", tail, re.S):
            return "S-punct"
        if kind in ("MemberExpression", "OptionalChain", "ImportCall") and re.fullmatch(r"[\])]", tail):
            child = [o for o in by_line if o is not r and o["kind"] == "SequenceExpression" and unq(o["text-P"]).endswith(tail)
                     and read_span(o, []) == "S-punct"]
            return "B-short-bracket" if child else "undecided"
    if sp[0] == sb[0] and tb.startswith(tp) and len(tb) > len(tp):
        if kind in ("BreakStatement", "ContinueStatement") and re.fullmatch(r"\s*;", tb[len(tp):]):
            return "S-semi"
    return "undecided"


counts = collections.Counter()
diff_lines = ["pair\tid\tvariant\tkind\tfrom\tto\tfrom-type\tfrom-message\tto-type\tto-message\tclass\tbasis\n"]
span_lines = ["id\tvariant\tline\tkind\tfield\tparent\tspan-P\tspan-B\treading\ttext-P\ttext-B\n"]
uncl = ["pair\tid\tvariant\tkind\tfrom\tto\tP-or-K-side\tB-or-K-on-side\treason\n"]
span_counts = collections.Counter()
for key in order:
    e = rep[key]
    for a, b in (("P", "B"), ("K", "K-on")):
        x, y = e[a], e[b]
        pair = a + "/" + b
        if x[3] != y[3]:
            kind = "outcome"
            cls, basis = move(pair, key, x, y, e)
        elif x[3] in ("refused", "crashed") and x[4] != y[4]:
            kind, cls, basis = "type", "unclassified", "the exception type differs"
        elif x[3] in ("refused", "crashed") and x[5] != y[5]:
            kind = "message"
            cls, basis = ("M", "same type, another text (the generated message table)") if x[3] == "refused" else ("unclassified", "a crash's message differs")
        elif x[3] == "accepted" and a == "P" and x[6] != y[6]:
            kind = "dump"
            sr = spans.get(key, [])
            cl = collections.Counter(r["class"] for r in sr)
            readings_here = collections.Counter()
            for r in sr:
                if r["class"] == "both-agree":
                    rd = read_span(r, sr)
                    readings_here[rd] += 1
                    label, parent = labels.get((r["id"], r["variant"], int(r["line"])), ("?", "?"))
                    span_lines.append("\t".join([r["id"], r["variant"], r["line"], r["kind"], label, parent, r["span-P"], r["span-B"], rd, r["text-P"], r["text-B"]]) + "\n")
                    span_counts[(r["kind"], rd)] += 1
            if not sr:
                cls, basis = "unclassified", "the dumps differ and spancheck.py lists no line"
            elif cl.get("not-span") or cl.get("unclassified"):
                ks = sorted(k for k in kp.get(key[0], ()) if k.split(":")[1] in ("W1a", "W1b", "W1c"))
                cls, basis = ("C1", "a tree difference outside spans; k-paths.tsv cites the input for " + ",".join(ks)) if ks else (
                    "unclassified", "a tree difference outside spans (%s)" % ", ".join("%s %d" % kv for kv in sorted(cl.items())))
            else:
                bad = {k: v for k, v in readings_here.items() if not k.startswith("S-")}
                rev = None
                if revised and set(bad) == {"P-eof-same-text"}:
                    rev = "rev-eof"
                elif revised and set(bad) == {"B-short-bracket"} and key[0] in REV_BRACKET and key[1] in REV_VARIANTS:
                    rev = "rev-bracket"
                if not bad:
                    parts = ["S %d" % cl["S"]] if cl.get("S") else []
                    parts += ["%s %d" % kv for kv in sorted(readings_here.items())]
                    cls, basis = "S", "every differing line S (%s)" % ", ".join(parts)
                elif rev:
                    parts = ["S %d" % cl["S"]] if cl.get("S") else []
                    parts += ["%s %d" % kv for kv in sorted(readings_here.items())]
                    cls, basis = rev, "every differing line S or the %s reading decision-rule-revised-2026-11-23.md admits (%s)" % (
                        next(iter(bad)), ", ".join(parts))
                else:
                    cls, basis = "unclassified", "span lines not S: %s" % ", ".join("%s %d" % kv for kv in sorted(bad.items()))
        else:
            continue
        counts[(pair, kind, cls)] += 1
        diff_lines.append("\t".join([pair, key[0], key[1], kind, x[3], y[3], x[4], x[5], y[4], y[5], cls, basis]) + "\n")
        if cls == "unclassified":
            side = lambda f: f[3] + ((" " + f[4] + ": " + f[5]) if f[3] != "accepted" else (" dump " + f[6][:12] if len(f) > 6 and f[6] != "-" else ""))
            uncl.append("\t".join([pair, key[0], key[1], kind, x[3], y[3], side(x), side(y), basis]) + "\n")

os.makedirs(out, exist_ok=True)
gz_write(os.path.join(out, "differences.tsv.gz"), "".join(diff_lines))
gz_write(os.path.join(out, "span-readings.tsv.gz"), "".join(span_lines))
with open(os.path.join(out, "unclassified.tsv"), "w", encoding="utf-8", newline="\n") as w:
    w.writelines(uncl)
lines = ["# classify_c.py's counts: differences by entry pair, kind and class; both-agree span lines by kind and reading"]
lines += ["%d\t%s\t%s\t%s" % (v, *k) for k, v in sorted(counts.items())]
lines += ["%d\tspan\t%s\t%s" % (v, *k) for k, v in sorted(span_counts.items())]
if codes is None:
    lines.append("%d\tE moves set aside B's text's rows that would not qualify, by reading the library's code as one of the classification's first rows" % sum(
        1 for l in diff_lines[1:] if l.split("\t")[10] == "E" and "which would not qualify" in l))
else:
    pb = [l.split("\t") for l in diff_lines[1:] if l.startswith("P/B\t")]
    lines.append("%d\tP/B variants B refused whose library code the reading aid gives (bridge-codes.tsv)" % sum(
        1 for f in pb if f[5] == "refused" and codes.get((f[1], f[2]), ("", []))[1]))
    lines.append("%d\tP/B outcome moves to refused where the message identifier of the library's first code is not one B's text renders" % sum(
        1 for f in pb if f[3] == "outcome" and f[5] == "refused" and codes.get((f[1], f[2]), ("", []))[1]
        and codes[(f[1], f[2])][1][0][1] not in msgids(f[9])))
lines.append("%d\tunclassified differences (unclassified.tsv)" % (len(uncl) - 1))
lines.append("%d\tinput variants with an unclassified difference" % len({(l.split("\t")[1], l.split("\t")[2]) for l in uncl[1:]}))
with open(os.path.join(out, "classes.txt"), "w", encoding="utf-8", newline="\n") as w:
    w.write("\n".join(lines) + "\n")
print("\n".join(lines))
