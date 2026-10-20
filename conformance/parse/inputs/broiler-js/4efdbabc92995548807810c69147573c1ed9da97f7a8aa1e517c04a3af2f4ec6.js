(function () {
  var a = 1;
  var fns = [];
  for (var i = 0; i < 3; i++) {
    fns.push(function () {
      return function () { return function () { return a + i; }; };
    });
  }
  return fns.map(function (f) { return f()()(); }).join(',');
})();