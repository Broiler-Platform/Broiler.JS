#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Write the class tables of bundle sjl-15-001 (Broiler.JS): corrections/class-e.tsv, class-t.tsv and corrections.tsv.

    python3 -I docs/evidence/sjl-15-001/tools/write_lists.py [check]

From Broiler.ECMAScript/docs/frontend/stages.tsv (every row whose stage is `semantics`) and the bundle's
probes/answers.tsv (Broiler.JS's answer to each probe at 6fda6ca4), by the rules corrections.md states:
- class E: every semantics row whose Broiler.JS stage (`js_stage`) is a compiler site;
- class T: every semantics row whose Broiler.JS site is FastCompiler.EarlyErrors, a Visit* method or DirectEvalSupport,
  which run as a function body is compiled or when eval runs, and every row a timing probe shows refused after any of
  the probe ran or not at all;
- C1, the corrections: every semantics row Broiler.JS checks nowhere (`js_stage` none), every row a timing probe shows
  answered with another error than a SyntaxError or not answered within its bound, and every value probe Broiler.JS
  answers otherwise than the specification, by its row of the correspondence.
`check` writes nothing and fails if a committed table differs from what it would write. Two runs write the same bytes.
"""
from __future__ import annotations

import csv
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
BUNDLE = os.path.dirname(HERE)
REPOSITORY = os.path.dirname(os.path.dirname(os.path.dirname(BUNDLE)))
STAGES = os.path.join(REPOSITORY, "Broiler.ECMAScript", "docs", "frontend", "stages.tsv")
ANSWERS = os.path.join(BUNDLE, "probes", "answers.tsv")
OUT = os.path.join(BUNDLE, "corrections")
COMPILER_SITES = ("SyntaxValidation", "FastCompiler.EarlyErrors", "Visit*", "DirectEvalSupport")
LAZY_SITES = ("FastCompiler.EarlyErrors", "Visit*", "DirectEvalSupport")
TEMPLATE_SE = "THROWS (template) SyntaxError"


def read_tsv(path):
    with open(path, encoding="utf-8", newline="") as fh:
        return list(csv.DictReader(fh, delimiter="\t"))


def short(answer):
    """A probe's answer without its message: the value, or THROWS [(template)] <name>, or TIMEOUT."""
    return answer.split(":", 1)[0] if answer.startswith("THROWS") else answer.split(" after")[0]


def tables():
    rows = [r for r in read_tsv(STAGES) if r["stage"] == "semantics"]
    answers = read_tsv(ANSWERS)
    timing = {}
    for a in answers:
        if a["group"] == "T":
            timing.setdefault(a["catalogue"], []).append(a)
    head = "# Bundle sjl-15-001 (Broiler.JS): {what}\n# Written by tools/write_lists.py from Broiler.ECMAScript/docs/frontend/stages.tsv and probes/answers.tsv; never\n# edited by hand. corrections.md says how it is read.\n"
    e = [head.format(what="class E, the semantics rows this engine checks in a compiler stage (decision D45)"),
         "id\tshared_code\tjs_stage\tjs_site\n"]
    t = [head.format(what="class T, the rows this engine raises late or not at all (decision D64, its second half)"),
         "id\tshared_code\tjs_stage\twhy\n"]
    c = [head.format(what="C1, the corrections the model makes of this engine, by row"),
         "row\tkind\tevidence\n"]
    for r in rows:
        stage = r["js_stage"]
        compiler = any(site in stage for site in COMPILER_SITES)
        if compiler:
            e.append("\t".join([r["id"], r["shared_code"], stage, r["js_site"]]) + "\n")
        probes = timing.get(r["id"], [])
        late = [p for p in probes if not p["broiler_js"].startswith(TEMPLATE_SE)
                and not p["broiler_js"].startswith("THROWS (template)") and not p["broiler_js"].startswith("TIMEOUT")
                and not (p["broiler_js"].startswith("THROWS ") and not p["broiler_js"].startswith("THROWS SyntaxError"))]
        why = []
        if any(site in stage for site in LAZY_SITES):
            why.append("site " + stage)
        if late:
            why.append("probes " + ", ".join(f"{p['id']} ({short(p['broiler_js'])})" for p in late))
        if why and compiler:
            t.append("\t".join([r["id"], r["shared_code"], stage, "; ".join(why)]) + "\n")
        wrong = [p for p in probes if not p["broiler_js"].startswith(TEMPLATE_SE)
                 and (p["broiler_js"].startswith("TIMEOUT")
                      or (p["broiler_js"].startswith("THROWS") and "SyntaxError" not in short(p["broiler_js"])))]
        if stage == "none":
            c.append("\t".join([r["id"], "checked nowhere (js_stage none)",
                                ", ".join(f"{p['id']} ({short(p['broiler_js'])})" for p in probes) or "no probe"]) + "\n")
        elif wrong:
            c.append("\t".join([r["id"], "another error than a SyntaxError, or no answer within the bound",
                                ", ".join(f"{p['id']} ({short(p['broiler_js'])})" for p in wrong)]) + "\n")
    values = {}
    for a in answers:
        if a["group"] == "V" and a["matches"][0] != "=":
            values.setdefault(a["correspondence"], []).append(a)
    for corr in sorted(values):
        c.append("\t".join([corr, "a completion value or outcome the specification does not give",
                            ", ".join(f"{p['id']} ({short(p['broiler_js'])}, specification {short(p['specification'])})"
                                      for p in values[corr])]) + "\n")
    return {"class-e.tsv": "".join(e), "class-t.tsv": "".join(t), "corrections.tsv": "".join(c)}


def main(argv):
    out = tables()
    if argv[1:] == ["check"]:
        bad = [n for n, text in out.items()
               if not os.path.exists(os.path.join(OUT, n)) or open(os.path.join(OUT, n), encoding="utf-8").read() != text]
        print("differs: " + ", ".join(bad) if bad else "every table is what the script writes")
        return 1 if bad else 0
    os.makedirs(OUT, exist_ok=True)
    for name, text in out.items():
        with open(os.path.join(OUT, name), "w", encoding="utf-8", newline="\n") as fh:
            fh.write(text)
    print(" ".join(f"{n}: {t.count(chr(10)) - 4} rows" for n, t in out.items()))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
