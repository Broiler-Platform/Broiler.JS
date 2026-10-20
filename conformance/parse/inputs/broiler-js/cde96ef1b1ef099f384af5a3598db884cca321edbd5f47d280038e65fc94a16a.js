function f() { return 1; }
function hot() { var s = f(); s++; ++s; return s; }
hot();