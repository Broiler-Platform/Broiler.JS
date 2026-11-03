// SJL-14.1, part J seed: loops whose head declares let, const, using or await using, with continue and closures (the
// for-loop pass reads ContainsContinue, which the bridge synthesises; decision D44).
var fns = [];
outer: for (let i = 0; i < 4; i++) {
  for (const j of [1, 2]) {
    if (j === 2) continue outer;
    fns.push(() => i * j);
  }
  if (i === 3) break;
}
for (let k in { a: 1, b: 2 }) { if (k === 'a') continue; fns.push(() => k); }
var n = 0; do { n++; if (n < 3) continue; } while (n < 5);
fns.map(function (f) { return f(); }).join(',') + ':' + n;
