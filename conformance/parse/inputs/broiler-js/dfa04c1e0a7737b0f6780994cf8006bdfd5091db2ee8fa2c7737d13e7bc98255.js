'use strict';
function outer() {
  var q = 1;
  var inner = function () { return q; };
  return inner();
}
outer();