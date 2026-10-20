function outer() {
  var q = 1;
  try { throw 2; } catch (e) {
    var inner = function () { return q + e; };
    return inner();
  }
}
outer();