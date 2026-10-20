function f(o, b) {
  var s = 0;
  // Both right-hand sides need two operators, or the one-operator tree is refused for
  // having no saving to make and mints no root box for its refusal to be charged to.
  for (var i = 0; i < 100; i++) { var t = o.x * b + i; var u = t * b + i; s = u; }
  return s;
}
f({ x: 2 }, 3);