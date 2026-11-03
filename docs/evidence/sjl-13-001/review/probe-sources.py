import sys, os
out = sys.argv[1]
N = 9_999
def ch(head, link, n): return head + link * n
probes = {
 # stage 4e's section 5 probes, 9,999 nodes on each side
 "s5-1-right-by-precedence": "x = 1 + " + ch("2", "*2", N) + ch("", "+3", N) + ";",
 "s5-2-right-parenthesised": "x = a + (" + ch("b", "+b", N) + ")" + ch("", "+c", N) + ";",
 "s5-3-prefix": "x = -(" + ch("a", "+a", N) + ")" + ch("", "+b", N) + ";",
 "s5-4-array": "x = [" + ch("a", "+a", N) + "]" + ch("", ".b", N) + ";",
 "s5-5-call-argument": "x = f(" + ch("a", "+a", N) + ")" + ch("", "+b", N) + ";",
 "s5-6-sequence": "x = (0, " + ch("a", "+a", N) + ")" + ch("", "+b", N) + ";",
 "s5-7-conditional": "x = (c ? 0 : " + ch("a", "+a", N) + ")" + ch("", "+b", N) + ";",
 "s5-8-control": "x = (" + ch("a", "+a", N) + ")" + ch("", "+b", N) + ";",
 # review counterexamples and the within-bound test's sources
 "w-1-member-plus-call": "x = " + ch("a", ".a", 5000) + " + f(" + ch("b", "+b", 5000) + ");",
 "w-2-array-plus-paren": "x = [" + ch("a", "+a", 9000) + "] + (" + ch("b", "+b", 2000) + ");",
 "w-3-chain-plus-paren": "x = " + ch("a", "+a", 5000) + " + (" + ch("b", "+b", 5001) + ");",
 "w-4-both-at-bound": "x = (" + ch("a", "+a", 9999) + ") + (" + ch("b", "+b", 9999) + ");",
 "w-5-right-past": "x = (" + ch("a", "+a", 9999) + ") + (" + ch("b", "+b", 10000) + ");",
 "w-6-left-past": "x = (" + ch("a", "+a", 10000) + ") + (" + ch("b", "+b", 9999) + ");",
 "w-7-key-arg-at-bound": "x = " + ch("a", ".a", 5000) + "[" + ch("b", "+b", 9998) + "](" + ch("c", "+c", 9998) + ");",
 "w-8-key-arg-past": "x = " + ch("a", ".a", 5000) + "[" + ch("b", "+b", 9998) + "](" + ch("c", "+c", 10000) + ");",
 # the new shapes' rows, past the bound (inner 5,000, outer at the bound + 1)
 "n-1-yield": "function* g() { x = (yield " + ch("a", "+a", 5000) + ")" + ch("", "+b", 5001) + "; }",
 "n-2-postfix": "x = (" + ch("a", ".a", 5000) + "++)" + ch("", "+b", 5001) + ";",
 "n-3-prefix-update": "x = (--" + ch("a", ".a", 5000) + ")" + ch("", "+b", 5001) + ";",
}
os.makedirs(out, exist_ok=True)
for k, v in probes.items():
    open(os.path.join(out, k + ".js"), "w").write(v)
print(len(probes))
