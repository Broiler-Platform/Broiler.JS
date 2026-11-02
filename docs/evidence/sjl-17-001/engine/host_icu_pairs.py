#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Ask the host's ICU 74.2 (libicui18n.so.74, through its C API) the pairs of Broiler.JS's revision-33 run that
collation-divergences.txt assigns between rows: a witness of the host's data, not of Broiler.JS's code.

    python3 -I host_icu_pairs.py <corpus vectors dir> <run report .tsv> > host-icu-pairs.txt

For each pair the run reports as a difference in the root's CollationTest, compatibility and Han groups and the options'
sensitivity group, it prints the vector, the expected outcome, Broiler.JS's, and the host's under the settings
the line names. ucol_open("en"), normalization on; tertiary non-ignorable for the non-ignorable file; UTS #10's shifted
alternate with maxVariable punctuation for the shifted file; the primary strength for `base` and the secondary for
`accent`; `case` is the primary strength with the case level on.
"""
import ctypes, glob, json, os, sys

i18n = ctypes.CDLL("libicui18n.so.74")
def f(name, res, args):
    fn = getattr(i18n, name + "_74"); fn.restype = res; fn.argtypes = args; return fn
P_INT = ctypes.POINTER(ctypes.c_int)
ucol_open = f("ucol_open", ctypes.c_void_p, [ctypes.c_char_p, P_INT])
ucol_setAttribute = f("ucol_setAttribute", None, [ctypes.c_void_p, ctypes.c_int, ctypes.c_int, P_INT])
ucol_setMaxVariable = f("ucol_setMaxVariable", None, [ctypes.c_void_p, ctypes.c_int, P_INT])
ucol_strcoll = f("ucol_strcoll", ctypes.c_int, [ctypes.c_void_p, ctypes.c_void_p, ctypes.c_int, ctypes.c_void_p, ctypes.c_int])
ALTERNATE, STRENGTH, NORMALIZATION, CASE_LEVEL = 1, 5, 4, 3
SHIFTED, ON, PRIMARY, SECONDARY, TERTIARY = 20, 17, 0, 1, 2
PUNCTUATION = 0x1001

def collator(setting):
    status = ctypes.c_int(0)
    c = ucol_open(b"en", ctypes.byref(status))
    ucol_setAttribute(c, NORMALIZATION, ON, ctypes.byref(status))
    strength = {"non-ignorable": TERTIARY, "shifted": TERTIARY, "base": PRIMARY, "accent": SECONDARY,
                "case": PRIMARY}[setting]
    ucol_setAttribute(c, STRENGTH, strength, ctypes.byref(status))
    if setting == "case":
        ucol_setAttribute(c, CASE_LEVEL, ON, ctypes.byref(status))
    if setting == "shifted":
        ucol_setAttribute(c, ALTERNATE, SHIFTED, ctypes.byref(status))
        ucol_setMaxVariable(c, PUNCTUATION, ctypes.byref(status))
    if status.value > 0:
        raise SystemExit(f"ICU status {status.value} for {setting}")
    return c

def compare(c, a, b):
    x, y = a.encode("utf-16-le", "surrogatepass"), b.encode("utf-16-le", "surrogatepass")
    r = ucol_strcoll(c, x, len(x) // 2, y, len(y) // 2)
    return "<" if r < 0 else "=" if r == 0 else ">"

def main():
    corpus, report = sys.argv[1], sys.argv[2]
    vectors = {}
    for path in sorted(glob.glob(os.path.join(corpus, "collation", "*.vectors"))):
        cur = {}
        for line in open(path, encoding="utf-8").read().split("\n") + [""]:
            if line.startswith("#") or line == "schema: 1":
                continue
            if not line:
                if cur:
                    vectors[cur["id"]] = cur
                cur = {}
                continue
            k, _, v = line.partition(": ")
            cur[k] = v
    print("# id\tsetting\texpected\tbroiler-js\thost ICU 74.2")
    for line in open(report, encoding="utf-8"):
        if line.startswith("#") or line.startswith("id\t"):
            continue
        fields = line.rstrip("\n").split("\t")
        if not fields[4].startswith("difference"):
            continue
        vid = fields[0]
        v = vectors[vid]
        values = v["in"].split(" ")
        if v["op"] != "Intl.Collator.prototype.compare" or len(values) != 3:
            continue
        group = vid.split("/")[3]
        if not (group.startswith("collationtest-") or group in ("sensitivity", "compatibility", "han")):
            continue
        script = v["script"]
        if group.startswith("collationtest-"):
            setting = group[len("collationtest-"):]
        elif 'sensitivity:"base"' in script:
            setting = "base"
        elif 'sensitivity:"accent"' in script:
            setting = "accent"
        elif 'sensitivity:"case"' in script:
            setting = "case"
        elif group == "han":
            setting = "non-ignorable"
        else:
            continue
        a, b = (json.loads(s[len("str:"):]) for s in values[1:])
        host = compare(collator(setting), a, b)
        print(f"{vid}\t{setting}\t{fields[2][len('value: str:'):]}\t{fields[3][len('value: str:'):]}\t\"{host}\"")

main()
