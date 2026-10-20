// The read comes first, so a base that cannot be coerced fails before the RHS is
// evaluated at all - the same order the assignable reference produced.
var rhs = 0;
function side() { rhs++; return 1; }
var nulls = 0, undefineds = 0;
for (var i = 0; i < 300; i++) {
    try { null.x += side(); } catch (e) { if (e instanceof TypeError) nulls++; }
    try { undefined.x += side(); } catch (e) { if (e instanceof TypeError) undefineds++; }
}
[nulls, undefineds, rhs].join('|');