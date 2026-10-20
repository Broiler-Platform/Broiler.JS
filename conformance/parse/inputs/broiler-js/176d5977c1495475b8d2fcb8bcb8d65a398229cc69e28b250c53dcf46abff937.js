var f = function g() {
  var inner = function () { return typeof g; };
  return inner();
};
f();