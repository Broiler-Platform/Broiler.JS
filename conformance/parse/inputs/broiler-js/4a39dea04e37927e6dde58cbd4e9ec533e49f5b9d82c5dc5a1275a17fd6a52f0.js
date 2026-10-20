function o(){
  var rowSize;
  function reset(){ rowSize = 12; }
  function f(){ var c = 2 * rowSize; c++; return c; }
  reset();
  return f();
}
o();