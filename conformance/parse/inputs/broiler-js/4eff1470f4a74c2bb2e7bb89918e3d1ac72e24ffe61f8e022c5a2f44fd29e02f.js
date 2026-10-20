let x = 'outside';
var probeBefore = function() { return x; };
var probeExpr;

for (let x of (probeExpr = function() { typeof x; }, [])) ;

[
  probeBefore(),
  (function() {
    try {
      probeExpr();
      return 'no-throw';
    } catch (e) {
      return e.name;
    }
  })()
].join('|');