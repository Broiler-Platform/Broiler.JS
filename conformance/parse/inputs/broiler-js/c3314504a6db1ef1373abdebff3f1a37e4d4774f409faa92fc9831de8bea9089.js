(function() {
  return eval('try { throw {}; } catch ({ f }) { { function f() {} } } typeof f;');
}())