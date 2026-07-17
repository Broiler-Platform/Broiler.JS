function fibonacci(n) {
  var a = 0;
  var b = 1;
  var i = 0;
  while (i < n) {
    var next = a + b;
    a = b;
    b = next;
    i = i + 1;
  }
  return a;
}
